using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.LuaEnv
{
    class LuaApis
    {
        public static event EventHandler PrintLuaLog;
        /// <summary>
        /// 打印日志
        /// </summary>
        /// <param name="log">日志内容</param>
        public static void PrintLog(string log)
        {
            Tools.Logger.AddLuaLog(log);
            PrintLuaLog(DateTime.Now.ToString("[HH:mm:ss:ffff]")+log, EventArgs.Empty);
        }

        /// <summary>
        /// utf8编码改为gbk的hex编码
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string Utf8ToAsciiHex(string input)
        {
            return BitConverter.ToString(Encoding.GetEncoding("GB2312").GetBytes(input)).Replace("-","");
        }

        /// <summary>
        /// 获取程序运行目录
        /// </summary>
        /// <returns>主程序运行目录</returns>
        public static string GetPath()
        {
            return AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
        }

        /// <summary>
        /// 发送串口数据
        /// </summary>
        /// <returns>是否成功</returns>
        public static bool SendUartData(string data)
        {
            if (Tools.Global.uart.serial.IsOpen)
            {
                try
                {
                    Tools.Global.uart.SendData(data);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}
