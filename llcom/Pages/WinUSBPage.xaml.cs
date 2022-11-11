using CoAP.Net;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
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
using WebSocketSharp;

namespace llcom.Pages
{
    /// <summary>
    /// WinUSBPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class WinUSBPage : Page
    {
        public WinUSBPage()
        {
            InitializeComponent();
        }

        public bool IsConnected { get; set; }
        public bool HexMode { get; set; } = false;
        public int InterfaceIndex { get; set; } = 0;

        private static bool loaded = false;

        private void ShowData(string title, byte[] data = null, bool send = false)
        {
            this.Dispatcher.Invoke(new Action(delegate
            {
                Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
                {
                    title = $"🔌 WinUSB: {title}",
                    data = data ?? new byte[0],
                    color = send ? Brushes.DarkRed : Brushes.DarkGreen,
                });
            }));
        }

        private List<DeviceInfo> GetUsbList()
        {
            var list = new List<DeviceInfo>();
            StringBuilder sb = new StringBuilder();
            using (UsbContext context = new UsbContext())
            {
                using var allDevices = context.List();
                foreach (var usbRegistry in allDevices)
                {
                    if (usbRegistry.TryOpen())
                    {
                        try
                        {
                            var info = new DeviceInfo()
                            {
                                Vid = usbRegistry.VendorId,
                                Pid = usbRegistry.ProductId,
                                SerialNumber = usbRegistry.Info.SerialNumber,
                            };
                            sb.AppendLine(info.ToString());//信息加上
                            //只取第0个吧，懒得写了，等大家贡献代码😍
                            UsbConfigInfo configInfo = usbRegistry.Configs[0];

                            ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                            for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
                            {
                                UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
                                if (info.Name.IsNullOrEmpty() && !interfaceInfo.Interface.IsNullOrEmpty())
                                    info.Name = interfaceInfo.Interface;
                                sb.AppendLine($"Interface[{iInterface}]: {interfaceInfo.Interface} =>");
                                ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.Endpoints;
                                for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
                                {
                                    sb.AppendLine($"\tEndpoint[{iEndpoint+1}] =>");
                                    sb.AppendLine($"\t\tMax packet size:{endpointList[iEndpoint].MaxPacketSize}");
                                    sb.AppendLine($"\t\tEndpoint address:{endpointList[iEndpoint].EndpointAddress}");
                                }
                            }
                            list.Add(info);
                        }
                        catch { }
                        usbRegistry.Close();
                        sb.AppendLine();
                    }
                }
            }
            ShowData("device list", Tools.Global.GetEncoding().GetBytes(sb.ToString()));
            return list;
        }

        private async Task RefreshUsbList()
        {
            List<DeviceInfo> r = null;
            await Task.Run(() =>
            {
                try
                {
                    r = GetUsbList();
                }
                catch { }
            });
            UsbListComboBox.Items.Clear();
            if(r != null)
                foreach (var i in r)
                    UsbListComboBox.Items.Add(i);
            UsbListComboBox.SelectedIndex = 0;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            //绑定
            MainGrid.DataContext = this;

            //combox填满
            for(int i=1;i<16;i++)
            {
                UsbInComboBox.Items.Add($"EP{i}");
                UsbOutComboBox.Items.Add($"EP{i}");
            }
            UsbInComboBox.SelectedIndex = 0;
            UsbOutComboBox.SelectedIndex = 1;
            await RefreshUsbList();
        }

        private void SendReceive(DeviceInfo device, WriteEndpointID epw, ReadEndpointID epr, byte[] send = null)
        {
            using (UsbContext context = new UsbContext())
            {
                using var allDevices = context.List();
                foreach (var usbRegistry in allDevices)
                {
                    if (usbRegistry.TryOpen())
                    {
                        if (usbRegistry.VendorId == device.Vid &&
                            usbRegistry.ProductId == device.Pid &&
                            usbRegistry.Info.SerialNumber == device.SerialNumber)
                        {
                            try
                            {
                                //Get the first config number of the interface
                                usbRegistry.ClaimInterface(usbRegistry.Configs[0].Interfaces[InterfaceIndex].Number);
                                //Open up the endpoints
                                var w = usbRegistry.OpenEndpointWriter(epw);
                                var r = usbRegistry.OpenEndpointReader(epr);
                                if(w!=null)
                                {
                                    w.Write(send, 5000, out var sentLen);
                                    if(sentLen > 0)
                                    {
                                        ShowData($"sent {sentLen} bytes", send.Take(sentLen).ToArray(), true);
                                    }
                                }
                                var buff = new byte[1024];
                                r.Read(buff, 5000, out var recvLen);
                                if (recvLen > 0)
                                {
                                    ShowData($"recv {recvLen} bytes", buff.Take(recvLen).ToArray());
                                }
                            }
                            catch { }
                            usbRegistry.Close();
                            break;
                        }
                        else
                        {
                            usbRegistry.Close();
                        }
                    }
                }
            }
        }

        private async void RefreshUsbButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUsbList();
        }


        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsbListComboBox.SelectedItem == null)
                return;
            var device = (DeviceInfo)UsbListComboBox.SelectedItem;
            var epw = (WriteEndpointID)((int)WriteEndpointID.Ep01 + UsbOutComboBox.SelectedIndex);
            var epr = (ReadEndpointID)((int)ReadEndpointID.Ep01 + UsbInComboBox.SelectedIndex);
            await Task.Run(() =>
            {
                try
                {
                    SendReceive(device, epw, epr);
                }
                catch { }
            });
        }

        private async void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsbListComboBox.SelectedItem == null)
                return;
            var device = (DeviceInfo)UsbListComboBox.SelectedItem;
            var epw = (WriteEndpointID)((int)WriteEndpointID.Ep01 + UsbOutComboBox.SelectedIndex);
            var epr = (ReadEndpointID)((int)ReadEndpointID.Ep01 + UsbInComboBox.SelectedIndex);
            byte[] buff = HexMode ? Tools.Global.Hex2Byte(toSendDataTextBox.Text) :
                    Tools.Global.GetEncoding().GetBytes(toSendDataTextBox.Text);
            await Task.Run(() =>
            {
                try
                {
                    SendReceive(device, epw, epr, buff);
                }
                catch { }
            });
        }
    }

    class DeviceInfo
    {
        public string Name { get; set; } = null;
        public ushort Pid { get; set; }
        public ushort Vid { get; set; }
        public string SerialNumber { get; set; }

        public unsafe override string ToString()
        {
            if(Name == null)
                return $"VID: 0x{Vid:X04}, PID: 0x{Pid:X04}, {SerialNumber}";
            else
                return $"{Name} (VID: 0x{Vid:X04}, PID: 0x{Pid:X04}, {SerialNumber})";
        }
    }
}
