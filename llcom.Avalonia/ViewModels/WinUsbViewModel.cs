using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LibUsbDotNet;
using LibUsbDotNet.Main;
using llcom.LuaEnv;
using llcom.Tools;
using llcom.Avalonia.Helpers;

namespace llcom.Avalonia.ViewModels;

/// <summary>
/// WinUSB device communication page (cross-platform).
/// Uses LibUsbDotNet for USB device enumeration and IO.
/// NOTE: Platform-specific policies may require udev rules on Linux.
/// </summary>
public partial class WinUsbViewModel : ViewModelBase
{
    // ── Device info model ───────────────────────────────────────────────

    public class DeviceInfo
    {
        public string Name { get; set; } = "";
        public int Pid { get; set; }
        public int Vid { get; set; }
        public string SerialNumber { get; set; } = "";
        public ReadEndpointID ReadEp { get; set; } = ReadEndpointID.Ep01;
        public WriteEndpointID WriteEp { get; set; } = WriteEndpointID.Ep01;
        public bool IsVendorSpec { get; set; }

        public override string ToString()
        {
            var vendor = IsVendorSpec ? "[WinUSB]" : "[General]";
            return string.IsNullOrEmpty(Name)
                ? $"{vendor} VID:0x{Vid:X04} PID:0x{Pid:X04} {SerialNumber}"
                : $"{vendor} {Name} VID:0x{Vid:X04} PID:0x{Pid:X04}";
        }
    }

    // ── Observable properties ──────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<DeviceInfo> _deviceList = new();

    [ObservableProperty]
    private DeviceInfo? _selectedDevice;

    [ObservableProperty]
    private string _inEpText = "0x81";

    [ObservableProperty]
    private string _outEpText = "0x01";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectText = Helpers.LocaleHelper.Get("WinUsbConnect");

    [ObservableProperty]
    private string _sendData = "";

    [ObservableProperty]
    private bool _hexSend;

    [ObservableProperty]
    private string _receivedData = "";

    [ObservableProperty]
    private string _statusText = Helpers.LocaleHelper.Get("StatusReady");

    [ObservableProperty]
    private bool _isBusy;

    // ── Internal state ──────────────────────────────────────────────────

    private UsbDevice? _device;
    private UsbEndpointReader? _reader;
    private UsbEndpointWriter? _writer;
    private Thread? _readThread;
    private volatile bool _needClose;
    private readonly List<byte[]> _sendBuffer = new();
    private readonly object _sendLock = new();

    public WinUsbViewModel()
    {
        LuaApis.SendChannelsRegister("winusb", (data, _) =>
        {
            if (!IsConnected) return false;
            lock (_sendLock) _sendBuffer.Add(data);
            return true;
        });
    }

    // ── Commands ────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task RefreshDevices()
    {
        IsBusy = true;
        DeviceList.Clear();
        try
        {
            var list = await Task.Run(() => GetUsbList());
            foreach (var dev in list)
                DeviceList.Add(dev);

            StatusText = DeviceList.Count == 0
                ? LocaleHelper.Get("WinUsbNoDevice")
                : LocaleHelper.Format("WinUsbFoundDevices", DeviceList.Count);
        }
        catch (Exception ex)
        {
            StatusText = LocaleHelper.Format("WinUsbEnumFailed", ex.Message);
        }
        finally { IsBusy = false; }
    }

    private static List<DeviceInfo> GetUsbList()
    {
        var list = new List<DeviceInfo>();
        try
        {
            var allDevices = UsbDevice.AllDevices;
            if (allDevices == null) return list;

            foreach (UsbRegistry reg in allDevices)
            {
                try
                {
                    var info = new DeviceInfo
                    {
                        Vid = reg.Vid,
                        Pid = reg.Pid,
                        Name = reg.Name ?? reg.FullName ?? "USB Device",
                        SerialNumber = "",
                    };
                    list.Add(info);
                }
                catch { /* skip problematic devices */ }
            }
        }
        catch { /* enumeration failed */ }
        return list;
    }

    [RelayCommand]
    private void ToggleConnect()
    {
        if (IsConnected)
        {
            Disconnect();
        }
        else
        {
            Connect();
        }
    }

    private void Connect()
    {
        if (SelectedDevice == null)
        {
            StatusText = LocaleHelper.Get("WinUsbSelectDevice");
            return;
        }

        var target = SelectedDevice;
        try
        {
            // Parse endpoint addresses from hex input
            byte readEpAddr;
            byte writeEpAddr;
            try
            {
                readEpAddr = Convert.ToByte(InEpText.Replace("0x", "").Replace("0X", ""), 16);
                writeEpAddr = Convert.ToByte(OutEpText.Replace("0x", "").Replace("0X", ""), 16);
            }
            catch
            {
                StatusText = LocaleHelper.Get("WinUsbInvalidEndpoint");
                return;
            }

            var readEpId = (ReadEndpointID)readEpAddr;
            var writeEpId = (WriteEndpointID)writeEpAddr;

            // Find and open the device
            UsbDeviceFinder finder = new UsbDeviceFinder(target.Vid, target.Pid);
            _device = UsbDevice.OpenUsbDevice(finder);

            if (_device == null)
            {
                StatusText = LocaleHelper.Get("WinUsbDeviceNotFound");
                return;
            }

            // If it's a whole USB device, configure it
            if (_device is IUsbDevice wholeDevice)
            {
                wholeDevice.SetConfiguration(1);
                wholeDevice.ClaimInterface(0);
            }

            _reader = _device.OpenEndpointReader(readEpId, 1024, EndpointType.Bulk);
            _writer = _device.OpenEndpointWriter(writeEpId, EndpointType.Bulk);

            if (_reader == null || _writer == null)
            {
                _device.Close();
                _device = null;
                StatusText = LocaleHelper.Get("WinUsbEndpointFailed");
                return;
            }

            _needClose = false;
            IsConnected = true;
            ConnectText = LocaleHelper.Get("WinUsbDisconnect");
            StatusText = LocaleHelper.Get("WinUsbConnected");

            StartReadThread();
        }
        catch (Exception ex)
        {
            try { _device?.Close(); } catch { }
            _device = null;
            StatusText = LocaleHelper.Format("WinUsbOpenFailed", ex.Message);
        }
    }

    private void StartReadThread()
    {
        _readThread = new Thread(() =>
        {
            int timeout = 50;
            var temp = new byte[1024];
            int readLen;
            while (!_needClose)
            {
                try
                {
                    if (_reader == null) break;
                    var err = _reader.Read(temp, timeout, out readLen);
                    if (err == ErrorCode.None || err == ErrorCode.IoTimedOut)
                    {
                        if (readLen > 0)
                        {
                            var data = temp.Take(readLen).ToArray();
                            AppendReceived($"<< Recv {readLen}B: {BitConverter.ToString(data)}\n");
                            LuaApis.SendChannelsReceived("winusb", data);
                        }
                    }
                    else if (err != ErrorCode.None && err != ErrorCode.IoTimedOut)
                    {
                        DisconnectInternal();
                        return;
                    }
                    // Send buffered data
                    lock (_sendLock)
                    {
                        while (_sendBuffer.Count > 0)
                        {
                            var sdata = _sendBuffer[0];
                            _sendBuffer.RemoveAt(0);
                            try
                            {
                                if (_writer == null) break;
                                var sr = _writer.Write(sdata, 1000, out int realSent);
                                if (sr != ErrorCode.None)
                                    AppendReceived($"Send err: {sr}\n");
                                if (realSent > 0)
                                    AppendReceived($">> Sent {realSent}B\n");
                            }
                            catch (Exception sex) { AppendReceived($"Send err: {sex.Message}\n"); }
                        }
                    }
                }
                catch (Exception e)
                {
                    AppendReceived($"IO err: {e.Message}\n");
                    DisconnectInternal();
                    break;
                }
            }
        }) { IsBackground = true, Name = "WinUSB-IO" };
        _readThread.Start();
    }

    private void Disconnect()
    {
        _needClose = true;
        DisconnectInternal();
    }

    private void DisconnectInternal()
    {
        _needClose = true;
        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        try { _device?.Close(); } catch { }
        _reader = null;
        _writer = null;
        _device = null;
        IsConnected = false;
        ConnectText = LocaleHelper.Get("WinUsbConnect");
        StatusText = LocaleHelper.Get("WinUsbDisconnected");
    }

    [RelayCommand]
    private void SendUsbData()
    {
        if (!IsConnected || string.IsNullOrEmpty(SendData)) return;
        try
        {
            byte[] data = HexSend
                ? ByteConvert.Hex2Byte(SendData)
                : System.Text.Encoding.UTF8.GetBytes(SendData);
            lock (_sendLock) _sendBuffer.Add(data);
        }
        catch (Exception ex)
        {
            StatusText = LocaleHelper.Format("WinUsbSendFailed", ex.Message);
        }
    }

    // ── Data display ────────────────────────────────────────────────────

    private int _maxLen = 20000;
    private void AppendReceived(string text)
    {
        ReceivedData = (ReceivedData + text)[..Math.Min(ReceivedData.Length + text.Length, _maxLen)];
    }

    public void Cleanup()
    {
        Disconnect();
    }
}
