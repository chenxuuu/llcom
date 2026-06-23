using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class LuaScriptViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _runScriptPath = "user_script_run/example.lua";

    [ObservableProperty]
    private ObservableCollection<string> _scriptList = new();

    [ObservableProperty]
    private string? _selectedScript;

    [ObservableProperty]
    private string _logOutput = "";

    [ObservableProperty]
    private string _commandInput = "";

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _runStopText = "▶ 运行";

    [ObservableProperty]
    private bool _testHex = true;

    [ObservableProperty]
    private string _testData = "";

    [ObservableProperty]
    private string _testResult = "";

    [ObservableProperty]
    private string _statusText = "就绪";

    public LuaScriptViewModel()
    {
        RefreshScriptList();
        LuaEnv.LuaApis.PrintLuaLog += OnLuaLog;
        LuaEnv.LuaRunEnv.LuaRunError += OnLuaError;
    }
    private void OnLuaLog(object? sender, EventArgs e) => AppendLog(sender?.ToString() ?? "");
    private void OnLuaError(object? sender, EventArgs e)
    {
        IsRunning = false;
        RunStopText = "▶ 运行";
        AppendLog("--- Lua stopped ---");
    }

    private void AppendLog(string msg)
    {
        var maxLen = 50000;
        LogOutput = (LogOutput + msg + "\n")[..Math.Min(LogOutput.Length + msg.Length + 1, maxLen)];
    }

    [RelayCommand]
    private void RefreshScriptList()
    {
        var dir = Path.Combine(PlatformHelper.ProfilePath, "user_script_run");
        if (Directory.Exists(dir))
        {
            var files = Directory.GetFiles(dir, "*.lua")
                .Select(f => "user_script_run/" + Path.GetFileName(f));
            ScriptList = new ObservableCollection<string>(files);
        }
    }

    [RelayCommand]
    private void ToggleRun()
    {
        if (IsRunning)
        {
            LuaEnv.LuaRunEnv.StopLua("");
            IsRunning = false;
            RunStopText = "▶ 运行";
            AppendLog("--- User stopped ---");
        }
        else
        {
            if (string.IsNullOrEmpty(SelectedScript))
            {
                StatusText = "请先选择一个脚本";
                return;
            }
            try
            {
                LuaEnv.LuaRunEnv.New(SelectedScript);
                // Allow triggers to process after script is loaded
                Task.Delay(200).ContinueWith(_ => { });
                IsRunning = true;
                RunStopText = "■ 停止";
                AppendLog($"--- Running: {SelectedScript} ---");
            }
            catch (Exception ex)
            {
                StatusText = $"Error: {ex.Message}";
                AppendLog($"Error loading script: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void RunCommand()
    {
        if (string.IsNullOrWhiteSpace(CommandInput)) return;
        if (!IsRunning)
        {
            AppendLog("Script not running. Start a script first.");
            return;
        }
        LuaEnv.LuaRunEnv.RunCommand(CommandInput);
        AppendLog($">> {CommandInput}");
        CommandInput = "";
    }

    [RelayCommand]
    private void TestHexConvert()
    {
        try
        {
            if (TestHex)
                TestResult = ByteConvert.Byte2Hex(
                    System.Text.Encoding.UTF8.GetBytes(TestData), " ");
            else
                TestResult = ByteConvert.Hex2String(TestData.Replace(" ", ""));
            StatusText = "转换完成";
        }
        catch (Exception ex)
        {
            TestResult = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void ClearLog() => LogOutput = "";
}
