using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    // ── Serial port properties ──────────────────────────────────────────

    [ObservableProperty]
    private string[] _portNames = SerialPort.GetPortNames();

    [ObservableProperty]
    private string? _selectedPort;

    [ObservableProperty]
    private ObservableCollection<string> _baudRates = new()
    {
        "300", "1200", "2400", "4800", "9600", "14400", "19200", "28800",
        "38400", "56000", "57600", "115200", "128000", "230400", "256000",
        "460800", "921600", "1000000", "2000000", "3000000"
    };

    [ObservableProperty]
    private string _selectedBaudRate = "115200";

    [ObservableProperty]
    private ObservableCollection<string> _dataBitsList = new() { "5", "6", "7", "8" };

    [ObservableProperty]
    private string _selectedDataBits = "8";

    [ObservableProperty]
    private ObservableCollection<string> _stopBitsList = new() { "1", "1.5", "2" };

    [ObservableProperty]
    private string _selectedStopBits = "1";

    [ObservableProperty]
    private ObservableCollection<string> _parityList = new() { "None", "Odd", "Even", "Mark", "Space" };

    [ObservableProperty]
    private string _selectedParity = "None";

    [ObservableProperty]
    private bool _isPortOpen;

    [ObservableProperty]
    private string _openCloseButtonText = "打开串口";

    // ── Data display ────────────────────────────────────────────────────

    [ObservableProperty]
    private string _receivedData = "";

    [ObservableProperty]
    private string _dataToSend = "";

    [ObservableProperty]
    private bool _hexSend;

    [ObservableProperty]
    private bool _hexDisplay;

    [ObservableProperty]
    private bool _showSend = true;

    [ObservableProperty]
    private bool _autoReconnect = true;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private long _sentCount;

    [ObservableProperty]
    private long _receivedCount;

    // ── Commands ─────────────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshPorts()
    {
        PortNames = SerialPort.GetPortNames();
    }

    [RelayCommand]
    private void TogglePort()
    {
        if (IsPortOpen)
        {
            // Close port
            try
            {
                UartManager.Instance.Close();
                IsPortOpen = false;
                OpenCloseButtonText = "打开串口";
                StatusText = "串口已关闭";
            }
            catch (Exception ex)
            {
                StatusText = $"关闭失败: {ex.Message}";
            }
        }
        else
        {
            // Open port
            if (string.IsNullOrEmpty(SelectedPort))
            {
                StatusText = "请选择串口";
                return;
            }

            try
            {
                var uart = UartManager.Instance;
                uart.SetName(SelectedPort);

                if (int.TryParse(SelectedBaudRate, out var baud))
                    uart.Serial.BaudRate = baud;
                uart.Serial.DataBits = int.Parse(SelectedDataBits);
                uart.Serial.StopBits = SelectedStopBits switch
                {
                    "1" => StopBits.One, "1.5" => StopBits.OnePointFive, "2" => StopBits.Two, _ => StopBits.One
                };
                uart.Serial.Parity = SelectedParity switch
                {
                    "Odd" => Parity.Odd, "Even" => Parity.Even, "Mark" => Parity.Mark, "Space" => Parity.Space, _ => Parity.None
                };

                uart.UartDataReceived += OnDataReceived;
                uart.UartDataSent += OnDataSent;

                uart.Open();
                IsPortOpen = true;
                OpenCloseButtonText = "关闭串口";
                StatusText = $"已连接 {SelectedPort} @ {SelectedBaudRate}";
            }
            catch (Exception ex)
            {
                StatusText = $"打开失败: {ex.Message}";
            }
        }
    }

    [RelayCommand]
    private void SendData()
    {
        if (!IsPortOpen || string.IsNullOrEmpty(DataToSend)) return;

        try
        {
            byte[] data;
            if (HexSend)
            {
                data = ByteConvert.Hex2Byte(DataToSend);
            }
            else
            {
                data = System.Text.Encoding.UTF8.GetBytes(DataToSend);
            }

            UartManager.Instance.SendData(data);
            SentCount += data.Length;
            StatusText = $"已发送 {data.Length} 字节";
        }
        catch (Exception ex)
        {
            StatusText = $"发送失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearData()
    {
        ReceivedData = "";
        SentCount = 0;
        ReceivedCount = 0;
    }

    // ── Event handlers ──────────────────────────────────────────────────

    private void OnDataReceived(object? sender, byte[] data)
    {
        var text = HexDisplay
            ? ByteConvert.Byte2Hex(data, " ")
            : ByteConvert.Byte2Readable(data);
        AppendReceivedData($"← {text}\n");
        ReceivedCount += data.Length;
    }

    private void OnDataSent(object? sender, byte[] data)
    {
        if (!ShowSend) return;
        var text = HexDisplay
            ? ByteConvert.Byte2Hex(data, " ")
            : ByteConvert.Byte2Readable(data);
        AppendReceivedData($"→ {text}\n");
    }

    private int _maxLines = 2000;
    private void AppendReceivedData(string text)
    {
        // Limit the displayed data to prevent memory issues
        var current = ReceivedData + text;
        var lines = current.Split('\n');
        if (lines.Length > _maxLines)
        {
            current = string.Join('\n', lines.Skip(lines.Length - _maxLines));
        }
        ReceivedData = current;
    }
}
