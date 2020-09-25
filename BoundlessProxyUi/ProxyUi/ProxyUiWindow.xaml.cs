using BoundlessProxyUi.ProxyManager;
using BoundlessProxyUi.ProxyManager.Components;
using BoundlessProxyUi.Util;
using BoundlessProxyUi.WsData;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BoundlessProxyUi.ProxyUi
{
    /// <summary>
    /// Interaction logic for ProxyUiWindow.xaml
    /// </summary>
    public partial class ProxyUiWindow : Window
    {
        private static ProxyUiWindow instance;
        public static ProxyUiWindow Instance { get
            {
                if (instance == null)
                {
                    instance = new ProxyUiWindow();
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
         
        public static void AddFrame(WsFrame frame, CommPacketDirection direction, ConnectionInstance connectionInstance)
        {
            if (instance == null || !instance.Data.CaptureEnabled)
            {
                return;
            }

            try
            {
                var parentPacket = new CommPacket
                {
                    Data = frame.HeaderBytes,
                    Direction = direction,
                    Id = Guid.NewGuid(),
                    Instance = connectionInstance,
                    ParentPacket = null,
                    Header = "Websocket Frame",
                };

                foreach (var curMessage in frame.Messages)
                {
                    var headerPacket = new CommPacket
                    {
                        Data = BitConverter.GetBytes((ushort)(curMessage.Buffer.Length + 1)).Concat(new byte[] { curMessage.ApiId ?? 0 }).ToArray(),
                        Direction = direction,
                        Id = Guid.NewGuid(),
                        Instance = connectionInstance,
                        ParentPacket = parentPacket,
                        Header = $"Websocket Message[0x{curMessage.ApiId ?? 0:X2}]",
                    };

                    var payloadPacket = new CommPacket
                    {
                        Data = curMessage.Buffer,
                        Direction = direction,
                        Id = Guid.NewGuid(),
                        Instance = connectionInstance,
                        ParentPacket = parentPacket,
                        Header = $"Websocket Payload",
                    };

                    headerPacket.ChildPackets.Add(payloadPacket);
                    parentPacket.ChildPackets.Add(headerPacket);
                };

                instance.Data.SendBytesToUi(connectionInstance, parentPacket);
            }
            catch (Exception ex) {
                Log.Error(ex, "Error in AddFrame");
            }
        }

        public static void WriteBytes(byte[] buffer, int count, CommPacketDirection direction, ConnectionInstance connectionInstance)
        {
            if (instance == null || !instance.Data.CaptureEnabled)
            {
                return;
            }

            try
            {
                byte[] saveData = new byte[count];
                Buffer.BlockCopy(buffer, 0, saveData, 0, count);

                var length = saveData.Search(saveData.Length, new byte[] { 13, 10 });
                var dataStringSegments = Encoding.UTF8.GetString(saveData, 0, length).Split(' ');

                string header = "HTTP Data";

                if (dataStringSegments[2].StartsWith("HTTP"))
                {
                    header = $"{dataStringSegments[0]} {dataStringSegments[1]}";
                }
                else if (dataStringSegments[0].StartsWith("HTTP"))
                {
                    var desc = dataStringSegments[2];

                    for (int i = 3; i < dataStringSegments.Length; ++i)
                    {
                        desc += $" {dataStringSegments[i]}";
                    }

                    header = $"{dataStringSegments[1]} {desc}";
                }

                instance.Data.SendBytesToUi(connectionInstance, new CommPacket
                {
                    Data = saveData,
                    Direction = direction,
                    Id = Guid.NewGuid(),
                    Instance = connectionInstance,
                    ParentPacket = null,
                    Header = header,
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in WriteBytes");
            }
        }

        public static void SendBytesToUi(ConnectionInstance connectionInstance, CommPacket packet)
        {
            if (instance == null || !instance.Data.CaptureEnabled)
            {
                return;
            }

            try
            {
                instance.Data.SendBytesToUi(connectionInstance, packet);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in SendBytesToUi");
            }
        }

        public ProxyUiWindow()
        {
            //ServicePointManager.ServerCertificateValidationCallback += delegate { return true; };
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3;

            InitializeComponent();

            try
            {
                cmbSearchType.ItemsSource = Enum.GetValues(typeof(UserSearchType));
                DataContext = new ProxyUiWindowViewModel();
            }
            catch (Exception ex)
            {
                var message = $"Fatal error during startup:\r\n{ex.Message}";
                Log.Error(ex, message);
                MessageBox.Show(message, "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private ProxyUiWindowViewModel Data
        {
            get
            {
                return (ProxyUiWindowViewModel)DataContext;
            }
        }

        private bool m_dontMainSelect = false;
        private readonly Regex numbersPattern = new Regex("[^0-9]+");

        private void LstPackets_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var oldPacket = e.OldValue as CommPacket;

            if (oldPacket != null)
            {
                while (oldPacket.ParentPacket != null)
                {
                    oldPacket = oldPacket.ParentPacket;
                }

                if (!oldPacket.Instance.ChildPackets.Contains(oldPacket))
                {
                    TreeViewItem item = lstPackets.ItemContainerGenerator.ContainerFromIndex(0) as TreeViewItem;
                    if (item != null)
                    {
                        item.IsSelected = true;
                        item.IsSelected = false;
                    }
                }
            }

            if (!m_dontMainSelect)
            {
                var stream = (lstPackets.SelectedItem as CommPacket)?.Stream;

                if (stream != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        hexMain.Stream = stream;
                    }));
                    lstSearchPackets.SelectedItem = null;
                }
            }
        }

        private void BtnAddSearch_Click(object sender, RoutedEventArgs e)
        {
            Data.Searches.Add(new UserSearch
            {
                UserSearchType = (UserSearchType)cmbSearchType.SelectedItem,
                UserValue = txtSearchValue.Text,
            });
        }

        private void BtnRemoveSearch_Click(object sender, RoutedEventArgs e)
        {
            Data.Searches.Remove(lstSearches.SelectedItem as UserSearch);
        }

        private void LstSearchPackets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var packet = lstSearchPackets.SelectedItem as CommPacket;

            if (packet != null)
            {
                hexMain.Stream = packet.Stream;

                new Thread(() =>
                {
                    GotoSearchPacket(packet);

                    bool retry = true;

                    while (retry)
                    {
                        retry = false;

                        Dispatcher.Invoke(new Action(() =>
                        {
                            try
                            {
                                //hexMain.SelectionStart = packet.Data.Search(packet.Data.Length, DataContext.SelectedSearch.searchBytes);
                                //hexMain.SelectionStop = hexMain.SelectionStart + DataContext.SelectedSearch.searchBytes.Length - 1;
                                hexMain.SetPosition(packet.Data.Search(packet.Data.Length, Data.SelectedSearch.searchBytes), Data.SelectedSearch.searchBytes.Length);
                            }
                            catch
                            {
                                retry = true;
                            }
                        }));
                    }
                }).Start();
            }
        }

        private void BtnGotoPacket_Click(object sender, RoutedEventArgs e)
        {
            var packet = lstSearchPackets.SelectedItem as CommPacket;

            if (packet != null)
            {
                GotoSearchPacket(packet);
            }
        }

        private void GotoSearchPacket(CommPacket packet)
        {
            m_dontMainSelect = true;

            Dispatcher.Invoke(new Action(() =>
            {
                lstGroups.SelectedItem = packet.Instance.Parent;
                lstInstances.SelectedItem = packet.Instance;

                ItemContainerGenerator doExpand(CommPacket curPacket)
                {
                    ItemContainerGenerator result = null;

                    if (curPacket.ParentPacket != null)
                    {
                        result = doExpand(curPacket.ParentPacket);
                    }

                    if (result == null)
                    {
                        result = lstPackets.ItemContainerGenerator;
                    }

                    var curItem = result.ContainerFromItem(curPacket) as TreeViewItem;

                    if (curItem != null)
                    {
                        curItem.IsSelected = true;
                        curItem.ExpandSubtree();
                        curItem.BringIntoView();

                        return curItem.ItemContainerGenerator;
                    }
                    else
                    {
                        return null;
                    }
                }

                doExpand(packet);
            }));

            m_dontMainSelect = false;
        }

        private void PreviewNumberInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = numbersPattern.IsMatch(e.Text);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            instance = null;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!ProxyManagerWindow.Instance.Data.ShutdownStarted)
            {
                e.Cancel = true;
                instance.Hide();
            }
        }
    }
}
