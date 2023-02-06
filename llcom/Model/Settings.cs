using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Model
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    class Settings
    {
        public event EventHandler MainWindowTop;
        private string _dataToSend = "uart data";
        private int _baudRate = 115200;
        private bool _autoReconnect = true;
        private bool _autoSaveLog = true;
        private int _showHexFormat = 0;
        private bool _hexSend = false;
        private bool _showSend = true;
        private int _parity = 0;
        private int _timeout = 50;
        private int _dataBits = 8;
        private int _stopBit = 1;
        private string _sendScript = "default";
        private string _recvScript = "default";
        private string _runScript = "example";
        private bool _topmost = false;
        public List<List<ToSendData>> quickSendList = new List<List<ToSendData>>();
        private int _quickSendSelect = -1;
        private bool _bitDelay = true;
        private bool _autoUpdate = true;
        private uint _maxLength = 10240;
        private string _language = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
        private int _encoding = 65001;
        private bool _terminal = true;
        private bool _extraEnter = false;
        private bool _enableSymbol = true;

        //窗口大小与位置
        private double _windowTop = 0;
        public double windowTop { get { return _windowTop; } set { _windowTop = value; Save(); } }
        private double _windowLeft = 0;
        public double windowLeft { get { return _windowLeft; } set { _windowLeft = value; Save(); } }
        private double _windowWidth = 0;
        public double windowWidth { get { return _windowWidth; } set { _windowWidth = value; Save(); } }
        private double _windowHeight = 0;
        public double windowHeight { get { return _windowHeight; } set { _windowHeight = value; Save(); } }

        public int SentCount { get; set; } = 0;
        public int ReceivedCount { get; set; } = 0;

        /// <summary>
        /// 保存配置
        /// </summary>
        private void Save()
        {
            File.WriteAllText(Tools.Global.ProfilePath+"settings.json", JsonConvert.SerializeObject(this));
        }

        /// <summary>
        /// 串口接收每包最大长度
        /// </summary>
        public uint maxLength
        {
            get
            {
                return _maxLength;
            }
            set
            {
                _maxLength = value;
                Save();
            }
        }

        /// <summary>
        /// 当前选中的快捷发送列表数据
        /// </summary>
        public List<ToSendData> quickSend
        {
            get
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return new List<ToSendData>();
                if (quickSendList.Count <= 10)
                {
                    for (var i = 0; i < 10; i++)
                        quickSendList.Add(new List<ToSendData>());
                }
                return quickSendList[_quickSendSelect];
            }
            set
            {
                if (_quickSendSelect < 0 || _quickSendSelect > 10)
                    return;
                if (quickSendList.Count <= 10)
                {
                    for (var i = 0; i < 10; i++)
                        quickSendList.Add(new List<ToSendData>());
                }
                quickSendList[_quickSendSelect] = value;
                Save();
            }
        }

        /// <summary>
        /// 当前选中的快速发送列表编号
        /// </summary>
        public int quickSendSelect
        {
            get
            {
                return _quickSendSelect;
            }
            set
            {
                _quickSendSelect = value;
                Save();
            }
        }

        /// <summary>
        /// 是否开启自动升级
        /// </summary>
        public bool autoUpdate
        {
            get
            {
                return _autoUpdate;
            }
            set
            {
                _autoUpdate = value;
                Save();
            }
        }

        public bool bitDelay
        {
            get
            {
                return _bitDelay;
            }
            set
            {
                _bitDelay = value;
                Save();
            }
        }

        public string dataToSend
        {
            get
            {
                return _dataToSend;
            }
            set
            {
                _dataToSend = value;
                Save();
            }
        }
        public int baudRate
        {
            get
            {
                return _baudRate;
            }
            set
            {
                try
                {
                    Tools.Global.uart.serial.BaudRate = value;
                    _baudRate = value;
                    Save();
                }
                catch(Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
            }
        }

        public bool autoReconnect
        {
            get
            {
                return _autoReconnect;
            }
            set
            {
                _autoReconnect = value;
                Save();
            }
        }

        public bool autoSaveLog
        {
            get
            {
                return _autoSaveLog;
            }
            set
            {
                _autoSaveLog = value;
                Save();
            }
        }

        /// <summary>
        /// 串口数据显示格式
        /// 0 都显示
        /// 1 只显示字符串
        /// 2 只显示Hex
        /// </summary>
        public int showHexFormat
        {
            get
            {
                return _showHexFormat;
            }
            set
            {
                _showHexFormat = value;
                Save();
            }
        }

        /// <summary>
        /// 主数据发送框是否发hex
        /// </summary>
        public bool hexSend
        {
            get
            {
                return _hexSend;
            }
            set
            {
                _hexSend = value;
                Save();
            }
        }

        public bool showSend
        {
            get
            {
                return _showSend;
            }
            set
            {
                _showSend = value;
                Save();
            }
        }

        public int parity
        {
            get
            {
                return _parity;
            }
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
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
            }
        }

        public int timeout
        {
            get
            {
                return _timeout;
            }
            set
            {
                _timeout = value;
                Save();
            }
        }

        public int dataBits
        {
            get
            {
                return _dataBits;
            }
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
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
            }
        }

        public int stopBit
        {
            get
            {
                return _stopBit;
            }
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
                    System.Windows.Forms.MessageBox.Show(e.Message);
                }
            }
        }

        public string sendScript
        {
            get
            {
                return _sendScript;
            }
            set
            {
                _sendScript = value;
                Save();
            }
        }

        public string recvScript
        {
            get
            {
                return _recvScript;
            }
            set
            {
                _recvScript = value;
                Save();
            }
        }

        public string runScript
        {
            get
            {
                return _runScript;
            }
            set
            {
                _runScript = value;
                Save();
            }
        }

        public bool topmost
        {
            get
            {
                return _topmost;
            }
            set
            {
                _topmost = value;
                try
                {
                    MainWindowTop(value, EventArgs.Empty);
                }
                catch { }
                Save();
            }
        }

        public bool terminal
        {
            get
            {
                return _terminal;
            }
            set
            {
                _terminal = value;
                Save();
            }
        }

        public string language
        {
            get
            {
                return _language;
            }
            set
            {
                _language = value;
                Tools.Global.LoadLanguageFile(value);
                Save();
            }
        }

        public int encoding
        {
            get
            {
                return _encoding;
            }
            set
            {
                try
                {
                    Encoding.GetEncoding(value);
                    _encoding = value;
                    Save();
                }
                catch { }//获取出错说明编码不对
            }
        }

        public bool extraEnter
        {
            get
            {
                return _extraEnter;
            }
            set
            {
                _extraEnter = value;
                Save();
            }
        }

        public bool DisableLog { get; set; } = false;

        public bool EnableSymbol
        {
            get => _enableSymbol;
            set
            {
                _enableSymbol = value;
                Save();
            }
        }

        private string _mqttServer = "broker.emqx.io";
        private int _mqttPort = 1883;
        private string _mqttClientID = Guid.NewGuid().ToString();
        private bool _mqttTLS = false;
        private bool _mqttTLSCert = false;
        private string _mqttTLSCertCaPath = "";
        private string _mqttTLSCertClientPath = "";
        private string _mqttTLSCertClientPassword = "";
        private bool _mqttWs = false;
        private string _mqttWsPath = "/mqtt";
        private string _mqttUser = "user";
        private string _mqttPassword = "password";
        private int _mqttKeepAlive = 120;
        private bool _mqttCleanSession = false;
        private string _mqttPublishTopic = "your/publish/topic";
        private string _mqttSubscribeTopic = "your/subcribe/topic";
        public string mqttServer { get { return _mqttServer; } set { _mqttServer = value; Save(); } }
        public int mqttPort { get { return _mqttPort; } set { _mqttPort = value; Save(); } }
        public string mqttClientID { get { return _mqttClientID; } set { _mqttClientID = value; Save(); } }
        public bool mqttTLS { get { return _mqttTLS; } set { _mqttTLS = value; Save(); } }
        public bool mqttTLSCert { get { return _mqttTLSCert; } set { _mqttTLSCert = value; Save(); } }
        public string mqttTLSCertCaPath { get { return _mqttTLSCertCaPath; } set { _mqttTLSCertCaPath = value; Save(); } }
        public string mqttTLSCertClientPath { get { return _mqttTLSCertClientPath; } set { _mqttTLSCertClientPath = value; Save(); } }
        public string mqttTLSCertClientPassword { get { return _mqttTLSCertClientPassword; } set { _mqttTLSCertClientPassword = value; Save(); } }
        public bool mqttWs { get { return _mqttWs; } set { _mqttWs = value; Save(); } }
        public string mqttWsPath { get { return _mqttWsPath; } set { _mqttWsPath = value; Save(); } }
        public string mqttUser { get { return _mqttUser; } set { _mqttUser = value; Save(); } }
        public string mqttPassword { get { return _mqttPassword; } set { _mqttPassword = value; Save(); } }
        public int mqttKeepAlive { get { return _mqttKeepAlive; } set { _mqttKeepAlive = value; Save(); } }
        public bool mqttCleanSession { get { return _mqttCleanSession; } set { _mqttCleanSession = value; Save(); } }
        public string mqttPublishTopic { get { return _mqttPublishTopic; } set { _mqttPublishTopic = value; Save(); } }
        public string mqttSubscribeTopic { get { return _mqttSubscribeTopic; } set { _mqttSubscribeTopic = value; Save(); } }


        private string _quickListName0 = "未命名0";
        public string quickListName0 { get { return _quickListName0; } set { _quickListName0 = value; Save(); } }

        private string _quickListName1 = "未命名1";
        public string quickListName1 { get { return _quickListName1; } set { _quickListName1 = value; Save(); } }

        private string _quickListName2 = "未命名2";
        public string quickListName2 { get { return _quickListName2; } set { _quickListName2 = value; Save(); } }

        private string _quickListName3 = "未命名3";
        public string quickListName3 { get { return _quickListName3; } set { _quickListName3 = value; Save(); } }

        private string _quickListName4 = "未命名4";
        public string quickListName4 { get { return _quickListName4; } set { _quickListName4 = value; Save(); } }

        private string _quickListName5 = "未命名5";
        public string quickListName5 { get { return _quickListName5; } set { _quickListName5 = value; Save(); } }

        private string _quickListName6 = "未命名6";
        public string quickListName6 { get { return _quickListName6; } set { _quickListName6 = value; Save(); } }

        private string _quickListName7 = "未命名7";
        public string quickListName7 { get { return _quickListName7; } set { _quickListName7 = value; Save(); } }

        private string _quickListName8 = "未命名8";
        public string quickListName8 { get { return _quickListName8; } set { _quickListName8 = value; Save(); } }

        private string _quickListName9 = "未命名9";
        public string quickListName9 { get { return _quickListName9; } set { _quickListName9 = value; Save(); } }

        public string GetQuickListNameNow()
        {
            return _quickSendSelect switch
            {
                0 => quickListName0,
                1 => quickListName1,
                2 => quickListName2,
                3 => quickListName3,
                4 => quickListName4,
                5 => quickListName5,
                6 => quickListName6,
                7 => quickListName7,
                8 => quickListName8,
                9 => quickListName9,
                _ => "??",
            };
        }
        public void SetQuickListNameNow(string name)
        {
            switch (_quickSendSelect)
            {
                case 0:
                    quickListName0 = name;
                    break;
                case 1:
                    quickListName1 = name;
                    break;
                case 2:
                    quickListName2 = name;
                    break;
                case 3:
                    quickListName3 = name;
                    break;
                case 4:
                    quickListName4 = name;
                    break;
                case 5:
                    quickListName5 = name;
                    break;
                case 6:
                    quickListName6 = name;
                    break;
                case 7:
                    quickListName7 = name;
                    break;
                case 8:
                    quickListName8 = name;
                    break;
                case 9:
                    quickListName9 = name;
                    break;
                default:
                    break;
            }
        }




        private string _tcpClientServer = "qq.com";
        private int _tcpClientPort = 80;
        private int _tcpClientProtocolType = 0;
        public string tcpClientServer { get { return _tcpClientServer; } set { _tcpClientServer = value; Save(); } }
        public int tcpClientPort { get { return _tcpClientPort; } set { _tcpClientPort = value; Save(); } }
        public int tcpClientProtocolType { get { return _tcpClientProtocolType; } set { _tcpClientProtocolType = value; Save(); } }


        private int _tcpServerPort = 2333;
        public int tcpServerPort { get { return _tcpServerPort; } set { _tcpServerPort = value; Save(); } }

        private int _udpServerPort = 2333;
        public int udpServerPort { get { return _udpServerPort; } set { _udpServerPort = value; Save(); } }
    }
}
