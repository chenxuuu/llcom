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
    /// 当数据被添加之后，被触发
    /// </summary>
    public EventHandler<int>? DataAddedEvent;
    
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

        DataAddedEvent?.Invoke(this, lengthToCopy); // Trigger the event after data is added
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
