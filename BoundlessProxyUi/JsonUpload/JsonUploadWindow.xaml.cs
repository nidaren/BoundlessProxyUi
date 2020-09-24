using BoundlessProxyUi.ProxyManager;
using BoundlessProxyUi.WsData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfHexaEditor.Core.MethodExtention;

namespace BoundlessProxyUi.JsonUpload
{
    /// <summary>
    /// Interaction logic for JsonUploadWindow.xaml
    /// </summary>
    public partial class JsonUploadWindow : Window
    {
        public static JsonUploadWindow Instance { get; set; }

        public JsonUploadWindow(ManagerWindowViewModel dc)
        {
            Instance = this;

            InitializeComponent();

            ParentDataContext = dc;
            DataContext = MyDataContext = new JsonUploadWindowViewModel();
        }

        ManagerWindowViewModel ParentDataContext;
        JsonUploadWindowViewModel MyDataContext;

        private void HandleWorldJson(int planetId, string planetDisplayName, WsMessage message)
        {
            JObject payload = null;

            try
            {
                payload = JObject.Parse(Encoding.UTF8.GetString(message.Buffer));
            }
            catch (Exception)
            {
                return;
            }

            if (MyDataContext.JsonSaveFile)
            {
                var invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                var fileName = new string(planetDisplayName.Where(cur => !invalidFileNameChars.Contains(cur)).ToArray());

                var filePath = $"{MyDataContext.BaseFolder}{System.IO.Path.DirectorySeparatorChar}{fileName}.json";
                try
                {
                    File.WriteAllText(filePath, payload.ToString(Formatting.Indented));
                    ParentDataContext.TextStatus = $"Successfully wrote {fileName}.json";
                }
                catch (Exception ex)
                {
                    ParentDataContext.TextStatus = $"Failed to write {filePath}:\r\n{ex.Message}";
                    if (MyDataContext.ShowErrors)
                    {
                        MessageBox.Show(ParentDataContext.TextStatus, "Error writing json", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
            }

            if (MyDataContext.JsonSaveApi)
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", MyDataContext.JsonApiKey);

                payload["world_id"] = planetId;

                string aikjbhgshdoi = payload.ToString();

                HttpResponseMessage response = null;

                try
                {
                    response = client.PostAsync($"{MyDataContext.ApiBaseUrl}/ingest-ws-data/", new StringContent(payload.ToString(), Encoding.UTF8, "application/json")).Result;
                }
                catch (Exception ex)
                {
                    ParentDataContext.TextStatus = $"Failed to upload {planetDisplayName} World JSON: {ex.Message}";
                    if (MyDataContext.ShowErrors)
                    {
                        MessageBox.Show(ParentDataContext.TextStatus, "Error uploading JSON", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                if (response != null && !response.IsSuccessStatusCode)
                {
                    ParentDataContext.TextStatus = $"Failed to upload {planetDisplayName} World JSON. Response code: {response.StatusCode}";
                    if (MyDataContext.ShowErrors)
                    {
                        MessageBox.Show(ParentDataContext.TextStatus, "Error uploading json", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }

                ParentDataContext.TextStatus = $"Successfully uploaded World JSON for {planetDisplayName}";
            }

            return;
        }

        private byte[] GetBytes(byte[] buffer, int offset, int numBytes)
        {
            if (BitConverter.IsLittleEndian)
            {
                return buffer.Skip(offset).Take(numBytes).ToArray();
            }
            return buffer.Skip(offset).Take(numBytes).Reverse().ToArray();
        }

        private String ParseWorldControlJson(int planetId, byte[] buffer, int offset)
        {
            StringWriter jsonString = new StringWriter();

            using (JsonWriter jsonWriter = new JsonTextWriter(jsonString))
            {
                jsonWriter.Formatting = Formatting.Indented;
                jsonWriter.WriteStartObject();

                jsonWriter.WritePropertyName("world_id");
                jsonWriter.WriteValue(planetId);

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
            }

            return jsonString.ToString();
        }

        private static byte START_BYTE = 65;
        private void HandleUploadWorldControl(int planetId, string planetDisplayName, WsMessage message)
        {
            if (message.Buffer.Length < 2000)
            {
                return;
            }

            try { 
                var offset = 0;
                while (offset + 2 < message.Buffer.Length) {
                    // World Control binary has 65 followed by a byte of 0-7 (global permissions)
                    var possibleStart = message.Buffer.Skip(offset).Take(2).ToArray();
                    if (possibleStart[0] == START_BYTE && possibleStart[1] <= 7)
                    {
                        offset += 1;
                        break;
                    }
                    offset += 1;
                }

                if (offset + 2 >= message.Buffer.Length)
                {
                    return;
                }

                var jsonString = ParseWorldControlJson(planetId, message.Buffer, offset);

                if (MyDataContext.JsonSaveWcFile)
                {
                    var invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                    var fileName = new string(planetDisplayName.Where(cur => !invalidFileNameChars.Contains(cur)).ToArray());

                    var filePath = $"{MyDataContext.BaseFolder}{System.IO.Path.DirectorySeparatorChar}{fileName}_WorldControl.json";
                    try
                    {
                        File.WriteAllText(filePath, jsonString);
                        ParentDataContext.TextStatus = $"Successfully wrote {fileName}_WorldControl.json";
                    }
                    catch (Exception ex)
                    {
                        ParentDataContext.TextStatus = $"Failed to write {filePath}:\r\n{ex.Message}";
                        if (MyDataContext.ShowErrors)
                        {
                            MessageBox.Show(ParentDataContext.TextStatus, "Error writing JSON", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        return;
                    }
                }

                if (MyDataContext.JsonSaveWcApi)
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Token", MyDataContext.JsonApiKey);

                    HttpResponseMessage response = null;

                    try
                    {
                        response = client.PostAsync($"{MyDataContext.ApiBaseUrl}/ingest-wc-data/", new StringContent(jsonString, Encoding.UTF8, "application/json")).Result;
                    }
                    catch (Exception ex)
                    {
                        // rate limit
                        return;
                    }

                    if (response != null)
                    {
                        // ignore rate limiting
                        if (response.StatusCode == ((System.Net.HttpStatusCode)429)) { }
                        else if (!response.IsSuccessStatusCode) {
                            ParentDataContext.TextStatus = $"Failed to upload {planetDisplayName} World Control JSON. Response code: {response.StatusCode}";
                            if (MyDataContext.ShowErrors)
                            {
                                MessageBox.Show(ParentDataContext.TextStatus, "Error uploading JSON", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            return;
                        }

                        ParentDataContext.TextStatus = $"Successfully uploaded World Control JSON for {planetDisplayName}";
                    }
                }
            }
            catch (Exception) { }
            //catch (Exception ex)
            //{
            //    ParentDataContext.TextStatus = $"Error decoding World Control:\r\n{ex.Message}";
            //    if (MyDataContext.ShowErrors)
            //    {
            //        MessageBox.Show(ParentDataContext.TextStatus, "Error decoding World Control", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }
            //    return;
            //}
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
                        case 5:
                            HandleUploadWorldControl(planetId, planetDisplayName, curMessage);
                            break;
                    }
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            JsonUploadWindow.Instance = null;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ParentDataContext.ShutdownStarted)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
