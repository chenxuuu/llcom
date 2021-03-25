using llcom.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WebSocketSharp;

namespace llcom.Pages
{
    /// <summary>
    /// tcpTest.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class tcpTest : Page
    {
        public tcpTest()
        {
            InitializeComponent();
        }

        public WebSocket ws = new WebSocket("ws://netlab.luatos.com/ws/netlab");
        ObservableCollection<string> clients = new ObservableCollection<string>();

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected { get; set; } = false;
        public bool HexMode { get; set; } = false;
        public string Address { get; set; } = "loading...";
        public string ConnectionType { get; set; } = "unknow";

        private static bool loaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            //绑定
            MainGrid.DataContext = this;
            ClientList.ItemsSource = clients;
            //心跳
            Timer heartbeat = new Timer();
            heartbeat.Interval = 30000;
            heartbeat.AutoReset = true;
            heartbeat.Elapsed += (ss, ee) => { try { if (IsConnected) ws.Send("{}"); } catch { } };
            //ws事件
            ws.OnOpen += (ss, ee) => { IsConnected = true; heartbeat.Start(); clients.Clear();};
            ws.OnClose += (ss, ee) => { IsConnected = false; heartbeat.Stop(); clients.Clear();};
            ws.OnMessage += (ss, ee) => {
                Debug.WriteLine(!ee.IsPing ? ee.Data : "ping");
                if (ee.IsPing)
                    return;
                try
                {
                    JObject o = (JObject)JsonConvert.DeserializeObject(ee.Data);
                    switch ((string)o["action"])
                    {
                        case "port":
                            var host = Dns.GetHostEntry("netlab.vue2.cn");
                            string ip = host.AddressList.Length > 0 ? host.AddressList[0].ToString() : "netlab.vue2.cn";
                            Address = $"{ConnectionType}://{ip}:{o["port"]}";
                            ShowData($"Created a {ConnectionType} server.");
                            break;
                        case "client":
                        case "connected":
                            ShowData($"[{o["client"]}]{o["addr"]} connected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Add((string)o["client"]);
                                if (ClientList.Text.Length == 0)
                                    ClientList.Text = (string)o["client"];
                            }));
                            break;
                        case "closed":
                            ShowData($"[{o["client"]}] disconnected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Remove((string)o["client"]);
                                if (ClientList.Text.Length == 0 && clients.Count > 0)
                                    ClientList.Text = clients[0];
                            }));
                            break;
                        case "data":
                            string data = (string)o["data"];
                            if ((bool)o["hex"] && (!HexMode))
                                data = Global.Hex2String(data);
                            ShowData($"[{o["client"]}] ← {(HexMode ? "[HEX]" : "")}{data}");
                            break;
                        case "error":
                            ShowData($"[error]{o["msg"]}");
                            break;
                        default:
                            break;
                    }
            }
                catch
            {
                //先不管错误
            }
        };
        }

        private void ShowData(string s)
        {
            this.Dispatcher.Invoke(new Action(delegate
            {
                DataTextBox.AppendText($"[{DateTime.Now.ToString("HH:mm:ss.ffff")}]{s}\r\n");
                DataTextBox.ScrollToEnd();
            }));
        }

        private void ConnectWebSocket(string ctype,string stype = null)
        {
            try
            {
                ConnectionType = ctype;
                ws.Connect();
                ws.Send(JsonConvert.SerializeObject(new {
                    action = "newp",
                    type = stype ?? ctype,
                }));
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        private void CreateTcpButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectWebSocket("tcp");
        }

        private void CreateUdpButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectWebSocket("udp");
        }

        private void CreateTcpSSLButton_OnClick(object sender, RoutedEventArgs e)
        {
            ConnectWebSocket("ssl","ssl-tcp");
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ws.Close();
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || ClientList.Text.Length == 0)
                return;
            try
            {
                ws.Send(JsonConvert.SerializeObject(new
                {
                    action = "sendc",
                    data = toSendDataTextBox.Text,
                    hex = HexMode,
                    client = ClientList.Text,
                }));
                ShowData($"[{ClientList.Text}] → {(HexMode ? "[HEX]" : "")}{toSendDataTextBox.Text}");
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            DataTextBox.Clear();
        }

        private void KickClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || ClientList.Text.Length == 0)
                return;
            try
            {
                ws.Send(JsonConvert.SerializeObject(new
                {
                    action = "closec",
                    client = ClientList.Text,
                }));
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message);
            }
        }

    }
}
