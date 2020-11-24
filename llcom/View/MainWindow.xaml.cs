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
using System.Text.RegularExpressions;
using llcom.Tools;

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
        private bool forcusClosePort = true;
        private bool canSaveSendList = true;
        private bool isOpeningPort = false;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //接收到、发送数据成功回调
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
            Tools.Global.uart.UartDataSent += Uart_UartDataSent;

            //初始化所有数据
            Tools.Global.Initial();

            //重写关闭窗口代码
            this.Closing += MainWindow_Closing;

            //窗口置顶事件
            Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);

            //usb刷新时触发
            usbDeviceNotifier.Enabled = true;
            usbDeviceNotifier.OnDeviceNotify += UsbDeviceNotifier_OnDeviceNotify; ;

            //收发数据显示页面
            dataShowFrame.Navigate(new Uri("Pages/DataShowPage.xaml", UriKind.Relative));

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
                        i["commit"] = FindResource("QuickSendButton") as string;
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
                MessageBox.Show($"{FindResource("QuickSendLoadError") as string}\r\n" + ex.ToString() + "\r\n" + Tools.Global.setting.quickData);
                Tools.Global.setting.quickData = "{\"data\":[{\"id\":1,\"text\":\"example string\",\"hex\":false},{\"id\":2,\"text\":\"lua可通过接口获取此处数据\",\"hex\":false},{\"id\":3,\"text\":\"aa 01 02 0d 0a\",\"hex\":true},{\"id\":4,\"text\":\"此处数据会被lua处理\",\"hex\":false}]}";
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
            aboutFrame.Navigate(new Uri("Pages/AboutPage.xaml", UriKind.Relative));

            //tcp测试页面
            tcpTestFrame.Navigate(new Uri("Pages/tcpTest.xaml", UriKind.Relative));

            //mqtt测试页面
            MqttTestFrame.Navigate(new Uri("Pages/MqttTestPage.xaml", UriKind.Relative));

            //编码转换工具页面
            EncodingToolsFrame.Navigate(new Uri("Pages/ConvertPage.xaml", UriKind.Relative));

            this.Title += $" - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";


        }

        private void Uart_UartDataSent(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                Tools.Logger.ShowData(sender as byte[], true);
            }));
        }

        private void Uart_UartDataRecived(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(new Action(delegate {
                Tools.Logger.ShowData(sender as byte[], false);
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
                        ManagementObjectSearcher searcher =new ManagementObjectSearcher("select * from Win32_SerialPort");
                        Regex regExp = new Regex("\\(COM\\d+\\)");
                        foreach (ManagementObject queryObj in searcher.Get())
                        {
                            if ((queryObj["Caption"] != null) && regExp.IsMatch(queryObj["Caption"].ToString()))
                            {
                                strs.Add(queryObj["Caption"].ToString());
                            }
                        }
                        break;
                    }
                    catch
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                    //MessageBox.Show("fail了");
                }


                foreach (string p in SerialPort.GetPortNames())//加上缺少的com口
                {
                    bool notMatch = true;
                    foreach(string n in strs)
                    {
                        if (n.Contains($"({p})"))//如果和选中项目匹配
                        {
                            notMatch = false;
                            break;
                        }
                    }
                    if(notMatch)
                        strs.Add($"Serial Port {p} ({p})");//如果列表中没有，就自己加上
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
                        if (c.Contains($"({Tools.Global.uart.GetName()})"))
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
                                        Tools.Global.uart.Open();
                                        Dispatcher.Invoke(new Action(delegate
                                        {
                                            openClosePortTextBlock.Text = (FindResource("OpenPort_close") as string);
                                            serialPortsListComboBox.IsEnabled = false;
                                            statusTextBlock.Text = (FindResource("OpenPort_open") as string);
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

        private void RefreshScriptList()
        {
            //刷新文件列表
            DirectoryInfo luaFileDir = new DirectoryInfo("user_script_run/");
            FileSystemInfo[] luaFiles = luaFileDir.GetFileSystemInfos();
            fileLoading = true;
            luaFileList.Items.Clear();
            for (int i = 0; i < luaFiles.Length; i++)
            {
                FileInfo file = luaFiles[i] as FileInfo;
                //是文件
                if (file != null && file.Name.ToLower().EndsWith(".lua"))
                {
                    luaFileList.Items.Add(file.Name.Substring(0, file.Name.Length - 4));
                }
            }
            luaFileList.Text = lastLuaFile = Tools.Global.setting.runScript;
            fileLoading = false;
        }

        private void UsbDeviceNotifier_OnDeviceNotify(object sender, DeviceNotifyEventArgs e)
        {
            if (Tools.Global.uart.IsOpen())
            {
                refreshPortList();
                foreach(string c in serialPortsListComboBox.Items)
                {
                    if (c.Contains($"({Tools.Global.uart.GetName()})"))
                    {
                        serialPortsListComboBox.Text = c;
                        break;
                    }
                }
            }
            else
            {
                openClosePortTextBlock.Text = (FindResource("OpenPort_open") as string);
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = (FindResource("OpenPort_close") as string);
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

        private void RefreshScriptListButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshScriptList();
        }

        private byte[] toSendData = null;//待发送的数据
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
                    if ((serialPortsListComboBox.SelectedItem as string).Contains($"({p})"))//如果和选中项目匹配
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
                            Tools.Global.uart.SetName(port);
                            Tools.Global.uart.Open();
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                openClosePortTextBlock.Text = (FindResource("OpenPort_close") as string);
                                serialPortsListComboBox.IsEnabled = false;
                                statusTextBlock.Text = (FindResource("OpenPort_open") as string);
                            }));
                            if(toSendData != null)
                            {
                                sendUartData(toSendData);
                                toSendData = null;
                            }
                        }
                        catch
                        {
                            //串口打开失败！
                            MessageBox.Show(FindResource("ErrorOpenPort") as string);
                        }
                        isOpeningPort = false;
                    });

                }
            }
        }
        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            if(!Tools.Global.uart.IsOpen())//打开串口逻辑
            {
                openPort();
            }
            else//关闭串口逻辑
            {
                try
                {
                    forcusClosePort = true;//不再重新开启串口
                    Tools.Global.uart.Close();
                }
                catch
                {
                    //串口关闭失败！
                    MessageBox.Show(FindResource("ErrorClosePort") as string);
                }
                openClosePortTextBlock.Text = (FindResource("OpenPort_open") as string);
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = (FindResource("OpenPort_close") as string);
            }

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Logger.ClearData();
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
            if (!Tools.Global.uart.IsOpen())
            {
                openPort();
                toSendData = (byte[])data.Clone();//带发送数据缓存起来，连上串口后发出去
            }

            if (Tools.Global.uart.IsOpen())
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
                    MessageBox.Show($"{FindResource("ErrorScript") as string}\r\n" + ex.ToString());
                    return;
                }
                try
                {
                    Tools.Global.uart.SendData(Tools.Global.Hex2Byte(dataConvert));
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"{FindResource("ErrorSendFail") as string}\r\n"+ex.ToString());
                    return;
                }
            }
        }

        private void SendUartData_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            sendUartData(Global.GetEncoding().GetBytes(toSendDataTextBox.Text));
        }

        private void AddSendListButton_Click(object sender, RoutedEventArgs e)
        {
            toSendListItems.Add(new ToSendData() { id = toSendListItems.Count + 1, text = "", hex = false , commit = FindResource("QuickSendButton") as string });
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
                sendUartData(Global.GetEncoding().GetBytes(data.text));
        }

        private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToSendData data = ((Button)sender).Tag as ToSendData;
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(FindResource("QuickSendSetButton") as string,
                data.commit, FindResource("QuickSendChangeButton") as string);
            if(ret.Item1)
            {
                ((Button)sender).Content = data.commit = ret.Item2;
            }
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
                MessageBox.Show(FindResource("LuaNoName") as string);
                return;
            }
            if (File.Exists($"user_script_run/{newLuaFileNameTextBox.Text}.lua"))
            {
                MessageBox.Show(FindResource("LuaExist") as string);
                return;
            }

            try
            {
                File.Create($"user_script_run/{newLuaFileNameTextBox.Text}.lua").Close();
                loadLuaFile(newLuaFileNameTextBox.Text);
            }
            catch
            {
                MessageBox.Show(FindResource("LuaCreateFail") as string);
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

            RefreshScriptList();
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
                        pauseLuaPrintButton.ToolTip = FindResource("LuaPause") as string;
                        pauseLuaPrintIcon.Icon = FontAwesomeIcon.Pause;
                    }
                    else
                    {
                        pauseLuaPrintButton.ToolTip = FindResource("LuaContinue") as string;
                        pauseLuaPrintIcon.Icon = FontAwesomeIcon.Play;
                    }
                }));
                _luaLogPrintable = value;
            }
        }
        private int luaLogCount = 0;
        private void LuaApis_PrintLuaLog(object sender, EventArgs e)
        {
            if (luaLogPrintable)
            {
                luaLogCount++;
                if(luaLogCount < 1000)
                {
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        luaLogTextBox.Select(luaLogTextBox.Text.Length, 0);//确保文字不再被选中，防止wpf卡死
                        luaLogTextBox.AppendText((sender as string) + "\r\n");
                        luaLogTextBox.ScrollToEnd();
                    }));
                }
                else
                {
                    luaLogCount = 0;
                    this.Dispatcher.Invoke(new Action(delegate
                    {
                        luaLogTextBox.Clear();
                        luaLogTextBox.AppendText("Lua log too long, auto clear.\r\n" +
                            "more logs see lua log file.\r\n");
                        luaLogTextBox.ScrollToEnd();
                    }));
                }

            }
        }

        private void StopLuaButton_Click(object sender, RoutedEventArgs e)
        {
            luaLogCount = 0;
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
            System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog.Filter = FindResource("QuickSendSSCOMFile") as string;
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Task.Run(() => {
                    canSaveSendList = false;
                    foreach (var i in Tools.Global.ImportFromSSCOM(OpenFileDialog.FileName))
                    {
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            toSendListItems.Add(new ToSendData()
                            {
                                id = toSendListItems.Count + 1,
                                text = i.text,
                                hex = i.hex,
                                commit = i.commit
                            });
                        }));
                    }
                    canSaveSendList = true;
                    SaveSendList(0, EventArgs.Empty);//保存并刷新数据列表
                });
            }
        }

        private void sentCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tools.Global.setting.SentCount = 0;
        }

        private void receivedCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tools.Global.setting.ReceivedCount = 0;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Tools.Global.setting.language = ((MenuItem)sender).Tag.ToString();
        }
    }
}
