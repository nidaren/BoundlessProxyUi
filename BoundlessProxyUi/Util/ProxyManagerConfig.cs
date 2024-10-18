using System;
using System.ComponentModel;
using System.IO;

namespace BoundlessProxyUi.Util
{
    class ProxyManagerConfig : INotifyPropertyChanged
    {
        private static ProxyManagerConfig instance;
        public static ProxyManagerConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ProxyManagerConfig();
                }
                return instance;
            }
        }

        #region General Settings
        public bool ShowErrors
        {
            get
            {
                return Config.GetSetting(nameof(ShowErrors), false);
            }
            set
            {
                Config.SetSetting(nameof(ShowErrors), value);
                OnPropertyChanged(nameof(ShowErrors));
            }
        }

        public bool IsPortable
        {
            get
            {
                return Config.IsPortable;
            }
        }

        public string BaseDirectory
        {
            get
            {
                return Config.BaseDirectory;
            }
        }
        #endregion

        #region Archive Settings
        public string ExportDirectory
        {
            get
            {
                string defaultPath = Path.Combine(BaseDirectory, "export");
                Directory.CreateDirectory(defaultPath);
                return Config.GetSetting(nameof(ExportDirectory), defaultPath);
            }
            set
            {
                Config.SetSetting(nameof(ExportDirectory), value);
                OnPropertyChanged(nameof(ExportDirectory));
            }
        }

        public bool SaveWorldJson
        {
            get
            {
                return Config.GetSetting(nameof(SaveWorldJson), false);
            }
            set
            {
                Config.SetSetting(nameof(SaveWorldJson), value);
                OnPropertyChanged(nameof(SaveWorldJson));
            }
        }

        public bool SaveWorldControlJson
        {
            get
            {
                return Config.GetSetting(nameof(SaveWorldControlJson), false);
            }
            set
            {
                Config.SetSetting(nameof(SaveWorldControlJson), value);
                OnPropertyChanged(nameof(SaveWorldControlJson));
            }
        }
        #endregion

        #region Boundlexx Settings
        public bool UploadWorldJson
        {
            get
            {
                return Config.GetSetting(nameof(UploadWorldJson), false);
            }
            set
            {
                Config.SetSetting(nameof(UploadWorldJson), value);
                OnPropertyChanged(nameof(UploadWorldJson));
            }
        }

        public bool UploadWorldControlJson
        {
            get
            {
                return Config.GetSetting(nameof(UploadWorldControlJson), false);
            }
            set
            {
                Config.SetSetting(nameof(UploadWorldControlJson), value);
                OnPropertyChanged(nameof(UploadWorldControlJson));
            }
        }

        public String BoundlexxApiKey
        {
            get
            {
                return Config.GetSetting(nameof(BoundlexxApiKey), "");
            }
            set
            {
                Config.SetSetting(nameof(BoundlexxApiKey), value);
                OnPropertyChanged(nameof(BoundlexxApiKey));
            }
        }

        public String BoundlexxApiBase
        {
            get
            {
                return Config.GetSetting(nameof(BoundlexxApiBase), Constants.BoundlexxApi);
            }
            set
            {
                Config.SetSetting(nameof(BoundlexxApiBase), value);
                OnPropertyChanged(nameof(BoundlexxApiBase));
            }
        }
        #endregion

        #region Boundless Settings
        public String BoundlessDS
        {
            get
            {
                return Config.GetSetting(nameof(BoundlessDS), Constants.DiscoveryServer);
            }
            set
            {
                Config.SetSetting(nameof(BoundlessDS), value);
                OnPropertyChanged(nameof(BoundlessDS));
            }
        }

        public String BoundlessAS
        {
            get
            {
                return Config.GetSetting(nameof(BoundlessAS), Constants.AccountServer);
            }
            set
            {
                Config.SetSetting(nameof(BoundlessAS), value);
                OnPropertyChanged(nameof(BoundlessAS));
            }
        }

        public String BoundlessServerPrefix
        {
            get
            {
                return Config.GetSetting(nameof(BoundlessServerPrefix), Constants.ServerPrefix);
            }
            set
            {
                Config.SetSetting(nameof(BoundlessServerPrefix), value);
                OnPropertyChanged(nameof(BoundlessServerPrefix));
            }
        }
        #endregion

        #region ProxyUI Settings
        public int PacketsPerInstance
        {
            get
            {
                return Config.GetSetting(nameof(PacketsPerInstance), 500);
            }
            set
            {
                Config.SetSetting(nameof(PacketsPerInstance), value);
                OnPropertyChanged(nameof(PacketsPerInstance));
            }
        }

        public int DeathTimeout
        {
            get
            {
                return Config.GetSetting(nameof(DeathTimeout), 60);
            }
            set
            {
                Config.SetSetting(nameof(DeathTimeout), value);
                OnPropertyChanged(nameof(DeathTimeout));
            }
        }
        #endregion


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
