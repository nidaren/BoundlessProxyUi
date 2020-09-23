using BoundlessProxyUi.JsonUpload;
using BoundlessProxyUi.ProxyUi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for Running.xaml
    /// </summary>
    public partial class Running : UserControl
    {
        public Running()
        {
            dc = ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext);

            InitializeComponent();

            dc.TextStatus = "Running...";
        }

        private ManagerWindowViewModel dc;

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (JsonUploadWindow.Instance == null) {
                new JsonUploadWindow(dc);
                JsonUploadWindow.Instance.Show();
            }
            else
            {
                JsonUploadWindow.Instance.Show();
                JsonUploadWindow.Instance.Focus();
            }
        }

        private void proxyUIButton_Click(object sender, RoutedEventArgs e)
        {
            if (ProxyUiWindow.Instance == null)
            {
                new ProxyUiWindow(dc);
                ProxyUiWindow.Instance.Show();
            }
            else
            {
                ProxyUiWindow.Instance.Show();
                ProxyUiWindow.Instance.Focus();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ProxyUiWindow.Instance == null)
            {
                new ProxyUiWindow(dc);
            }

            if (JsonUploadWindow.Instance == null)
            {
                new JsonUploadWindow(dc);
            }

        }
    }
}

