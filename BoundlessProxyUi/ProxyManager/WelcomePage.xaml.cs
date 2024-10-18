using BoundlessProxyUi.ProxyManager.Components;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for WelcomePage.xaml
    /// </summary>
    public partial class WelcomePage : UserControl
    {
        public WelcomePage()
        {
            InitializeComponent();
        }

        private void BtnAcceept_Click(object sender, RoutedEventArgs e)
        {
            btnAcceept.IsEnabled = false;
            btnCancel.IsEnabled = false;
            ComponentEngine.Instance.Start();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            btnAcceept.IsEnabled = false;
            btnCancel.IsEnabled = false;
            ProxyManagerWindow.Instance.Close();
        }
    }
}
