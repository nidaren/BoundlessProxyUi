using BoundlessProxyUi.ProxyManager;
using BoundlessProxyUi.ProxyManager.Components;
using BoundlessProxyUi.ProxyUi;
using BoundlessProxyUi.Util;
using BoundlessProxyUi.WsData;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace BoundlessProxyUi.Mitm
{
    public partial class SslMitmInstance
    {
        private static readonly Dictionary<int, KeyValuePair<string, int>> planetLookup = [];

        static SslMitmInstance()
        {
            return;
        }

        internal static bool Terminate = false;

        internal static string PlayerName = null;

        internal static Dictionary<int, UdpProxy> planetPorts = [];

        internal static object playerPlanetLock = new();
        internal static string playerPlanet = string.Empty;
        internal static bool ShutdownInitiated { get; set; } = false;

        private readonly Stream m_client;
        private readonly Stream m_server;
        private readonly ConnectionInstance m_connectionInstance;
        internal static readonly CancellationTokenSource ForwardStreamThreads_CTS = new();
        private static readonly CancellationToken forwardStreamToken = ForwardStreamThreads_CTS.Token;

        private readonly Dictionary<CommPacketDirection, BlockingCollection<WsFrame>> websocketDataQueue = new()
        {
            { CommPacketDirection.ClientToServer, new BlockingCollection<WsFrame>() },
            { CommPacketDirection.ServerToClient, new BlockingCollection<WsFrame>() },
        };

        private static readonly Dictionary<CommPacketDirection, ConcurrentDictionary<int, BlockingCollection<WsMessage>>> OutgoingQueueDirection = new()
        {
            { CommPacketDirection.ClientToServer, new ConcurrentDictionary<int, BlockingCollection<WsMessage>>() },
            { CommPacketDirection.ServerToClient, new ConcurrentDictionary<int, BlockingCollection<WsMessage>>() },
        };

        public bool ReplaceIpaddr { get; set; } = false;

        public delegate void OnFrameHandler(int planetId, string planetDisplayName, WsFrame frame);

        private OnFrameHandler onFrameIn;

        public event OnFrameHandler OnFrameIn
        {
            add
            {
                onFrameIn += value;
            }
            remove
            {
                onFrameIn -= value;
            }
        }

        private OnFrameHandler onFrameOut;

        public event OnFrameHandler OnFrameOut
        {
            add
            {
                onFrameOut += value;
            }
            remove
            {
                onFrameOut -= value;
            }
        }

        private string planetStringName = null;
        private string planetDisplayName = null;
        private int planetId = -1;

        public static async Task InitPlanets(Dictionary<string, string> hostLookup)
        {
            string blah = $"https://{ProxyManagerConfig.Instance.BoundlexxApiBase}/api/v2/worlds/?limit=10000&active=True&is_locked=False";

            var result = await Network.NidHttpClient.GetAsync(blah);
            if (!result.IsSuccessStatusCode)
            {
                throw new Exception("Error getting planets from server");
            }

            var serverList = JObject.Parse(await result.Content.ReadAsStringAsync());

            var auth = Network.NidHttpClient.DefaultRequestHeaders.Authorization;

            int curPort = 1000;

            foreach (var something in serverList["results"])
            {
                //string planetId = something["name"].Value<string>();
                int planetId = something["id"].Value<int>();
                string planetName = something["display_name"].Value<string>();
                //var planetNum = something["id"].Value<int>();

                string addr = something["address"].Value<string>();
                string ipAddr = hostLookup[addr];

                try
                {
                    planetLookup.Add(planetId, new KeyValuePair<string, int>(planetName, planetId));
                }
                catch (System.ArgumentException)
                {
                    System.Diagnostics.Debug.WriteLine("Duplicate planet: {0}", planetId);
                }


                UdpProxy proxy = null;

                while (curPort.ToString().Length == 4)
                {

                    try
                    {
                        proxy = new UdpProxy(curPort++, ipAddr, addr);
                    }
                    catch
                    {
                        continue;
                    }

                    break;
                }

                if (proxy == null)
                {
                    throw new Exception("too many ports in use");
                }

                planetPorts.Add(planetId, proxy);
            }

            // UDP Thread
            new Thread(() =>
            {
                var tasks = planetPorts.Values.Select(cur => cur.StartAsync()).ToArray();
                Task.WaitAny(tasks);

                var exception = tasks.FirstOrDefault(cur => cur.Exception != null)?.Exception?.InnerException;

                if (exception != null)
                {
                    Log.Error(exception, "UDP Thread error");
                    MessageBox.Show(exception.Message);
                    KillUdp();
                }
            }).Start();
        }

        public static void KillUdp()
        {
            planetPorts.Values.ToList().ForEach(cur => cur.Kill());
            planetPorts.Clear();
        }

        public SslMitmInstance(Stream client, Stream server, ConnectionInstance connectionInstance, int chunkSize = 4096)
        {
            ProxyManagerWindow.Instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).RefreshConversations();
            }));

            m_connectionInstance = connectionInstance;
            connectionInstance.SslMitmInstance = this;

            m_client = client;
            m_server = server;

            OnFrameIn += WsEventHander.Instance.OnFrameIn;

            ForwardStreamNew(client, server, new byte[chunkSize], CommPacketDirection.ClientToServer, forwardStreamToken);
            ForwardStreamNew(server, client, new byte[chunkSize], CommPacketDirection.ServerToClient, forwardStreamToken);

            Dispatcher(CommPacketDirection.ServerToClient);
            Dispatcher(CommPacketDirection.ClientToServer);
        }

        public static void AddOutgoingMessage(int planetId, CommPacketDirection direction, WsMessage message)
        {
            var outgoingQueue = OutgoingQueueDirection[direction];

            if (!outgoingQueue.TryGetValue(planetId, out var collection))
            {
                collection = [];
                if (!outgoingQueue.TryAdd(planetId, collection))
                {
                    collection = outgoingQueue[planetId];
                }
            }

            collection.Add(message);
        }

        private static List<WsMessage> GetOutgoingMessages(int planetId, CommPacketDirection direction)
        {
            var outgoingQueue = OutgoingQueueDirection[direction];

            List<WsMessage> result = [];

            if (outgoingQueue.TryGetValue(planetId, out var collection))
            {
                while (collection.TryTake(out var curItem))
                {
                    result.Add(curItem);
                }
            }

            return result;
        }

        public void Kill(bool client)
        {
            m_connectionInstance.IsConnectionOpen = false;

            ProxyManagerWindow.Instance.Dispatcher.BeginInvoke(new Action(() =>
            {
                ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).RefreshConversations();
            }));

            if (client)
            {
                try
                {
                    m_client.Flush();
                    m_client.Close();
                    m_client.Dispose();
                }
                catch { }

                try
                {
                    websocketDataQueue[CommPacketDirection.ServerToClient].CompleteAdding();
                }
                catch { }
            }
            else
            {
                try
                {
                    m_server.Flush();
                    m_server.Close();
                    m_server.Dispose();
                }
                catch { }

                try
                {
                    websocketDataQueue[CommPacketDirection.ClientToServer].CompleteAdding();
                }
                catch { }
            }

            var managerWindowInstance = ProxyManagerWindow.Instance;

            if (managerWindowInstance != null)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(ProxyManagerConfig.Instance.DeathTimeout * 1000);

                    managerWindowInstance.Dispatcher.Invoke(new Action(() =>
                    {
                        m_connectionInstance.Parent.Instances.Remove(m_connectionInstance);
                    }));
                });
            }
        }

        private void Dispatcher(CommPacketDirection direction)
        {
            new Thread(() =>
            {
                try
                {
                    var myQueue = websocketDataQueue[direction];
                    OnFrameHandler myHandler()
                    {
                        return direction == CommPacketDirection.ClientToServer ? onFrameOut : onFrameIn;
                    }

                    while (true)
                    {
                        WsFrame curFrame;

                        try
                        {
                            //while (myQueue.TryTake(out curFrame, TimeSpan.FromMilliseconds(100)))
                            //{
                            //    Log.Information("Took frame from collection.");
                            //}
                            curFrame = myQueue.Take();
                        }
                        catch
                        {
                            break;
                        }

                        var curHandler = myHandler();

                        if (curHandler != null)
                        {
                            foreach (OnFrameHandler curInvoke in curHandler.GetInvocationList().Cast<OnFrameHandler>())
                            {
                                try
                                {
                                    curInvoke(planetId, planetDisplayName ?? string.Empty, curFrame);
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }).Start();
        }

        private void ForwardStreamNew(Stream source, Stream destination, byte[] buffer, CommPacketDirection direction, CancellationToken token)
        {
            var rawVerbs = new string[] { "GET", "HEAD", "POST", "PUT", "DELETE", "TRACE", "CONNECT" };
            byte[][] verbs = rawVerbs.Select(cur => Encoding.UTF8.GetBytes(cur).Take(2).ToArray()).ToArray();
            Regex requestLinePattern = new($"^({string.Join("|", rawVerbs)}) [^ ]+ HTTP/1.1$");
            Regex contentLengthPattern = ContentLengthRegex();
            Regex chunkedPattern = TransferEncodingRegex();
            Regex statusLinePattern = HttpversionRegex();
            Regex websocketPlanet = GetRegex();
            Regex udpPortPattern = UdpPortRegex();


            try
            {
                destination = new BufferedStream(destination);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error creating buffered stream");
                return;
            }

            new Thread(() =>
            {
                MemoryStream ms = null;

                try
                {
                    bool isWebSocket = false;

                    while (true)
                    {
                        token.ThrowIfCancellationRequested();

                        try
                        {
                            destination.Flush();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error Flushing stream");
                            Kill(direction == CommPacketDirection.ServerToClient);
                        }

                        if (!m_connectionInstance.IsConnectionOpen)
                        {
                            break;
                        }

                        //if (ms != null)
                        //{
                        //    ms.Position = 0;
                        //    messagecache[direction].Enqueue(ms);
                        //}

                        //while (messagecache[direction].Count > 10)
                        //{
                        //    messagecache[direction].Dequeue();
                        //}

                        ms = new MemoryStream();
                        StreamWriter sw = null;

                        bool ReadBytes(int count)
                        {
                            int offset = 0;

                            while (offset < count)
                            {
                                int bytesRead = 0;

                                try
                                {
                                    bytesRead = source.Read(buffer, offset, count - offset);
                                }
                                catch (IOException io) when (io.InnerException is SocketException)
                                {
                                    if (!ShutdownInitiated)
                                    {
                                        Log.Error("Stream Reader: An established connection was aborted.");
                                    }

                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex, "Error Reading stream");
                                    Kill(direction == CommPacketDirection.ClientToServer);
                                }

                                if (bytesRead == 0)
                                {
                                    return false;
                                }

                                offset += bytesRead;
                            }

                            return true;
                        }

                        string ReadLine()
                        {
                            List<byte> result = [];

                            byte? prevByte = null;

                            while (true)
                            {
                                byte[] readBuffer = new byte[1];
                                if (source.Read(readBuffer, 0, 1) != 1)
                                {
                                    return null;
                                }

                                result.Add(readBuffer[0]);

                                if (prevByte == '\r' && readBuffer[0] == '\n')
                                {
                                    break;
                                }

                                prevByte = readBuffer[0];
                            }

                            return Encoding.UTF8.GetString(result.ToArray()).TrimEnd('\r', '\n');
                        }

                        void DoHttpHeadersContentAndForward()
                        {
                            List<string> headers = [];

                            ulong contentLength = 0;
                            bool chunked = false;

                            string curLine = null;
                            while ((curLine = ReadLine()) != null && curLine != string.Empty)
                            {
                                sw.WriteLine(curLine);

                                curLine = curLine.Trim();

                                Match m = contentLengthPattern.Match(curLine);
                                if (m.Success)
                                {
                                    contentLength = Convert.ToUInt64(m.Groups[1].Value);
                                }

                                m = chunkedPattern.Match(curLine);
                                if (m.Success)
                                {
                                    chunked = true;
                                }

                                headers.Add(curLine.ToLower());
                            }

                            sw.WriteLine();
                            sw.Flush();

                            if (curLine == null)
                            {
                                throw new Exception("HTTP unexpected end of stream while reading headers");
                            }

                            if (chunked && contentLength > 0)
                            {
                                throw new Exception("Chunked content with length not supported");
                            }

                            void DoReadLength(ulong curLength)
                            {
                                while (curLength > 0)
                                {
                                    int bytesRead = 0;

                                    try
                                    {
                                        bytesRead = source.Read(buffer, 0, Math.Min(buffer.Length, (int)Math.Min(int.MaxValue, curLength)));
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex, "Error reading bytes");
                                        Kill(direction == CommPacketDirection.ClientToServer);
                                    }

                                    if (bytesRead == 0)
                                    {
                                        throw new Exception("HTTP unexpected end of stream while reading content");
                                    }

                                    ms.Write(buffer, 0, bytesRead);
                                    curLength -= (ulong)bytesRead;
                                }
                            }

                            DoReadLength(contentLength);

                            if (chunked)
                            {
                                bool lastChunk = false;

                                while (!lastChunk && (curLine = ReadLine()) != null && curLine != string.Empty)
                                {
                                    sw.WriteLine(curLine);
                                    sw.Flush();

                                    var length = ulong.Parse(curLine.Trim());

                                    if (length > 0)
                                    {
                                        DoReadLength(length);
                                    }
                                    else
                                    {
                                        lastChunk = true;
                                    }

                                    curLine = ReadLine();

                                    if (curLine == null || curLine.Length != 0)
                                    {
                                        throw new Exception("HTTP protocol failure");
                                    }

                                    sw.WriteLine();
                                    sw.Flush();
                                }
                            }

                            ms.Position = 0;
                            if (!headers.Contains("content-type: application/json".ToLower()))
                            {
                                DestinationWrite(destination, ms.ToArray(), (int)ms.Length, direction);
                            }
                            else
                            {
                                var orgLength = ms.Length;
                                string entireMessage = new StreamReader(ms).ReadToEnd();
                                ForwardHttpData(destination, entireMessage, direction);
                            }
                        }

                        void forwardWebsocketFrame()
                        {
                            WsFrame frame = null;

                            try
                            {
                                frame = new WsFrame(buffer, 2, source);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error creating WsFrame");
                            }

                            var worldData = frame?.Messages.FirstOrDefault(cur => cur.ApiId.HasValue && cur.ApiId.Value == 0);

                            if (worldData != null)
                            {
                                string theJson = Encoding.UTF8.GetString(worldData.Buffer, 0, worldData.Buffer.Length);

                                Match m = udpPortPattern.Match(theJson);

                                if (m.Success && planetId != -1)
                                {
                                    int serverPort = Convert.ToInt32(m.Groups[1].Value);

                                    if (serverPort.ToString().Length != 4)
                                    {
                                        throw new Exception("Length change of udpPort");
                                    }

                                    if (!planetPorts.TryGetValue(planetId, out UdpProxy value))
                                    {
                                        //throw new Exception($"Planet dictionary does not contain {planetStringName}");
                                    }
                                    else
                                    {
                                        value.RemotePort = serverPort;
                                        theJson = udpPortPattern.Replace(theJson, $"\"udpPort\":{value.LocalPort},");

                                        byte[] sendData = Encoding.UTF8.GetBytes(theJson);

                                        if (sendData.Length != worldData.Buffer.Length)
                                        {
                                            throw new Exception("JSON length error");
                                        }

                                        worldData.Buffer = sendData;
                                    }
                                }
                            }

                            if (planetId != -1)
                            {
                                frame?.Messages.AddRange(GetOutgoingMessages(planetId, direction));
                            }

                            try
                            {
                                if (destination.CanWrite)
                                {
                                    frame?.Send(destination);
                                }

                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error sending frame");
                                Kill(direction == CommPacketDirection.ServerToClient);
                            }

                            //if (frame.readStream.Length != frame.writeStream.Length)
                            //{
                            //    throw new Exception("frame length mismatch.");
                            //}

                            //if (!frame.readStream.ToArray().SequenceEqual(frame.writeStream.ToArray()))
                            //{
                            //    throw new Exception("frame data mismatch.");
                            //}

                            frame ??= new WsFrame()
                            {
                                Messages =
                                    [
                                        new(24, null, Encoding.UTF8.GetBytes("Frame decoding failure!")),
                                    ],
                            };

                            try
                            {
                                if (websocketDataQueue[direction].IsCompleted)
                                {
                                    websocketDataQueue[direction] = [];
                                }

                                websocketDataQueue[direction].Add(frame);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error adding frame to queue");
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProxyUiWindow.AddFrame(frame, direction, m_connectionInstance);
                            });
                        }

                        if (!ReadBytes(2))
                        {
                            // Connection terminated waiting for new message. This is fine.
                            break;
                        }

                        if (direction == CommPacketDirection.ClientToServer)
                        {
                            if (!verbs.Any(cur => cur[0] == buffer[0] && cur[1] == buffer[1]) && !isWebSocket)
                            {
                                isWebSocket = true;
                                ProxyManagerWindow.Instance.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    m_connectionInstance.Parent.Instances.Remove(m_connectionInstance);

                                    var wsg = ComponentEngine.Instance.GetComponent<TcpComponent>().websocketGroup;
                                    m_connectionInstance.Parent = wsg;
                                    wsg.Instances.Add(m_connectionInstance);
                                }));
                            }

                            if (!isWebSocket)
                            {
                                // GET /index.html HTTP/1.1
                                sw = new StreamWriter(ms);

                                string requestLine = (Encoding.UTF8.GetString(buffer, 0, 2) + ReadLine()).Trim();
                                sw.WriteLine(requestLine);

                                if (!requestLinePattern.IsMatch(requestLine))
                                {
                                    throw new Exception("HTTP request line invalid");
                                }

                                Match m = websocketPlanet.Match(requestLine);
                                if (m.Success)
                                {
                                    var newVal = Convert.ToInt32(m.Groups[1].Value);

                                    if (planetId != -1)
                                    {
                                        if (planetId != newVal)
                                        {
                                            throw new Exception("Multiple planets detected on a single stream");
                                        }
                                    }
                                    else
                                    {
                                        planetId = newVal;
                                        planetStringName = planetLookup.Where(cur => cur.Value.Value == newVal).FirstOrDefault().Key.ToString();
                                        planetDisplayName = planetLookup[planetId].Key;
                                    }
                                }

                                DoHttpHeadersContentAndForward();
                            }
                            else
                            {
                                forwardWebsocketFrame();
                            }
                        }
                        else
                        {
                            if ((buffer[0] != 'H' || buffer[1] != 'T') && !isWebSocket)
                            {
                                isWebSocket = true;
                                ProxyManagerWindow.Instance.Dispatcher.BeginInvoke(new Action(() =>
                                {
                                    m_connectionInstance.Parent.Instances.Remove(m_connectionInstance);

                                    var wsg = ComponentEngine.Instance.GetComponent<TcpComponent>().websocketGroup;
                                    m_connectionInstance.Parent = wsg;
                                    wsg.Instances.Add(m_connectionInstance);
                                }));
                            }

                            if (!isWebSocket)
                            {
                                // HTTP/1.1 200 OK
                                sw = new StreamWriter(ms);

                                string statusLine = (Encoding.UTF8.GetString(buffer, 0, 2) + ReadLine()).Trim();

                                sw.WriteLine(statusLine);

                                if (!statusLinePattern.IsMatch(statusLine))
                                {
                                    throw new Exception("HTTP status line invalid");
                                }

                                DoHttpHeadersContentAndForward();
                            }
                            else
                            {
                                forwardWebsocketFrame();
                            }
                        }
                    }

                    //Log.Information("Killing connection");
                    Kill(direction == CommPacketDirection.ClientToServer);
                }
                catch (OperationCanceledException)
                {
                    Kill(direction == CommPacketDirection.ClientToServer);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Forward Stream error");
                    Kill(direction == CommPacketDirection.ClientToServer);
                }
            }).Start();
        }

        private void ForwardHttpData(Stream destination, string entireMessage, CommPacketDirection direction)
        {
            if (planetDisplayName == null &&
                entireMessage.Contains("\"worldData\"") &&
                entireMessage.Contains("\"displayName\"") &&
                entireMessage.Contains("\"id\"") &&
                entireMessage.Contains("\"name\""))
            {
                try
                {
                    var gameserverJson = JObject.Parse(entireMessage[(entireMessage.IndexOf("\r\n\r\n") + 4)..]);

                    planetId = gameserverJson["worldData"]["id"].Value<int>();
                    planetDisplayName = gameserverJson["worldData"]["displayName"].ToString();
                    planetStringName = gameserverJson["worldData"]["name"].ToString();

                    if (!planetLookup.ContainsKey(planetId))
                    {
                        planetLookup.Add(planetId, new KeyValuePair<string, int>(planetDisplayName, planetId));
                    }
                }
                catch { }
            }

            if (ReplaceIpaddr && entireMessage.Contains("ipAddr"))
            {
                Regex ipSubPattern = IpAddrRegex();
                Match ipSubMatch = ipSubPattern.Match(entireMessage);

                if (ipSubMatch.Success)
                {
                    Regex reg = ContentLenRegex();
                    Match m = reg.Match(entireMessage);

                    int length = Convert.ToInt32(m.Groups[1].Value);
                    int newLength = length;

                    if (!m.Success)
                    {
                        throw new Exception("This shouldn't happen...");
                    }

                    int lenBeforeReplace = entireMessage.Length;

                    while (ipSubMatch.Success)
                    {
                        string org = ipSubMatch.Groups[0].Value;
                        string rep = ",\"ipAddr\":\"127.0.0.1\",";
                        entireMessage = entireMessage.Replace(org, rep);

                        ipSubMatch = ipSubPattern.Match(entireMessage);
                    }

                    int lenAfterReplace = entireMessage.Length;
                    newLength += lenAfterReplace - lenBeforeReplace;

                    entireMessage = entireMessage.Replace(m.Groups[0].Value, $"Content-Length: {newLength}");

                    DestinationWrite(destination, entireMessage, direction);
                }
                else
                {
                    DestinationWrite(destination, entireMessage, direction);
                }
            }
            else
            {
                DestinationWrite(destination, entireMessage, direction);
            }
        }

        private void DestinationWrite(Stream destination, string entireMessage, CommPacketDirection direction)
        {
            byte[] sendBytes = Encoding.UTF8.GetBytes(entireMessage);
            DestinationWrite(destination, sendBytes, sendBytes.Length, direction);
        }

        private void DestinationWrite(Stream destination, byte[] buffer, int count, CommPacketDirection direction)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                ProxyUiWindow.WriteBytes(buffer, count, direction, m_connectionInstance);
            });
            TryWriteStream(destination, buffer, 0, count, direction == CommPacketDirection.ServerToClient);
        }

        private void TryWriteStream(Stream destination, byte[] buffer, int offset, int count, bool client)
        {
            try
            {
                destination.Write(buffer, offset, count);
            }
            catch
            {
                Kill(client);
            }
        }

        public static void ChunkToBoundless(out int boundlessEast, out int boundlessNorth,
                                       int chunkEast, int chunkSouth,
                                       int blockEast, int blockSouth)
        {
            if (chunkEast > 144)
            {
                chunkEast -= 288;
            }

            if (chunkSouth > 144)
            {
                chunkSouth -= 288;
            }

            boundlessEast = chunkEast * 16 + blockEast;
            boundlessNorth = -(chunkSouth * 16 + blockSouth);
        }

        [GeneratedRegex("^Content-Length: ([0-9]+)$")]
        private static partial Regex ContentLengthRegex();

        [GeneratedRegex("^Transfer-Encoding: chunked$")]
        private static partial Regex TransferEncodingRegex();

        [GeneratedRegex("^HTTP/1.1 ([0-9]+) (.*)$")]
        private static partial Regex HttpversionRegex();

        [GeneratedRegex("GET /([0-9]+)/websocket/game HTTP/1.1")]
        private static partial Regex GetRegex();

        [GeneratedRegex("\"udpPort\"\\:([0-9]+)\\,")]
        private static partial Regex UdpPortRegex();

        [GeneratedRegex("\\,\"ipAddr\":\"(?!127\\.0\\.0\\.1)[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\\.[0-9]{1,3}\"\\,")]
        private static partial Regex IpAddrRegex();

        [GeneratedRegex("Content-Length\\: ([0-9]+)", RegexOptions.IgnoreCase, "en-GB")]
        private static partial Regex ContentLenRegex();
    }
}
