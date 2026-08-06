using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace llcom.Tools;

    /// <summary>
    /// 编码与格式转换服务。
    /// 提供字符串 / HEX / 字节数组 之间的互转，以及全局设置的编码获取。
    /// 编码来源为 Global.setting.encoding（与原来 Tools.Global 中行为完全一致）。
    /// 从原 Tools/Global.cs 拆出（Step 2）。
    /// </summary>
    internal static class EncodingHelper
    {
        public static Encoding GetEncoding() => Encoding.GetEncoding(Tools.Global.setting.encoding);

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space)
        {
            return BitConverter.ToString(GetEncoding().GetBytes(str)).Replace("-", space);
        }

        /// <summary>
        /// hex值转字符串
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static string Hex2String(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return GetEncoding().GetString(vBytes);
        }

        /// <summary>
        /// byte转string
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2String(byte[] vBytes, int len = -1)
        {
            var br = from e in vBytes
                     where e != 0
                     select e;
            if (len == -1 || len > br.Count())
                len = br.Count();
            return GetEncoding().GetString(br.Take(len).ToArray());
        }

        private static byte[] b_del = Encoding.GetEncoding(65001).GetBytes("␡");

        private static byte[][] symbols =
        {
            new byte[]{226,144,128},new byte[]{226,144,129},new byte[]{226,144,130},new byte[]{226,144,131},new byte[]{226,144,132},
            new byte[]{226,144,133},new byte[]{226,144,134},new byte[]{226,144,135},new byte[]{226,144,136},new byte[]{226,144,137},
            new byte[]{226,144,138},new byte[]{226,144,139},new byte[]{226,144,140},new byte[]{226,144,141},new byte[]{226,144,142},
            new byte[]{226,144,143},new byte[]{226,144,144},new byte[]{226,144,145},new byte[]{226,144,146},new byte[]{226,144,147},
            new byte[]{226,144,148},new byte[]{226,144,149},new byte[]{226,144,150},new byte[]{226,144,151},new byte[]{226,144,152},
            new byte[]{226,144,153},new byte[]{226,144,154},new byte[]{226,144,155},new byte[]{226,144,156},new byte[]{226,144,157},
            new byte[]{226,144,158},new byte[]{226,144,159},
        };
        /// <summary>
        /// byte转string（可读）：将 \r \n \t 等控制字符替换为可视化符号再显示，
        /// 便于在日志区直接看出不可见字符。仅 UTF-8 编码 + 开启符号显示时生效。
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2Readable(byte[] vBytes, int len = -1)
        {
            if (len == -1)
                len = vBytes.Length;
            if (vBytes == null)//fix
                return "";
            //没开这个功能/非utf8就别搞了
            if (!Tools.Global.setting.EnableSymbol || Tools.Global.setting.encoding != 65001)
                return Byte2String(vBytes, len);
            var tb = new System.Collections.Generic.List<byte>();
            for (int i = 0; i < len; i++)
            {
                switch(vBytes[i])
                {
                    case 0x0d:
                        //遇到成对出现
                        if(i < len - 1 && vBytes[i+1] == 0x0a)
                        {
                            tb.AddRange(symbols[0x0d]);
                            tb.AddRange(symbols[0x0a]);
                            tb.Add(0x0d);
                            tb.Add(0x0a);
                            i++;
                        }
                        else
                        {
                            tb.AddRange(symbols[0x0d]);
                            tb.Add(vBytes[i]);
                        }
                        break;
                    case 0x0a:
                    case 0x09://tab字符
                        tb.AddRange(symbols[vBytes[i]]);
                        tb.Add(vBytes[i]);
                        break;
                    default:
                        //普通的字符
                        if(vBytes[i] <= 0x1f)
                            tb.AddRange(symbols[vBytes[i]]);
                        else if (vBytes[i] == 0x7f)//del
                            tb.AddRange(b_del);
                        else
                            tb.Add(vBytes[i]);
                        break;
                }
            }
            return GetEncoding().GetString(tb.ToArray());
        }

        /// <summary>
        /// hex转byte
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static byte[] Hex2Byte(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return new byte[0];
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return vBytes;
        }

        /// <summary>
        /// byte转hex
        /// </summary>
        /// <param name="d">字节数组</param>
        /// <param name="s">间隔符号</param>
        /// <param name="len">参与转换的长度，-1 表示全部</param>
        /// <returns></returns>
        public static string Byte2Hex(byte[] d, string s = "", int len = -1)
        {
            if (len == -1)
                len = d.Length;
            return BitConverter.ToString(d, 0, len).Replace("-", s);
        }
    }
