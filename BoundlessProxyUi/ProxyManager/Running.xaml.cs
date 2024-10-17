using BoundlessProxyUi.ProxyUi;
using BoundlessProxyUi.SettingsUi;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary> 
    /// Interaction logic for Running.xaml
    /// </summary>
    public partial class Running : UserControl
    {
        public Running()
        {
            InitializeComponent();

            ProxyManagerWindow.Instance.SetStatusText("Running...");
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow.Instance.Show();
            SettingsWindow.Instance.Focus();
        }

        private void ProxyUIButton_Click(object sender, RoutedEventArgs e)
        {
            ProxyUiWindow.Instance.Show();
            ProxyUiWindow.Instance.Focus();
        }
    }
}

