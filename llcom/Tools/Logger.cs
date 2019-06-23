using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Tools
{
    class Logger
    {
        private static string uartLogFile = "";
        private static string luaLogFile = "";

        /// <summary>
        /// 初始化串口日志文件
        /// </summary>
        public static void InitUartLog()
        {
            uartLogFile = "logs/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log";
            //File.Create(uartLogFile);
        }

        /// <summary>
        /// 写入一条串口日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddUartLog(string l)
        {
            File.AppendAllText(uartLogFile, DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss:ffff]") + l + "\r\n");
        }


        /// <summary>
        /// 初始化lua日志文件
        /// </summary>
        public static void InitLuaLog()
        {
            luaLogFile = "user_script_run/logs/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log";
        }

        /// <summary>
        /// 写入一条lua日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddLuaLog(string l)
        {
            if (luaLogFile == "")
                InitLuaLog();
            File.AppendAllText(luaLogFile, DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss:ffff]") + l + "\r\n");
        }
    }
}
