using System;

namespace LLCOM.Models;

public class PacketData
{
    /// <summary>
    /// 此包收到的时间
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 包内的原始数据
    /// </summary>
    public byte[] Data { get; set; } = [];

    /// <summary>
    /// 包的额外信息
    /// </summary>
    public string? Extra { get; set; } = null;
    
    /// <summary>
    /// 该包的字符串表示
    /// </summary>
    public string String { get; set; } = string.Empty;

    /// <summary>
    /// 数据包的方向
    /// </summary>
    public MessageWay Way { get; set; } = MessageWay.Unknown;

    /// <summary>
    /// 消息通道类型
    /// </summary>
    public string Channel { get; set; } = string.Empty;
}

public enum MessageWay
{
    Unknown,
    /// <summary>
    /// 从该软件发出的数据包
    /// </summary>
    Send,
    /// <summary>
    /// 从外部发到该软件的数据包
    /// </summary>
    Receive
}