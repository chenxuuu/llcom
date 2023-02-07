using llcom.LuaEnv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace llcom.Pages
{
    /// <summary>
    /// SocketClientPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class SocketClientPage : Page
    {
        public SocketClientPage()
        {
            InitializeComponent();
        }
        private bool initial = false;

        //收到消息的事件
        public event EventHandler<byte[]> DataRecived;
        public bool IsConnected { get; set; } = false;
        //是否可更改服务器信息
        public bool Changeable { get; set; } = true;
        public bool HexMode { get; set; } = false;

        //暂存一个对象
        Socket socketNow = null;

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (initial)
                return;
            initial = true;

            this.DataContext = this;

            ServerTextBox.DataContext = Tools.Global.setting;
            PortTextBox.DataContext = Tools.Global.setting;
            ProtocolTypeComboBox.DataContext = Tools.Global.setting;

            //收到消息显示
            DataRecived += (_, buff) =>
            {
                ShowData($" → receive", buff);
            };

            //适配一下通用通道
            LuaApis.SendChannelsRegister("socket-client", (data, _) =>
            {
                if (socketNow != null && data != null)
                {
                    return Send(data);
                }
                else
                    return false;
            });
            //通用通道收到消息
            DataRecived += (_, data) =>
            {
                LuaApis.SendChannelsReceived("socket-client", data);
            };
        }

        private void ShowData(string title, byte[] data = null, bool send = false)
        {
            Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
            {
                title = $"🔗 socket client: {title}",
                data = data ?? new byte[0],
                color = send ? Brushes.DarkRed : Brushes.DarkGreen,
            });
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Changeable)
                return;
            IPEndPoint ipe = null;
            Socket s = null;
            try
            {
                Changeable = false;
                IPAddress ip = null;
                try
                {
                    ip = IPAddress.Parse(ServerTextBox.Text);
                }
                catch
                {
                    var hostEntry = Dns.GetHostEntry(ServerTextBox.Text);
                    ip = hostEntry.AddressList[0];
                }
                ipe = new IPEndPoint(ip, int.Parse(PortTextBox.Text));
                s = new Socket(ipe.AddressFamily,
                    ProtocolTypeComboBox.SelectedIndex == 0 ? SocketType.Stream : SocketType.Dgram, 
                    ProtocolTypeComboBox.SelectedIndex == 0 ? ProtocolType.Tcp : ProtocolType.Udp);
            }
            catch(Exception ex)
            {
                ShowData($"❗ Server information error {ex.Message}");
                Changeable = true;
                return;
            }
            ShowData("📢 Connecting......");
            try
            {
                s.BeginConnect(ipe, new AsyncCallback((r) =>
                {
                    var s = (Socket)r.AsyncState;
                    if (s.Connected)
                    {
                        socketNow = s;
                        IsConnected = true;
                        ShowData("✔ Server connected");
                    }
                    else
                    {
                        Changeable = true;
                        ShowData("❗ Server connect failed");
                        return;
                    }

                    StateObject so = new StateObject();
                    so.workSocket = s;
                    s.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, 0, new AsyncCallback(Read_Callback), so);
                }), s);
            }
            catch (Exception ex)
            {
                ShowData($"❗ Server connect error {ex.Message}");
                Changeable = true;
                return;
            }
        }

        public void Read_Callback(IAsyncResult ar)
        {
            StateObject so = (StateObject)ar.AsyncState;
            Socket s = so.workSocket;
            try
            {

                int read = s.EndReceive(ar);

                if (read > 0)
                {
                    var buff = new byte[read];
                    for (int i = 0; i < buff.Length; i++)
                        buff[i] = so.buffer[i];
                    DataRecived?.Invoke(null, buff);
                    s.BeginReceive(so.buffer, 0, StateObject.BUFFER_SIZE, 0,
                                             new AsyncCallback(Read_Callback), so);
                }
                else//断了？
                {
                    try
                    {
                        s.Close();
                        s.Dispose();
                    }
                    catch { }
                    socketNow = null;
                    IsConnected = false;
                    Changeable = true;
                    ShowData("❌ Server disconnected");
                }
            }
            catch { }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if(socketNow != null)
            {
                try
                {
                    socketNow.Close();
                    socketNow.Dispose();
                }
                catch { }
                socketNow = null;
                IsConnected = false;
                Changeable = true;
                ShowData("❌ Server disconnected");
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            if (socketNow != null)
            {
                byte[] buff = HexMode ? Tools.Global.Hex2Byte(ToSendTextBox.Text) :
                    Tools.Global.GetEncoding().GetBytes(ToSendTextBox.Text);
                Send(buff);
            }
        }

        private bool Send(byte[] buff)
        {
            try
            {
                socketNow.Send(buff);
                ShowData($" ← send", buff, true);
                return true;
            }
            catch(Exception ex)
            {
                ShowData($"❗ Send data error {ex.Message}");
                return false;
            }
        }

        public class StateObject
        {
            public Socket workSocket = null;
            public const int BUFFER_SIZE = 2048;
            public byte[] buffer = new byte[BUFFER_SIZE];
        }
    }
}
