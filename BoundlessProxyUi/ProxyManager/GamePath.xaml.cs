﻿using BoundlessProxyUi.ProxyManager.Components;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;

namespace BoundlessProxyUi.ProxyManager
{
    /// <summary>
    /// Interaction logic for GamePath.xaml
    /// </summary>
    public partial class GamePath : UserControl
    {
        public GamePath(string message)
        {
            InitializeComponent();

            txtMessage.Text = message;
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).UserVerifiedGamePath = true;

            btnBrowse.IsEnabled = false;
            btnContinue.IsEnabled = false;
            ComponentEngine.Instance.Start();
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            string startPath = ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).GamePath;

            if (startPath.Length == 0)
            {
                startPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            }
            else
            {
                try
                {
                    startPath = System.IO.Path.GetDirectoryName(startPath);
                }
                catch
                {
                    startPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                }
            }

            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "Boundless Executable (boundless.exe)|boundless.exe",
                InitialDirectory = startPath,
            };

            var result = ofd.ShowDialog(ProxyManagerWindow.Instance);

            if (!result.HasValue || !result.Value)
            {
                return;
            }

            ((ManagerWindowViewModel)ProxyManagerWindow.Instance.DataContext).GamePath = ofd.FileName;
        }
    }
}
