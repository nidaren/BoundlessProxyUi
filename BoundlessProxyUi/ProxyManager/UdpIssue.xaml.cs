using BoundlessProxyUi.ProxyManager.Components;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for UdpIssue.xaml
    /// </summary>
    public partial class UdpIssue : UserControl
    {
        public UdpIssue(string message)
        {
            InitializeComponent();

            txtMessage.Text = message;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            btnContinue.IsEnabled = false;

            ComponentEngine.Instance.Start();
        }
    }
}
