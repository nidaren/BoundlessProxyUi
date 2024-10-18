using BoundlessProxyUi.ProxyManager.Components;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for CheckCert.xaml
    /// </summary>
    public partial class CheckCert : UserControl
    {
        public CheckCert(string message)
        {
            InitializeComponent();

            txtMessage.Text = message;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).UserAuthorizedCert = true;

            btnContinue.IsEnabled = false;
            ComponentEngine.Instance.Start();
        }
    }
}
