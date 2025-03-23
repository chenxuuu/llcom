using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Media;

namespace LLCOM.Models;

public class PacketData
{
    public PacketData(byte[] data, MessageWay way, string channel, string? extra = null,
        Encoding? encoding = null, bool readable = true, string? s = null, IBrush? brush = null)
    {
        Data = data;
        Way = way;
        Channel = channel;
        Extra = extra ?? Time.ToString("yyyy/MM/dd HH:mm:ss.fff");
        encoding ??= Encoding.UTF8;
        String = s ?? GenerateString(Data, encoding, readable);
        HexString = way == MessageWay.Unknown ? String : GenerateHexString(Data);
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
    
    /// <summary>
    /// 根据Data生成一个十六进制字符串
    /// </summary>
    public static string GenerateHexString(byte[] data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
        {
            sb.Append(b.ToString("X2"));
            sb.Append(" ");
        }
        return sb.ToString();
    }

    private static readonly byte[] BDel = "\u2421"u8.ToArray();
    private static readonly byte[][] Symbols =
    [
        "\u2400"u8.ToArray(), "\u2401"u8.ToArray(), "\u2402"u8.ToArray(), "\u2403"u8.ToArray(),
            "\u2404"u8.ToArray(),
            "\u2405"u8.ToArray(), "\u2406"u8.ToArray(), "\u2407"u8.ToArray(), "\u2408"u8.ToArray(),
            "\u2409"u8.ToArray(),
            "\u240a"u8.ToArray(), "\u240b"u8.ToArray(), "\u240c"u8.ToArray(), "\u240d"u8.ToArray(),
            "\u240e"u8.ToArray(),
            "\u240f"u8.ToArray(), "\u2410"u8.ToArray(), "\u2411"u8.ToArray(), "\u2412"u8.ToArray(),
            "\u2413"u8.ToArray(),
            "\u2414"u8.ToArray(), "\u2415"u8.ToArray(), "\u2416"u8.ToArray(), "\u2417"u8.ToArray(),
            "\u2418"u8.ToArray(),
            "\u2419"u8.ToArray(), "\u241a"u8.ToArray(), "\u241b"u8.ToArray(), "\u241c"u8.ToArray(),
            "\u241d"u8.ToArray(),
            "\u241e"u8.ToArray(), "\u241f"u8.ToArray()
    ];
    
    /// <summary>
    /// 根据指定的编码生成一个字符串
    /// </summary>
    /// <param name="encoding">指定的编码</param>
    /// <param name="readable">是否将不可见字符转义为可见字符</param>
    /// <returns>转换后的字符串</returns>
    public static string GenerateString(byte[] data,Encoding encoding, bool readable = true)
    {
        //非utf8编码就不转义了
        if (!readable || encoding.CodePage != 65001)
        {
            return Byte2String(encoding, data, true);
        }
        var temp = new List<byte>();
        for (int i = 0; i < data.Length; i++)
        {
            switch(data[i])
            {
                case 0x00:
                    temp.AddRange(Symbols[0x00]);
                    break;
                case 0x0d:
                    //遇到成对出现
                    if(i < data.Length - 1 && data[i+1] == 0x0a)
                    {
                        temp.AddRange(Symbols[0x0d]);
                        temp.AddRange(Symbols[0x0a]);
                        temp.Add(0x0d);
                        temp.Add(0x0a);
                        i++;
                    }
                    else
                    {
                        temp.AddRange(Symbols[0x0d]);
                        temp.Add(data[i]);
                    }
                    break;
                case 0x0a:
                case 0x09://tab字符
                    temp.AddRange(Symbols[data[i]]);
                    temp.Add(data[i]);
                    break;
                default:
                    //普通的字符
                    if(data[i] <= 0x1f)
                        temp.AddRange(Symbols[data[i]]);
                    else if (data[i] == 0x7f)//del
                        temp.AddRange(BDel);
                    else
                        temp.Add(data[i]);
                    break;
            }
        }
        return Byte2String(encoding, temp.ToArray());
    }

    /// <summary>
    /// byte转string
    /// </summary>
    /// <param name="encoding">编码</param>
    /// <param name="bytes">数据</param>
    /// <param name="skip_zero">跳过0x00，防止字符串被截断</param>
    /// <returns>转换结果</returns>
    public static string Byte2String(Encoding encoding, byte[] bytes, bool skip_zero = false)
    {
        if(skip_zero)
            return encoding.GetString(Array.FindAll(bytes, b => b != 0x00));
        return encoding.GetString(bytes);
    }
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