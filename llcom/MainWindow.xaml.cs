using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using LibUsbDotNet.DeviceNotify;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace llcom
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        ObservableCollection<ToSendData> items = new ObservableCollection<ToSendData>();
        sentCount sentCount = new sentCount();
        private static IDeviceNotifier usbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
        ScrollViewer sv;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //初始化所有数据
            Tools.Global.Initial();

            //重写关闭窗口代码
            this.Closing += MainWindow_Closing;

            //窗口置顶事件
            Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);

            //收发统计数据绑定
            sentCount.sent = 0;
            sentCount.received = 0;
            bottomStatusBar.DataContext = sentCount;

            //usb刷新时触发
            usbDeviceNotifier.Enabled = true;
            usbDeviceNotifier.OnDeviceNotify += UsbDeviceNotifier_OnDeviceNotify; ;

            //接收到、发送数据成功回调
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
            Tools.Global.uart.UartDataSent += Uart_UartDataSent;

            sv = uartDataFlowDocument.Template.FindName("PART_ContentHost", uartDataFlowDocument) as ScrollViewer;

            Tools.Global.uart.serial.PortName = "COM10";
            Tools.Global.uart.serial.Open();

            toSendList.ItemsSource = items;

            items.Add(new ToSendData() { id = 1, text = "AT", hex = false });
            items.Add(new ToSendData() { id = 2, text = "ATI", hex = false });
            items.Add(new ToSendData() { id = 3, text = "AT+CREG?", hex = false });
            items.Add(new ToSendData() { id = 4, text = "AT+CGATT?", hex = false });
            items.Add(new ToSendData() { id = 5, text = "AT+CIPSEND=2,0", hex = false });
            items.Add(new ToSendData() { id = 6, text = "AA BB CC DD", hex = true });
            items.Add(new ToSendData() { id = 7, text = "11 22 66 22 44", hex = true });

            

            //快速搜索
            textEditor.TextArea.DefaultInputHandler.NestedInputHandlers.Add(
                new SearchInputHandler(textEditor.TextArea));
            string name = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + ".Lua.xshd";
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (System.IO.Stream s = assembly.GetManifestResourceStream(name))
            {
                using (XmlTextReader reader = new XmlTextReader(s))
                {
                    var xshd = HighlightingLoader.LoadXshd(reader);
                    textEditor.SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                }
            }

        }

        private void Uart_UartDataSent(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                addUartLog(sender as string, true);
            }));
        }

        private void Uart_UartDataRecived(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                addUartLog(sender as string, false);
            }));
        }

        private void UsbDeviceNotifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// 响应其他代码传来的窗口置顶事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void topEvent(object sender, EventArgs e)
        {
            this.Topmost = (bool)sender;
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Tools.Global.isMainWindowsClosed = true;
            foreach (Window win in App.Current.Windows)
            {
                if (win != this)
                {
                    win.Close();
                }
            }
            e.Cancel = false;//正常关闭
        }

        /// <summary>
        /// 添加串口日志数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="send">true为发送，false为接收</param>
        private void addUartLog(string data, bool send)
        {
            Paragraph p = new Paragraph(new Run(""));

            Span text = new Span(new Run(DateTime.Now.ToString("[yyyy/MM/dd HH:mm:ss.ffff]")));
            text.Foreground = Brushes.DarkSlateGray;
            p.Inlines.Add(text);

            if(send)
                text = new Span(new Run(" ← "));
            else
                text = new Span(new Run(" → "));
            text.Foreground = Brushes.Black;
            text.FontWeight = FontWeights.Bold;
            p.Inlines.Add(text);

            text = new Span(new Run(data));
            if (send)
                text.Foreground = Brushes.DarkRed;
            else
                text.Foreground = Brushes.DarkGreen;
            text.FontSize = 15;
            p.Inlines.Add(text);

            if (!Tools.Global.setting.showHex)
                p.Margin = new Thickness(0,0,0,8);
            uartDataFlowDocument.Document.Blocks.Add(p);

            if (Tools.Global.setting.showHex)
            {
                p = new Paragraph(new Run("HEX:" + Tools.Global.String2Hex(data, " ")));
                p.Foreground = Brushes.DarkGray;
                p.Margin = new Thickness(0, 0, 0, 8);
                uartDataFlowDocument.Document.Blocks.Add(p);
            }

            sv.ScrollToBottom();
        }

        Window settingPage = new SettingWindow();
        private void MoreSettingButton_Click(object sender, RoutedEventArgs e)
        {
            settingPage.Show();
        }


        private void ApiDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(Tools.Global.apiDocumentUrl);
        }

        private void OpenScriptFolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", "user_script_run");
        }

        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            uartDataFlowDocument.Document.Blocks.Clear();
        }

        private void SendDataButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (baudRateComboBox.SelectedItem != null)
            {
                Tools.Global.uart.serial.BaudRate = 
                    int.Parse((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString());
            }
        }

        private void SerialPortsListComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }

    public class ToSendData
    {
        public int id { get; set; }
        public string text { get; set; }
        public bool hex { get; set; }

    }

    //收发数据统计
    public class sentCount
    {
        public int sent { get; set; }
        public int received { get; set; }
    }
}
