using LibUsbDotNet;
using LibUsbDotNet.Main;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        public static string apiDocumentUrl = "https://github.com/chenxuuu/llcom";
        //主窗口是否被关闭？
        public static bool isMainWindowsClosed = false;
        //给全局使用的设置参数项
        public static Model.Settings setting = new Model.Settings();
        public static Model.Uart uart = new Model.Uart();

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial()
        {
            Logger.InitUartLog();
            uart.Init();
            uart.serial.BaudRate = setting.baudRate;
            uart.serial.Parity = (Parity)setting.parity;
            uart.serial.DataBits = setting.dataBits;
            uart.serial.StopBits = (StopBits)setting.stopBit;
            uart.UartDataRecived += Uart_UartDataRecived;
            uart.UartDataSent += Uart_UartDataSent;
            LuaEnv.LuaRunEnv.init();
        }


        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLog($"sent:\t{(string)sender}\r\nhex: {String2Hex((string)sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLog($"received:\t{(string)sender}\r\nhex: {String2Hex((string)sender, " ")}");
        }

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space)
        {
             return BitConverter.ToString(Encoding.GetEncoding("ISO-8859-1").GetBytes(str)).Replace("-", space);
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
            return Encoding.GetEncoding("ISO-8859-1").GetString(vBytes);
        }

    }
}
