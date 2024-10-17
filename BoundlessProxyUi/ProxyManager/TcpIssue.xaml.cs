using BoundlessProxyUi.ProxyManager.Components;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for TcpIssue.xaml
    /// </summary>
    public partial class TcpIssue : UserControl
    {
        public TcpIssue(string message)
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
