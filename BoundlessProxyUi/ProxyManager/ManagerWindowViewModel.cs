using BoundlessProxyUi.ProxyManager.Components;
using BoundlessProxyUi.Util;
using System;
using System.ComponentModel;
using System.Linq;

namespace BoundlessProxyUi.ProxyManager
{
    public class ManagerWindowViewModel : INotifyPropertyChanged
    {
        public bool UserVerifiedGamePath
        {
            get
            {
                return Config.GetSetting(nameof(UserVerifiedGamePath), false);
            }
            set
            {
                Config.SetSetting(nameof(UserVerifiedGamePath), value);
                OnPropertyChanged(nameof(UserVerifiedGamePath));
            }
        }

        public string GamePath
        {
            get
            {
                return Config.GetSetting(nameof(GamePath), @"C:\Program Files (x86)\Steam\steamapps\common\Boundless\boundless.exe");
            }
            set
            {
                Config.SetSetting(nameof(GamePath), value);
                OnPropertyChanged(nameof(GamePath));
            }
        }

        public bool UserAuthorizedCert
        {
            get
            {
                return Config.GetSetting(nameof(UserAuthorizedCert), false);
            }
            set
            {
                Config.SetSetting(nameof(UserAuthorizedCert), value);
                OnPropertyChanged(nameof(UserAuthorizedCert));
            }
        }

        public bool UserAuthorizedHostFile
        {
            get
            {
                return Config.GetSetting(nameof(UserAuthorizedHostFile), false);
            }
            set
            {
                Config.SetSetting(nameof(UserAuthorizedHostFile), value);
                OnPropertyChanged(nameof(UserAuthorizedHostFile));
            }
        }

        private string _processingText = "Please Wait";
        public string ProcessingText
        {
            get
            {
                return _processingText;
            }
            set
            {
                _processingText = value;
                OnPropertyChanged(nameof(ProcessingText));
            }
        }

        private int _conversations = 0;
        public int Conversations
        {
            get
            {
                return _conversations;
            }
            set
            {
                _conversations = value;
                OnPropertyChanged(nameof(Conversations));
            }
        }

        private string _textStatus = "";
        public string TextStatus
        {
            get { return _textStatus; }
            set
            {
                _textStatus = value;
                OnPropertyChanged(nameof(TextStatus));
            }
        }

        public bool ShutdownStarted
        {
            get;
            set;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RefreshConversations()
        {
            Conversations = ComponentEngine.Instance.GetComponent<TcpComponent>().groups.SelectMany(cur => cur.Instances).Where(cur => cur.IsConnectionOpen).Count();
        }
    }
}
