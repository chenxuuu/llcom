using FontAwesome.WPF;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using LibUsbDotNet.DeviceNotify;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Http;
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
using llcom.Model;

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
        ObservableCollection<ToSendData> toSendListItems = new ObservableCollection<ToSendData>();
        private static IDeviceNotifier usbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
        ScrollViewer sv;
        private bool forcusClosePort = true;
        private bool canSaveSendList = true;
        private bool isOpeningPort = false;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //初始化所有数据
            Tools.Global.Initial();

            //重写关闭窗口代码
            this.Closing += MainWindow_Closing;

            //窗口置顶事件
            Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);

            //usb刷新时触发
            usbDeviceNotifier.Enabled = true;
            usbDeviceNotifier.OnDeviceNotify += UsbDeviceNotifier_OnDeviceNotify; ;

            //接收到、发送数据成功回调
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
            Tools.Global.uart.UartDataSent += Uart_UartDataSent;

            //使日志富文本区域滚动可控制
            sv = uartDataFlowDocument.Template.FindName("PART_ContentHost", uartDataFlowDocument) as ScrollViewer;

            //加载初始波特率
            baudRateComboBox.Text = Tools.Global.setting.baudRate.ToString();

            //刷新设备列表
            refreshPortList();

            //绑定数据
            this.toSendDataTextBox.DataContext = Tools.Global.setting;
            toSendList.ItemsSource = toSendListItems;
            this.sentCountTextBlock.DataContext = Tools.Global.setting;
            this.receivedCountTextBlock.DataContext = Tools.Global.setting;

            //初始化快捷发送栏的数据
            canSaveSendList = false;
            ToSendData.DataChanged += SaveSendList;
            try
            {
                JObject jo = (JObject)JsonConvert.DeserializeObject(Tools.Global.setting.quickData);
                foreach(var i in jo["data"])
                {
                    if (i["commit"] == null)
                        i["commit"] = "发送";
                    toSendListItems.Add(new ToSendData() {
                        id = (int)i["id"],
                        text = (string)i["text"],
                        hex = (bool)i["hex"],
                        commit = (string)i["commit"]
                    });
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show("待发送列表，数据损坏，全部清空\r\n" + ex.ToString() + "\r\n" + Tools.Global.setting.quickData);
                Tools.Global.setting.quickData = "{\"data\":[{\"id\":1,\"text\":\"example string\",\"hex\":false},{\"id\":2,\"text\":\"中文默认utf8编码\",\"hex\":false},{\"id\":3,\"text\":\"aa 01 02 0d 0a\",\"hex\":true},{\"id\":4,\"text\":\"此处数据会被lua处理\",\"hex\":false}]}";
            }
            canSaveSendList = true;

            //快速搜索
            SearchPanel.Install(textEditor.TextArea);
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

            //加载上次打开的文件
            loadLuaFile(Tools.Global.setting.runScript);

            //加载lua日志打印事件
            LuaEnv.LuaApis.PrintLuaLog += LuaApis_PrintLuaLog;
            //lua代码出错/结束运行事件
            LuaEnv.LuaRunEnv.LuaRunError += LuaRunEnv_LuaRunError;

            //关于页面
            aboutScrollViewer.ScrollToTop();
            versionTextBlock.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            this.Title += " - " + versionTextBlock.Text;

            //检查更新
            Task.Run(async() =>
            {
                if(!await CheckUpdate("httaps://api.github.com/repos/chenxuuu/llcom/releases/latest", "httaps://github.com/chenxuuu/llcom/releases/latest"))
                {
                    await CheckUpdate("https://gitee.com/api/v5/repos/chenxuuu/llcom/releases/latest", "https://gitee.com/chenxuuu/llcom/releases");
                }
            });
        }

        private async Task<bool> CheckUpdate(string url, string download)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("user-agent", "llcom");
                string data = await client.GetStringAsync(url);
                JObject jo = (JObject)JsonConvert.DeserializeObject(data);
                if (int.Parse(((string)jo["tag_name"]).Replace(".", "")) >
                    int.Parse(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Replace(".", "")))
                {
                    var result = MessageBox.Show($"发现新版本{(string)jo["tag_name"]}，是否前往官网进行更新？\r\n" +
                        $"更新内容：{(string)jo["body"]}",
                        "更新检查",
                        MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(download);
                    }
                }
                return true;
            }
            catch
            {
                //MessageBox.Show(ex.ToString());
                return false;
            }
        }

        private void Uart_UartDataSent(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                addUartLog(sender as byte[], true);
                sentCountTextBlock.Text = (int.Parse(sentCountTextBlock.Text) + (sender as byte[]).Length).ToString();
            }));
        }

        private void Uart_UartDataRecived(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                addUartLog(sender as byte[], false);
                receivedCountTextBlock.Text = (int.Parse(receivedCountTextBlock.Text) + (sender as byte[]).Length).ToString();
            }));
        }

        private bool refreshLock = false;
        /// <summary>
        /// 刷新设备列表
        /// </summary>
        private void refreshPortList()
        {
            if (refreshLock)
                return;
            refreshLock = true;
            serialPortsListComboBox.Items.Clear();
            List<string> strs = new List<string>();
            Task.Run(() =>
            {
                while(true)
                {
                    try
                    {
                        ManagementObjectSearcher searcher =new ManagementObjectSearcher("root\\CIMV2","SELECT * FROM Win32_PnPEntity");
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            if ((queryObj["Caption"] != null) && (queryObj["Caption"].ToString().Contains("(COM")))
                            {
                                strs.Add(queryObj["Caption"].ToString());
                            }
                        }
                        break;
                    }
                    catch
                    {
                        Task.Delay(500).Wait();
                    }
                    //MessageBox.Show("fail了");
                }
                this.Dispatcher.Invoke(new Action(delegate {
                    foreach (string i in strs)
                        serialPortsListComboBox.Items.Add(i);
                    if (strs.Count >= 1)
                    {
                        openClosePortButton.IsEnabled = true;
                        serialPortsListComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        openClosePortButton.IsEnabled = false;
                    }
                    refreshLock = false;


                    //选定上次的com口
                    foreach (string c in serialPortsListComboBox.Items)
                    {
                        if ((c).Contains(Tools.Global.uart.serial.PortName))
                        {
                            serialPortsListComboBox.Text = c;
                            //自动重连，不管结果
                            if (!forcusClosePort && Tools.Global.setting.autoReconnect && !isOpeningPort)
                            {
                                Task.Run(() =>
                                {
                                    isOpeningPort = true;
                                    try
                                    {
                                        Tools.Global.uart.serial.Open();
                                        Dispatcher.Invoke(new Action(delegate
                                        {
                                            openClosePortTextBlock.Text = "关闭";
                                            serialPortsListComboBox.IsEnabled = false;
                                            statusTextBlock.Text = "开启";
                                        }));
                                    }
                                    catch
                                    {
                                        //MessageBox.Show("串口打开失败！");
                                    }
                                    isOpeningPort = false;
                                });
                            }
                            break;
                        }
                    }
                }));
            });
        }
        private void UsbDeviceNotifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (Tools.Global.uart.serial.IsOpen)
            {
                refreshPortList();
                foreach(string c in serialPortsListComboBox.Items)
                {
                    if ((c).Contains(Tools.Global.uart.serial.PortName))
                    {
                        serialPortsListComboBox.Text = c;
                        break;
                    }
                }
            }
            else
            {
                openClosePortTextBlock.Text = "打开";
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = "关闭";
                refreshPortList();
            }
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
            //自动保存脚本
            if (lastLuaFile != "")
                saveLuaFile(lastLuaFile);
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
        private void addUartLog(byte[] data, bool send)
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

            text = new Span(new Run(Tools.Global.Byte2String(data)));
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
                p = new Paragraph(new Run("HEX:" + Tools.Global.Byte2Hex(data," ")));
                if (send)
                    p.Foreground = Brushes.LightPink;
                else
                    p.Foreground = Brushes.LightGreen;
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


        private void openPort()
        {
            if (isOpeningPort)
                return;
            if (serialPortsListComboBox.SelectedItem != null)
            {
                string[] ports = SerialPort.GetPortNames();//获取所有串口列表
                string port = "";//最终串口名
                foreach (string p in ports)//循环查找符合名称串口
                {
                    if ((serialPortsListComboBox.SelectedItem as string).Contains(p))//如果和选中项目匹配
                    {
                        port = p;
                        break;
                    }
                }
                if (port != "")
                {
                    Task.Run(() =>
                    {
                        isOpeningPort = true;
                        try
                        {
                            forcusClosePort = false;//不再强制关闭串口
                            Tools.Global.uart.serial.PortName = port;
                            Tools.Global.uart.serial.Open();
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                openClosePortTextBlock.Text = "关闭";
                                serialPortsListComboBox.IsEnabled = false;
                                statusTextBlock.Text = "开启";
                            }));
                        }
                        catch
                        {
                            MessageBox.Show("串口打开失败！");
                        }
                        isOpeningPort = false;
                    });

                }
            }
        }
        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            if(openClosePortTextBlock.Text == "打开")//打开串口逻辑
            {
                openPort();
            }
            else//关闭串口逻辑
            {
                try
                {
                    forcusClosePort = true;//不再重新开启串口
                    Tools.Global.uart.serial.Close();
                }
                catch
                {
                    MessageBox.Show("串口关闭失败！");
                }
                openClosePortTextBlock.Text = "打开";
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = "关闭";
            }

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            uartDataFlowDocument.Document.Blocks.Clear();
        }

        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (baudRateComboBox.SelectedItem != null)
            {
                Tools.Global.setting.baudRate = 
                    int.Parse((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString());
            }
        }

        /// <summary>
        /// 发串口数据
        /// </summary>
        /// <param name="data"></param>
        private void sendUartData(byte[] data)
        {
            if (!Tools.Global.uart.serial.IsOpen)
                openPort();
            if (Tools.Global.uart.serial.IsOpen)
            {
                string dataConvert;
                try
                {
                    dataConvert = LuaEnv.LuaLoader.Run(
                        $"{Tools.Global.setting.sendScript}.lua",
                        new System.Collections.ArrayList { "uartData", Tools.Global.Byte2Hex(data) });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("处理发送数据的脚本运行错误，请检查发送脚本后再试：\r\n" + ex.ToString());
                    return;
                }
                try
                {
                    Tools.Global.uart.SendData(Tools.Global.Hex2Byte(dataConvert));
                }
                catch(Exception ex)
                {
                    MessageBox.Show("串口数据发送失败！请检查连接！\r\n"+ex.ToString());
                    return;
                }
            }
        }

        private void SendUartData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            sendUartData(Encoding.Default.GetBytes(toSendDataTextBox.Text));
        }

        private void AddSendListButton_Click(object sender, RoutedEventArgs e)
        {
            toSendListItems.Add(new ToSendData() { id = toSendListItems.Count + 1, text = "", hex = false , commit = "发送"});
        }

        private void DeleteSendListButton_Click(object sender, RoutedEventArgs e)
        {
            if (toSendListItems.Count > 0)
            {
                toSendListItems.RemoveAt(toSendListItems.Count - 1);
            }
            SaveSendList(null, EventArgs.Empty);
        }

        private void knowSendDataButton_click(object sender, RoutedEventArgs e)
        {
            ToSendData data = ((Button)sender).Tag as ToSendData;
            if (data.hex)
                sendUartData(Tools.Global.Hex2Byte(data.text));
            else
                sendUartData(Encoding.Default.GetBytes(data.text));
        }

        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToSendData data = ((Button)sender).Tag as ToSendData;
            ((Button)sender).Content = data.commit = data.text;
        }

        public void SaveSendList(object sender, EventArgs e)
        {
            if (!canSaveSendList)
                return;
            var data = new JObject();
            var list = new JArray();
            foreach (ToSendData i in toSendListItems)
            {
                list.Add(new JObject {
                    { "id", i.id },
                    { "text", i.text },
                    { "hex", i.hex },
                    { "commit", i.commit }
                });
            }
            data.Add("data", list);
            Tools.Global.setting.quickData = data.ToString();
        }

        private void NewScriptButton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Visible;
        }

        private void RunScriptButton_Click(object sender, RoutedEventArgs e)
        {
            if (luaFileList.SelectedItem != null && !fileLoading)
            {
                luaLogTextBox.Clear();
                LuaEnv.LuaRunEnv.New($"user_script_run/{luaFileList.SelectedItem as string}.lua");
                luaScriptEditorGrid.Visibility = Visibility.Collapsed;
                luaLogShowGrid.Visibility = Visibility.Visible;
                luaLogPrintable = true;
            }
            LuaEnv.LuaRunEnv.canRun = true;
        }

        private void NewLuaFilebutton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(newLuaFileNameTextBox.Text))
            {
                MessageBox.Show("请输入文件名！");
                return;
            }
            if (File.Exists($"user_script_run/{newLuaFileNameTextBox.Text}.lua"))
            {
                MessageBox.Show("该文件已存在！");
                return;
            }

            try
            {
                File.Create($"user_script_run/{newLuaFileNameTextBox.Text}.lua").Close();
                loadLuaFile(newLuaFileNameTextBox.Text);
            }
            catch
            {
                MessageBox.Show("新建失败，请检查！");
                return;
            }
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        private void NewLuaFileCancelbutton_Click(object sender, RoutedEventArgs e)
        {
            newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
        }

        //重载锁，防止逻辑卡死
        private static bool fileLoading = false;
        //上次打开文件名
        private static string lastLuaFile = "";
        /// <summary>
        /// 加载lua脚本文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void loadLuaFile(string fileName)
        {
            //检查文件是否存在
            if (!File.Exists($"user_script_run/{fileName}.lua"))
            {
                Tools.Global.setting.runScript = "example";
                if (!File.Exists($"user_script_run/{Tools.Global.setting.runScript}.lua"))
                {
                    File.Create($"user_script_run/{Tools.Global.setting.runScript}.lua").Close();
                }
            }
            else
            {
                Tools.Global.setting.runScript = fileName;
            }

            //文件内容显示出来
            textEditor.Text = File.ReadAllText($"user_script_run/{Tools.Global.setting.runScript}.lua");

            //刷新文件列表
            DirectoryInfo luaFileDir = new DirectoryInfo("user_script_run/");
            FileSystemInfo[] luaFiles = luaFileDir.GetFileSystemInfos();
            fileLoading = true;
            luaFileList.Items.Clear();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                FileInfo file = luaFiles[i] as FileInfo;
                //是文件
                if (file != null && file.Name.IndexOf(".lua") == file.Name.Length - (".lua").Length)
                {
                    luaFileList.Items.Add(file.Name.Substring(0, file.Name.Length - 4));
                }
            }
            luaFileList.Text = lastLuaFile = Tools.Global.setting.runScript;
            fileLoading = false;
        }

        /// <summary>
        /// 保存lua文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void saveLuaFile(string fileName)
        {
            File.WriteAllText($"user_script_run/{fileName}.lua", textEditor.Text);
        }

        private void LuaFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (luaFileList.SelectedItem != null && !fileLoading)
            {
                if (lastLuaFile != "")
                    saveLuaFile(lastLuaFile);
                string fileName = luaFileList.SelectedItem as string;
                loadLuaFile(fileName);
            }
        }
        private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            //自动保存脚本
            if (lastLuaFile != "")
                saveLuaFile(lastLuaFile);
        }

        //是否可打印标记
        private bool _luaLogPrintable = true;
        private bool luaLogPrintable
        {
            get
            {
                return _luaLogPrintable;
            }
            set
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    if (value)
                    {
                        pauseLuaPrintButton.ToolTip = "暂停打印";
                        pauseLuaPrintIcon.Icon = FontAwesomeIcon.Pause;
                    }
                    else
                    {
                        pauseLuaPrintButton.ToolTip = "继续打印";
                        pauseLuaPrintIcon.Icon = FontAwesomeIcon.Play;
                    }
                }));
                _luaLogPrintable = value;
            }
        }
        private void LuaApis_PrintLuaLog(object sender, EventArgs e)
        {
            if (luaLogPrintable)
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    luaLogTextBox.AppendText((sender as string) + "\r\n");
                    luaLogTextBox.ScrollToEnd();
                }));
            }
        }

        private void StopLuaButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LuaEnv.LuaRunEnv.isRunning)
            {
                luaLogTextBox.Clear();
                luaScriptEditorGrid.Visibility = Visibility.Visible;
                luaLogShowGrid.Visibility = Visibility.Collapsed;
                luaLogPrintable = true;
            }
            luaLogPrintable = true;
            LuaEnv.LuaRunEnv.StopLua("");
        }

        private void LuaRunEnv_LuaRunError(object sender, EventArgs e)
        {
            luaLogPrintable = true;
        }

        private void PauseLuaPrintButton_Click(object sender, RoutedEventArgs e)
        {
            luaLogPrintable = !luaLogPrintable;
        }

        private void SendLuaScriptButton_Click(object sender, RoutedEventArgs e)
        {
            LuaEnv.LuaRunEnv.RunCommand(runOneLineLuaTextBox.Text);
            //runOneLineLuaTextBox.Clear();
        }

        private void RunOneLineLuaTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
                LuaEnv.LuaRunEnv.RunCommand(runOneLineLuaTextBox.Text);
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void NewissueButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom/issues/new/choose");
        }

        private void ScriptShareButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom/blob/master/scripts");
        }

        private void RefreshPortButton_Click(object sender, RoutedEventArgs e)
        {
            refreshPortList();
        }

        private void ImportSSCOMButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("请选择SSCOM所在目录的“sscom.ini”文件。");
            System.Windows.Forms.OpenFileDialog OpenFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog1.Filter = "SSCOM配置文件|sscom51.ini;sscom.ini";
            if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = OpenFileDialog1.FileName;
                FileStream fs = new FileStream(path, FileMode.Open);
            }
        }
    }
}
