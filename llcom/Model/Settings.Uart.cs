using System;
using System.IO.Ports;
using System.Text;

namespace llcom.Model;

/// <summary>
/// 串口相关设置（Settings 分部类）。
/// 属性 setter 中涉及串口参数（波特率/校验位/数据位/停止位）的会同步应用到
/// Global.uart.serial，与重构前行为一致。
/// </summary>
partial class Settings
{
    private string _dataToSend = "uart data";
    public string dataToSend
    {
        get => _dataToSend;
        set
        {
            if (SetProperty(ref _dataToSend, value))
                Save();
        }
    }

    private int _baudRate = 115200;
    public int baudRate
    {
        get => _baudRate;
        set
        {
            try
            {
                Tools.Global.uart.serial.BaudRate = value;
                SetProperty(ref _baudRate, value);
                Save();
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show(e.Message);
            }
        }
    }

    private bool _autoReconnect = true;
    public bool autoReconnect
    {
        get => _autoReconnect;
        set
        {
            if (SetProperty(ref _autoReconnect, value))
                Save();
        }
    }

    private bool _autoSaveLog = true;
    public bool autoSaveLog
    {
        get => _autoSaveLog;
        set
        {
            if (SetProperty(ref _autoSaveLog, value))
                Save();
        }
    }

    /// <summary>
    /// 串口数据显示格式
    /// 0 都显示
    /// 1 只显示字符串
    /// 2 只显示Hex
    /// </summary>
    private int _showHexFormat = 0;
    public int showHexFormat
    {
        get => _showHexFormat;
        set
        {
            if (SetProperty(ref _showHexFormat, value))
                Save();
        }
    }

    private bool _hexSend = false;

    /// <summary>
    /// 主数据发送框是否发hex
    /// </summary>
    public bool hexSend
    {
        get => _hexSend;
        set
        {
            if (SetProperty(ref _hexSend, value))
                Save();
        }
    }

    private bool _showSend = true;
    public bool showSend
    {
        get => _showSend;
        set
        {
            if (SetProperty(ref _showSend, value))
                Save();
        }
    }

    private bool _showSendRaw = true;
    public bool showSendRaw
    {
        get => _showSendRaw;
        set
        {
            if (SetProperty(ref _showSendRaw, value))
                Save();
        }
    }

    private int _parity = 0;
    public int parity
    {
        get => _parity;
        set
        {
            try
            {
                _parity = value;
                Tools.Global.uart.serial.Parity = (Parity)value;
                Save();
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show(e.Message);
            }
        }
    }

    private int _timeout = 50;
    public int timeout
    {
        get => _timeout;
        set
        {
            if (SetProperty(ref _timeout, value))
                Save();
        }
    }

    private int _dataBits = 8;
    public int dataBits
    {
        get => _dataBits;
        set
        {
            try
            {
                _dataBits = value;
                Tools.Global.uart.serial.DataBits = value;
                Save();
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show(e.Message);
            }
        }
    }

    private int _stopBit = 1;
    public int stopBit
    {
        get => _stopBit;
        set
        {
            try
            {
                _stopBit = value;
                Tools.Global.uart.serial.StopBits = (StopBits)value;
                Save();
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show(e.Message);
            }
        }
    }

    private string _sendScript = "default";
    public string sendScript
    {
        get => _sendScript;
        set
        {
            if (SetProperty(ref _sendScript, value))
                Save();
        }
    }

    private string _recvScript = "default";
    public string recvScript
    {
        get => _recvScript;
        set
        {
            if (SetProperty(ref _recvScript, value))
                Save();
        }
    }

    private string _runScript = "example";
    public string runScript
    {
        get => _runScript;
        set
        {
            if (SetProperty(ref _runScript, value))
                Save();
        }
    }

    private bool _topmost = false;
    public bool topmost
    {
        get => _topmost;
        set
        {
            if (SetProperty(ref _topmost, value))
            {
                try
                {
                    MainWindowTop?.Invoke(value, EventArgs.Empty);
                }
                catch { }
                Save();
            }
        }
    }

    private bool _terminal = true;
    public bool terminal
    {
        get => _terminal;
        set
        {
            if (SetProperty(ref _terminal, value))
                Save();
        }
    }

    private int _encoding = 65001;
    public int encoding
    {
        get => _encoding;
        set
        {
            try
            {
                Encoding.GetEncoding(value);
                SetProperty(ref _encoding, value);
                Save();
            }
            catch { } //获取出错说明编码不对
        }
    }

    private bool _extraEnter = false;
    public bool extraEnter
    {
        get => _extraEnter;
        set
        {
            if (SetProperty(ref _extraEnter, value))
                Save();
        }
    }

    private bool _enableSymbol = true;
    public bool EnableSymbol
    {
        get => _enableSymbol;
        set
        {
            if (SetProperty(ref _enableSymbol, value))
                Save();
        }
    }
}
