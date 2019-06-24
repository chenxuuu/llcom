using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using LibUsbDotNet.DeviceNotify;

namespace llcom.Model
{
    class Uart
    {
        public SerialPort serial = new SerialPort();
        public event EventHandler UartDataRecived;
        public event EventHandler UartDataSent;

        /// <summary>
        /// 初始化串口各个触发函数
        /// </summary>
        public void Init()
        {
            //声明接收到事件
            serial.DataReceived += Serial_DataReceived;
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data">数据内容</param>
        public void SendData(byte[] data)
        {
            if (data.Length == 0)
                return;
            serial.Write(data, 0, data.Length);
            UartDataSent(data, EventArgs.Empty);//回调
        }

        //接收到事件
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Task.Delay(Tools.Global.setting.timeout).Wait();//等待时间
            if (!serial.IsOpen)//串口被关了，不读了
                return;
            int length = ((SerialPort)sender).BytesToRead;
            byte[] rev = new byte[length];
            ((SerialPort)sender).Read(rev, 0, length);
            if (rev.Length == 0)
                return;
            UartDataRecived(rev, EventArgs.Empty);//回调事件
        }
    }
}
