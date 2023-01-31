using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace llcom.Pages
{
    /// <summary>
    /// DataShowPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class DataShowPage : Page
    {
        public DataShowPage()
        {
            InitializeComponent();
        }

        ScrollViewer sv;
        public bool LockLog { get; set; } = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            //使日志富文本区域滚动可控制
            sv = uartDataFlowDocument.Template.FindName("PART_ContentHost", uartDataFlowDocument) as ScrollViewer;
            sv.CanContentScroll = true;
            Tools.Logger.DataShowEvent += addUartLog;
            Tools.Logger.DataShowRawEvent += Logger_DataShowRawEvent;
            Tools.Logger.DataClearEvent += (xx,x) =>
            {
                uartDataFlowDocument.Document.Blocks.Clear();
            };
            LockIcon.DataContext = this;
            UnLockIcon.DataContext = this;
            UnLockText.DataContext = this;
            RTSCheckBox.DataContext = this;
            DTRCheckBox.DataContext = this;
            Rts = false;
            Dtr = true;
            HEXBox.DataContext = Tools.Global.setting;
            HexSendCheckBox.DataContext = Tools.Global.setting;
            this.ExtraEnterCheckBox.DataContext = Tools.Global.setting;
            DisableLogCheckBox.DataContext = Tools.Global.setting;
            EnableSymbolCheckBox.DataContext = Tools.Global.setting;
        }

        private void Logger_DataShowRawEvent(object sender, Tools.DataShowRaw e)
        {
            uartDataFlowDocument.IsSelectionEnabled = false;

            Paragraph p = new Paragraph(new Run(""));
            Span text = new Span(new Run(DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss.ffff]")));
            text.Foreground = Brushes.DarkSlateGray;
            p.Inlines.Add(text);
            text = new Span(new Run(e.title));
            text.Foreground = Brushes.Black;
            text.FontWeight = FontWeights.Bold;
            p.Inlines.Add(text);
            uartDataFlowDocument.Document.Blocks.Add(p);

            if(e.data.Length > 0)//有数据时才显示信息
            {
                //主要显示数据
                string showData = Tools.Global.setting.showHexFormat switch
                {
                    2 => Tools.Global.Byte2Hex(e.data, " "),
                    _ => Tools.Global.Byte2Readable(e.data)
                };
                p = new Paragraph(new Run(""));
                text = new Span(new Run(showData));
                text.Foreground = e.color;
                text.FontSize = 15;
                p.Inlines.Add(text);
                uartDataFlowDocument.Document.Blocks.Add(p);

                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat == 0)
                {
                    if (e.data.Length > MaxDataLength)
                        p = new Paragraph(new Run("HEX:" + Tools.Global.Byte2Hex(e.data.Skip(0).Take(MaxDataLength).ToArray(), " ")
                        + "\r\nData too long, check log folder for remaining data."));
                    else
                        p = new Paragraph(new Run("HEX:" + Tools.Global.Byte2Hex(e.data, " ")));
                    p.Foreground = e.color;
                    p.Margin = new Thickness(0, 0, 0, 8);
                    uartDataFlowDocument.Document.Blocks.Add(p);
                }
            }
            //条目过多，自动清空
            CheckPacks();
            if (!LockLog)//如果允许拉到最下面
                sv.ScrollToBottom();
            uartDataFlowDocument.IsSelectionEnabled = true;
        }

        //最长一包数据长度，因为太长会把工具卡死机
        private int MaxDataLength
        {
            get
            {
                return (int)Tools.Global.setting.maxLength;
            }
        }

        /// <summary>
        /// 添加串口日志数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="send">true为发送，false为接收</param>
        private void addUartLog(object e,Tools.DataShowPara input)
        {
            byte[] data = input.data;
            if (data.Length == 0)
                return;
            bool send = input.send;
            if (!Tools.Global.setting.showSend && send)
                return;

            uartDataFlowDocument.IsSelectionEnabled = false;

            //转换下接收数据
            if(!send)
            {
                try
                {
                    data = LuaEnv.LuaLoader.Run(
                        $"{Tools.Global.setting.recvScript}.lua",
                        new System.Collections.ArrayList { "uartData", data },
                        "user_script_recv_convert/");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                }
            }

            if (Tools.Global.setting.timeout >= 0)
            {
                Paragraph p = new Paragraph(new Run(""));

                Span text = new Span(new Run(DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss.ffff]")));
                text.Foreground = Brushes.DarkSlateGray;
                p.Inlines.Add(text);

                if (send)
                    text = new Span(new Run(" ← "));
                else
                    text = new Span(new Run(" → "));
                text.Foreground = Brushes.Black;
                text.FontWeight = FontWeights.Bold;
                p.Inlines.Add(text);

                //主要显示数据
                if (data.Length > MaxDataLength)
                {
                    text = new Span(new Run(Tools.Global.setting.showHexFormat switch
                    {
                        2 => Tools.Global.Byte2Hex(data.Skip(0).Take(MaxDataLength).ToArray(), " "),
                        _ => Tools.Global.Byte2Readable(data.Skip(0).Take(MaxDataLength).ToArray())
                    } + "\r\nData too long, check log folder for remaining data."));
                }
                else
                {
                    text = new Span(new Run(Tools.Global.setting.showHexFormat switch
                    {
                        2 => Tools.Global.Byte2Hex(data, " "),
                        _ => Tools.Global.Byte2Readable(data),
                    }));
                }

                if (send)
                    text.Foreground = Brushes.DarkRed;
                else
                    text.Foreground = Brushes.DarkGreen;
                text.FontSize = 15;
                p.Inlines.Add(text);

                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat != 0)
                    p.Margin = new Thickness(0, 0, 0, 8);
                uartDataFlowDocument.Document.Blocks.Add(p);

                //同时显示模式时，才显示小字hex
                if (Tools.Global.setting.showHexFormat == 0)
                {
                    if (data.Length > MaxDataLength)
                        p = new Paragraph(new Run("HEX:" + Tools.Global.Byte2Hex(data.Skip(0).Take(MaxDataLength).ToArray(), " ")
                        + "\r\nData too long, check log folder for remaining data."));
                    else
                        p = new Paragraph(new Run("HEX:" + Tools.Global.Byte2Hex(data, " ")));

                    if (send)
                        p.Foreground = Brushes.IndianRed;
                    else
                        p.Foreground = Brushes.ForestGreen;
                    p.Margin = new Thickness(0, 0, 0, 8);
                    uartDataFlowDocument.Document.Blocks.Add(p);
                }
            }
            else//不分包
            {
                if(uartDataFlowDocument.Document.Blocks.LastBlock == null ||
                   uartDataFlowDocument.Document.Blocks.LastBlock.GetType() != typeof(Paragraph))
                    uartDataFlowDocument.Document.Blocks.Add(new Paragraph(new Run("")));

                //待显示的数据
                string s;
                if (Tools.Global.setting.showHexFormat == 2)
                    s = Tools.Global.Byte2Hex(data, " ");
                else
                    s = Tools.Global.Byte2Readable(data);
                Span text = new Span(new Run(s));
                text.FontSize = 15;
                if (send)
                    text.Foreground = Brushes.DarkRed;
                else
                    text.Foreground = Brushes.DarkGreen;
                (uartDataFlowDocument.Document.Blocks.LastBlock as Paragraph).Inlines.Add(text);
            }

            //条目过多，自动清空
            CheckPacks();

            if (!LockLog)//如果允许拉到最下面
                sv.ScrollToBottom();
            uartDataFlowDocument.IsSelectionEnabled = true;
        }

        int maxDataPack = 10_000;//最大同时显示数据包数，因为太多会把工具卡死机
        /// <summary>
        /// 条目过多，自动清空
        /// </summary>
        private void CheckPacks()
        {
            maxDataPack++;
            if (uartDataFlowDocument.Document.Blocks.Count > maxDataPack)
            {
                maxDataPack = 0;
                uartDataFlowDocument.Document.Blocks.Clear();
                addUartLog(null, new Tools.DataShowPara
                {
                    data = Encoding.Default.GetBytes("Too much packs, please check your log folder for log data."),
                    send = true
                });
            }
        }

        private void uartDataFlowDocument_GotFocus(object sender, RoutedEventArgs e)
        {
            if(Tools.Global.setting.terminal)
                uartDataFlowDocument.BorderThickness = new Thickness(1);
        }

        private void uartDataFlowDocument_LostFocus(object sender, RoutedEventArgs e)
        {
            uartDataFlowDocument.BorderThickness = new Thickness(0);
        }

        private void uartDataFlowDocument_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.TextComposition.Text.Length < 1 || !Tools.Global.setting.terminal)
                return;
            if(Tools.Global.uart.IsOpen())
                try
                {
                    Tools.Global.uart.SendData(Encoding.ASCII.GetBytes(e.TextComposition.Text));
                }
                catch { }
        }

        private void uartDataFlowDocument_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) || 
                !Tools.Global.setting.terminal)
                return;
            if(e.Key >= Key.A && e.Key <= Key.Z && Tools.Global.uart.IsOpen())
                try
                {
                    Tools.Global.uart.SendData(new byte[] {(byte)((int)e.Key - (int)Key.A + 1) });
                }
                catch { }
        }

        private void LockLogButton_Click(object sender, RoutedEventArgs e)
        {
            LockLog = !LockLog;
        }


        public bool Rts {
            get
            {
                return Tools.Global.uart.Rts;
            }
            set
            {
                Tools.Global.uart.Rts = value;
            }
        }
        public bool Dtr
        {
            get
            {
                return Tools.Global.uart.Dtr;
            }
            set
            {
                Tools.Global.uart.Dtr = value;
            }
        }
    }
}
