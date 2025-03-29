using CoAP.Net;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using LibUsbDotNet.Main;
using llcom.LuaEnv;
using llcom.Tools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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

        private static bool loaded = false;

        private void ShowData(string title, byte[] data = null, bool send = false)
        {
            Tools.Logger.ShowDataRaw(new Tools.DataShowRaw
            {
                title = $"🔌 WinUSB: {title}",
                data = data ?? Array.Empty<byte>(),
                color = send ? Brushes.DarkRed : Brushes.DarkGreen,
            });
        }

        private List<DeviceInfo> GetUsbList()
        {
            var list = new List<DeviceInfo>();
            using (UsbContext context = new UsbContext())
            {
                using var allDevices = context.List();
                foreach (var usbRegistry in allDevices)
                {
                    if (usbRegistry.TryOpen())
                    {
                        try
                        {
                            UsbConfigInfo configInfo = usbRegistry.Configs[0];
                            ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                            
                            for (int iInterface = 0; iInterface < interfaceList.Count; iInterface++)
                            {
                                UsbInterfaceInfo interfaceInfo = interfaceList[iInterface];
                                //判断是否为winusb设备
                                if (interfaceInfo.Class != ClassCode.VendorSpec)
                                    continue;
                                var info = new DeviceInfo()
                                {
                                    Vid = usbRegistry.VendorId,
                                    Pid = usbRegistry.ProductId,
                                    SerialNumber = usbRegistry.Info.SerialNumber,
                                    Interface = iInterface,
                                    Name = String.IsNullOrEmpty(interfaceInfo.Interface) ? "Device" : interfaceInfo.Interface,
                                };
                                
                                //ep
                                ReadOnlyCollection<UsbEndpointInfo> endpointList = interfaceInfo.Endpoints;
                                for (int iEndpoint = 0; iEndpoint < endpointList.Count; iEndpoint++)
                                {
                                    var addr = endpointList[iEndpoint].EndpointAddress;
                                    if ((WriteEndpointID)addr <= WriteEndpointID.Ep15)
                                        info.SendEP.Add(((WriteEndpointID)addr, endpointList[iEndpoint].MaxPacketSize));
                                    else
                                        info.RecvEP.Add(((ReadEndpointID)addr, endpointList[iEndpoint].MaxPacketSize));
                                }
                                if(info.SendEP.Count > 0 || info.RecvEP.Count > 0)
                                    list.Add(info);
                            }
                        }
                        catch { }
                        usbRegistry.Close();
                    }
                }
            }
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

            if(Tools.Global.IsMSIX())
            {
                Tools.MessageBox.Show("微软商店版无法使用该功能\r\n" +
                    "Can't use this in MS Store Version");
                MainGrid.IsEnabled = false;
            }

            //绑定
            MainGrid.DataContext = this;

            //适配一下通用通道
            LuaApis.SendChannelsRegister("winusb", (data,_) =>
            {
                if (!IsConnected)
                    return false;
                toSendBuffer.Add(data);
                return true;
            });

            await RefreshUsbList();
        }

        private async void RefreshUsbButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUsbList();
        }


        private bool needClose = false;
        /// <summary>
        /// 发送buff
        /// </summary>
        private List<byte[]> toSendBuffer { get; set; } = new List<byte[]>();

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (UsbListComboBox.SelectedItem == null || IsConnected)
                return;
            var target = (DeviceInfo)UsbListComboBox.SelectedItem;
            UsbContext context = new UsbContext();
            var allDevices = context.List();
            var matched = false;
            foreach (var device in allDevices)
            {
                if (matched)
                    break;
                //pid vid不匹配
                if (device.ProductId != target.Pid || device.VendorId != target.Vid)
                    continue;
                //序列号不匹配
                if (!device.TryOpen() || device.Info.SerialNumber != target.SerialNumber)
                    continue;
                //匹配上了
                matched = true;
                try
                {
                    device.ResetDevice();
                    if (!device.TryOpen())
                    {
                        matched = false;
                        break;//没打开
                    }
                    UsbConfigInfo configInfo = device.Configs[0];
                    ReadOnlyCollection<UsbInterfaceInfo> interfaceList = configInfo.Interfaces;
                    if (interfaceList.Count < target.Interface - 1)
                        continue;
                    device.SetConfiguration(1);
                    device.ClaimInterface(target.Interface);
                    device.SetAltInterface(0);
                }
                catch(Exception err)
                {
                    matched = false;
                    ShowData($"open failed:\r\n{err}");
                    break;//没打开
                }
                IsConnected = true;
                ShowData($"open success");
                UsbConfigInfo configInfo1 = device.Configs[0];
                ReadOnlyCollection<UsbInterfaceInfo> interfaceList1 = configInfo1.Interfaces;
                //ep
                var rep = (ReadEndpointID)UsbInComboBox.SelectedItem;
                var wep = (WriteEndpointID)UsbOutComboBox.SelectedItem;
                //包大小
                var maxPack = 64;
                foreach (var (ep,size) in target.SendEP)
                {
                    if (wep == ep)
                    {
                        maxPack = size;
                        break;
                    }
                }
                new Thread(() =>
                {
                    needClose = false;
                    var timeout = 50;
                    var temp = new byte[1024];
                    var readLength = 0;
                    var reader = device.OpenEndpointReader(rep, 1024);
                    var writer = device.OpenEndpointWriter(wep);
                    while (true)
                    {
                        try
                        {
                            //读数据
                            var err = reader.Read(temp, timeout, out readLength);
                            switch (err)
                            {
                                case Error.Success:
                                case Error.Timeout:
                                case Error.Busy:
                                case Error.Pipe:
                                    break;
                                default:
                                    //断了，退出吧
                                    try { device.Close(); } catch { }
                                    allDevices.Dispose();
                                    context.Dispose();
                                    IsConnected = false;
                                    ShowData($"disconnect");
                                    return;
                            }
                            if (readLength > 0)//有数据了
                            {
                                var data = temp.Take(readLength).ToArray();
                                ShowData($"recv {readLength} bytes", data);
                                //适配一下通用通道
                                LuaApis.SendChannelsReceived("winusb", data);
                            }
                            lock (toSendBuffer)//发数据
                            {
                                while (toSendBuffer.Count > 0)
                                {
                                    var data = toSendBuffer[0];
                                    toSendBuffer.RemoveAt(0);
                                    try
                                    {
                                        var sent = 0;
                                        while (sent < data.Length)
                                        {
                                            var len = Math.Min(data.Length - sent, maxPack);
                                            var realSent = 0;
                                            var sr = writer.Write(
                                                data,
                                                sent,
                                                len,
                                                1000,
                                                out realSent);
                                            if (sr != Error.Success)
                                                ShowData($"send error: {sr}");
                                            if(realSent > 0)
                                                ShowData($"sent {realSent} bytes", 
                                                    data.Skip(sent).Take(realSent).ToArray(),
                                                    true);
                                            sent += len;
                                        }
                                    }
                                    catch(Exception serr)
                                    {
                                        ShowData($"send error:\r\n{serr}");
                                    }
                                }
                            }
                            if (needClose || Tools.Global.isMainWindowsClosed) //主动关闭
                            {
                                try { device.Close(); } catch { }
                                allDevices.Dispose();
                                context.Dispose();
                                IsConnected = false;
                                ShowData($"disconnect");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            try { device.Close(); } catch { }
                            allDevices.Dispose();
                            context.Dispose();
                            IsConnected = false;
                            ShowData($"disconnect by exception:\r\n{e}");
                            break;
                        }
                    }
                }).Start();
            }
        }

        private void DisonnectButton_Click(object sender, RoutedEventArgs e)
        {
            needClose = true;
        }

        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsConnected)
                return;
            byte[] data = HexMode ? Tools.Global.Hex2Byte(toSendDataTextBox.Text) :
                            Tools.Global.GetEncoding().GetBytes(toSendDataTextBox.Text);
            lock (toSendBuffer)
                toSendBuffer.Add(data);
        }

        private void UsbListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsbListComboBox.SelectedItem == null || IsConnected)
                return;
            var target = (DeviceInfo)UsbListComboBox.SelectedItem;
            //把ep填充上
            UsbInComboBox.Items.Clear();
            UsbOutComboBox.Items.Clear();
            foreach (var (ep,_) in target.RecvEP)
                UsbInComboBox.Items.Add(ep);
            foreach (var (ep, _) in target.SendEP)
                UsbOutComboBox.Items.Add(ep);
            if (UsbInComboBox.Items.Count > 0)
                UsbInComboBox.SelectedIndex = 0;
            if (UsbOutComboBox.Items.Count > 0)
                UsbOutComboBox.SelectedIndex = 0;
        }
    }

    class DeviceInfo
    {
        public string Name { get; set; } = null;
        public ushort Pid { get; set; }
        public ushort Vid { get; set; }
        public string SerialNumber { get; set; }
        public int Interface { get; set; } = -1;
        public List<(WriteEndpointID,int)> SendEP { get; set; } = new List<(WriteEndpointID, int)>();
        public List<(ReadEndpointID,int)> RecvEP { get; set; } = new List<(ReadEndpointID, int)>();
        public int MaxPack { get; set; } = 0;

        public unsafe override string ToString()
        {
            if(Name == null)
                return $"VID: 0x{Vid:X04}, PID: 0x{Pid:X04}, Interface: {Interface}\r\n{SerialNumber}";
            else
                return $"{Name}\r\nVID: 0x{Vid:X04}, PID: 0x{Pid:X04}, Interface: {Interface}\r\n{SerialNumber}";
        }
    }
}
