using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for Processing.xaml
    /// </summary>
    public partial class Processing : UserControl
    {
        public Processing(string title = "Processing")
        {
            InitializeComponent();

            txtMain.Text = title;
        }
    }
}
