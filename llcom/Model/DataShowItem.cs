using System;
using System.Linq;
using System.Windows.Media;

namespace llcom.Model;

    /// <summary>
    /// 收发数据显示条目（分包模式下列表的一项）。
    /// 由 DataShowViewModel 生成，DataShowPage 的 ItemsControl 绑定显示。
    /// 从原 DataShowPage.xaml.cs 内部类 DataShow 迁移（Step 8）。
    /// </summary>
    public class DataShowItem
    {
        public string TimeText { get; set; }
        public string ArrowText { get; set; }
        public string DataText { get; set; }
        public SolidColorBrush DataTextColor { get; set; }
        public string RawTitle { get; set; }
        /// <summary>前面要加换行符</summary>
        public string RawText { get; set; }
        public SolidColorBrush RawTextColor { get; set; }
        /// <summary>前面要加换行符</summary>
        public string HexText { get; set; }
        public SolidColorBrush HexTextColor { get; set; }

        /// <summary>
        /// 普通收发数据条目（接收时先经接收转换脚本处理）
        /// </summary>
        public DataShowItem(byte[] data, DateTime time, bool sent)
        {
            if (data == null || data.Count() == 0)
                return;
            byte[] temp = data.ToArray();
            //转换下接收数据
            if (!sent)
            {
                try
                {
                    temp = LuaEnv.LuaLoader.Run(
                        $"{Tools.Global.setting.recvScript}.lua",
                        new System.Collections.ArrayList { "uartData", temp },
                        "user_script_recv_convert/");
                }
                catch (Exception ex)
                {
                    Tools.MessageBox.Show($"receive convert lua script error\r\n" + ex.ToString());
                    return;
                }
                if (temp == null)
                    return;
            }

            TimeText = time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");
            ArrowText = sent ? " ← " : " → ";
            DataTextColor = sent ? Brushes.DarkRed : Brushes.DarkGreen;
            HexTextColor = sent ? Brushes.IndianRed : Brushes.ForestGreen;

            var len = temp.Length;
            //主要数据
            if (temp != null && temp.Length > 0)
            {
                DataText = Tools.Global.setting.showHexFormat switch
                {
                    2 => Tools.Global.Byte2Hex(temp, " ", len),
                    _ => Tools.Global.Byte2Readable(temp, len),
                };
                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat == 0)
                    HexText = "\nHex: " + Tools.Global.Byte2Hex(temp, " ", len);
            }
        }

        /// <summary>
        /// 通用日志条目（带标题与颜色，如 MQTT/TCP 等通道数据）
        /// </summary>
        public DataShowItem(string title, byte[] data, DateTime time, SolidColorBrush color)
        {
            byte[] temp = data.ToArray();

            TimeText = time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");

            var len = temp.Length;
            //主要数据
            if (temp != null && temp.Length > 0)
            {
                RawText = "\n" + Tools.Global.setting.showHexFormat switch
                {
                    2 => Tools.Global.Byte2Hex(temp, " ", len),
                    _ => Tools.Global.Byte2Readable(temp, len),
                };
                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat == 0)
                    HexText = "\nHex: " + Tools.Global.Byte2Hex(temp, " ", len);
            }

            RawTitle = title;
            RawTextColor = color;
            HexTextColor = color;
        }
    }
