using System;
using Avalonia.Media;

namespace llcom.Avalonia.ViewModels;

/// <summary>
/// 格式化数据显示行：支持时间戳、方向箭头、Hex/文本颜色区分
/// </summary>
public class DataLineItem
{
    private static readonly SolidColorBrush ReceivedBrush = new(Color.Parse("#569CD6"));
    private static readonly SolidColorBrush SentBrush = new(Color.Parse("#6A9955"));

    public DateTime Timestamp { get; set; }
    public string TimestampText => Timestamp.ToString("HH:mm:ss.fff");
    public bool IsSent { get; set; }
    public string Arrow => IsSent ? "→" : "←";
    public IBrush Foreground => IsSent ? SentBrush : ReceivedBrush;
    public string Data { get; set; } = "";
    public bool IsHex { get; set; }

    /// <summary>格式化后的完整显示行</summary>
    public string DisplayText => $"[{TimestampText}] {Arrow} {Data}";
}
