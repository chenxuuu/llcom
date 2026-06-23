using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.LuaEnv;

namespace llcom.Avalonia.ViewModels;

/// <summary>
/// WinUSB device communication page (cross-platform).
/// Uses LibUsbDotNet for USB device enumeration and IO.
/// NOTE: Platform-specific policies may require udev rules on Linux.
/// </summary>
public partial class WinUsbViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _deviceList = new();

    [ObservableProperty]
    private string? _selectedDevice;

    [ObservableProperty]
    private string _inEndpoint = "";

    [ObservableProperty]
    private string _outEndpoint = "";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectText = "连接";

    [ObservableProperty]
    private string _sendData = "";

    [ObservableProperty]
    private bool _hexSend;

    [ObservableProperty]
    private string _receivedData = "";

    [ObservableProperty]
    private string _statusText = "就绪";

    public WinUsbViewModel()
    {
        LuaApis.SendChannelsRegister("winusb", (data, _) =>
        {
            if (!IsConnected) return false;
            AppendReceived($">> Sent: {BitConverter.ToString(data)}\n");
            return true;
        });
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        DeviceList.Clear();
        try
        {
            var allDevices = LibUsbDotNet.UsbDevice.AllDevices;
            foreach (var dev in allDevices)
            {
                var info = dev.ToString() ?? "Unknown USB device";
                DeviceList.Add(info);
            }
            if (DeviceList.Count == 0)
                StatusText = "未发现 USB 设备 (可能需要 sudo 权限)";
            else
                StatusText = $"发现 {DeviceList.Count} 个 USB 设备";
        }
        catch (Exception ex)
        {
            StatusText = $"枚举失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleConnect()
    {
        if (IsConnected)
        {
            IsConnected = false;
            ConnectText = "连接";
            StatusText = "已断开";
        }
        else
        {
            if (string.IsNullOrEmpty(SelectedDevice))
            {
                StatusText = "请选择一个设备";
                return;
            }
            StatusText = "WinUSB IO 功能待平台适配测试";
        }
    }

    [RelayCommand]
    private void SendDataCommand()
    {
        if (!IsConnected || string.IsNullOrEmpty(SendData)) return;
        try
        {
            byte[] data = HexSend
                ? Tools.ByteConvert.Hex2Byte(SendData)
                : System.Text.Encoding.UTF8.GetBytes(SendData);
            AppendReceived($">> Sent ({data.Length} bytes)\n");
        }
        catch (Exception ex) { StatusText = $"失败: {ex.Message}"; }
    }

    private int _maxLen = 20000;
    private void AppendReceived(string text)
    {
        ReceivedData = (ReceivedData + text)[..Math.Min(ReceivedData.Length + text.Length, _maxLen)];
    }

    public void Cleanup() { IsConnected = false; }
}
