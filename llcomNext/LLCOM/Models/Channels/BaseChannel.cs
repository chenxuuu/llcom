using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LLCOM.Models.Channels;

public class BaseChannel(int bufferSize = 1024 * 10)
{
    private readonly byte[] _buffer = new byte[bufferSize];
    private int _bufferLength = 0;
    
    /// <summary>
    /// 通道事件
    /// </summary>
    public EventHandler<ChannelEvent>? ChannelEvent;
    

    /// <summary>
    /// 获取当前buffer的长度
    /// </summary>
    public virtual int BufferSize => bufferSize;
    
    /// <summary>
    /// 获取当前buffer中已存储的数据长度
    /// </summary>
    public virtual int BufferToRead => _bufferLength;

    /// <summary>
    /// 是否打开了通道
    /// </summary>
    public virtual bool IsOpen { get; set; } = false;

    /// <summary>
    /// 打开通道
    /// </summary>
    public virtual void Open()
    {
        IsOpen = true;
        _bufferLength = 0; // Reset buffer length when opening
        ChannelEvent?.Invoke(this, Channels.ChannelEvent.Opened);
    }

    /// <summary>
    /// 关闭通道
    /// </summary>
    public virtual void Close()
    {
        IsOpen = false;
        _bufferLength = 0; // Clear buffer when closing
        ChannelEvent?.Invoke(this, Channels.ChannelEvent.Closed);
    }
    
    /// <summary>
    /// 发送数据到通道
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="options">选项，给mqtt之类的用</param>
    /// <returns>是否发送成功</returns>
    public virtual bool SendData(Span<byte> data, Object? options = null)
    {
        if (!IsOpen)
            return false;
        
        if (data.Length == 0)
            return true; // Nothing to send
        
        // Simulate sending data
        ChannelEvent?.Invoke(this, Channels.ChannelEvent.DataSent);
        return true; // Assume sending is always successful for this base class
    }
    
    /// <summary>
    /// 向buffer中添加数据
    /// </summary>
    /// <param name="data">数据</param>
    /// <returns>成功添加进buffer的长度</returns>
    public int AddData(Span<byte> data)
    {
        if (data.Length == 0)
            return 0;

        var availableSpace = _buffer.Length - _bufferLength;
        if (availableSpace <= 0)
            return 0; // Buffer is full

        var lengthToCopy = Math.Min(data.Length, availableSpace);
        data[..lengthToCopy].CopyTo(_buffer.AsSpan(_bufferLength));
        _bufferLength += lengthToCopy;

        ChannelEvent?.Invoke(this, Channels.ChannelEvent.DataReceived); // Trigger the event after data is added
        if(_bufferLength == BufferSize)
            ChannelEvent?.Invoke(this, Channels.ChannelEvent.BufferFull); // Trigger the event if buffer is full
        return lengthToCopy;
    }

    /// <summary>
    /// 获取当前buffer中的数据
    /// </summary>
    /// <param name="maxLength">最大读取长度</param>
    /// <param name="keepUtf8Character">是否检查utf8截断</param>
    /// <returns>获取到的数据</returns>
    public byte[] GetData(int? maxLength = null, bool keepUtf8Character = false)
    {
        var length = maxLength ?? _bufferLength;
        if (length <= 0 || length > _bufferLength)
            length = _bufferLength;
        if (length == 0)
            return [];
        
        if (keepUtf8Character)//如果需要保留utf8字符，则需要判断末尾是否有不完整的utf8字符
            length = Services.StringHelper.GetCompleteUtf8Length(_buffer.AsSpan(0, length));
        
        if (length == 0)
            return [];

        var result = new byte[length];
        Array.Copy(_buffer, result, length);
        _bufferLength -= length;
        if (_bufferLength > 0)
            Array.Copy(_buffer, length, _buffer, 0, _bufferLength);
        return result;
    }
}
