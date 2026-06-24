using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace llcom.Tools;

/// <summary>
/// Byte/Hex/String conversion utilities.
/// Encoding-aware, cross-platform compatible.
/// </summary>
public static class ByteConvert
{
    private static Encoding GetEncoding(int codePage = 65001)
    {
        return Encoding.GetEncoding(codePage);
    }

    /// <summary>Convert string to HEX string.</summary>
    public static string String2Hex(string str, string space = "", int encoding = 65001)
    {
        return BitConverter.ToString(GetEncoding(encoding).GetBytes(str)).Replace("-", space);
    }

    /// <summary>Convert HEX string to string.</summary>
    public static string Hex2String(string mHex, int encoding = 65001)
    {
        mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
        if (mHex.Length % 2 != 0)
            mHex = mHex.Remove(mHex.Length - 1, 1);
        if (mHex.Length <= 0) return "";
        byte[] vBytes = new byte[mHex.Length / 2];
        for (int i = 0; i < mHex.Length; i += 2)
            if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                vBytes[i / 2] = 0;
        return GetEncoding(encoding).GetString(vBytes);
    }

    /// <summary>Convert bytes to string.</summary>
    public static string Byte2String(byte[] vBytes, int encoding = 65001, int len = -1)
    {
        var br = from e in vBytes where e != 0 select e;
        if (len == -1 || len > br.Count()) len = br.Count();
        return GetEncoding(encoding).GetString(br.Take(len).ToArray());
    }

    /// <summary>Readable byte to string with control character symbols.</summary>
    public static string Byte2Readable(byte[] vBytes, int encoding = 65001, int len = -1, bool enableSymbol = true)
    {
        if (len == -1) len = vBytes.Length;
        if (vBytes == null) return "";

        if (!enableSymbol || encoding != 65001)
            return Byte2String(vBytes, encoding, len);

        var b_del = GetEncoding(65001).GetBytes("␡");
        byte[][] symbols = Enumerable.Range(0, 32).Select(i =>
            GetEncoding(65001).GetBytes(((char)(0x2400 + i)).ToString())
        ).ToArray();

        var tb = new List<byte>();
        for (int i = 0; i < len; i++)
        {
            switch (vBytes[i])
            {
                case 0x0d when i < len - 1 && vBytes[i + 1] == 0x0a:
                    tb.AddRange(symbols[0x0d]); tb.AddRange(symbols[0x0a]);
                    tb.Add(0x0d); tb.Add(0x0a); i++;
                    break;
                case 0x0d:
                    tb.AddRange(symbols[0x0d]); tb.Add(vBytes[i]);
                    break;
                case 0x0a:
                case 0x09:
                    tb.AddRange(symbols[vBytes[i]]); tb.Add(vBytes[i]);
                    break;
                default:
                    if (vBytes[i] <= 0x1f) tb.AddRange(symbols[vBytes[i]]);
                    else if (vBytes[i] == 0x7f) tb.AddRange(b_del);
                    else tb.Add(vBytes[i]);
                    break;
            }
        }
        return GetEncoding(encoding).GetString(tb.ToArray());
    }

    /// <summary>Convert HEX string to byte array.</summary>
    public static byte[] Hex2Byte(string mHex)
    {
        if (string.IsNullOrEmpty(mHex)) return Array.Empty<byte>();
        // Sanitize: strip non-hex characters, limit input size to prevent DoS
        mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
        const int maxHexLen = 2 * 1024 * 1024; // 1MB worth of hex
        if (mHex.Length > maxHexLen)
            mHex = mHex[..maxHexLen];
        if (mHex.Length % 2 != 0)
            mHex = mHex.Remove(mHex.Length - 1, 1);
        if (mHex.Length <= 0) return Array.Empty<byte>();
        byte[] vBytes = new byte[mHex.Length / 2];
        for (int i = 0; i < mHex.Length; i += 2)
            if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                vBytes[i / 2] = 0;
        return vBytes;
    }

    /// <summary>Convert byte array to HEX string.</summary>
    public static string Byte2Hex(byte[] d, string s = "", int len = -1)
    {
        if (len == -1) len = d.Length;
        return BitConverter.ToString(d, 0, len).Replace("-", s);
    }
}
