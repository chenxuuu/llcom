using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace llcom.Model
{
    class Settings
    {
        private string _dataToSend = Properties.Settings.Default.dataToSend;
        private int _baudRate = Properties.Settings.Default.BaudRate;
        private bool _autoReconnect = Properties.Settings.Default.autoReconnect;
        private bool _autoSaveLog = Properties.Settings.Default.autoSaveLog;
        private bool _showHex = Properties.Settings.Default.showHex;
        private int _parity = Properties.Settings.Default.parity;
        private int _timeout = Properties.Settings.Default.timeout;
        private int _dataBits = Properties.Settings.Default.dataBits;
        private int _stopBit = Properties.Settings.Default.stopBit;
        private string _sendScript = Properties.Settings.Default.sendScript;
        private string _runScript = Properties.Settings.Default.runScript;
        private bool _topmost = Properties.Settings.Default.topmost;

        public string dataToSend
        {
            get
            {
                return _dataToSend;
            }
            set
            {
                _dataToSend = value;
                Properties.Settings.Default.dataToSend = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.BaudRate = value;
                Tools.Global.uart.serial.BaudRate = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.autoReconnect = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.autoSaveLog = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.showHex = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.parity = value;
                Tools.Global.uart.serial.Parity = (Parity)value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.timeout = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.dataBits = value;
                Tools.Global.uart.serial.DataBits = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.stopBit = value;
                Tools.Global.uart.serial.StopBits = (StopBits)value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.sendScript = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.runScript = value;
                Properties.Settings.Default.Save();
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
                Properties.Settings.Default.topmost = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
