using System;
using System.IO.Ports;
using System.Threading;

namespace llcom.Services;

/// <summary>
/// 串口服务接口。
/// 封装 SerialPort 的打开/关闭/收发，供 UI（MainWindow/DataShowPage）、
/// 设置（Settings）、初始化（ProfileInitializer）及 Lua uart 通道共用。
/// 从原 Model/Uart.cs 抽象（Step 4），Global.uart 属性名不变，调用点无感知。
/// </summary>
interface ISerialPortService
{
    /// <summary>
    /// 底层串口对象（暴露给设置类直接修改波特率/校验位等参数）
    /// </summary>
    SerialPort serial { get; }

    /// <summary>收到数据事件（sender 为 byte[]）</summary>
    event EventHandler UartDataRecived;

    /// <summary>发送数据事件（sender 为 byte[]，Lua 处理后的最终数据）</summary>
    event EventHandler UartDataSent;

    /// <summary>发送原始数据事件（sender 为 byte[]，未经 Lua 处理的原始数据）</summary>
    event EventHandler UartDataRawSent;

    /// <summary>RTS 信号线状态</summary>
    bool Rts { get; set; }

    /// <summary>DTR 信号线状态</summary>
    bool Dtr { get; set; }

    /// <summary>获取当前串口名（COMx）</summary>
    string GetName();

    /// <summary>设置串口名（COMx）</summary>
    void SetName(string s);

    /// <summary>串口是否已打开</summary>
    bool IsOpen();

    /// <summary>打开串口</summary>
    void Open();

    /// <summary>关闭串口</summary>
    void Close();

    /// <summary>
    /// 发送数据
    /// </summary>
    /// <param name="data">实际发送的字节</param>
    /// <param name="dataRaw">原始数据（Lua 处理前），用于区分回显；与 data 相同时忽略</param>
    void SendData(byte[] data, byte[] dataRaw = null);

    /// <summary>串口接收事件信号量（用于唤醒接收线程，退出时置位）</summary>
    EventWaitHandle WaitUartReceive { get; }
}
