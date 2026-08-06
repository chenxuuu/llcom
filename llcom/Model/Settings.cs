using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading;

namespace llcom.Model
{
    /// <summary>
    /// 全局设置（分部类主文件）。
    /// 继承 ObservableObject（MVVM Toolkit）替代 PropertyChanged.Fody 自动织入，
    /// 属性保留小写命名以兼容现有 XAML 绑定与 settings.json 格式。
    /// 保存策略：属性 setter 调用 Save()，Save() 为 600ms 防抖合并写盘；
    /// 正常退出（Global.isMainWindowsClosed=true）时由 Global 调用 Flush() 强制落盘。
    /// 领域拆分见各分部文件：
    ///   Settings.Uart.cs —— 串口参数
    ///   Settings.Mqtt.cs —— MQTT 参数
    ///   Settings.TcpUdp.cs —— TCP/UDP 参数
    ///   Settings.QuickSend.cs —— 快捷发送列表
    ///   Settings.Display.cs —— 显示/编码参数
    /// </summary>
    internal partial class Settings : ObservableObject
    {
        public event EventHandler MainWindowTop;

        //窗口大小与位置
        private double _windowTop = 0;
        public double windowTop { get { return _windowTop; } set { _windowTop = value; Save(); } }
        private double _windowLeft = 0;
        public double windowLeft { get { return _windowLeft; } set { _windowLeft = value; Save(); } }
        private double _windowWidth = 0;
        public double windowWidth { get { return _windowWidth; } set { _windowWidth = value; Save(); } }
        private double _windowHeight = 0;
        public double windowHeight { get { return _windowHeight; } set { _windowHeight = value; Save(); } }

        private int _sentCount = 0;
        /// <summary>
        /// 已发送字节计数（仅 UI 通知，不落盘）
        /// </summary>
        public int SentCount
        {
            get => _sentCount;
            set => SetProperty(ref _sentCount, value);
        }

        private int _receivedCount = 0;
        /// <summary>
        /// 已接收字节计数（仅 UI 通知，不落盘）
        /// </summary>
        public int ReceivedCount
        {
            get => _receivedCount;
            set => SetProperty(ref _receivedCount, value);
        }

        private bool _disableLog = false;
        /// <summary>
        /// 是否禁用日志显示（仅运行时状态，不落盘）
        /// </summary>
        public bool DisableLog
        {
            get => _disableLog;
            set => SetProperty(ref _disableLog, value);
        }

        private string _language = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
        public string language
        {
            get => _language;
            set
            {
                if (SetProperty(ref _language, value))
                {
                    Tools.Global.LoadLanguageFile(value);
                    Save();
                }
            }
        }

        // ========== 配置持久化（防抖保存） ==========

        private static readonly object saveLock = new object();
        private static System.Threading.Timer saveTimer = null;
        private const int SaveDelayMs = 600;

        /// <summary>
        /// 保存配置（防抖）：600ms 内的多次修改合并为一次写盘，
        /// 避免拖动窗口/改参数时频繁全量写 settings.json。
        /// </summary>
        private void Save()
        {
            lock (saveLock)
            {
                saveTimer ??= new System.Threading.Timer(_ => Flush(), null, Timeout.Infinite, Timeout.Infinite);
                saveTimer.Change(SaveDelayMs, Timeout.Infinite);
            }
        }

        /// <summary>
        /// 立即写盘。正常退出（窗口关闭）时必须调用，确保防抖队列中的修改不丢失。
        /// 线程安全：可被防抖定时器与退出流程重复调用。
        /// </summary>
        internal void Flush()
        {
            lock (saveLock)
            {
                saveTimer?.Dispose();
                saveTimer = null;
            }
            try
            {
                File.WriteAllText(Tools.Global.ProfilePath + "settings.json", JsonConvert.SerializeObject(this));
            }
            catch
            {
                //写配置失败不阻塞主流程（最坏结果：本次设置未持久化）
            }
        }
    }
}
