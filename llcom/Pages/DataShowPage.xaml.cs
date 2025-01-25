using llcom.Tools;
using ScottPlot.Drawing.Colormaps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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

        /// <summary>
        /// 禁止自动滚动？
        /// </summary>
        public bool LockLog { get; set; } = false;
        private bool loaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            //添加待显示数据到缓冲区
            Tools.Logger.DataShowTask += Logger_DataShowTask;
            Tools.Logger.DataClearEvent += (xx,x) =>
            {
                MainList.Items.Clear();
                MainTextBox.Clear();
            };
            LockIcon.DataContext = this;
            UnLockIcon.DataContext = this;
            UnLockText.DataContext = this;
            RTSCheckBox.DataContext = this;
            DTRCheckBox.DataContext = this;
            Rts = false;
            Dtr = true;

            MainList.DataContext = Tools.Global.setting;
            MainTextBox.DataContext = Tools.Global.setting;

            HEXBox.DataContext = Tools.Global.setting;
            HexSendCheckBox.DataContext = Tools.Global.setting;
            this.ExtraEnterCheckBox.DataContext = Tools.Global.setting;
            DisableLogCheckBox.DataContext = Tools.Global.setting;
            EnableSymbolCheckBox.DataContext = Tools.Global.setting;

            lastPackShowMode = Tools.Global.setting.timeout >= 0;
            MainListScrollViewer.Visibility = lastPackShowMode ? Visibility.Visible : Visibility.Collapsed;
            MainTextBox.Visibility = lastPackShowMode ? Visibility.Collapsed : Visibility.Visible;
        }

        //记录一下上次是不是分包显示的
        bool lastPackShowMode = false;
        private void Logger_DataShowTask(object sender, Tools.DataShow e)
        {
            //先判断下要不要清空
            var needPack = Tools.Global.setting.timeout >= 0;
            if (lastPackShowMode != needPack)
            {
                lastPackShowMode = needPack;
                DoInvoke(() =>
                {
                    MainList.Items.Clear();
                    MainTextBox.Clear();
                    MainListScrollViewer.Visibility = needPack ? Visibility.Visible : Visibility.Collapsed;
                    MainTextBox.Visibility = needPack ? Visibility.Collapsed : Visibility.Visible;
                });
            }

            //如果不开回显，就别打印
            if(!Tools.Global.setting.showSend && e is DataShowPara para && para.send)
                return;

            //显示到列表
            if (!needPack && e is not DataShowRaw)//不分包模式
            {
                var DataText = Tools.Global.setting.showHexFormat switch
                {
                    2 => Tools.Global.Byte2Hex(e.data, " ", e.data.Length) + " ",
                    _ => Tools.Global.Byte2Readable(e.data, e.data.Length),
                };
                DoInvoke(() =>
                {
                    MainTextBox.AppendText(DataText);
                    if (!LockLog)
                        MainTextBox.ScrollToEnd();
                });
            }
            else//分包模式
            {
                var data = e is DataShowRaw ? 
                    new DataShow((e as DataShowRaw).title, e.data, e.time, (e as DataShowRaw).color) :
                    new DataShow(e.data, e.time, (e as DataShowPara).send);
                if (data != null)
                {
                    DoInvoke(() =>
                    {
                        MainList.Items.Add(data);
                        if (!LockLog)
                            MainListScrollViewer.ScrollToEnd();
                    });
                }
            }
        }

        private bool DoInvoke(Action action)
        {
            if (Tools.Global.isMainWindowsClosed)
                return false;
            Dispatcher.Invoke(action);
            return true;
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

        /// <summary>
        /// 显示要用到的数据结构
        /// </summary>
        public class DataShow
        {
            public string TimeText { get; set; }
            public string ArrowText { get; set; }
            public string DataText { get; set; }
            public SolidColorBrush DataTextColor { get; set; }
            public string RawTitle { get; set; }
            /// <summary>
            /// 前面要加换行符
            /// </summary>
            public string RawText { get; set; }
            public SolidColorBrush RawTextColor { get; set; }
            /// <summary>
            /// 前面要加换行符
            /// </summary>
            public string HexText { get; set; }
            public SolidColorBrush HexTextColor { get; set; }


            public DataShow(byte[] data, DateTime time, bool sent)
            {
                if (data == null || data.Count() == 0)
                    return;
                byte[] temp = data.ToArray();
                //转换下接收数据
                if (!sent)
                {
                    try
                    {
                        temp = LuaEnv.LuaLoader.Run(
                            $"{Tools.Global.setting.recvScript}.lua",
                            new System.Collections.ArrayList { "uartData", temp },
                            "user_script_recv_convert/");
                    }
                    catch (Exception ex)
                    {
                        Tools.MessageBox.Show($"receive convert lua script error\r\n" + ex.ToString());
                        return;
                    }
                    if (temp == null)
                        return;
                }

                TimeText = time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");
                ArrowText = sent ? " ← " : " → ";
                DataTextColor = sent ? Brushes.DarkRed : Brushes.DarkGreen;
                HexTextColor = sent ? Brushes.IndianRed : Brushes.ForestGreen;

                var len = temp.Length;
                //主要数据
                if (temp != null && temp.Length > 0)
                {
                    DataText = Tools.Global.setting.showHexFormat switch
                    {
                        2 => Tools.Global.Byte2Hex(temp, " ", len),
                        _ => Tools.Global.Byte2Readable(temp, len),
                    };
                    //同时显示模式时，才显示小字hex
                    if (Tools.Global.setting.showHexFormat == 0)
                        HexText = "\nHex: " + Tools.Global.Byte2Hex(temp, " ", len);
                }
            }

            public DataShow(string title, byte[] data, DateTime time, SolidColorBrush color)
            {
                byte[] temp = data.ToArray();

                TimeText = time.ToString("[yyyy/MM/dd HH:mm:ss.fff]");

                var len = temp.Length;
                //主要数据
                if (temp != null && temp.Length > 0)
                {
                    RawText = "\n" + Tools.Global.setting.showHexFormat switch
                    {
                        2 => Tools.Global.Byte2Hex(temp, " ", len),
                        _ => Tools.Global.Byte2Readable(temp, len),
                    };
                    //同时显示模式时，才显示小字hex
                    if (Tools.Global.setting.showHexFormat == 0)
                        HexText = "\nHex: " + Tools.Global.Byte2Hex(temp, " ", len);
                }

                RawTitle = title;
                RawTextColor = color;
                HexTextColor = color;
            }
        }
    }
}
