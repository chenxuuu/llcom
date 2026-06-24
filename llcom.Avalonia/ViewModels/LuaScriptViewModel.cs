using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
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
    private string _runStopText = "▶";

    [ObservableProperty]
    private bool _isEditorVisible = true;

    [ObservableProperty]
    private bool _isLogVisible;

    [ObservableProperty]
    private bool _isEditorEnabled = true;

    [ObservableProperty]
    private bool _isNewScriptPanelVisible;

    [ObservableProperty]
    private string _newScriptName = "new script";

    [ObservableProperty]
    private bool _isEditorModified;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _pauseButtonText = "⏸";

    [ObservableProperty]
    private bool _isLogPaused;

    [ObservableProperty]
    private TextDocument? _document = new();

    // ── Test hex convert ─────────────────────────────────────────────
    [ObservableProperty]
    private bool _testHex = true;
    [ObservableProperty]
    private string _testData = "";
    [ObservableProperty]
    private string _testResult = "";

    // Auto-save tracking
    private string _lastLuaFile = "";
    private DateTime _lastLuaFileTime = DateTime.MinValue;
    private DateTime _lastLuaChangeTime = DateTime.MinValue;

    // Log buffering
    private readonly object _logLock = new();
    private readonly List<string> _logBuffer = new();
    private readonly EventWaitHandle _logSignal = new(false, EventResetMode.AutoReset);
    private int _logCount;
    private bool _isLogTaskRunning;

    public LuaScriptViewModel()
    {
        RefreshScriptList();
        LuaEnv.LuaApis.PrintLuaLog += OnLuaLog;
        LuaEnv.LuaRunEnv.LuaRunError += OnLuaError;
        StartLogTask();
    }

    partial void OnSelectedScriptChanged(string? value)
    {
        if (value != null)
        {
            // Auto-save previous file
            if (!string.IsNullOrEmpty(_lastLuaFile) && _lastLuaChangeTime > _lastLuaFileTime)
                SaveLuaFile(_lastLuaFile);

            LoadScriptContent(value);
        }
    }

    /// <summary>Auto-save when editor loses focus or window deactivates.</summary>
    public void OnEditorLostFocus()
    {
        if (!string.IsNullOrEmpty(_lastLuaFile) && _lastLuaChangeTime > _lastLuaFileTime)
            SaveLuaFile(_lastLuaFile);
    }

    /// <summary>Check for external file changes when window is activated.</summary>
    public void OnWindowActivated()
    {
        if (string.IsNullOrEmpty(_lastLuaFile)) return;
        var fullPath = Path.Combine(PlatformHelper.ProfilePath, "user_script_run", _lastLuaFile + ".lua");
        try
        {
            if (File.Exists(fullPath))
            {
                var fileTime = File.GetLastWriteTime(fullPath);
                if (fileTime > _lastLuaFileTime)
                {
                    Document = new TextDocument(File.ReadAllText(fullPath));
                    _lastLuaFileTime = fileTime;
                    _lastLuaChangeTime = fileTime;
                    StatusText = $"检测到外部更改，已重新加载: {_lastLuaFile}";
                }
            }
        }
        catch { }
    }

    private void LoadScriptContent(string path)
    {
        try
        {
            // Validate path to prevent directory traversal
            var fullPath = GetSafeScriptPath(path, "user_script_run");
            if (fullPath == null)
            {
                StatusText = $"无效的脚本路径: {path}";
                return;
            }
            if (File.Exists(fullPath))
            {
                Document = new TextDocument(File.ReadAllText(fullPath));
                StatusText = $"已加载: {path}";
                IsEditorModified = false;
                _lastLuaFile = Path.GetFileNameWithoutExtension(fullPath);
                _lastLuaFileTime = File.GetLastWriteTime(fullPath);
                _lastLuaChangeTime = _lastLuaFileTime;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"加载脚本失败: {ex.Message}";
        }
    }

    /// <summary>Ensure the script path doesn't traverse outside the scripts directory.</summary>
    private static string? GetSafeScriptPath(string path, string subDir)
    {
        var baseDir = Path.GetFullPath(Path.Combine(PlatformHelper.ProfilePath, subDir));
        var fullPath = Path.GetFullPath(Path.Combine(PlatformHelper.ProfilePath, path));
        return fullPath.StartsWith(baseDir) ? fullPath : null;
    }

    private void SaveLuaFile(string fileName)
    {
        if (Document == null) return;
        try
        {
            var fullPath = Path.Combine(PlatformHelper.ProfilePath, "user_script_run", fileName + ".lua");
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            if (!fullPath.StartsWith(Path.GetFullPath(Path.Combine(PlatformHelper.ProfilePath, "user_script_run"))))
                return;
            File.WriteAllText(fullPath, Document.Text);
            _lastLuaFileTime = File.GetLastWriteTime(fullPath);
            IsEditorModified = false;
        }
        catch { }
    }

    /// <summary>Mark document as modified for auto-save tracking.</summary>
    public void MarkDocumentChanged()
    {
        _lastLuaChangeTime = DateTime.Now;
        IsEditorModified = true;
    }

    private void OnLuaLog(object? sender, EventArgs e)
    {
        if (sender is string msg && msg != null)
        {
            lock (_logLock)
            {
                if (_logBuffer.Count > 500)
                {
                    _logBuffer.Clear();
                    _logBuffer.Add("too many logs!");
                    Thread.Sleep(200); // throttle
                }
                else
                    _logBuffer.Add(msg);
            }
            _logSignal.Set();
        }
    }

    private void OnLuaError(object? sender, EventArgs e)
    {
        IsRunning = false;
        RunStopText = "▶";
        AppendLog("--- Lua stopped ---");
    }

    private int _maxLogLen = 50000;
    private void AppendLog(string msg)
    {
        var newText = LogOutput + msg + "\n";
        if (newText.Length > _maxLogLen)
            newText = newText[^(Math.Min(_maxLogLen, newText.Length))..];
        LogOutput = newText;
    }

    private void StartLogTask()
    {
        if (_isLogTaskRunning) return;
        _isLogTaskRunning = true;
        new Thread(() =>
        {
            while (!GlobalState.Instance.IsMainWindowClosed)
            {
                _logSignal.WaitOne(200);
                if (GlobalState.Instance.IsMainWindowClosed) return;
                if (IsLogPaused) continue;

                string[] logs;
                lock (_logLock)
                {
                    logs = _logBuffer.ToArray();
                    _logBuffer.Clear();
                }
                if (logs.Length == 0) continue;

                _logCount += logs.Length;
                foreach (var log in logs)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        if (_logCount >= 1000)
                        {
                            LogOutput = "Lua log too long, auto cleared.\nMore logs see lua log file.\n";
                            _logCount = 0;
                        }
                        AppendLog(log);
                    });
                }
                Thread.Sleep(10); // throttle
            }
        }) { IsBackground = true }.Start();
    }

    // ── Commands ────────────────────────────────────────────────────

    [RelayCommand]
    private void RefreshScriptList()
    {
        var dir = Path.Combine(PlatformHelper.ProfilePath, "user_script_run");
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var files = Directory.GetFiles(dir, "*.lua")
            .Select(f => "user_script_run/" + Path.GetFileName(f));
        ScriptList = new ObservableCollection<string>(files);
    }

    [RelayCommand]
    private async Task ToggleRun()
    {
        if (IsRunning)
        {
            LuaEnv.LuaRunEnv.StopLua("");
            IsRunning = false;
            RunStopText = "▶";
            AppendLog("--- User stopped ---");
        }
        else
        {
            // Save current content to temp file before running
            if (Document != null && IsEditorModified && SelectedScript != null)
            {
                SaveCurrentScript();
            }

            if (string.IsNullOrEmpty(SelectedScript))
            {
                StatusText = "请先选择一个脚本";
                return;
            }

            IsLogVisible = true;
            IsEditorVisible = false;

            try
            {
                LuaEnv.LuaRunEnv.New(SelectedScript);
                await Task.Delay(200);
                IsRunning = true;
                RunStopText = "■";
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
    private void StopLua()
    {
        if (IsRunning)
        {
            LuaEnv.LuaRunEnv.StopLua("");
            IsRunning = false;
            RunStopText = "▶";
            AppendLog("--- Stopped ---");
        }
        IsLogVisible = false;
        IsEditorVisible = true;
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
    private void SaveScript()
    {
        SaveCurrentScript();
    }

    private void SaveCurrentScript()
    {
        if (Document == null || SelectedScript == null) return;
        try
        {
            var fullPath = Path.Combine(PlatformHelper.ProfilePath, SelectedScript);
            File.WriteAllText(fullPath, Document.Text);
            IsEditorModified = false;
            StatusText = "脚本已保存";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void NewScript()
    {
        IsNewScriptPanelVisible = true;
        NewScriptName = "new script";
    }

    [RelayCommand]
    private void ConfirmNewScript()
    {
        if (string.IsNullOrWhiteSpace(NewScriptName))
        {
            StatusText = "请输入文件名";
            return;
        }

        try
        {
            var fileName = NewScriptName.EndsWith(".lua") ? NewScriptName : NewScriptName + ".lua";
            var fullPath = Path.Combine(PlatformHelper.ProfilePath, "user_script_run", fileName);

            if (File.Exists(fullPath))
            {
                StatusText = "该文件已存在";
                return;
            }

            File.WriteAllText(fullPath, "-- New Lua Script\n");
            IsNewScriptPanelVisible = false;
            RefreshScriptList();

            var relativePath = "user_script_run/" + fileName;
            SelectedScript = relativePath;
            StatusText = $"已创建: {fileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"创建失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelNewScript()
    {
        IsNewScriptPanelVisible = false;
    }

    [RelayCommand]
    private void OpenScriptFolder()
    {
        var dir = Path.Combine(PlatformHelper.ProfilePath, "user_script_run");
        Directory.CreateDirectory(dir);
        PlatformHelper.OpenUrl(dir);
    }

    [RelayCommand]
    private void OpenApiDoc()
    {
        PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/blob/master/LuaApi.md");
    }

    [RelayCommand]
    private void ScriptShare()
    {
        PlatformHelper.OpenUrl("https://github.com/chenxuuu/llcom/discussions");
    }

    [RelayCommand]
    private void TogglePauseLog()
    {
        IsLogPaused = !IsLogPaused;
        PauseButtonText = IsLogPaused ? "▶" : "⏸";
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
