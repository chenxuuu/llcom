using llcom.LuaEnv;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace llcom.Services;

    /// <summary>
    /// 串口服务实现（原 Model/Uart.cs 迁移，Step 4）。
    /// 保留原行为：独立接收线程 + 信号量、refreshSerialDevice 规避
    /// SerialPort 的 SafeHandle 崩溃问题、RTS/DTR 控制、Lua uart 通道注册。
    /// 相比原版清理了 refreshSerialDevice/Open/Close 中的调试日志噪音，
    /// 接收线程由 new Thread 改为 Task.Run。
    /// </summary>
    class SerialPortService : ISerialPortService
    {
        //废弃的串口对象，存放处，尝试fix[System.ObjectDisposedException: 已关闭 Safe handle]
        //https://drdump.com/Problem.aspx?ProblemID=524533
        private List<SerialPort> useless = new List<SerialPort>();

        public SerialPort serial { get; private set; } = new SerialPort();
        public event EventHandler UartDataRecived;
        public event EventHandler UartDataSent;
        public event EventHandler UartDataRawSent;
        private Stream lastPortBaseStream = null;
        private bool _rts = false;
        private bool _dtr = true;

        public bool Rts
        {
            get => _rts;
            set => serial.RtsEnable = _rts = value;
        }
        public bool Dtr
        {
            get => _dtr;
            set => serial.DtrEnable = _dtr = value;
        }

        /// <summary>
        /// 初始化串口各个触发函数
        /// </summary>
        public SerialPortService()
        {
            //声明接收到事件
            serial.DataReceived += Serial_DataReceived;
            serial.RtsEnable = Rts;
            serial.DtrEnable = Dtr;
            //常驻后台接收线程（原为 new Thread，改为 Task.Run 更符合现代写法）
            Task.Run(() => ReadData());

            //适配一下通用通道
            LuaApis.SendChannelsRegister("uart", (data, _) =>
            {
                if (IsOpen() && data != null)
                {
                    SendData(data);
                    return true;
                }
                else
                    return false;
            });
        }

        /// <summary>
        /// 刷新串口对象。
        /// 微软 SerialPort 存在 SafeHandle 释放崩溃问题，此处把旧对象扔进 useless 列表
        /// 并在后台线程 Dispose，避免直接释放导致崩溃（见类顶部链接）。
        /// </summary>
        private void refreshSerialDevice()
        {
            //以下 Dispose 可能卡住/崩溃，全部扔到后台线程执行
            Task.Run(() =>
            {
                try { lastPortBaseStream?.Dispose(); } catch { }
                try { serial.BaseStream.Dispose(); } catch { }
                try { serial.Dispose(); } catch { }
            });
            lock (useless)//存起来
                useless.Add(serial);
            serial = new SerialPort();
            //声明接收到事件
            serial.DataReceived += Serial_DataReceived;
            serial.BaudRate = Tools.Global.setting.baudRate;
            serial.Parity = (Parity)Tools.Global.setting.parity;
            serial.DataBits = Tools.Global.setting.dataBits;
            serial.StopBits = (StopBits)Tools.Global.setting.stopBit;
            serial.RtsEnable = Rts;
            serial.DtrEnable = Dtr;
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
        /// <param name="dataRaw">原始数据（Lua 处理前），用于区分回显</param>
        public void SendData(byte[] data, byte[] dataRaw = null)
        {
            if (data.Length == 0)
                return;
            serial.Write(data, 0, data.Length);
            Tools.Global.setting.SentCount += data.Length;

            //判断data与dataRaw是否相同，如果相同就只显示一个
            if (dataRaw != null)
            {
                if (dataRaw.Length == data.Length)
                {
                    bool same = true;
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != dataRaw[i])
                        {
                            same = false;
                            break;
                        }
                    }
                    if (same)
                        dataRaw = null;
                }
            }
            if (dataRaw != null && Tools.Global.setting.showSendRaw) UartDataRawSent?.Invoke(dataRaw, EventArgs.Empty);
            if (Tools.Global.setting.showSend) UartDataSent?.Invoke(data, EventArgs.Empty);//回调
        }

        //收到串口事件的信号量
        public EventWaitHandle WaitUartReceive { get; } = new AutoResetEvent(true);

        //接收到事件
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            WaitUartReceive.Set();
        }

        /// <summary>
        /// 单独开个线程接收数据。
        /// 等待 timeout 时间让数据包凑齐，避免中文等多字节字符被分割。
        /// </summary>
        private void ReadData()
        {
            WaitUartReceive.Reset();
            while (true)
            {
                WaitUartReceive.WaitOne();
                if (Tools.Global.isMainWindowsClosed)
                    return;
                if (Tools.Global.setting.timeout > 0)
                    System.Threading.Thread.Sleep(Tools.Global.setting.timeout);//等待时间
                else
                    System.Threading.Thread.Sleep(10);//等待时间默认给个10ms吧，防止中文被分割
                List<byte> result = new List<byte>();
                while (true)//循环读
                {
                    if (serial == null || !serial.IsOpen)//串口被关了，不读了
                        break;
                    try
                    {
                        int length = serial.BytesToRead;
                        if (length == 0)//没数据，退出去
                            break;
                        byte[] rev = new byte[length];
                        serial.Read(rev, 0, length);//读数据
                        if (rev.Length == 0)
                            break;
                        result.AddRange(rev);//加到list末尾
                    }
                    catch { break; }//崩了？

                    if (result.Count > Tools.Global.setting.maxLength)//长度超了
                        break;
                    if (Tools.Global.setting.bitDelay && Tools.Global.setting.timeout > 0)//如果是设置了等待间隔时间
                    {
                        System.Threading.Thread.Sleep(Tools.Global.setting.timeout);//等待时间
                    }
                    else if (Tools.Global.setting.timeout < 0)//如果是设置了等待间隔时间
                    {
                        System.Threading.Thread.Sleep(10);//等待时间默认给个10ms吧，防止中文被分割
                    }
                }
                Tools.Global.setting.ReceivedCount += result.Count;
                if (result.Count > 0)
                    try
                    {
                        var r = result.ToArray();
                        UartDataRecived(r, EventArgs.Empty);//回调事件
                        LuaApis.SendChannelsReceived("uart", r);
                    }
                    catch { }
            }
        }
    }
