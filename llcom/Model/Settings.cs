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
        private bool _showHex = true;
        private bool _showSend = true;
        private int _parity = 0;
        private int _timeout = 50;
        private int _dataBits = 8;
        private int _stopBit = 1;
        private string _sendScript = "默认";
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
                if (_quickSendSelect == -1)
                    return new List<ToSendData>();
                if (quickSendList.Count == 0)
                {
                    for (var i = 0; i < 10; i++)
                        quickSendList.Add(new List<ToSendData>());
                }
                return quickSendList[_quickSendSelect];
            }
            set
            {
                if (_quickSendSelect == -1)
                    return;
                if (quickSendList.Count == 0)
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
                _baudRate = value;
                Tools.Global.uart.serial.BaudRate = value;
                Save();
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

        public bool showHex
        {
            get
            {
                return _showHex;
            }
            set
            {
                _showHex = value;
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
                _parity = value;
                Tools.Global.uart.serial.Parity = (Parity)value;
                Save();
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
                _dataBits = value;
                Tools.Global.uart.serial.DataBits = value;
                Save();
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
                _stopBit = value;
                Tools.Global.uart.serial.StopBits = (StopBits)value;
                Save();
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
    }
}
