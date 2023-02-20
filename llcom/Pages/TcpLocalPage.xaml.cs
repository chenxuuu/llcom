using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
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
using System.Threading;
using CoAP.Server;
using System.Diagnostics;
using static llcom.Pages.SocketClientPage;
using llcom.LuaEnv;

namespace llcom.Pages
{
    /// <summary>
    /// TcpLocalPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class TcpLocalPage : Page
    {
        public TcpLocalPage()
        {
            InitializeComponent();
        }

        //收到消息的事件
        public event EventHandler<byte[]> DataRecived;
        public bool IsConnected { get; set; } = false;

        private static bool loaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            RefreshIp();
            //绑定
            MainGrid.DataContext = this;
            IpPortTextBox.DataContext = Tools.Global.setting;

            //收到消息，显示日志
            DataRecived += (name, data) =>
            {
                ShowData($" → receive ({(string)name})", data);
            };

            //适配一下通用通道
            LuaApis.SendChannelsRegister("tcp-server", (data, _) =>
            {
                if (Server != null && data != null)
                {
                    return Broadcast(data);
                }
                else
                    return false;
            });
            //通用通道收到消息
            DataRecived += (name, data) =>
            {
                LuaApis.SendChannelsReceived("tcp-server", 
                    new
                    {
                        from = (string)name,
                        data
                    });
            };
        }

        /// <summary>
        /// 刷新本机ip列表
        /// </summary>
        private void RefreshIp()
        {
            IpListComboBox.Items.Clear();
            IpListComboBox.Items.Add("0.0.0.0");
            IpListComboBox.Items.Add("::");
            var temp = new List<string>();
            try
            {
                string name = Dns.GetHostName();
                IPAddress[] ipadrlist = Dns.GetHostAddresses(name);
                foreach (IPAddress ipa in ipadrlist)
                {
                    if (ipa.AddressFamily == AddressFamily.InterNetwork ||
                        ipa.AddressFamily == AddressFamily.InterNetworkV6)
                        temp.Add(ipa.ToString());
                }
            }
            catch { }
            //去重
            temp.Distinct().ToList().ForEach(ip => IpListComboBox.Items.Add(ip));
            IpListComboBox.SelectedIndex = 0;
        }
        private void ShowData(string title, byte[] data = null, bool send = false)
        {
            Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
            {
                title = $"🛰 local tcp server: {title}",
                data = data ?? new byte[0],
                color = send ? Brushes.DarkRed : Brushes.DarkGreen,
            });
        }

        /// <summary>
        /// 获取客户端的名字
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string GetClientName(Socket s)
        {
            var remote = (IPEndPoint)s.RemoteEndPoint;
            var remoteIsV6 = remote.Address.ToString().Contains(":");
            var local = (IPEndPoint)s.LocalEndPoint;
            var localIsV6 = local.Address.ToString().Contains(":");
            return $"{(remoteIsV6 ? "[" : "")}{remote.Address}{(remoteIsV6 ? "]" : "")}:{remote.Port} → " +
                $"{(localIsV6 ? "[" : "")}{local.Address}{(localIsV6 ? "]" : "")}:{local.Port}";
        }


        private TcpListener Server = null;
        private List<Socket> Clients = new List<Socket>();


        public void Read_Callback(IAsyncResult ar)
        {
            StateObject so = (StateObject)ar.AsyncState;
            Socket s = so.workSocket;
            try
            {
                var name = GetClientName(s);
                int read = s.EndReceive(ar);

                if (read > 0)
                {
                    var buff = new byte[read];
                    for (int i = 0; i < buff.Length; i++)
                        buff[i] = so.buffer[i];
                    DataRecived?.Invoke(name, buff);
                    s.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, 0,
                                             new AsyncCallback(Read_Callback), so);
                }
            }
            catch//断了？
            {
                try
                {
                    var name = GetClientName(s);
                    lock (Clients)
                        Clients.Remove(s);
                    try
                    {
                        s.Close();
                        s.Dispose();
                    }
                    catch { }
                    ShowData($"☠ {name}");
                }
                catch { }
            }
        }

        /// <summary>
        /// 开始监听服务器
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        private bool StartServer(string ip, int port)
        {
            if (Server != null)
                return false;
            IPAddress localAddr = IPAddress.Parse(ip);
            Server = new TcpListener(localAddr, port);
            Server.Start();
            var isV6 = ip.Contains(":");
            ShowData($"🛰 {(isV6 ? "[" : "")}{ip}{(isV6 ? "]" : "")}:{port}");
            AsyncCallback newConnectionCb = null;
            newConnectionCb = new AsyncCallback((ar) =>
            {
                TcpListener listener = (TcpListener)ar.AsyncState;
                try
                {
                    Socket client = listener.EndAcceptSocket(ar);//必须有这一句，不然新的请求没反应
                    ShowData($"😀 {GetClientName(client)}");
                    lock (Clients)
                        Clients.Add(client);//加到列表里

                    //客户端数据接收回调
                    StateObject so = new StateObject();
                    so.workSocket = client;
                    client.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(Read_Callback), so);

                    //恢复服务端的回调函数，方便下次接收
                    Server.BeginAcceptSocket(newConnectionCb, Server);
                }
                catch { }
            });
            try
            {
                Server.BeginAcceptSocket(newConnectionCb, Server);
            }
            catch (Exception ex)
            {
                ShowData($"❗ Server create error {ex.Message}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 关闭服务器，断开所有连接
        /// </summary>
        private void StopServer()
        {
            lock (Clients)
            {
                foreach(var c in Clients)
                    try
                    {
                        var name = GetClientName(c);
                        c.Close();
                        c.Dispose();
                        ShowData($"☠ {name}");
                    }
                    catch { }
                Clients.Clear();
            }
            Server?.Stop();
            Server = null;
        }

        private void RefreshIpButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshIp();
        }

        private void ListenButton_Click(object sender, RoutedEventArgs e)
        {
            int port;
            if(int.TryParse(IpPortTextBox.Text, out port))
            {
                try
                {
                    IsConnected = StartServer(IpListComboBox.Text, port);
                }
                catch(Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
                }
            }
        }

        private void StopListenButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StopServer();
                IsConnected = false;
                ShowData($"🚫 server closed");
            }
            catch { }
        }

        public bool HexMode { get; set; } = false;
        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (Server != null)
            {
                byte[] buff = HexMode ? Tools.Global.Hex2Byte(toSendDataTextBox.Text) :
                    Tools.Global.GetEncoding().GetBytes(toSendDataTextBox.Text);
                Broadcast(buff);
            }
        }

        private bool Broadcast(byte[] buff)
        {
            try
            {
                lock (Clients)
                {
                    foreach (var c in Clients)
                        try
                        {
                            c.Send(buff);
                        }
                        catch { }
                }
                ShowData($"💥 broadcast", buff, true);
                return true;
            }
            catch (Exception ex)
            {
                ShowData($"❗ broadcast error {ex.Message}");
                return false;
            }
        }
    }
}
