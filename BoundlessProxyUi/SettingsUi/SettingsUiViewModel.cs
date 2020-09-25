using BoundlessProxyUi.Util;
using System;
using System.ComponentModel;

namespace BoundlessProxyUi.SettingsUi
{
    class SettingsUiViewModel : INotifyPropertyChanged
    {
        #region General Settings
        public bool ShowErrors
        {
            get
            {
                return ProxyManagerConfig.Instance.ShowErrors;
            }
            set
            {
                ProxyManagerConfig.Instance.ShowErrors = value;
                OnPropertyChanged(nameof(ShowErrors));
            }
        }

        public bool EnableLogging
        {
            get
            {
                return ProxyManagerConfig.Instance.EnableLogging;
            }
            set
            {
                ProxyManagerConfig.Instance.EnableLogging = value;
                OnPropertyChanged(nameof(EnableLogging));
            }
        }
        #endregion

        #region Archive Settings
        public String ExportDirectory
        {
            get
            {
                return ProxyManagerConfig.Instance.ExportDirectory;
            }
            set
            {
                ProxyManagerConfig.Instance.ExportDirectory = value;
                OnPropertyChanged(nameof(ExportDirectory));
            }
        }

        public bool SaveWorldJson
        {
            get
            {
                return ProxyManagerConfig.Instance.SaveWorldJson;
            }
            set
            {
                ProxyManagerConfig.Instance.SaveWorldJson = value;
                OnPropertyChanged(nameof(SaveWorldJson));
            }
        }

        public bool SaveWorldControlJson
        {
            get
            {
                return ProxyManagerConfig.Instance.SaveWorldControlJson;
            }
            set
            {
                ProxyManagerConfig.Instance.SaveWorldControlJson = value;
                OnPropertyChanged(nameof(SaveWorldControlJson));
            }
        }
        #endregion

        #region Boundlexx Settings
        public bool UploadWorldJson
        {
            get
            {
                return ProxyManagerConfig.Instance.UploadWorldJson;
            }
            set
            {
                ProxyManagerConfig.Instance.UploadWorldJson = value;
                OnPropertyChanged(nameof(UploadWorldJson));
            }
        }

        public bool UploadWorldControlJson
        {
            get
            {
                return ProxyManagerConfig.Instance.UploadWorldControlJson;
            }
            set
            {
                ProxyManagerConfig.Instance.UploadWorldControlJson = value;
                OnPropertyChanged(nameof(UploadWorldControlJson));
            }
        }

        public String BoundlexxApiKey
        {
            get
            {
                return ProxyManagerConfig.Instance.BoundlexxApiKey;
            }
            set
            {
                ProxyManagerConfig.Instance.BoundlexxApiKey = value;
                OnPropertyChanged(nameof(BoundlexxApiKey));
            }
        }

        public String BoundlexxApiBase
        {
            get
            {
                return ProxyManagerConfig.Instance.BoundlexxApiBase;
            }
            set
            {
                ProxyManagerConfig.Instance.BoundlexxApiBase = value;
                OnPropertyChanged(nameof(BoundlexxApiBase));
            }
        }
        #endregion

        #region Boundless Settings
        public String BoundlessDS
        {
            get
            {
                return ProxyManagerConfig.Instance.BoundlessDS;
            }
            set
            {
                ProxyManagerConfig.Instance.BoundlessDS = value;
                OnPropertyChanged(nameof(BoundlessDS));
            }
        }
        public String BoundlessAS
        {
            get
            {
                return ProxyManagerConfig.Instance.BoundlessAS;
            }
            set
            {
                ProxyManagerConfig.Instance.BoundlessAS = value;
                OnPropertyChanged(nameof(BoundlessAS));
            }
        }
        #endregion


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
