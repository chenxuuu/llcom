using System;
using System.Collections.Generic;
using System.Text;

namespace LLCOM.Services;

public abstract class StringHelper
{
        /// <summary>
    /// 根据Data生成一个十六进制字符串
    /// </summary>
    public static string GenerateHexString(Span<byte> data)
    {
        var sb = new StringBuilder();
        foreach (var b in data)
        {
            sb.Append(b.ToString("X2"));
            sb.Append(' ');
        }
        return sb.ToString();
    }

    private static readonly byte[] BDel = "\u2421"u8.ToArray();
    private static readonly byte[][] Symbols =
    [
        "\u2400"u8.ToArray(), "\u2401"u8.ToArray(), "\u2402"u8.ToArray(), "\u2403"u8.ToArray(), "\u2404"u8.ToArray(),
        "\u2405"u8.ToArray(), "\u2406"u8.ToArray(), "\u2407"u8.ToArray(), "\u2408"u8.ToArray(), "\u2409"u8.ToArray(),
        "\u240a"u8.ToArray(), "\u240b"u8.ToArray(), "\u240c"u8.ToArray(), "\u240d"u8.ToArray(), "\u240e"u8.ToArray(),
        "\u240f"u8.ToArray(), "\u2410"u8.ToArray(), "\u2411"u8.ToArray(), "\u2412"u8.ToArray(), "\u2413"u8.ToArray(),
        "\u2414"u8.ToArray(), "\u2415"u8.ToArray(), "\u2416"u8.ToArray(), "\u2417"u8.ToArray(), "\u2418"u8.ToArray(),
        "\u2419"u8.ToArray(), "\u241a"u8.ToArray(), "\u241b"u8.ToArray(), "\u241c"u8.ToArray(), "\u241d"u8.ToArray(),
        "\u241e"u8.ToArray(), "\u241f"u8.ToArray()
    ];

    /// <summary>
    /// 根据指定的编码生成一个字符串
    /// </summary>
    /// <param name="data">原始数据</param>
    /// <param name="encoding">指定的编码</param>
    /// <param name="readable">是否将不可见字符转义为可见字符</param>
    /// <returns>转换后的字符串</returns>
    public static string GenerateString(Span<byte> data,Encoding encoding, bool readable = true)
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
    /// <param name="skipZero">跳过0x00，防止字符串被截断</param>
    /// <returns>转换结果</returns>
    public static string Byte2String(Encoding encoding, Span<byte> bytes, bool skipZero = false)
    {
        if(skipZero)
            return encoding.GetString(Array.FindAll(bytes.ToArray(), b => b != 0x00));
        return encoding.GetString(bytes);
    }
    
    /// <summary>
    /// 查找UTF-8字符的边界
    /// </summary>
    /// <param name="bytes">字节数组</param>
    /// <returns>有效长度</returns>
    public static int GetCompleteUtf8Length(Span<byte> bytes)
    {
        if (bytes == null || bytes.Length == 0)
            return 0;

        //从后往前找是否有符合要求的边界值
        var offset = bytes.Length - 1;
        while (offset >= 0)
        {
            // UTF-8字符边界：
            // - ASCII字符 (0xxxxxxx)
            // - 多字节字符的开始 (11xxxxxx)
            // 不是延续字节 (10xxxxxx)
            if ((bytes[offset] & 0x80) == 0 || (bytes[offset] & 0xC0) == 0xC0)
                break;
            offset--;
        }
        //如果没有找到边界，说明整个数据都是不完整的utf8字符
        if (offset < 0)
            return 0;
        //如果是多字节字符的开始，检查一下最后一个字符是否为完整字符
        // 1字节：0xxxxxxx，最高位为0
        // 2字节：110xxxxx 10xxxxxx，最高位依次为110和10
        // 3字节：1110xxxx 10xxxxxx 10xxxxxx，最高位依次为1110和10
        // 4字节：11110xxx 10xxxxxx 10xxxxxx 10xxxxxx，最高位依次为11110和10

        int charLength = 0;
        if ((bytes[offset] & 0xE0) == 0xC0) // 2字节字符
            charLength = 2;
        else if ((bytes[offset] & 0xF0) == 0xE0) // 3字节字符
            charLength = 3;
        else if ((bytes[offset] & 0xF8) == 0xF0) // 4字节字符
            charLength = 4;
        else
            return offset + 1; // 如果不是多字节字符的开始，返回实际长度
        // 检查是否有足够的字节来构成完整的UTF-8字符
        if (offset + charLength <= bytes.Length)
            return offset + charLength;
        else//不够组成完整字符了
            return offset;//返回除去不完整字符的长度
    }
}










