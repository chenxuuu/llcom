using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Tools
{
    class Global
    {
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
        }
    }
}
