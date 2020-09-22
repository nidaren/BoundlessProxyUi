using BoundlessProxyUi.ProxyManager;
using BoundlessProxyUi.WsData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        public void OnFrameIn<T>(int planetId, string planetDisplayName, T frame_object)
        {
            var frame = frame_object as WsFrame;

            foreach (var curMessage in frame.Messages)
            {
                if (curMessage.ApiId.HasValue && curMessage.ApiId.Value == 0 && curMessage.Buffer.Length > 0)
                {
                    JObject payload = null;

                    try
                    {
                        payload = JObject.Parse(Encoding.UTF8.GetString(curMessage.Buffer));
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    if (MyDataContext.JsonSaveFile)
                    {
                        var invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();
                        var fileName = new string(planetDisplayName.Where(cur => !invalidFileNameChars.Contains(cur)).ToArray());

                        try
                        {
                            File.WriteAllText($"{fileName}.json", payload.ToString(Formatting.Indented));
                        }
                        catch (Exception ex)
                        {
                            ParentDataContext.TextStatus = $"Failed to write {fileName}.json:\r\n{ex.Message}";
                            MessageBox.Show(ParentDataContext.TextStatus, "Error writing json", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            ParentDataContext.TextStatus = $"Failed to upload {planetDisplayName} json: {ex.Message}";
                            MessageBox.Show(ParentDataContext.TextStatus, "Error uploading json", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (response != null && !response.IsSuccessStatusCode)
                        {
                            ParentDataContext.TextStatus = $"Failed to upload {planetDisplayName} json. Response code: {response.StatusCode}";
                            MessageBox.Show(ParentDataContext.TextStatus, "Error uploading json", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        ParentDataContext.TextStatus = $"Successfully uploaded world JSON for {planetDisplayName}";
                    }
                }
            }
        }
    }
}
