using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
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
    /// SerialMonitorPage.xaml 的交互逻辑
    ///
    /// Serial Monitor功能通过DLL注入和API Hook实现对其他进程串口通信的监听
    /// DLL实现已从闭源Delphi版本迁移到开源Rust版本 (serial_monitor_rust/)
    ///
    /// 新实现的优势：
    /// - 完全支持x86和x64架构
    /// - 更稳定的Hook机制，减少目标程序崩溃
    /// - 开源可维护的代码
    /// - 更好的兼容性
    ///
    /// 详细文档见: serial_monitor_rust/MIGRATION.md
    /// </summary>
    public partial class SerialMonitorPage : Page
    {
        /// <summary>
        /// 回调函数委托，当监听到串口数据时被调用
        /// </summary>
        public delegate int CallbackDelegate(IntPtr param);

        /// <summary>
        /// 停止监听串口通信
        /// 由serial_monitor.dll导出 (Rust实现)
        /// </summary>
        [DllImport("serial_monitor.dll")]
        static extern bool UnMonitorComm();

        /// <summary>
        /// 开始监听指定进程的串口通信
        /// 由serial_monitor.dll导出 (Rust实现)
        /// </summary>
        /// <param name="Pid">目标进程ID</param>
        /// <param name="ComIndex">串口号 (例如: 1 表示 COM1)</param>
        /// <param name="lpCallFunc">回调函数指针</param>
        /// <returns>成功返回true，失败返回false</returns>
        [DllImport("serial_monitor.dll")]
        static extern bool MonitorComm(uint Pid, uint ComIndex, CallbackDelegate lpCallFunc);

        /// <summary>
        /// 事件类型，对应CommState
        /// </summary>
        enum State
        {
            Disconnect = 2,
            Receive,
            Send
        }

        CallbackDelegate myDelegate = new CallbackDelegate((e) =>
        {
            Udata d = Marshal.PtrToStructure<Udata>(e);
            byte[] b = new byte[d.DataSize];
            for (int i = 0; i < d.DataSize; i++)
                b[i] = d.Data[i];
            var c = Brushes.Black;
            string show = "unknow";
            switch(d.CommState)
            {
                case (byte)State.Send:
                    show = "→";
                    c = Brushes.DarkRed;
                    break;
                case (byte)State.Receive:
                    show = "←";
                    c = Brushes.DarkGreen;
                    break;
                case (byte)State.Disconnect:
                    show = "❌";
                    break;
                default:
                    break;
            };
            Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
            {
                title = $"monitor COM{d.ComPort} {show}",
                data = b,
                color = c
            });
            return 1;
        });

        public SerialMonitorPage()
        {
            InitializeComponent();
        }

        bool first = true;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if(first)
            {
                Refresh();
                first = false;
                try
                {
                    UnMonitorComm();
                }
                catch (Exception ex)
                {
                    MonitorButton.IsEnabled = false;
                    Tools.MessageBox.Show($"串口监听插件加载失败:\r\n{ex.Message}\r\n\r\n" +
                        "请确保serial_monitor.dll存在且未被占用。");
                }
            }
        }

        /// <summary>
        /// 串口监听数据结构，与DLL中的定义保持一致
        /// 使用Pack=1确保内存布局与Rust #[repr(C, packed(1))]匹配
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Udata
        {
            /// <summary>串口号</summary>
            public byte ComPort;
            /// <summary>通信状态 (2=断开, 3=接收, 4=发送)</summary>
            public byte CommState;
            /// <summary>文件句柄</summary>
            public int FileHandle;
            /// <summary>数据大小 (最大8192字节)</summary>
            public int DataSize;
            /// <summary>数据缓冲区</summary>
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8192)]
            public byte[] Data;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private bool connected = false;
        private void MonitorButton_Click(object sender, RoutedEventArgs e)
        {
            if(!connected)
            {
                if (PidComboBox.SelectedItem == null ||
                    SerialPortComboBox.SelectedItem == null)
                    return;
                var start = PidComboBox.Text.IndexOf("[");
                var pid = uint.Parse(PidComboBox.Text.Substring(start + 1, PidComboBox.Text.Length - start - 2));
                var com = uint.Parse(SerialPortComboBox.Text.Substring(3));
                try
                {
                    connected = MonitorComm(pid, com, myDelegate);
                }
                catch(Exception ex)
                {
                    MonitorButton.IsEnabled = false;
                    Tools.MessageBox.Show("加载失败："+ex.Message);
                }
            }
            else
            {
                UnMonitorComm();
                connected = false;
            }
            if(connected)
            {
                RefreshButton.IsEnabled = false;
                PidComboBox.IsEnabled = false;
                SerialPortComboBox.IsEnabled = false;
                MonitorButton.Content = TryFindResource("SerialMonitorStop") as string ?? "?!";
            }
            else
            {
                RefreshButton.IsEnabled = true;
                PidComboBox.IsEnabled = true;
                SerialPortComboBox.IsEnabled = true;
                MonitorButton.Content = TryFindResource("SerialMonitorStart") as string ?? "?!";
            }
        }

        private void Refresh()
        {
            string lastP = PidComboBox.Text;
            PidComboBox.Items.Clear();
            var sl = new List<string>();
            foreach (var p in Process.GetProcesses())
            {
                try
                {
                    sl.Add($"{p.ProcessName}[{p.Id}]");
                }
                catch { }
            }
            sl.Sort();
            foreach(var i in sl)
                PidComboBox.Items.Add(i);
            if (PidComboBox.Items.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(lastP) && sl.Contains(lastP))
                    PidComboBox.Text = lastP;
                else
                    PidComboBox.SelectedIndex = 0;
            }

            lastP = SerialPortComboBox.Text;
            SerialPortComboBox.Items.Clear();
            foreach(var p in SerialPort.GetPortNames())
            {
                if(p.IndexOf("COM") == 0)
                    SerialPortComboBox.Items.Add(p);
            }
            if (SerialPortComboBox.Items.Count > 0)
            {
                if (!string.IsNullOrWhiteSpace(lastP) && SerialPortComboBox.Items.Contains(lastP))
                    SerialPortComboBox.Text = lastP;
                else
                    SerialPortComboBox.SelectedIndex = 0;
            }
        }
    }
}
