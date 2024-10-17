using System;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for HostsFileUpdate.xaml
    /// </summary>
    public partial class HostsFileUpdate : UserControl
    {
        public HostsFileUpdate(string message, string[] requiredEntries)
        {
            InitializeComponent();
            txtMessage.Text = message;
            txtHostsFileLocation.Text += "\r\n" + System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"System32\drivers\etc\hosts");
            txtAddItems.Text = string.Join("\r\n", requiredEntries);
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            btnContinue.IsEnabled = false;
            ProxyManagerWindow.Instance.FadeControl(new HostsFileConfirm());
        }
    }
}
