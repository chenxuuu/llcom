using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.LuaEnv;
using llcom.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace llcom.ViewModels
{
    /// <summary>
    /// 串口控制区条目：显示名（含设备描述）与端口名（COMx）。
    /// </summary>
    public class PortInfo
    {
        public string DisplayName { get; }
        public string PortName { get; }

        public PortInfo(string displayName)
        {
            DisplayName = displayName;
            var m = Regex.Match(displayName, @"\(COM\d+\)");
            PortName = m.Success ? m.Value.Trim('(', ')') : displayName;
        }

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 波特率下拉条目：Value 为 null 时表示"自定义"入口。
    /// </summary>
    public class BaudRateItem
    {
        public string Display { get; }
        public int? Value { get; }
        public bool IsCustom => Value == null;
        public BaudRateItem(string display, int? value) { Display = display; Value = value; }
        public override string ToString() => Display;
    }

    /// <summary>
    /// 主窗口串口控制区 ViewModel（Step 5）。
    /// 职责：端口列表刷新（WMI + 注册表权威过滤）、打开/关闭串口（含缓存发送与自动重连）、
    /// 波特率选择（含自定义）、数据发送管线（Lua 发送脚本 → extraEnter → SendData）、
    /// 收发计数、USB 热插拔刷新。
    /// 逻辑从 MainWindow code-behind 逐字迁移，行为保持一致。
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly ISerialPortService _uart;

        //串口控制状态
        private bool _isOpeningPort;          //打开串口进行中
        //初始为 true：禁止启动时自动重连（serial.PortName 默认 "COM1"，
        //若本机恰有 COM1 设备，启动刷新会误判为上次端口而自动打开）；
        //用户手动打开串口（OpenPort）后置 false，才允许后续拔插自动重连
        private bool _forcusClosePort = true;
        private byte[] _pendingSendData;      //未开串口时的缓存发送数据
        private bool _refreshLock;            //端口列表刷新锁
        private bool _skipSearch;             //WMI 查询连续失败后跳过
        private int _searchCount;
        private string _lastPortName;         //上次使用的端口名（自动重连/恢复选中用）

        /// <summary>端口下拉列表</summary>
        public ObservableCollection<PortInfo> Ports { get; } = new ObservableCollection<PortInfo>();
        /// <summary>波特率下拉列表（末尾为"自定义"入口）</summary>
        public ObservableCollection<BaudRateItem> BaudRates { get; } = new ObservableCollection<BaudRateItem>();

        [ObservableProperty] private PortInfo? _selectedPort;
        [ObservableProperty] private BaudRateItem? _selectedBaudRate;
        [ObservableProperty] private bool _isPortOpen;
        [ObservableProperty] private bool _isPortComboEnabled = true;
        [ObservableProperty] private bool _canOpenClosePort;
        [ObservableProperty] private string _statusText = "";
        [ObservableProperty] private string _openCloseButtonText = "";

        /// <summary>发送框文本（绑定到 setting.dataToSend 保持持久化）</summary>
        public string SendText
        {
            get => Tools.Global.setting.dataToSend;
            set { if (Tools.Global.setting.dataToSend != value) Tools.Global.setting.dataToSend = value; }
        }

        /// <summary>快捷发送区（10 页列表管理）</summary>
        public QuickSendViewModel QuickSend { get; }

        /// <summary>已发送字节计数（来自设置，仅 UI 通知）</summary>
        public int SentCount => Tools.Global.setting.SentCount;
        /// <summary>已接收字节计数（来自设置，仅 UI 通知）</summary>
        public int ReceivedCount => Tools.Global.setting.ReceivedCount;

        public IRelayCommand RefreshPortsCommand { get; }
        public IRelayCommand OpenClosePortCommand { get; }
        public IRelayCommand SendCommand { get; }

        public MainViewModel() : this(Tools.Global.uart, Tools.Global.setting) { }

        internal MainViewModel(ISerialPortService uart, Model.Settings setting)
        {
            _uart = uart;
            QuickSend = new QuickSendViewModel(setting);

            RefreshPortsCommand = new RelayCommand(() => RefreshPorts());
            OpenClosePortCommand = new RelayCommand(OpenClosePort);
            SendCommand = new RelayCommand(SendData);

            //波特率列表初始化（预设 + 自定义入口）
            foreach (var rate in new[] { 110, 330, 600, 1200, 2400, 4800, 9600, 14400, 19200, 38400, 56000,
                                         57600, 115200, 128000, 230400, 256000, 460800, 500000, 128000, 512000,
                                         600000, 750000, 921600, 1000000, 1500000, 2000000, 3000000 })
            {
                BaudRates.Add(new BaudRateItem(rate.ToString(), rate));
            }
            BaudRates.Add(CustomRateEntry);
            //选中当前设置中的波特率；不在预设中则值化自定义入口
            var cur = setting.baudRate;
            SelectedBaudRate = BaudRates.FirstOrDefault(x => x.Value == cur);
            if (SelectedBaudRate == null)
            {
                BaudRates[BaudRates.Count - 1] = new BaudRateItem(cur.ToString(), cur);
                BaudRates.Add(CustomRateEntry);
                SelectedBaudRate = BaudRates[BaudRates.Count - 2];
            }

            //计数变化通知
            setting.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(setting.SentCount)) OnPropertyChanged(nameof(SentCount));
                else if (e.PropertyName == nameof(setting.ReceivedCount)) OnPropertyChanged(nameof(ReceivedCount));
            };

            //初始状态文本
            StatusText = Localize("OpenPort_close");
            OpenCloseButtonText = Localize("OpenPort_open");
        }

        private static readonly BaudRateItem CustomRateEntry = new BaudRateItem(null, null);

        /// <summary>
        /// 取本地化资源字符串（等价于 Window.TryFindResource）
        /// </summary>
        private static string Localize(string key) => Application.Current?.TryFindResource(key) as string ?? "?!";

        /// <summary>
        /// 刷新串口设备列表。
        /// 列表来源：Win32_PnPEntity WMI 查询（含设备描述名），并用注册表权威
        /// SerialPort.GetPortNames() 过滤掉已拔掉的设备（WMI 有延迟/缓存）。
        /// </summary>
        /// <param name="lastPort">需要恢复选中的端口名，null 时用上次使用的端口</param>
        public void RefreshPorts(string lastPort = null)
        {
            if (_refreshLock)
                return;
            _refreshLock = true;
            Ports.Clear();
            var strs = new List<string>();
            _searchCount = 0;
            Task.Run(() =>
            {
                while (!_skipSearch)
                {
                    try
                    {
                        var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
                        var regExp = new Regex("\\(COM\\d+\\)");
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            if ((queryObj["Caption"] != null) && regExp.IsMatch(queryObj["Caption"].ToString()))
                            {
                                strs.Add(queryObj["Caption"].ToString());
                            }
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        if (++_searchCount >= 3)
                        {
                            _skipSearch = true;
                            Tools.MessageBox.Show(ex.Message);
                        }
                        else
                            Task.Delay(500).Wait();
                    }
                }

                // WMI 枚举可能有延迟/缓存（设备刚拔掉时仍返回旧数据），
                // 以注册表权威端口列表 SerialPort.GetPortNames() 为准，过滤掉已拔掉的设备
                try
                {
                    var currentPorts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var p in SerialPort.GetPortNames())
                    {
                        //有些人遇到了微软库的bug，所以需要手动从0x00截断
                        currentPorts.Add(p.IndexOf("\0") > 0 ? p.Substring(0, p.IndexOf("\0")) : p);
                    }
                    strs.RemoveAll(s =>
                    {
                        var m = Regex.Match(s, @"\(COM\d+\)");
                        return m.Success && !currentPorts.Contains(m.Value.Trim('(', ')'));
                    });
                }
                catch { }

                //加上缺少的com口
                try
                {
                    foreach (string p in SerialPort.GetPortNames())
                    {
                        var pp = p;
                        if (p.IndexOf("\0") > 0)
                            pp = p.Substring(0, p.IndexOf("\0"));
                        bool notMatch = true;
                        foreach (string n in strs)
                        {
                            if (n.Contains($"({pp})"))
                            {
                                notMatch = false;
                                break;
                            }
                        }
                        if (notMatch)
                            strs.Add($"Serial Port {pp} ({pp})");
                    }
                }
                catch { }

                Application.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var i in strs)
                        Ports.Add(new PortInfo(i));
                    CanOpenClosePort = Ports.Count >= 1;
                    _refreshLock = false;

                    if (string.IsNullOrEmpty(lastPort))
                        //当前串口对象上设置的端口名（即使串口已断开/被拔掉仍存在），
                        //用于设备重新插回时恢复选中并触发自动重连
                        lastPort = _uart.GetName();
                    //恢复选中：优先上次端口，否则第一个
                    var target = Ports.FirstOrDefault(p => p.PortName == lastPort);
                    SelectedPort = target ?? Ports.FirstOrDefault();
                    if (target != null)
                    {
                        //自动重连：非主动关闭且开启自动重连时尝试重新打开
                        if (!_forcusClosePort && Tools.Global.setting.autoReconnect && !_isOpeningPort)
                        {
                            Task.Run(() =>
                            {
                                _isOpeningPort = true;
                                try
                                {
                                    _uart.Open();
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        OpenCloseButtonText = Localize("OpenPort_close");
                                        IsPortComboEnabled = false;
                                        StatusText = Localize("OpenPort_open");
                                    });
                                }
                                catch
                                {
                                    //MessageBox.Show("串口打开失败！");
                                }
                                _isOpeningPort = false;
                            });
                        }
                    }
                });
            });
        }

        /// <summary>
        /// USB 设备拔插事件处理（由窗口 WndProc 转发调用）
        /// </summary>
        public void OnUsbDeviceChanged()
        {
            if (_uart.IsOpen())
            {
                RefreshPorts(_uart.GetName());
            }
            else
            {
                OpenCloseButtonText = Localize("OpenPort_open");
                IsPortComboEnabled = true;
                StatusText = Localize("OpenPort_close");
                RefreshPorts();
            }
        }

        /// <summary>
        /// 打开/关闭串口（按钮命令）
        /// </summary>
        private void OpenClosePort()
        {
            if (!_uart.IsOpen())
                OpenPort();
            else
                ClosePort();
        }

        /// <summary>
        /// 关闭串口
        /// </summary>
        private void ClosePort()
        {
            try
            {
                _forcusClosePort = true;//不再自动重连
                _lastPortName = _uart.GetName();
                _uart.Close();
            }
            catch
            {
                Tools.MessageBox.Show(Localize("ErrorClosePort"));
            }
            OpenCloseButtonText = Localize("OpenPort_open");
            IsPortComboEnabled = true;
            StatusText = Localize("OpenPort_close");
            RefreshPorts(_lastPortName);
        }

        /// <summary>
        /// 打开串口（异步；成功后若有缓存发送数据则补发）
        /// </summary>
        private void OpenPort()
        {
            if (_isOpeningPort)
                return;
            if (SelectedPort == null)
                return;

            string[] ports;
            try { ports = SerialPort.GetPortNames(); }
            catch { ports = new string[0]; }

            string port = "";
            foreach (var p in ports)
            {
                //有些人遇到了微软库的bug，所以需要手动从0x00截断
                var pp = p;
                if (p.IndexOf("\0") > 0)
                    pp = p.Substring(0, p.IndexOf("\0"));
                if (SelectedPort.DisplayName.Contains($"({pp})"))
                {
                    port = pp;
                    break;
                }
            }

            if (port == "")
                return;

            Task.Run(() =>
            {
                _isOpeningPort = true;
                try
                {
                    _forcusClosePort = false;//不再强制关闭串口
                    _uart.SetName(port);
                    _uart.Open();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        OpenCloseButtonText = Localize("OpenPort_close");
                        IsPortComboEnabled = false;
                        StatusText = Localize("OpenPort_open");
                    });
                    //串口打开成功后补发缓存数据
                    if (_pendingSendData != null)
                    {
                        SendUartData(_pendingSendData);
                        _pendingSendData = null;
                    }
                }
                catch (Exception)
                {
                    //串口打开失败！
                    Tools.MessageBox.Show(Localize("ErrorOpenPort"));
                }
                _isOpeningPort = false;
            });
        }

        /// <summary>
        /// 发送框发送命令（Ctrl+Enter / 发送按钮）
        /// </summary>
        private void SendData()
        {
            Tools.Global.setting.recvScript = MainWindow.recvScriptBackup;
            var data = Tools.Global.GetEncoding().GetBytes(SendText);
            SendUartData(data);
        }

        /// <summary>
        /// 发送串口数据（完整管线：Lua 发送脚本 → extraEnter → SendData → 回显）
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <param name="is_hex">强制 hex 模式标志</param>
        internal void SendUartData(byte[] data, bool? is_hex = null)
        {
            if (!_uart.IsOpen())
            {
                OpenPort();
                _pendingSendData = (byte[])data.Clone();//缓存起来，连上串口后发出去
            }

            if (_uart.IsOpen())
            {
                byte[] dataConvert;
                try
                {
                    dataConvert = LuaEnv.LuaLoader.Run(
                        $"{Tools.Global.setting.sendScript}.lua",
                        new System.Collections.ArrayList
                        {
                            "uartData",
                            is_hex == null ?
                            (Tools.Global.setting.hexSend ? Tools.Global.Hex2Byte(Tools.Global.Byte2String(data)) : data) : data
                        });
                }
                catch (Exception ex)
                {
                    Tools.MessageBox.Show($"{Localize("ErrorScript")}\r\n" + ex.ToString());
                    return;
                }
                try
                {
                    if (Tools.Global.setting.extraEnter)
                    {
                        var temp = dataConvert.ToList();
                        temp.Add(0x0d);
                        temp.Add(0x0a);
                        dataConvert = temp.ToArray();
                    }
                    _uart.SendData(dataConvert, data);
                }
                catch (Exception ex)
                {
                    Tools.MessageBox.Show($"{Localize("ErrorSendFail")}\r\n" + ex.ToString());
                    return;
                }
            }
        }

        /// <summary>
        /// 选中的波特率变化：预设直接应用；"自定义"弹输入框
        /// </summary>
        partial void OnSelectedBaudRateChanged(BaudRateItem? value)
        {
            if (value == null)
                return;
            if (!value.IsCustom)
            {
                Tools.Global.setting.baudRate = value.Value.Value;
            }
            else
            {
                var ret = Tools.InputDialog.OpenDialog(
                    Localize("ShowBaudRate"), "115200", Localize("OtherRate"));
                if (ret.Item1 && int.TryParse(ret.Item2, out var br))
                {
                    //把"自定义"入口项替换为实际波特率并选中，末尾补充新的自定义入口
                    var idx = BaudRates.IndexOf(value);
                    var item = new BaudRateItem(br.ToString(), br);
                    BaudRates[idx] = item;
                    if (!BaudRates.Any(x => x.IsCustom))
                        BaudRates.Add(CustomRateEntry);
                    SelectedBaudRate = item;
                    Tools.Global.setting.baudRate = br;
                }
                else
                {
                    //取消/输入非法：提示并回到当前实际波特率对应项
                    Tools.MessageBox.Show(Localize("OtherRateFail"));
                    SelectedBaudRate = BaudRates.FirstOrDefault(x => x.Value == Tools.Global.setting.baudRate)
                        ?? BaudRates.First(x => x.IsCustom);
                }
            }
        }
    }
}
