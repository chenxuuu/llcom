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
    /// </summary>
    public partial class SerialMonitorPage : Page
    {
        public delegate int CallbackDelegate(IntPtr param);
        [DllImport("serial_monitor.dll")]
        static extern bool UnMonitorComm();
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
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Udata
        {
            public byte ComPort;
            public byte CommState;
            public int FileHandle;
            public int DataSize;
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
                connected = MonitorComm(pid, com, myDelegate);
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
