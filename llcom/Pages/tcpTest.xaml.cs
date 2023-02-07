using llcom.LuaEnv;
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

        public WebSocket ws = new WebSocket("wss://netlab.luatos.com/ws/netlab");
        public WebSocket wsV6 = new WebSocket("wss://netlab.luatos.org/ws/netlab");
        ObservableCollection<string> clients = new ObservableCollection<string>();

        /// <summary>
        /// 连接状态
        /// </summary>
        public bool IsConnected { get; set; } = false;
        public bool HexMode { get; set; } = false;
        public string Address { get; set; } = "loading...";
        public string AddressV6 { get; set; } = "loading...";
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

            Timer heartbeatV6 = new Timer();
            heartbeatV6.Interval = 30000;
            heartbeatV6.AutoReset = true;
            heartbeatV6.Elapsed += (ss, ee) => { try { if (IsConnected) wsV6.Send("{}"); } catch { } };

            //ws事件
            ws.OnOpen += (ss, ee) => { IsConnected = true; heartbeat.Start(); this.Dispatcher.Invoke(() => { clients.Clear(); }); };
            wsV6.OnOpen += (ss, ee) => { IsConnected = true; heartbeatV6.Start(); this.Dispatcher.Invoke(() => { clients.Clear(); }); };
            ws.OnClose += (ss, ee) => { IsConnected = false; heartbeat.Stop(); this.Dispatcher.Invoke(() => { clients.Clear(); }); };
            wsV6.OnClose += (ss, ee) => { IsConnected = false; heartbeatV6.Stop(); this.Dispatcher.Invoke(() => { clients.Clear(); }); };
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
                            Address = $"{ConnectionType}://112.125.89.8:{o["port"]}";
                            AddressV6 = "not suppoer ipv6";
                            ShowData($"📢 Created a {ConnectionType} server.");
                            break;
                        case "client":
                        case "connected":
                            ShowData($"✔ [{o["client"]}]{o["addr"]} connected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Add((string)o["client"]);
                                if (ClientList.Text.Length == 0)
                                    ClientList.Text = (string)o["client"];
                            }));
                            break;
                        case "closed":
                            ShowData($"❌ [{o["client"]}] disconnected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Remove((string)o["client"]);
                                if (ClientList.Text.Length == 0 && clients.Count > 0)
                                    ClientList.Text = clients[0];
                            }));
                            break;
                        case "data":
                            string data = (string)o["data"];
                            byte[] buff = (bool)o["hex"] ? Global.Hex2Byte(data) : Global.GetEncoding().GetBytes(data);
                            ShowData($" → receive from [{o["client"]}]", buff);
                            //适配一下通用通道
                            LuaApis.SendChannelsReceived("netlab",
                                new
                                {
                                    client = (string)o["client"],
                                    data = buff,
                                });
                            break;
                        case "error":
                            ShowData($"❔ error:{o["msg"]}");
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
            wsV6.OnMessage += (ss, ee) => {
                Debug.WriteLine(!ee.IsPing ? ee.Data : "ping");
                if (ee.IsPing)
                    return;
                try
                {
                    JObject o = (JObject)JsonConvert.DeserializeObject(ee.Data);
                    switch ((string)o["action"])
                    {
                        case "port":
                            Address = $"{ConnectionType}://152.70.80.204:{o["port"]}";
                            AddressV6 = $"{ConnectionType}://[2603:c023:1:5fcc:c028:8ed:49a7:6e08]:{o["port"]}";
                            ShowData($"📢 Created a {ConnectionType} server.");
                            break;
                        case "client":
                        case "connected":
                            ShowData($"✔ [{o["client"]}]{o["addr"]} connected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Add((string)o["client"]);
                                if (ClientList.Text.Length == 0)
                                    ClientList.Text = (string)o["client"];
                            }));
                            break;
                        case "closed":
                            ShowData($"❌ [{o["client"]}] disconnected.");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                clients.Remove((string)o["client"]);
                                if (ClientList.Text.Length == 0 && clients.Count > 0)
                                    ClientList.Text = clients[0];
                            }));
                            break;
                        case "data":
                            string data = (string)o["data"];
                            byte[] buff = (bool)o["hex"] ? Global.Hex2Byte(data) : Global.GetEncoding().GetBytes(data);
                            ShowData($" → receive from [{o["client"]}]", buff);
                            //适配一下通用通道
                            LuaApis.SendChannelsReceived("netlab",
                                new
                                {
                                    client = (string)o["client"],
                                    data = buff,
                                });
                            break;
                        case "error":
                            ShowData($"❔ error:{o["msg"]}");
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

            ws.OnError += (ss, ee) =>
            {
                ShowData($"📢 Create failed");
            };
            wsV6.OnError += (ss, ee) =>
            {
                ShowData($"📢 Create failed");
            };

            //适配一下通用通道
            LuaApis.SendChannelsRegister("netlab", (_, t) =>
            {
                if (IsConnected && t != null)
                {
                    return Send(
                        Tools.Global.Byte2Hex(t.Get<byte[]>("data")),
                        true,
                        t.Get<string>("client")
                        );
                }
                else
                    return false;
            });
        }


        private void ShowData(string title, byte[] data = null, bool send = false)
        {
            Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
            {
                title = $"🚀 public socket server: {title}",
                data = data ?? new byte[0],
                color = send ? Brushes.DarkRed : Brushes.DarkGreen,
            });
        }

        public bool connecting { get; set; } = false;
        private async void ConnectWebSocket(string ctype,string stype = null)
        {
            if (connecting)
                return;
            connecting = true;
            ShowData($"📢 Server is creating...");
            await Task.Run(() =>
            {
                try
                {
                    if (ctype == "tcpv6")
                    {
                        ConnectionType = "tcp";
                        wsV6.Connect();
                        wsV6.Send(JsonConvert.SerializeObject(new
                        {
                            action = "newp",
                            type = "tcp",
                        }));
                    }
                    else
                    {
                        ConnectionType = ctype;
                        ws.Connect();
                        ws.Send(JsonConvert.SerializeObject(new
                        {
                            action = "newp",
                            type = stype ?? ctype,
                        }));
                    }
                }
                catch (Exception e)
                {
                    //ShowData($"📢 Create failed, {e.Message}");
                }
            });
            connecting = false;
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
                (ws.IsAlive ? ws : wsV6).Close();
                ShowData($"📢 Server closed.");
                Address = "loading...";
                AddressV6 = "loading...";
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
            Send(toSendDataTextBox.Text,HexMode, ClientList.Text);
        }

        private bool Send(string data, bool isHex, string client)
        {
            try
            {
                (ws.IsAlive ? ws : wsV6).Send(JsonConvert.SerializeObject(new
                {
                    action = "sendc",
                    data,
                    hex = isHex,
                    client,
                }));
                ShowData($" ← send to [{client}]",
                    isHex ? Global.Hex2Byte(data) : Global.GetEncoding().GetBytes(data),
                    true);
                return true;
            }
            catch (Exception ee)
            {
                ShowData($"sent error: {ee.Message}");
                return false;
            }
        }

        private void KickClientButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected || ClientList.Text.Length == 0)
                return;
            try
            {
                (ws.IsAlive ? ws : wsV6).Send(JsonConvert.SerializeObject(new
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

        private void CreateTcpIpv6Button_Click(object sender, RoutedEventArgs e)
        {
            ConnectWebSocket("tcpv6");
        }
    }
}
