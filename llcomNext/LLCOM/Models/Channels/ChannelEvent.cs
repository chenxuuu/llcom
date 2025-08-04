namespace LLCOM.Models.Channels;

public enum ChannelEvent
{
    Opened,    // 通道已打开
    Closed,    // 通道已关闭
    DataReceived, // 通道接收到数据
    DataSent,  // 通道发送了数据
    Error,     // 通道发生错误
    BufferFull, // 通道缓冲区已满
}