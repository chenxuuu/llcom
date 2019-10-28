using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace llcom.Tools
{
    class Global
    {
        //api接口文档网址
        public static string apiDocumentUrl = "https://github.com/chenxuuu/llcom/blob/master/LuaApi.md";
        //主窗口是否被关闭？
        public static bool isMainWindowsClosed = false;
        //给全局使用的设置参数项
        public static Model.Settings setting;
        public static Model.Uart uart = new Model.Uart();

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial()
        {
            if (!Directory.Exists("core_script"))
            {
                System.Windows.MessageBox.Show("core_script文件夹丢失！请确保没有乱删文件！如无法恢复，请去官网重新下载该软件。");
                Environment.Exit(1);
            }
            if (!Directory.Exists("logs"))
                Directory.CreateDirectory("logs");
            if (!Directory.Exists("user_script_run"))
                Directory.CreateDirectory("user_script_run");
            if (!Directory.Exists("user_script_send_convert"))
                Directory.CreateDirectory("user_script_send_convert");
            if (!Directory.Exists("user_script_run\\logs"))
                Directory.CreateDirectory("user_script_run\\logs");


            //导入之前的配置文件
            if (Properties.Settings.Default.UpgradeRequired)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeRequired = false;
                Properties.Settings.Default.Save();
            }
            setting = new Model.Settings();
            Model.Settings.UpdateQuickSend();

            uart.Init();
            uart.serial.BaudRate = setting.baudRate;
            uart.serial.Parity = (Parity)setting.parity;
            uart.serial.DataBits = setting.dataBits;
            uart.serial.StopBits = (StopBits)setting.stopBit;
            uart.UartDataRecived += Uart_UartDataRecived;
            uart.UartDataSent += Uart_UartDataSent;
            LuaEnv.LuaRunEnv.init();

            if (File.Exists("core_script/last_path"))
            {
                if(File.ReadAllText("core_script/last_path") != AppDomain.CurrentDomain.SetupInformation.ApplicationBase)
                {
                    System.Windows.MessageBox.Show(
                        "检测到软件被移动到其他路径！\r\n" +
                        "请注意，移动该软件后，配置文件将会被改为默认\r\n" +
                        "如需读取之前的配置文件，请将软件移动回之前的目录\r\n" +
                        AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "\r\n" +
                        "然后进入更多设置--导入/导出配置文件来手动备份之前的配置信息");
                    File.WriteAllText("core_script/last_path", AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
                }
            }
            else
            {
                File.WriteAllText("core_script/last_path", AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            }
        }


        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLog($"sent:\t{Byte2String((byte[])sender)}\r\nhex: {Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLog($"received:\t{Byte2String((byte[])sender)}\r\nhex: {Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space)
        {
             return BitConverter.ToString(Encoding.UTF8.GetBytes(str)).Replace("-", space);
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
            return Encoding.UTF8.GetString(vBytes);
        }


        /// <summary>
        /// byte转string
        /// </summary>
        /// <param name="mHex"></param>
        /// <returns></returns>
        public static string Byte2String(byte[] vBytes)
        {
            return Encoding.Default.GetString(vBytes);
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


        public static string Byte2Hex(byte[] d, string s = "")
        {
            return BitConverter.ToString(d).Replace("-", s);
        }


        /// <summary>
        /// 导入SSCOM配置文件数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Model.ToSendData> ImportFromSSCOM(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.GetEncoding("GB2312"));
            var r = new List<Model.ToSendData>();
            Regex title = new Regex(@"N1\d\d=\d*,");
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    var temp = new Model.ToSendData();
                    //Console.WriteLine(lines[i]);
                    if (title.IsMatch(lines[i]))//匹配上了
                    {
                        var strs = lines[i].Split(",".ToCharArray()[0]);
                        temp.commit = strs[1].Replace(((char)2).ToString(), ",");
                        if (string.IsNullOrWhiteSpace(temp.commit))
                            temp.commit = "发送";
                        //Console.WriteLine(temp.commit);

                        int dot = lines[i + 1].IndexOf(",");
                        temp.hex = lines[i + 1].Substring(dot - 1, 1) == "H";
                        //Console.WriteLine(strs[0].Substring(strs[0].Length - 1));

                        string text = lines[i + 1].Substring(dot + 1);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            temp.text = text.Replace(((char)2).ToString(), ",");
                            r.Add(temp);
                        }
                    }
                }
                catch
                {
                    //先不处理
                }
            }
            return r;
        }
    }
}
