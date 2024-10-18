using BoundlessProxyUi.ProxyManager;
using BoundlessProxyUi.WsData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#pragma warning disable CA1822

namespace BoundlessProxyUi.Util
{
    public partial class WsEventHander
    {
        private static WsEventHander instance;
        private static readonly TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
        public static WsEventHander Instance
        {
            get
            {
                instance ??= new WsEventHander();
                return instance;
            }
        }

        public void OnFrameIn<T>(int planetId, string planetDisplayName, T frame_object)
        {
            var frame = frame_object as WsFrame;

            foreach (var curMessage in frame.Messages)
            {
                if (curMessage.ApiId.HasValue && curMessage.Buffer.Length > 0)
                {
                    switch (curMessage.ApiId.Value)
                    {
                        case 0:
                            HandleWorldJson(planetId, planetDisplayName, curMessage);
                            break;
                            //case 5:
                            //    HandleWorldControlJson(planetId, planetDisplayName, curMessage);
                            //    break;
                    }
                }
            }
        }

        private static void WriteExportFile(string filename, string contents)
        {
            var invalidFileNameChars = Path.GetInvalidFileNameChars();
            filename = new string(filename.Where(cur => !invalidFileNameChars.Contains(cur)).ToArray());

            var filepath = $"{ProxyManagerConfig.Instance.ExportDirectory}{Path.DirectorySeparatorChar}{filename}";

            try
            {
                File.WriteAllText(filepath, contents);
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ProxyManagerWindow.Instance.SetStatusText($"Successfully wrote {filename}");
                });
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ProxyManagerWindow.Instance.ShowError($"Failed to write {filepath}:\r\n{ex.Message}", "Error writing JSON", ex);
                });
            }
        }

        private static async void UploadJson(string content, string path, string name, string type, bool slientFail = false)
        {
            Network.NidHttpClient.DefaultRequestHeaders.Authorization = new("Token", ProxyManagerConfig.Instance.BoundlexxApiKey);

            HttpResponseMessage response = null;

            try
            {
                response = await Network.NidHttpClient.PostAsync($"https://{ProxyManagerConfig.Instance.BoundlexxApiBase}/api{path}", new StringContent(content, Encoding.UTF8, "application/json"));
            }
            catch (HttpRequestException ex)
            {
                var message = $"Failed to upload {type} JSON for {name}: {ex.InnerException.Message}";
                if (!slientFail)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProxyManagerWindow.Instance.ShowError(message, "Error uploading JSON", ex);
                    });
                }
                else
                {
                    Log.Error(ex, message);
                }
                return;
            }

            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                var messageTmout = $"Failed to upload {type} JSON for {name}: request timeout.";
                Log.Error(ex, messageTmout);
                return;
            }
            catch (Exception ex)
            {
                var msg = $"Failed to upload {type} JSON for {name}: {ex.Message}";
                Log.Error(ex, msg);
                return;
            }

            if (response != null && !response.IsSuccessStatusCode)
            {
                if (response.StatusCode is (System.Net.HttpStatusCode)429)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProxyManagerWindow.Instance.SetStatusText($"Rate limited! Did not upload {type} JSON for {name}. You exceeded number of requests to API.", true);
                    });
                    return;
                }

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ProxyManagerWindow.Instance.ShowError($"Failed to upload {type} JSON for {name}", "Error uploading JSON");
                });
                return;
            }

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ProxyManagerWindow.Instance.SetStatusText($"Successfully uploaded {type} JSON for {name}", false);
                Log.Information($"Successfully uploaded {type} JSON for {name}. API Response: {response.ReasonPhrase}, with code: {(int)response.StatusCode}");
            });
        }

        private void HandleWorldJson(int planetId, string planetDisplayName, WsMessage message)
        {
            JObject payload;

            string tagFree = FilenameFilterRegex().Replace(planetDisplayName, "");
            tagFree = textInfo.ToTitleCase(tagFree.ToLower());

            try
            {
                payload = JObject.Parse(Encoding.UTF8.GetString(message.Buffer));
                payload["world_id"] = planetId;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error decoding World JSON message");
                return;
            }

            var jsonString = payload.ToString(Formatting.Indented);

            if (ProxyManagerConfig.Instance.SaveWorldJson)
            {
                var filename = $"{tagFree}.json";
                WriteExportFile(filename, jsonString);
            }

            if (ProxyManagerConfig.Instance.UploadWorldJson)
            {
                UploadJson(jsonString, "/ingest-ws-data/", tagFree, "World");
            }
        }

        private readonly byte START_BYTE = 64;
        private void HandleWorldControlJson(int planetId, string planetDisplayName, WsMessage message)
        {
            if (message.Buffer.Length < 2000)
            {
                return;
            }

            string jsonString;
            bool isSimple;
            try
            {
                var offset = 0;
                while (offset + 2 < message.Buffer.Length)
                {
                    // World Control binary has 65 followed by a byte of 0-7 (global permissions)
                    var possibleStart = message.Buffer.Skip(offset).Take(2).ToArray();
                    // 64 = colors not finalized | 65 = colors finalized
                    if (possibleStart[1] <= 7 && (possibleStart[0] == START_BYTE || possibleStart[0] == (START_BYTE + 1)))
                    {
                        break;
                    }
                    offset += 1;
                }

                if (offset + 2 >= message.Buffer.Length)
                {
                    return;
                }

                var result = ParseWorldControlJson(planetId, message.Buffer, offset);
                jsonString = result.Item1;
                isSimple = result.Item2;

                if (isSimple)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error decoding World Control: {planetDisplayName} message");
                return;
            }

            if (ProxyManagerConfig.Instance.SaveWorldControlJson)
            {
                string filename;
                if (isSimple)
                {
                    filename = $"{planetDisplayName}_WorldPermissions.json";
                }
                else
                {
                    filename = $"{planetDisplayName}_WorldControl.json";
                }
                WriteExportFile(filename, jsonString);
            }

            if (ProxyManagerConfig.Instance.UploadWorldControlJson)
            {
                if (isSimple)
                {
                    UploadJson(jsonString, "/ingest-wcsimple-data/", planetDisplayName, "World Permissons", true);
                }
                else
                {
                    UploadJson(jsonString, "/ingest-wc-data/", planetDisplayName, "World Control", true);
                }
            }
        }

        private Tuple<string, bool> ParseWorldControlJson(int planetId, byte[] buffer, int offset)
        {
            StringWriter jsonString = new();
            var isSimple = false;

            using (JsonTextWriter jsonWriter = new(jsonString))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("world_id");
                jsonWriter.WriteValue(planetId);

                var colorsFinalized = !(buffer.Skip(offset).First() == START_BYTE);
                offset += 1;

                jsonWriter.WritePropertyName("finalized");
                jsonWriter.WriteValue(colorsFinalized);

                var globalPerms = buffer.Skip(offset).First();
                offset += 1;

                jsonWriter.WritePropertyName("global_perms");
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("can_visit");
                jsonWriter.WriteValue((globalPerms & 1) == 1);

                jsonWriter.WritePropertyName("can_edit");
                jsonWriter.WriteValue((globalPerms & 4) == 4);

                jsonWriter.WritePropertyName("can_claim");
                jsonWriter.WriteValue((globalPerms & 2) == 2);

                jsonWriter.WriteEndObject();

                var numGuilds = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                offset += 2;
                if (numGuilds > 0)
                {
                    // 6 bytes of guild data
                    // last bit is permission data
                    offset += (5 * numGuilds);
                }

                var playerCount = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                offset += 2;

                jsonWriter.WritePropertyName("players");
                jsonWriter.WriteStartArray();
                for (var i = 0; i < playerCount; i++)
                {
                    jsonWriter.WriteStartObject();
                    jsonWriter.WritePropertyName("id");
                    jsonWriter.WriteValue(planetId);
                    var playerId = BitConverter.ToUInt32(GetBytes(buffer, offset, 4), 0);
                    offset += 4;

                    var playerNameLength = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                    offset += 2;

                    var playerName = Encoding.GetEncoding("ISO-8859-1").GetString(GetBytes(buffer, offset, playerNameLength), 0, playerNameLength);
                    // player name + 8 unknown bytes
                    offset += playerNameLength + 8;

                    jsonWriter.WritePropertyName("name");
                    jsonWriter.WriteValue(playerName);
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndArray();

                // unknown 8 bytes
                offset += 8;

                var colorsCount = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                offset += 2;

                jsonWriter.WritePropertyName("colors");
                jsonWriter.WriteStartObject();
                for (var i = 0; i < colorsCount; i++)
                {
                    var blockId = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                    offset += 2;

                    jsonWriter.WritePropertyName(blockId.ToString());
                    jsonWriter.WriteStartObject();

                    var defaultColor = buffer.Skip(offset).First();
                    offset += 1;

                    jsonWriter.WritePropertyName("default");
                    jsonWriter.WriteValue(defaultColor);

                    var possibleColorsCount = BitConverter.ToUInt16(GetBytes(buffer, offset, 2), 0);
                    offset += 2;

                    jsonWriter.WritePropertyName("possible");
                    jsonWriter.WriteStartArray();
                    for (var j = 0; j < possibleColorsCount; j++)
                    {
                        var possibleColor = buffer.Skip(offset).First();
                        offset += 1;

                        jsonWriter.WriteValue(possibleColor);
                    }
                    jsonWriter.WriteEndArray();
                    jsonWriter.WriteEndObject();
                }
                jsonWriter.WriteEndObject();

                jsonWriter.WriteEndObject();

                isSimple = playerCount == 0 && colorsCount == 0;
            }

            return Tuple.Create(jsonString.ToString(), isSimple);
        }

        private byte[] GetBytes(byte[] buffer, int offset, int numBytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                return buffer.Skip(offset).Take(numBytes).ToArray();
            }
            return buffer.Skip(offset).Take(numBytes).Reverse().ToArray();
        }

        [GeneratedRegex(@"\s*\:.*?\:\s*")]
        private static partial Regex FilenameFilterRegex();
    }
}
