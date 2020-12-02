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
        private string _quickData = "{\"data\":[{\"id\":1,\"text\":\"example string\",\"commit\":\"右击更改此处文字\",\"hex\":false},{\"id\":2,\"text\":\"lua可通过接口获取此处数据\",\"hex\":false},{\"id\":3,\"text\":\"aa 01 02 0d 0a\",\"hex\":true},{\"id\":4,\"text\":\"此处数据会被lua处理\",\"hex\":false}]}";
        private bool _bitDelay = true;
        private bool _autoUpdate = true;
        private uint _maxLength = 10240;
        private string _language = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
        private int _encoding = 65001;
        private bool _terminal = true;
        public static List<string> toSendDatas = new List<string>();

        public int SentCount { get; set; } = 0;
        public int ReceivedCount { get; set; } = 0;

        public void UpdateQuickSend()
        {
            toSendDatas.Clear();
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(_quickData);
                foreach (var i in jo["data"])
                {
                    if (i["commit"] == null)
                        i["commit"] = "发送";
                    if ((bool)i["hex"])
                        toSendDatas.Add("H" + (string)i["text"]);
                    else
                        toSendDatas.Add("S" + (string)i["text"]);
                }
            }
            catch
            {
                quickData = "{\"data\":[{\"id\":1,\"text\":\"example string\",\"hex\":false},{\"id\":2,\"text\":\"lua可通过接口获取此处数据\",\"hex\":false},{\"id\":3,\"text\":\"aa 01 02 0d 0a\",\"hex\":true},{\"id\":4,\"text\":\"此处数据会被lua处理\",\"hex\":false}]}";
            }
        }

        /// <summary>
        /// 保存配置
        /// </summary>
        private void Save()
        {
            File.WriteAllText(Tools.Global.ProfilePath+"settings.json", JsonConvert.SerializeObject(this));
        }

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

        public string quickData
        {
            get
            {
                return _quickData;
            }
            set
            {
                _quickData = value;
                Save();

                //更新快捷发送区参数
                UpdateQuickSend();
            }
        }

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
    }
}
