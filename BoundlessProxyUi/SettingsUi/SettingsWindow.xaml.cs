using System;
using System.Windows;

namespace BoundlessProxyUi.SettingsUi
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static SettingsWindow instance;
        public static SettingsWindow Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new SettingsWindow();
                }
                return instance;
            }
        }

        public static void CloseInstance()
        {
            if (instance != null)
            {
                instance.Close();
            }
        }

        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsUiViewModel();
        }

        private SettingsUiViewModel Data
        {
            get
            {
                return (SettingsUiViewModel)DataContext;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            instance = null;
        }

        private void ExportDir_Clicked(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                SelectedPath = Data.ExportDirectory
            };

            var result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Data.ExportDirectory = dialog.SelectedPath;
            }
        }
    }
}
