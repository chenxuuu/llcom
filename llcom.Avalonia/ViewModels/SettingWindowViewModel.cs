using System;
using System.Collections.ObjectModel;
using System.IO;
using AvaloniaEdit.Document;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;
using Newtonsoft.Json;

namespace llcom.Avalonia.ViewModels;

public class DisplayFormatItem
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

public partial class SettingWindowViewModel : ViewModelBase
{
    // ── Basic serial settings ────────────────────────────────────────
    [ObservableProperty] private bool _autoReconnect = true;
    [ObservableProperty] private int _displayFormat; // 0=both, 1=text, 2=hex
    [ObservableProperty] private bool _showSendData = true;
    [ObservableProperty] private bool _showSendRaw;
    [ObservableProperty] private bool _keepTop;
    [ObservableProperty] private bool _terminalMode;
    [ObservableProperty] private int _packetTimeout = 10; // ms
    [ObservableProperty] private bool _timeoutFromBlank = true;
    [ObservableProperty] private int _maxPacketSize = 1024;
    [ObservableProperty] private int _maxShowLen = 4096;
    [ObservableProperty] private int _autoClearPacks = 500;
    [ObservableProperty] private bool _lagAutoClear = true;

    [ObservableProperty] private string _selectedDataBits = "8";
    [ObservableProperty] private string _selectedStopBits = "1";
    [ObservableProperty] private string _selectedParity = "None";
    [ObservableProperty] private string _selectedEncoding = "UTF8";

    public ObservableCollection<string> DataBitsList { get; } = new() { "5", "6", "7", "8" };
    public ObservableCollection<string> StopBitsList { get; } = new() { "1", "1.5", "2" };
    public ObservableCollection<string> ParityList { get; } = new() { "None", "Odd", "Even", "Mark", "Space" };
    public ObservableCollection<string> EncodingList { get; } = new()
    {
        "UTF8", "ASCII", "GB2312", "BIG5", "Shift_JIS", "EUC-KR", "ISO-8859-1", "Windows-1252"
    };

    // ── Script editors ──────────────────────────────────────────────
    [ObservableProperty] private TextDocument? _sendScriptDocument = new();
    [ObservableProperty] private TextDocument? _recvScriptDocument = new();

    // Script test values
    [ObservableProperty] private string _sendTestInput = "";
    [ObservableProperty] private string _sendTestResult = "";
    [ObservableProperty] private string _recvTestInput = "";
    [ObservableProperty] private string _recvTestResult = "";
    [ObservableProperty] private bool _sendTestHex;
    [ObservableProperty] private bool _recvTestHex;
    [ObservableProperty] private string _sendTestPara = "";
    [ObservableProperty] private string _recvTestPara = "0";

    public SettingWindowViewModel()
    {
        LoadSettings();
    }

    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var state = GlobalState.Instance;
            var s = state.Settings;
            var baseDir = PlatformHelper.ProfilePath;

            // Mirror to GlobalState.Settings for immediate use
            s.autoReconnect = AutoReconnect;
            s.showHexFormat = DisplayFormat;
            s.showSend = ShowSendData;
            s.showSendRaw = ShowSendRaw;
            s.topmost = KeepTop;
            s.terminal = TerminalMode;

            // Persist settings.json
            var json = JsonConvert.SerializeObject(s, Formatting.Indented);
            File.WriteAllText(Path.Combine(baseDir, "settings.json"), json);

            // Save scripts
            if (SendScriptDocument != null)
                File.WriteAllText(Path.Combine(baseDir, "send_script.lua"), SendScriptDocument.Text);
            if (RecvScriptDocument != null)
                File.WriteAllText(Path.Combine(baseDir, "recv_script.lua"), RecvScriptDocument.Text);

            PlatformHelper.ShowMessage("设置已保存");
        }
        catch (Exception ex) { PlatformHelper.ShowMessage($"保存设置失败: {ex.Message}"); }
    }

    private void LoadSettings()
    {
        try
        {
            var state = GlobalState.Instance;
            var s = state.Settings;
            var baseDir = PlatformHelper.ProfilePath;

            // Load from GlobalState.Settings
            AutoReconnect = s.autoReconnect;
            DisplayFormat = s.showHexFormat;
            ShowSendData = s.showSend;
            ShowSendRaw = s.showSendRaw;
            KeepTop = s.topmost;
            TerminalMode = s.terminal;

            // Load scripts
            var sendPath = Path.Combine(baseDir, "send_script.lua");
            if (File.Exists(sendPath))
                SendScriptDocument = new TextDocument(File.ReadAllText(sendPath));
            var recvPath = Path.Combine(baseDir, "recv_script.lua");
            if (File.Exists(recvPath))
                RecvScriptDocument = new TextDocument(File.ReadAllText(recvPath));
        }
        catch (Exception) { /* ignore */ }
    }

    [RelayCommand]
    private void TestSendScript()
    {
        try
        {
            var raw = SendTestInput ?? "";
            byte[] data = SendTestHex
                ? ByteConvert.Hex2Byte(raw)
                : System.Text.Encoding.UTF8.GetBytes(raw);
            // pass to Lua engine (placeholder: return hex representation)
            SendTestResult = BitConverter.ToString(data).Replace("-", " ");
        }
        catch (Exception ex)
        {
            SendTestResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void TestRecvScript()
    {
        try
        {
            var raw = RecvTestInput ?? "";
            byte[] data = RecvTestHex
                ? ByteConvert.Hex2Byte(raw)
                : System.Text.Encoding.UTF8.GetBytes(raw);
            RecvTestResult = BitConverter.ToString(data).Replace("-", " ");
        }
        catch (Exception ex)
        {
            RecvTestResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenLogFolder()
    {
        var dir = Path.Combine(PlatformHelper.ProfilePath, "log");
        Directory.CreateDirectory(dir);
        PlatformHelper.OpenUrl(dir);
    }
}
