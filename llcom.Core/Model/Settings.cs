using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using llcom.Tools;

namespace llcom.Model;

public class Settings : INotifyPropertyChanged
{
    public event EventHandler? MainWindowTop;
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _dataToSend = "uart data";
    private int _baudRate = 115200;
    private bool _autoReconnect = true;
    private bool _autoSaveLog = true;
    private int _showHexFormat = 0;
    private bool _hexSend;
    private bool _showSend = true;
    private bool _showSendRaw = true;
    private int _parity;
    private int _timeout = 50;
    private int _dataBits = 8;
    private int _stopBit = 1;
    private string _sendScript = "default";
    private string _recvScript = "default";
    private string _runScript = "example";
    private bool _topmost;
    private bool _bitDelay = true;
    private bool _autoUpdate = true;
    private uint _maxLength = 10240;
    private string _language = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
    private int _encoding = 65001;
    private bool _terminal = true;
    private bool _extraEnter;
    private bool _enableSymbol = true;

    public List<List<ToSendData>> quickSendList = new();
    private int _quickSendSelect = -1;

    // Window position
    private double _windowTop;
    private double _windowLeft;
    private double _windowWidth;
    private double _windowHeight;

    public double windowTop { get => _windowTop; set { _windowTop = value; Save(); } }
    public double windowLeft { get => _windowLeft; set { _windowLeft = value; Save(); } }
    public double windowWidth { get => _windowWidth; set { _windowWidth = value; Save(); } }
    public double windowHeight { get => _windowHeight; set { _windowHeight = value; Save(); } }

    public int SentCount { get; set; }
    public int ReceivedCount { get; set; }
    public bool DisableLog { get; set; }

    // Core settings
    public uint maxLength { get => _maxLength; set { _maxLength = value; Save(); } }
    public int quickSendSelect { get => _quickSendSelect; set { _quickSendSelect = value; Save(); } }
    public bool autoUpdate { get => _autoUpdate; set { _autoUpdate = value; Save(); } }
    public bool bitDelay { get => _bitDelay; set { _bitDelay = value; Save(); } }

    public string dataToSend { get => _dataToSend; set { _dataToSend = value; Save(); } }

    public int baudRate
    {
        get => _baudRate;
        set
        {
            try
            {
                UartManager.Instance.Serial.BaudRate = value;
                _baudRate = value; Save();
            }
            catch (Exception e) { ShowMessage(e.Message); }
        }
    }

    public bool autoReconnect { get => _autoReconnect; set { _autoReconnect = value; Save(); } }
    public bool autoSaveLog { get => _autoSaveLog; set { _autoSaveLog = value; Save(); } }
    public int showHexFormat { get => _showHexFormat; set { _showHexFormat = value; Save(); } }
    public bool hexSend { get => _hexSend; set { _hexSend = value; Save(); } }
    public bool showSend { get => _showSend; set { _showSend = value; Save(); } }
    public bool showSendRaw { get => _showSendRaw; set { _showSendRaw = value; Save(); } }

    public int parity
    {
        get => _parity;
        set
        {
            try { _parity = value; UartManager.Instance.Serial.Parity = (Parity)value; Save(); }
            catch (Exception e) { ShowMessage(e.Message); }
        }
    }

    public int timeout { get => _timeout; set { _timeout = value; Save(); } }
    public int dataBits
    {
        get => _dataBits;
        set
        {
            try { _dataBits = value; UartManager.Instance.Serial.DataBits = value; Save(); }
            catch (Exception e) { ShowMessage(e.Message); }
        }
    }

    public int stopBit
    {
        get => _stopBit;
        set
        {
            try { _stopBit = value; UartManager.Instance.Serial.StopBits = (StopBits)value; Save(); }
            catch (Exception e) { ShowMessage(e.Message); }
        }
    }

    public string sendScript { get => _sendScript; set { _sendScript = value; Save(); } }
    public string recvScript { get => _recvScript; set { _recvScript = value; Save(); } }
    public string runScript { get => _runScript; set { _runScript = value; Save(); } }

    public bool topmost
    {
        get => _topmost;
        set { _topmost = value; try { MainWindowTop?.Invoke(value, EventArgs.Empty); } catch { } Save(); }
    }

    public bool terminal { get => _terminal; set { _terminal = value; Save(); } }
    public bool extraEnter { get => _extraEnter; set { _extraEnter = value; Save(); } }

    public string language
    {
        get => _language;
        set { _language = value; PlatformHelper.LoadLanguageFile(value); Save(); }
    }

    public int encoding
    {
        get => _encoding;
        set { try { Encoding.GetEncoding(value); _encoding = value; Save(); } catch { } }
    }

    public bool EnableSymbol { get => _enableSymbol; set { _enableSymbol = value; Save(); } }

    // MQTT settings
    private string _mqttServer = "broker.emqx.io"; private int _mqttPort = 1883;
    private string _mqttClientID = Guid.NewGuid().ToString(); private bool _mqttTLS;
    private bool _mqttTLSCert; private string _mqttTLSCertCaPath = ""; private string _mqttTLSCertClientPath = "";
    private string _mqttTLSCertClientPassword = ""; private bool _mqttWs; private string _mqttWsPath = "/mqtt";
    private string _mqttUser = "user"; private string _mqttPassword = "password"; private int _mqttKeepAlive = 120;
    private bool _mqttCleanSession; private string _mqttPublishTopic = "your/publish/topic";
    private string _mqttSubscribeTopic = "your/subcribe/topic";

    public string mqttServer { get => _mqttServer; set { _mqttServer = value; Save(); } }
    public int mqttPort { get => _mqttPort; set { _mqttPort = value; Save(); } }
    public string mqttClientID { get => _mqttClientID; set { _mqttClientID = value; Save(); } }
    public bool mqttTLS { get => _mqttTLS; set { _mqttTLS = value; Save(); } }
    public bool mqttTLSCert { get => _mqttTLSCert; set { _mqttTLSCert = value; Save(); } }
    public string mqttTLSCertCaPath { get => _mqttTLSCertCaPath; set { _mqttTLSCertCaPath = value; Save(); } }
    public string mqttTLSCertClientPath { get => _mqttTLSCertClientPath; set { _mqttTLSCertClientPath = value; Save(); } }
    public string mqttTLSCertClientPassword { get => _mqttTLSCertClientPassword; set { _mqttTLSCertClientPassword = value; Save(); } }
    public bool mqttWs { get => _mqttWs; set { _mqttWs = value; Save(); } }
    public string mqttWsPath { get => _mqttWsPath; set { _mqttWsPath = value; Save(); } }
    public string mqttUser { get => _mqttUser; set { _mqttUser = value; Save(); } }
    public string mqttPassword { get => _mqttPassword; set { _mqttPassword = value; Save(); } }
    public int mqttKeepAlive { get => _mqttKeepAlive; set { _mqttKeepAlive = value; Save(); } }
    public bool mqttCleanSession { get => _mqttCleanSession; set { _mqttCleanSession = value; Save(); } }
    public string mqttPublishTopic { get => _mqttPublishTopic; set { _mqttPublishTopic = value; Save(); } }
    public string mqttSubscribeTopic { get => _mqttSubscribeTopic; set { _mqttSubscribeTopic = value; Save(); } }

    // TCP/UDP
    private string _tcpClientServer = "qq.com"; private int _tcpClientPort = 80; private int _tcpClientProtocolType;
    public string tcpClientServer { get => _tcpClientServer; set { _tcpClientServer = value; Save(); } }
    public int tcpClientPort { get => _tcpClientPort; set { _tcpClientPort = value; Save(); } }
    public int tcpClientProtocolType { get => _tcpClientProtocolType; set { _tcpClientProtocolType = value; Save(); } }
    private int _tcpServerPort = 2333; public int tcpServerPort { get => _tcpServerPort; set { _tcpServerPort = value; Save(); } }
    private bool _tcpReconnect; public bool tcpReconnect { get => _tcpReconnect; set { _tcpReconnect = value; Save(); } }
    private int _tcpReconnectInterval = 5; public int tcpReconnectInterval { get => _tcpReconnectInterval; set { _tcpReconnectInterval = value; Save(); } }
    private int _udpServerPort = 2333; public int udpServerPort { get => _udpServerPort; set { _udpServerPort = value; Save(); } }

    // Lua
    private bool _luaTestHex; private bool _luaTestHexRev;
    public bool luaTestHex { get => _luaTestHex; set { _luaTestHex = value; Save(); } }
    public bool luaTestHexRev { get => _luaTestHexRev; set { _luaTestHexRev = value; Save(); } }

    // Quick list names
    private string _quickListName0 = "未命名0"; public string quickListName0 { get => _quickListName0; set { _quickListName0 = value; Save(); } }
    private string _quickListName1 = "未命名1"; public string quickListName1 { get => _quickListName1; set { _quickListName1 = value; Save(); } }
    private string _quickListName2 = "未命名2"; public string quickListName2 { get => _quickListName2; set { _quickListName2 = value; Save(); } }
    private string _quickListName3 = "未命名3"; public string quickListName3 { get => _quickListName3; set { _quickListName3 = value; Save(); } }
    private string _quickListName4 = "未命名4"; public string quickListName4 { get => _quickListName4; set { _quickListName4 = value; Save(); } }
    private string _quickListName5 = "未命名5"; public string quickListName5 { get => _quickListName5; set { _quickListName5 = value; Save(); } }
    private string _quickListName6 = "未命名6"; public string quickListName6 { get => _quickListName6; set { _quickListName6 = value; Save(); } }
    private string _quickListName7 = "未命名7"; public string quickListName7 { get => _quickListName7; set { _quickListName7 = value; Save(); } }
    private string _quickListName8 = "未命名8"; public string quickListName8 { get => _quickListName8; set { _quickListName8 = value; Save(); } }
    private string _quickListName9 = "未命名9"; public string quickListName9 { get => _quickListName9; set { _quickListName9 = value; Save(); } }

    public List<ToSendData> quickSend
    {
        get
        {
            if (_quickSendSelect < 0 || _quickSendSelect > 10) return new();
            while (quickSendList.Count <= 10) quickSendList.Add(new());
            return quickSendList[_quickSendSelect];
        }
        set
        {
            if (_quickSendSelect < 0 || _quickSendSelect > 10) return;
            while (quickSendList.Count <= 10) quickSendList.Add(new());
            quickSendList[_quickSendSelect] = value;
            Save();
        }
    }

    public string GetQuickListNameNow() => _quickSendSelect switch
    {
        0 => quickListName0, 1 => quickListName1, 2 => quickListName2, 3 => quickListName3, 4 => quickListName4,
        5 => quickListName5, 6 => quickListName6, 7 => quickListName7, 8 => quickListName8, 9 => quickListName9,
        _ => "??",
    };

    public void SetQuickListNameNow(string name)
    {
        switch (_quickSendSelect)
        {
            case 0: quickListName0 = name; break;
            case 1: quickListName1 = name; break;
            case 2: quickListName2 = name; break;
            case 3: quickListName3 = name; break;
            case 4: quickListName4 = name; break;
            case 5: quickListName5 = name; break;
            case 6: quickListName6 = name; break;
            case 7: quickListName7 = name; break;
            case 8: quickListName8 = name; break;
            case 9: quickListName9 = name; break;
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(PlatformHelper.ProfilePath + "settings.json", JsonConvert.SerializeObject(this));
        }
        catch { }
    }

    private static void ShowMessage(string msg)
    {
        PlatformHelper.ShowMessage(msg);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
