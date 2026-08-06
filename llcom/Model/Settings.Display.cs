using System.Text;

namespace llcom.Model;

/// <summary>
/// 显示与编码相关设置（Settings 分部类）。
/// </summary>
partial class Settings
{
    private bool _bitDelay = true;
    public bool bitDelay
    {
        get => _bitDelay;
        set
        {
            if (SetProperty(ref _bitDelay, value))
                Save();
        }
    }

    private bool _autoUpdate = true;

    /// <summary>
    /// 是否开启自动升级
    /// </summary>
    public bool autoUpdate
    {
        get => _autoUpdate;
        set
        {
            if (SetProperty(ref _autoUpdate, value))
                Save();
        }
    }

    /// <summary>
    /// 串口接收每包最大长度
    /// </summary>
    private uint _maxLength = 10240;
    public uint maxLength
    {
        get => _maxLength;
        set
        {
            if (SetProperty(ref _maxLength, value))
                Save();
        }
    }
}
