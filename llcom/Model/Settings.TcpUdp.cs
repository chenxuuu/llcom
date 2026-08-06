namespace llcom.Model;

/// <summary>
/// TCP / UDP 客户端与本地服务器相关设置（Settings 分部类）。
/// </summary>
partial class Settings
{
    private string _tcpClientServer = "qq.com";
    private int _tcpClientPort = 80;
    private int _tcpClientProtocolType = 0;
    public string tcpClientServer
    {
        get => _tcpClientServer;
        set
        {
            if (SetProperty(ref _tcpClientServer, value))
                Save();
        }
    }
    public int tcpClientPort
    {
        get => _tcpClientPort;
        set
        {
            if (SetProperty(ref _tcpClientPort, value))
                Save();
        }
    }
    public int tcpClientProtocolType
    {
        get => _tcpClientProtocolType;
        set
        {
            if (SetProperty(ref _tcpClientProtocolType, value))
                Save();
        }
    }

    private int _tcpServerPort = 2333;
    public int tcpServerPort
    {
        get => _tcpServerPort;
        set
        {
            if (SetProperty(ref _tcpServerPort, value))
                Save();
        }
    }

    private bool _tcpReconnect = false;
    public bool tcpReconnect
    {
        get => _tcpReconnect;
        set
        {
            if (SetProperty(ref _tcpReconnect, value))
                Save();
        }
    }
    private int _tcpReconnectInterval = 5;
    public int tcpReconnectInterval
    {
        get => _tcpReconnectInterval;
        set
        {
            if (SetProperty(ref _tcpReconnectInterval, value))
                Save();
        }
    }

    private int _udpServerPort = 2333;
    public int udpServerPort
    {
        get => _udpServerPort;
        set
        {
            if (SetProperty(ref _udpServerPort, value))
                Save();
        }
    }

    private bool _luaTestHex = false;
    private bool _luaTestHexRev = false;
    public bool luaTestHex
    {
        get => _luaTestHex;
        set
        {
            if (SetProperty(ref _luaTestHex, value))
                Save();
        }
    }
    public bool luaTestHexRev
    {
        get => _luaTestHexRev;
        set
        {
            if (SetProperty(ref _luaTestHexRev, value))
                Save();
        }
    }
}
