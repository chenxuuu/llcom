using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace llcom.Tools
{
    class Logger
    {
        //显示日志数据的回调函数
        public static event EventHandler<DataShow> DataShowTask;
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
            //不刷新日志
            if (Tools.Global.setting.DisableLog)
                return;
            DataShowTask?.Invoke(null, new DataShowPara
            {
                data = data,
                send = send
            });
        }
        //显示日志数据
        public static void ShowDataRaw(DataShowRaw s)
        {
            //不刷新日志
            if (Tools.Global.setting.DisableLog)
                return;
            DataShowTask?.Invoke(null, s);
        }


        private static Serilog.Core.Logger uartLogFile = null;
        private static Serilog.Core.Logger luaLogFile = null;

        /// <summary>
        /// 初始化串口日志文件
        /// </summary>
        public static void InitUartLog()
        {
            uartLogFile = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Tools.Global.ProfilePath + "logs/log.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    encoding: Encoding.UTF8,
                    rollOnFileSizeLimit: true)
                .CreateLogger();
            AddUartLogInfo("[SRART]Logs by LLCOM. https://github.com/chenxuuu/llcom");
        }

        public static void CloseUartLog()
        {
            if (uartLogFile == null)
                return;
            uartLogFile.Dispose();
            uartLogFile = null;
        }

        /// <summary>
        /// 写入一条串口日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddUartLogInfo(string l)
        {
            if (uartLogFile == null)
                InitUartLog();
            uartLogFile.Information(l);
        }
        /// <summary>
        /// 写入一条串口日志
        /// </summary>
        /// <param name="l"></param>
        public static void AddUartLogDebug(string l)
        {
            if (uartLogFile == null)
                InitUartLog();
            uartLogFile.Debug(l);
        }

        /// <summary>
        /// 初始化lua日志文件
        /// </summary>
        public static void InitLuaLog()
        {
            luaLogFile = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(Tools.Global.ProfilePath + "user_script_run/logs/log.txt",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    encoding: Encoding.UTF8,
                    rollOnFileSizeLimit: true)
                .CreateLogger();
        }

        public static void CloseLuaLog()
        {
            if (luaLogFile == null)
                return;
            luaLogFile.Dispose();
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
            luaLogFile.Information(l);
        }
    }

    //整个父类统一下
    class DataShow
    {
        public DateTime time { get; set; } = DateTime.Now;
        public byte[] data;
    }

    /// <summary>
    /// 显示到日志显示页面的类
    /// </summary>
    class DataShowPara : DataShow
    {
        public bool send;
    }

    /// <summary>
    /// 更通用的日志数据
    /// </summary>
    class DataShowRaw : DataShow
    {
        public string title;
        public SolidColorBrush color;
    }
}
