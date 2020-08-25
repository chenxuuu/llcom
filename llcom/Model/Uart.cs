using System;
using System.Collections.Generic;
using System.IO;
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
        private Stream lastPortBaseStream = null;

        private static readonly object objLock = new object();
        
        /// <summary>
        /// 初始化串口各个触发函数
        /// </summary>
        public Uart()
        {
            //声明接收到事件
            serial.DataReceived += Serial_DataReceived;
        }

        /// <summary>
        /// 刷新串口对象
        /// </summary>
        private void refreshSerialDevice()
        {
            try
            {
                lastPortBaseStream?.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"portBaseStream?.Dispose error:{e.Message}");
            }
            try
            {
                serial.BaseStream.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine($"serial.BaseStream.Dispose error:{e.Message}");
            }
            serial.Dispose();
            serial = new SerialPort();
            //声明接收到事件
            serial.DataReceived += Serial_DataReceived;
            serial.BaudRate = Tools.Global.setting.baudRate;
            serial.Parity = (Parity)Tools.Global.setting.parity;
            serial.DataBits = Tools.Global.setting.dataBits;
            serial.StopBits = (StopBits)Tools.Global.setting.stopBit;
        }

        /// <summary>
        /// 获取串口设备COM名
        /// </summary>
        /// <returns></returns>
        public string GetName()
        {
            return serial.PortName;
        }

        /// <summary>
        /// 设置串口设备COM名
        /// </summary>
        /// <returns></returns>
        public void SetName(string s)
        {
            serial.PortName = s;
        }

        /// <summary>
        /// 查看串口打开状态
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            return serial.IsOpen;
        }

        /// <summary>
        /// 开启串口
        /// </summary>
        public void Open()
        {
            string temp = serial.PortName;
            refreshSerialDevice();
            serial.PortName = temp;
            serial.Open();
            lastPortBaseStream = serial.BaseStream;
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        public void Close()
        {
            refreshSerialDevice();
            serial.Close();
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
            Tools.Global.setting.SentCount += data.Length;
            UartDataSent(data, EventArgs.Empty);//回调
        }

        //接收到事件
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (objLock)
            {
                if(Tools.Global.setting.timeout > 0)
                    System.Threading.Thread.Sleep(Tools.Global.setting.timeout);//等待时间
                List<byte> result = new List<byte>();
                while (true)//循环读
                {
                    if (!serial.IsOpen)//串口被关了，不读了
                        break;
                    try
                    {
                        int length = ((SerialPort)sender).BytesToRead;
                        if (length == 0)//没数据，退出去
                            break;
                        byte[] rev = new byte[length];
                        ((SerialPort)sender).Read(rev, 0, length);//读数据
                        if (rev.Length == 0)
                            break;
                        result.AddRange(rev);//加到list末尾
                    }catch { break; }//崩了？

                    if (result.Count > Tools.Global.setting.maxLength)//长度超了
                        break;
                    if (Tools.Global.setting.bitDelay && Tools.Global.setting.timeout > 0)//如果是设置了等待间隔时间
                    {
                        System.Threading.Thread.Sleep(Tools.Global.setting.timeout);//等待时间
                    }
                }
                Tools.Global.setting.ReceivedCount += result.Count;
                if(result.Count > 0)
                    UartDataRecived(result.ToArray(), EventArgs.Empty);//回调事件
                System.Diagnostics.Debug.WriteLine("end");
            }
        }
    }
}
