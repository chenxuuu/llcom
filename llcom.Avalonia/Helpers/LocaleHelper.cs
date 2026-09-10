using System.Collections.Generic;

namespace llcom.Avalonia.Helpers;

/// <summary>
/// Provides localized strings for C# code (complements XAML DynamicResource bindings).
/// After SwitchLanguage() is called, all ViewModel properties must be refreshed.
/// </summary>
public static class LocaleHelper
{
    public static string CurrentLanguage { get; private set; } = "zh-CN";

    private static readonly Dictionary<string, string> ZhCn = new()
    {
        ["OpenPortButton"] = "打开串口",
        ["ClosePortButton"] = "关闭串口",
        ["StatusReady"] = "就绪",
        ["StatusPortClosed"] = "串口已关闭",
        ["StatusCloseFailed"] = "关闭失败: {0}",
        ["StatusSelectPort"] = "请选择串口",
        ["StatusConnected"] = "已连接 {0} @ {1}",
        ["StatusOpenFailed"] = "打开失败: {0}",
        ["StatusSentBytes"] = "已发送 {0} 字节",
        ["StatusSendFailed"] = "发送失败: {0}",
        ["StatusLogSaved"] = "日志已保存: {0}",
        ["StatusSaveFailed"] = "保存失败: {0}",
        ["StatusLangSwitched"] = "语言已切换为简体中文",
        ["AppTitle"] = "LLCOM - 能跑Lua脚本的串口调试工具",
        // WinUSB
        ["WinUsbConnect"] = "连接",
        ["WinUsbDisconnect"] = "断开",
        ["WinUsbConnected"] = "已连接",
        ["WinUsbDisconnected"] = "已断开",
        ["WinUsbNoDevice"] = "未发现 USB 设备 (可能需要 sudo 权限)",
        ["WinUsbFoundDevices"] = "发现 {0} 个 USB 设备",
        ["WinUsbEnumFailed"] = "枚举失败: {0}",
        ["WinUsbSelectDevice"] = "请选择一个设备",
        ["WinUsbSelectEndpoint"] = "请选择 IN/OUT 端点",
        ["WinUsbOpenFailed"] = "打开失败: {0}",
        ["WinUsbDeviceNotFound"] = "设备未找到",
        ["WinUsbSendFailed"] = "发送失败: {0}",
        ["WinUsbSendPlaceholder"] = "发送数据",
        ["WinUsbInvalidEndpoint"] = "端点地址格式无效",
        ["WinUsbEndpointFailed"] = "端点打开失败",
        // Serial Monitor
        ["SerialMonitorStart"] = "开始监听",
        ["SerialMonitorStop"] = "停止监听",
        ["SerialMonitorStopped"] = "监听已停止",
        ["SerialMonitorMonitoring"] = "监听中...",
        ["SerialMonitorStartFailed"] = "启动监听失败: {0}",
        ["SerialMonitorSelectBoth"] = "请选择进程和串口",
        ["SerialMonitorInvalidPid"] = "无效的进程ID",
        // Input dialogs
        ["InputDialogConfirm"] = "确定",
        ["InputDialogCancel"] = "取消",
        // QuickSend messages
        ["QuickSendListDefault"] = "列表",
        ["QuickSendImportSuccess"] = "数据导入成功",
        ["QuickSendImportFailed"] = "导入失败: {0}",
        ["QuickSendExportSuccess"] = "数据导出成功！",
        ["QuickSendExportFailed"] = "导出失败: {0}",
        ["QuickSendSavedToProfile"] = "数据已保存到配置目录",
        ["QuickSendNeedFilePicker"] = "请在 UI 中启用文件选择器",
        ["QuickSendImportSSCOMSuccess"] = "已导入 SSCOM {0} 条数据",
        ["QuickSendImportSSCOMFailed"] = "导入SSCOM失败: {0}",
        // Online scripts
        ["LoadingOnlineScripts"] = "正在加载在线脚本...",
        ["LoadingProgress"] = "加载中... {0}/{1}",
        ["OnlineScriptFileExists"] = "脚本文件已存在！",
        ["OnlineScriptSaveSuccess"] = "保存成功！",
        ["OnlineScriptSaveFailed"] = "保存失败: {0}",
        // Lua editor
        ["LuaSaveScript"] = "保存脚本",
        ["LuaRunOneLine"] = "单行执行...",
        // About
        ["AboutSubTitle"] = "能跑Lua脚本的串口调试工具",
    };

    private static readonly Dictionary<string, string> EnUs = new()
    {
        ["OpenPortButton"] = "Open Port",
        ["ClosePortButton"] = "Close Port",
        ["StatusReady"] = "Ready",
        ["StatusPortClosed"] = "Port closed",
        ["StatusCloseFailed"] = "Close failed: {0}",
        ["StatusSelectPort"] = "Please select a port",
        ["StatusConnected"] = "Connected {0} @ {1}",
        ["StatusOpenFailed"] = "Open failed: {0}",
        ["StatusSentBytes"] = "Sent {0} bytes",
        ["StatusSendFailed"] = "Send failed: {0}",
        ["StatusLogSaved"] = "Log saved: {0}",
        ["StatusSaveFailed"] = "Save failed: {0}",
        ["StatusLangSwitched"] = "Language switched to English",
        ["AppTitle"] = "LLCOM - Debug serial port with Lua!",
        // WinUSB
        ["WinUsbConnect"] = "Connect",
        ["WinUsbDisconnect"] = "Disconnect",
        ["WinUsbConnected"] = "Connected",
        ["WinUsbDisconnected"] = "Disconnected",
        ["WinUsbNoDevice"] = "No USB device found (try sudo)",
        ["WinUsbFoundDevices"] = "Found {0} USB devices",
        ["WinUsbEnumFailed"] = "Enumeration failed: {0}",
        ["WinUsbSelectDevice"] = "Please select a device",
        ["WinUsbSelectEndpoint"] = "Please select IN/OUT endpoints",
        ["WinUsbOpenFailed"] = "Open failed: {0}",
        ["WinUsbDeviceNotFound"] = "Device not found",
        ["WinUsbSendFailed"] = "Send failed: {0}",
        ["WinUsbSendPlaceholder"] = "Send data",
        ["WinUsbInvalidEndpoint"] = "Invalid endpoint address",
        ["WinUsbEndpointFailed"] = "Failed to open endpoint",
        // Serial Monitor
        ["SerialMonitorStart"] = "Start Monitor",
        ["SerialMonitorStop"] = "Stop Monitor",
        ["SerialMonitorStopped"] = "Monitor stopped",
        ["SerialMonitorMonitoring"] = "Monitoring...",
        ["SerialMonitorStartFailed"] = "Start monitor failed: {0}",
        ["SerialMonitorSelectBoth"] = "Please select process and COM port",
        ["SerialMonitorInvalidPid"] = "Invalid process ID",
        // Input dialogs
        ["InputDialogConfirm"] = "OK",
        ["InputDialogCancel"] = "Cancel",
        // QuickSend messages
        ["QuickSendListDefault"] = "List",
        ["QuickSendImportSuccess"] = "Data imported successfully",
        ["QuickSendImportFailed"] = "Import failed: {0}",
        ["QuickSendExportSuccess"] = "Data exported successfully!",
        ["QuickSendExportFailed"] = "Export failed: {0}",
        ["QuickSendSavedToProfile"] = "Data saved to profile directory",
        ["QuickSendNeedFilePicker"] = "Please enable file picker in UI",
        ["QuickSendImportSSCOMSuccess"] = "Imported {0} items from SSCOM",
        ["QuickSendImportSSCOMFailed"] = "Import SSCOM failed: {0}",
        // Online scripts
        ["LoadingOnlineScripts"] = "Loading online scripts...",
        ["LoadingProgress"] = "Loading... {0}/{1}",
        ["OnlineScriptFileExists"] = "Script file already exists!",
        ["OnlineScriptSaveSuccess"] = "Saved successfully!",
        ["OnlineScriptSaveFailed"] = "Save failed: {0}",
        // Lua editor
        ["LuaSaveScript"] = "Save script",
        ["LuaRunOneLine"] = "Run one line...",
        // About
        ["AboutSubTitle"] = "Debug serial port with Lua!",
    };

    public static void SetLanguage(string language)
    {
        CurrentLanguage = language;
    }

    public static string Get(string key)
    {
        var dict = CurrentLanguage == "zh-CN" ? ZhCn : EnUs;
        return dict.TryGetValue(key, out var value) ? value : $"<{key}>";
    }

    public static string Format(string key, params object[] args)
    {
        return string.Format(Get(key), args);
    }
}
