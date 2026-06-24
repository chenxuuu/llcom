using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Avalonia.Helpers;
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
        "110", "300", "600", "1200", "2400", "4800", "9600", "14400",
        "19200", "28800", "38400", "56000", "57600", "115200", "128000",
        "230400", "256000", "460800", "500000", "512000", "600000",
        "750000", "921600", "1000000", "1500000", "2000000", "3000000",
        Helpers.LocaleHelper.Get("OtherRate")
    };
    private int _lastBaudRateIndex = -1;
    [ObservableProperty]
    private string _selectedBaudRate = "115200";

    partial void OnSelectedBaudRateChanged(string value)
    {
        // Skip if index hasn't changed (programmatic set)
        var idx = BaudRates.IndexOf(value);
        if (idx == _lastBaudRateIndex) return;
        _lastBaudRateIndex = idx;

        if (value == LocaleHelper.Get("OtherRate"))
        {
            // Custom baud rate — show input dialog
            var result = PlatformHelper.ShowInputDialog(
                LocaleHelper.Get("ShowBaudRate"),
                "115200",
                LocaleHelper.Get("OtherRate"));
            if (result.Item1 && int.TryParse(result.Item2, out var customBaud) && customBaud > 0)
            {
                BaudRates[BaudRates.Count - 1] = customBaud.ToString();
                SelectedBaudRate = customBaud.ToString();
                _lastBaudRateIndex = BaudRates.Count - 1;
            }
            else
            {
                PlatformHelper.ShowMessage(LocaleHelper.Get("OtherRateFail"));
                BaudRates[BaudRates.Count - 1] = LocaleHelper.Get("OtherRate");
                SelectedBaudRate = "115200";
                _lastBaudRateIndex = BaudRates.IndexOf("115200");
            }
        }
    }
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
    private string _openCloseButtonText = Helpers.LocaleHelper.Get("OpenPortButton");

    // ── Data display ────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<DataLineItem> _receivedLines = new();
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
    private string _statusText = Helpers.LocaleHelper.Get("StatusReady");
    [ObservableProperty]
    private long _sentCount;
    [ObservableProperty]
    private long _receivedCount;
    [ObservableProperty]
    private bool _lockScroll;
    [ObservableProperty]
    private bool _isReady = true;
    [ObservableProperty]
    private bool _showSymbol;

    // ── RTS / DTR ───────────────────────────────────────────────────────
    [ObservableProperty]
    private bool _rtsEnabled;
    [ObservableProperty]
    private bool _dtrEnabled = true;

    partial void OnRtsEnabledChanged(bool value)
    {
        if (IsPortOpen) UartManager.Instance.Rts = value;
    }

    partial void OnDtrEnabledChanged(bool value)
    {
        if (IsPortOpen) UartManager.Instance.Dtr = value;
    }

    // ── Window position persistence ─────────────────────────────────────
    [ObservableProperty]
    private double _windowLeft = double.NaN;
    [ObservableProperty]
    private double _windowTop = double.NaN;
    [ObservableProperty]
    private double _windowWidth = 900;
    [ObservableProperty]
    private double _windowHeight = 500;

    // ── Tabs ────────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _selectedTabIndex;

    // Sub-page ViewModels
    public QuickSendViewModel QuickSendPage { get; } = new();
    public ConvertPageViewModel ConvertPage { get; } = new();
    public EncodingFixViewModel EncodingFixPage { get; } = new();
    public MqttViewModel MqttPage { get; } = new();
    public TcpTestViewModel TcpTestPage { get; } = new();
    public TcpTestViewModel TcpLocalPage { get; } = new();
    public TcpTestViewModel UdpLocalPage { get; } = new();
    public SocketClientViewModel SocketClientPage { get; } = new();
    public PlotViewModel PlotPage { get; } = new();
    public LuaScriptViewModel LuaScriptPage { get; } = new();
    public OnlineScriptsViewModel OnlineScriptsPage { get; } = new();
    public WinUsbViewModel WinUsbPage { get; } = new();
    public SerialMonitorViewModel SerialMonitorPage { get; } = new();
    public AboutViewModel AboutPage { get; } = new();

    [ObservableProperty]
    private string _title = Helpers.LocaleHelper.Get("AppTitle");

    [ObservableProperty]
    private string _platformInfo = $"{PlatformHelper.GetPlatformName()} - .NET 8";

    private string _currentLanguage = "zh-CN";

    public MainWindowViewModel()
    {
        PlatformHelper.ShowMessageCallback = msg =>
        {
            StatusText = msg;
        };
        // LoadLanguageFileCallback is set by App.axaml.cs after window creation
    }

    // ── Commands ────────────────────────────────────────────────────────
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
            try
            {
                UartManager.Instance.Close();
                IsPortOpen = false;
                OpenCloseButtonText = LocaleHelper.Get("OpenPortButton");
                StatusText = LocaleHelper.Get("StatusPortClosed");
            }
            catch (Exception ex) { StatusText = LocaleHelper.Format("StatusCloseFailed", ex.Message); }
        }
        else
        {
            if (string.IsNullOrEmpty(SelectedPort))
            {
                StatusText = LocaleHelper.Get("StatusSelectPort");
                return;
            }
            try
            {
                var uart = UartManager.Instance;
                uart.SetName(SelectedPort);
                if (int.TryParse(SelectedBaudRate, out var baud) && baud > 0)
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
                uart.Rts = RtsEnabled;
                uart.Dtr = DtrEnabled;
                uart.UartDataReceived += OnDataReceived;
                uart.UartDataSent += OnDataSent;
                uart.UartDataRawSent += OnDataRawSent;
                uart.Open();
                IsPortOpen = true;
                OpenCloseButtonText = LocaleHelper.Get("ClosePortButton");
                StatusText = LocaleHelper.Format("StatusConnected", SelectedPort!, SelectedBaudRate);
            }
            catch (Exception ex) { StatusText = LocaleHelper.Format("StatusOpenFailed", ex.Message); }
        }
    }

    [RelayCommand]
    private void SendData()
    {
        if (!IsPortOpen || string.IsNullOrEmpty(DataToSend)) return;
        try
        {
            byte[] rawData = HexSend
                ? ByteConvert.Hex2Byte(DataToSend)
                : GlobalState.Instance.GetEncoding().GetBytes(DataToSend);

            // Run through send script Lua pipeline (sendScript.lua)
            byte[] processedData;
            try
            {
                var state = GlobalState.Instance;
                processedData = LuaEnv.LuaLoader.Run(
                    $"{state.Settings.sendScript}.lua",
                    new System.Collections.ArrayList { "uartData", rawData });
                if (processedData.Length == 0)
                    processedData = rawData; // fallback if script returns empty
            }
            catch
            {
                processedData = rawData;
            }

            // Append CRLF if configured
            if (GlobalState.Instance.Settings.extraEnter)
            {
                var temp = processedData.ToList();
                temp.Add(0x0d);
                temp.Add(0x0a);
                processedData = temp.ToArray();
            }

            UartManager.Instance.SendData(processedData, rawData);
            SentCount += rawData.Length;
            StatusText = LocaleHelper.Format("StatusSentBytes", rawData.Length);
        }
        catch (Exception ex) { StatusText = LocaleHelper.Format("StatusSendFailed", ex.Message); }
    }

    [RelayCommand]
    private void ClearData()
    {
        ReceivedLines.Clear();
        SentCount = 0;
        ReceivedCount = 0;
    }

    [RelayCommand]
    private void SaveLog()
    {
        var fileName = $"llcom_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
        var path = System.IO.Path.Combine(PlatformHelper.ProfilePath, fileName);
        try
        {
            System.IO.Directory.CreateDirectory(PlatformHelper.ProfilePath);
            var text = string.Join(Environment.NewLine,
                ReceivedLines.Select(line => line.DisplayText));
            System.IO.File.WriteAllText(path, text);
            StatusText = LocaleHelper.Format("StatusLogSaved", fileName);
        }
        catch (Exception ex) { StatusText = LocaleHelper.Format("StatusSaveFailed", ex.Message); }
    }

    [RelayCommand]
    private void SwitchLanguage()
    {
        _currentLanguage = _currentLanguage == "zh-CN" ? "en-US" : "zh-CN";
        LocaleHelper.SetLanguage(_currentLanguage);
        PlatformHelper.LoadLanguageFile(_currentLanguage);

        // Refresh all ViewModel text properties
        Title = LocaleHelper.Get("AppTitle");
        OpenCloseButtonText = IsPortOpen
            ? LocaleHelper.Get("ClosePortButton")
            : LocaleHelper.Get("OpenPortButton");
        StatusText = IsPortOpen
            ? LocaleHelper.Format("StatusConnected", SelectedPort ?? "", SelectedBaudRate)
            : LocaleHelper.Get("StatusReady");
    }

    [RelayCommand]
    private void OpenSettings() { /* Open settings window */ }

    [RelayCommand]
    private void OpenScriptFolder()
    {
        PlatformHelper.OpenUrl(PlatformHelper.ProfilePath);
    }

    [RelayCommand]
    private void OpenApiDoc()
    {
        PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/blob/master/LuaApi.md");
    }

    // ── Event handlers ──────────────────────────────────────────────────
    private void OnDataReceived(object? sender, byte[] data)
    {
        var text = HexDisplay
            ? ByteConvert.Byte2Hex(data, " ")
            : ByteConvert.Byte2Readable(data);
        var line = new DataLineItem
        {
            Timestamp = DateTime.Now,
            IsSent = false,
            Data = text,
            IsHex = HexDisplay
        };
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            AppendDataLine(line);
            ReceivedCount += data.Length;
        });
    }

    private void OnDataSent(object? sender, byte[] data)
    {
        if (!ShowSend) return;
        var text = HexDisplay
            ? ByteConvert.Byte2Hex(data, " ")
            : ByteConvert.Byte2Readable(data);
        var line = new DataLineItem
        {
            Timestamp = DateTime.Now,
            IsSent = true,
            Data = text,
            IsHex = HexDisplay
        };
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => AppendDataLine(line));
    }

    private void OnDataRawSent(object? sender, byte[] data)
    {
        // Show raw sent data (before Lua processing) if ShowSendRaw is enabled
        if (!GlobalState.Instance.Settings.showSendRaw) return;
        var text = HexDisplay
            ? ByteConvert.Byte2Hex(data, " ")
            : ByteConvert.Byte2Readable(data);
        var line = new DataLineItem
        {
            Timestamp = DateTime.Now,
            IsSent = true,
            Data = LocaleHelper.Get("RawDataSentTitle") + ": " + text,
            IsHex = HexDisplay
        };
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => AppendDataLine(line));
    }

    private int _maxLines = 2000;
    private void AppendDataLine(DataLineItem line)
    {
        ReceivedLines.Add(line);
        while (ReceivedLines.Count > _maxLines)
            ReceivedLines.RemoveAt(0);
    }

    public void Cleanup()
    {
        var uart = UartManager.Instance;
        uart.UartDataReceived -= OnDataReceived;
        uart.UartDataSent -= OnDataSent;
        uart.UartDataRawSent -= OnDataRawSent;
        uart.Close();
        MqttPage.Cleanup();
        TcpTestPage.Cleanup();
        SocketClientPage.Cleanup();
        PlotPage.Cleanup();
        WinUsbPage.Cleanup();
        SerialMonitorPage.Cleanup();
        LuaEnv.LuaRunEnv.StopLua("");
        LuaEnv.LuaLoader.ClearRun();
    }
}
