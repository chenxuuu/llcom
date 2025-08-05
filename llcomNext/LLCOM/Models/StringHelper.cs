using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;
using LLCOM.Services;

namespace LLCOM.Models;

public class PackData
{
    public PackData(byte[] data, MessageWay way, string channel, string? extra = null,
        Encoding? encoding = null, bool readable = true, string? s = null, IBrush? brush = null)
    {
        Data = data;
        Way = way;
        Channel = channel;
        Extra = extra ?? Time.ToString("yyyy/MM/dd HH:mm:ss.fff");
        encoding ??= Encoding.UTF8;
        String = s ?? StringHelper.GenerateString(Data, encoding, readable);
        HexString = way == MessageWay.Unknown ? String : StringHelper.GenerateHexString(Data);
    }

    /// <summary>
    /// 此包收到的时间
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 包内的原始数据
    /// </summary>
    public byte[] Data { get; set; }

    public string HexString { get; }

    /// <summary>
    /// 包的额外信息，一般是日期时间的字符串展示
    /// </summary>
    public string Extra { get; set; }
    
    /// <summary>
    /// 该包的字符串表示
    /// </summary>
    public string String { get; set; }

    /// <summary>
    /// 数据包的方向
    /// </summary>
    public MessageWay Way { get; set; }

    /// <summary>
    /// 消息通道类型
    /// </summary>
    public string Channel { get; set; }
    
    public bool IsWayUnknown => Way == MessageWay.Unknown;
    public bool IsWaySend => Way == MessageWay.Send;
    public bool IsWayReceive => Way == MessageWay.Receive;
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