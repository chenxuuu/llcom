using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using llcom.Tools;

namespace llcom.ViewModels;

    /// <summary>
    /// 收发数据区 ViewModel（Step 8）。
    /// 职责：订阅 Logger.DataShowTask，将串口/TCP/MQTT 等通道数据分发显示——
    /// 分包模式（timeout≥0）写入 ObservableCollection<DataShowItem>（绑定列表，虚拟化），
    /// 流式模式（timeout&lt;0）追加到流式文本框（经 SetTextBridge 桥接，保持增量追加性能）。
    /// 逻辑从 DataShowPage code-behind 迁移，行为保持一致。
    /// </summary>
    public partial class DataShowViewModel : ObservableObject
    {
        /// <summary>全局设置（供 XAML 绑定各显示开关）</summary>
        public Model.Settings Setting { get; }

        /// <summary>分包模式显示条目（绑定 MainList）</summary>
        public ObservableCollection<Model.DataShowItem> Items { get; } = new ObservableCollection<Model.DataShowItem>();

        /// <summary>锁定滚动（绑定锁定按钮）</summary>
        [ObservableProperty] private bool _lockLog;

        /// <summary>RTS 信号线（绑定复选框，转发到串口服务）</summary>
        public bool Rts
        {
            get => Tools.Global.uart.Rts;
            set => Tools.Global.uart.Rts = value;
        }

        /// <summary>DTR 信号线（绑定复选框，转发到串口服务）</summary>
        public bool Dtr
        {
            get => Tools.Global.uart.Dtr;
            set => Tools.Global.uart.Dtr = value;
        }

        //流式模式文本桥（AvalonEdit/TextBox 增量追加保持性能）
        private Action<string> _appendStreamBridge = _ => { };
        private Action _clearStreamBridge = () => { };
        private readonly StringBuilder _streamBuffer = new StringBuilder();

        //当前显示模式（分包/流式）切换标记
        private bool _lastPackShowMode = true;
        /// <summary>分包模式变化事件（切换 MainList/MainTextBox 显示）</summary>
        public event Action<bool> PackModeChanged;
        /// <summary>分包模式新增条目后触发（View 据此滚动到底部）</summary>
        public event Action ScrollRequested;

        public DataShowViewModel(Model.Settings setting)
        {
            Setting = setting;
            Tools.Logger.DataShowTask += Logger_DataShowTask;
            Tools.Logger.DataClearEvent += (_, _) => Clear();
            Rts = false;
            Dtr = true;
            _lastPackShowMode = setting.timeout >= 0;
        }

        /// <summary>
        /// 由 View 接入流式文本框的追加/清空（TextBox.AppendText 增量追加）
        /// </summary>
        public void SetTextBridge(Action<string> append, Action clear)
        {
            _appendStreamBridge = append;
            _clearStreamBridge = clear;
        }

        /// <summary>清空显示（清空列表 + 流式缓冲 + 流式文本框）</summary>
        public void Clear()
        {
            Items.Clear();
            _streamBuffer.Clear();
            _clearStreamBridge();
        }

        /// <summary>
        /// 保存显示内容到文件（分包模式遍历 Items，流式模式输出缓冲文本）
        /// </summary>
        public void SaveLog(string path)
        {
            var needPack = Setting.timeout >= 0;
            using (var sw = new System.IO.StreamWriter(path, false, Encoding.UTF8))
            {
                if (!needPack)
                {
                    sw.Write(_streamBuffer.ToString());
                }
                else
                {
                    for (int i = 0; i < Items.Count; i++)
                    {
                        var item = Items[i];
                        if (string.IsNullOrEmpty(item.RawTitle))
                            sw.WriteLine(item.TimeText + (item.ArrowText == " ← " ? " [send] " : " [recv] ") + item.DataText);
                        else
                            sw.WriteLine(item.TimeText + " [" + item.RawTitle + "] " + item.RawText);
                    }
                }
                sw.Flush();
            }
        }

        /// <summary>
        /// 数据到达事件处理（Logger.DataShowTask）
        /// </summary>
        private void Logger_DataShowTask(object sender, DataShow e)
        {
            //Logger 事件可能来自后台线程（串口接收线程），切到 UI 线程处理
            Application.Current.Dispatcher.Invoke(() =>
            {
                //先判断下要不要清空/切换显示模式
                var needPack = Setting.timeout >= 0;
                if (_lastPackShowMode != needPack)
                {
                    _lastPackShowMode = needPack;
                    Items.Clear();
                    _streamBuffer.Clear();
                    PackModeChanged?.Invoke(needPack);
                }

                //如果不开回显，就别打印
                if (!Setting.showSend && !Setting.showSendRaw && e is DataShowPara para && para.send)
                    return;

                //显示到列表
                if (!needPack && e is not DataShowRaw)//不分包模式
                {
                    var dataText = Setting.showHexFormat switch
                    {
                        2 => Tools.Global.Byte2Hex(e.data, " ", e.data.Length) + " ",
                        _ => Tools.Global.Byte2Readable(e.data, e.data.Length),
                    };
                    _streamBuffer.Append(dataText);
                    _appendStreamBridge(dataText);
                }
                else//分包模式
                {
                    var item = e is DataShowRaw raw
                        ? new Model.DataShowItem(raw.title, e.data, e.time, raw.color)
                        : new Model.DataShowItem(e.data, e.time, (e as DataShowPara).send);
                    if (item != null)
                    {
                        Items.Add(item);
                        ScrollRequested?.Invoke();
                    }
                }
            });
        }
    }
