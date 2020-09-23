using BoundlessProxyUi.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoundlessProxyUi.JsonUpload
{
    class JsonUploadWindowViewModel : INotifyPropertyChanged
    {
        public bool ShowErrors
        {
            get
            {
                return Config.GetSetting(nameof(ShowErrors), true);
            }
            set
            {
                Config.SetSetting(nameof(ShowErrors), value);
                OnPropertyChanged(nameof(ShowErrors));
            }
        }

        public bool JsonSaveFile
        {
            get
            {
                return Config.GetSetting(nameof(JsonSaveFile), false);
            }
            set
            {
                Config.SetSetting(nameof(JsonSaveFile), value);
                OnPropertyChanged(nameof(JsonSaveFile));
            }
        }
        public bool JsonSaveWcFile
        {
            get
            {
                return Config.GetSetting(nameof(JsonSaveWcFile), false);
            }
            set
            {
                Config.SetSetting(nameof(JsonSaveWcFile), value);
                OnPropertyChanged(nameof(JsonSaveWcFile));
            }
        }

        public bool JsonSaveApi
        {
            get
            {
                return Config.GetSetting(nameof(JsonSaveApi), false);
            }
            set
            {
                Config.SetSetting(nameof(JsonSaveApi), value);
                OnPropertyChanged(nameof(JsonSaveApi));
            }
        }

        public bool JsonSaveWcApi
        {
            get
            {
                return Config.GetSetting(nameof(JsonSaveWcApi), false);
            }
            set
            {
                Config.SetSetting(nameof(JsonSaveWcApi), value);
                OnPropertyChanged(nameof(JsonSaveWcApi));
            }
        }

        public string JsonApiKey
        {
            get
            {
                return Config.GetSetting(nameof(JsonApiKey), "");
            }
            set
            {
                Config.SetSetting(nameof(JsonApiKey), value);
                OnPropertyChanged(nameof(JsonApiKey));
            }
        }

        public string BaseFolder
        {
            get
            {
                return Config.GetSetting(nameof(BaseFolder), Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            }
            set
            {
                Config.SetSetting(nameof(BaseFolder), value);
                OnPropertyChanged(nameof(BaseFolder));
            }
        }

        public string ApiBaseUrl
        {
            get
            {
                return Config.GetSetting(nameof(ApiBaseUrl), "https://api.boundlexx.app/api");
            }
            set
            {
                Config.SetSetting(nameof(ApiBaseUrl), value);
                OnPropertyChanged(nameof(ApiBaseUrl));
            }
        }

        /// <summary>
        /// Occurs when a property value changes
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when a property value changes
        /// </summary>
        /// <param name="propertyName">The name  of the property that changed</param>
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
