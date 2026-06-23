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
