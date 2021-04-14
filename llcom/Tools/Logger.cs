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
        //显示日志数据的回调函数
        public static event EventHandler<DataShowPara> DataShowEvent;
        public static event EventHandler<string> DataShowRawEvent;
        //清空显示的回调函数
        public static event EventHandler DataClearEvent;
        //清空日志显示
        public static void ClearData()
        {
            DataClearEvent?.Invoke(null,null);
        }
        //显示日志数据
        public static void ShowData(byte[] data, bool send)
        {
            DataShowEvent?.Invoke(null, new DataShowPara
            {
                data = data,
                send = send
            });
        }
        //显示日志数据
        public static void ShowDataRaw(string s)
        {
            DataShowRawEvent?.Invoke(null, s);
        }


        private static FileStream uartLogFile = null;
        private static FileStream luaLogFile = null;

        /// <summary>
        /// 初始化串口日志文件
        /// </summary>
        public static void InitUartLog()
        {
            string uartLog = Tools.Global.ProfilePath + "logs/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log";
            uartLogFile = File.Open(uartLog, FileMode.Append);
            AddUartLog("[INFO]Logs by LLCOM. https://github.com/chenxuuu/llcom");
        }

        public static void CloseUartLog()
        {
            if (uartLogFile == null)
                return;
            lock (uartLogFile)
                uartLogFile.Close();
            uartLogFile = null;
        }

        /// <summary>
        /// 写入一条串口日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddUartLog(string l)
        {
            if (uartLogFile == null)
                InitUartLog();
            try
            {
                byte[] log = Tools.Global.GetEncoding().GetBytes(
                    DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss:ffff]") + l + "\r\n");
                lock (uartLogFile)
                    uartLogFile.Write(log, 0, log.Length);
            }
            catch { }
        }


        /// <summary>
        /// 初始化lua日志文件
        /// </summary>
        public static void InitLuaLog()
        {
            string luaLog = Tools.Global.ProfilePath + "user_script_run/logs/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log";
            luaLogFile = File.Open(luaLog, FileMode.Append);
        }

        public static void CloseLuaLog()
        {
            if (luaLogFile == null)
                return;
            lock (luaLogFile)
                luaLogFile.Close();
            luaLogFile = null;
        }

        /// <summary>
        /// 写入一条lua日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddLuaLog(string l)
        {
            if (luaLogFile == null)
                InitLuaLog();
            try
            {
                byte[] log = Tools.Global.GetEncoding().GetBytes(
                    DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss:ffff]") + l + "\r\n");
                lock(luaLogFile)
                    luaLogFile.Write(log, 0, log.Length);
            }
            catch { }
        }
    }

    /// <summary>
    /// 显示到日志显示页面的类
    /// </summary>
    class DataShowPara
    {
        public byte[] data;
        public bool send;
    }
}
