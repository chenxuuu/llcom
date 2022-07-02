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
using ICSharpCode.AvalonEdit.Folding;

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
            //延迟启动，加快软件第一屏出现速度
            Task.Run(() =>
            {
                this.Dispatcher.Invoke(new Action(delegate {
                    //接收到、发送数据成功回调
                    Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
                    Tools.Global.uart.UartDataSent += Uart_UartDataSent;

                    //初始化所有数据
                    Tools.Global.Initial();//cost 299ms

                    //重写关闭窗口代码
                    this.Closing += MainWindow_Closing;

                    if(Tools.Global.setting.windowHeight != 0 &&
                        Tools.Global.setting.windowLeft > 0 &&
                        Tools.Global.setting.windowTop > 0 &&
                        Tools.Global.setting.windowTop < SystemParameters.FullPrimaryScreenHeight &&
                        Tools.Global.setting.windowLeft < SystemParameters.FullPrimaryScreenWidth)
                    {
                        this.Left = Tools.Global.setting.windowLeft;
                        this.Top = Tools.Global.setting.windowTop;
                        this.Width = Tools.Global.setting.windowWidth;
                        this.Height = Tools.Global.setting.windowHeight;
                    }

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
                    QuiclListName0.DataContext = Tools.Global.setting;
                    QuiclListName1.DataContext = Tools.Global.setting;
                    QuiclListName2.DataContext = Tools.Global.setting;
                    QuiclListName3.DataContext = Tools.Global.setting;
                    QuiclListName4.DataContext = Tools.Global.setting;
                    QuiclListName5.DataContext = Tools.Global.setting;
                    QuiclListName6.DataContext = Tools.Global.setting;
                    QuiclListName7.DataContext = Tools.Global.setting;
                    QuiclListName8.DataContext = Tools.Global.setting;
                    QuiclListName9.DataContext = Tools.Global.setting;

                    //初始化快捷发送栏的数据
                    canSaveSendList = false;
                    if (Global.setting.quickSendSelect == -1)
                        Global.setting.quickSendSelect = 0;
                    ToSendData.DataChanged += SaveSendList;
                    LoadQuickSendList();
                    canSaveSendList = true;


                    //快速搜索
                    SearchPanel.Install(textEditor.TextArea);

                    var foldingManager = FoldingManager.Install(textEditor.TextArea);
                    var foldingStrategy = new Model.LuaFolding();

                    Task.Run(() =>
                    {
                        while (true)
                        {
                            Task.Delay(1000).Wait();
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                try
                                {
                                    foldingStrategy.UpdateFoldings(foldingManager, textEditor.Document);
                                }
                                catch { }
                            }));
                        }
                    });

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

                    //乱码修复
                    EncodingFixFrame.Navigate(new Uri("Pages/EncodingFixPage.xaml", UriKind.Relative));

                    //串口监听
                    SerialMonitorFrame.Navigate(new Uri("Pages/SerialMonitorPage.xaml", UriKind.Relative));

                    //绘制曲线
                    PlotFrame.Navigate(new Uri("Pages/PlotPage.xaml", UriKind.Relative));

                    this.Title += $" - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";

                    //检查更新
                    if (!Tools.Global.IsMSIX())
                    {
                        Task.Run(() => {
                            bool runed = false;
                            AutoUpdaterDotNET.AutoUpdater.CheckForUpdateEvent += (args) =>
                            {
                                if (runed) return; runed = true;
                                if (args.IsUpdateAvailable)
                                {
                                    Global.HasNewVersion = true;//有新版本
                                    if(Tools.Global.setting.autoUpdate)//开了自动升级功能再开
                                    {
                                        this.Dispatcher.Invoke(new Action(delegate
                                        {
                                            AutoUpdaterDotNET.AutoUpdater.ShowUpdateForm(args);
                                        }));
                                    }
                                }
                            };
                            Random r = new Random();//加上随机参数，确保获取的是最新数据
                            try
                            {
                                AutoUpdaterDotNET.AutoUpdater.Start("https://llcom.papapoi.com/autoUpdate.xml?" + r);
                            }
                            catch
                            {
                                runed = true;
                            }
                        });
                    }
                }));
            });
        }

        /// <summary>
        /// 加载快捷发送区数据
        /// </summary>
        private void LoadQuickSendList()
        {
            if (Tools.Global.setting.quickSend.Count == 0)
            {
                Tools.Global.setting.quickSend = new List<ToSendData>
                        {
                            new ToSendData{id = 1,text="example string",commit="右击更改此处文字",hex=false},
                            new ToSendData{id = 2,text="lua可通过接口获取此处数据",hex=false},
                            new ToSendData{id = 3,text="aa 01 02 0d 0a",commit="Hex数据也能发",hex=true},
                            new ToSendData{id = 4,text="此处数据会被lua处理",hex=false},
                            new ToSendData{id = 5,text="右击序号可以更改这一行的位置",hex=false},
                            new ToSendData{id = 6,text="",hex=false},
                        };
            }
            foreach (var i in Tools.Global.setting.quickSend)
            {
                if (i.commit == null)
                    i.commit = TryFindResource("QuickSendButton") as string ?? "?!";
                toSendListItems.Add(i);
            }
            CheckToSendListId();
            QuickListPageTextBlock.Text = Global.setting.GetQuickListNameNow();
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
                        ManagementObjectSearcher searcher =new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_PnPEntity");
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
                        Task.Delay(500).Wait();
                    }
                    //MessageBox.Show("fail了");
                }

                try
                {
                    foreach (string p in SerialPort.GetPortNames())//加上缺少的com口
                    {
                        bool notMatch = true;
                        foreach (string n in strs)
                        {
                            if (n.Contains($"({p})"))//如果和选中项目匹配
                            {
                                notMatch = false;
                                break;
                            }
                        }
                        if (notMatch)
                            strs.Add($"Serial Port {p} ({p})");//如果列表中没有，就自己加上
                    }
                }
                catch{ }


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
                                            openClosePortTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                                            serialPortsListComboBox.IsEnabled = false;
                                            statusTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
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
            DirectoryInfo luaFileDir = new DirectoryInfo(Tools.Global.ProfilePath + "user_script_run/");
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
                openClosePortTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
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
            Tools.Global.setting.windowLeft = this.Left;
            Tools.Global.setting.windowTop = this.Top;
            Tools.Global.setting.windowWidth = this.Width;
            Tools.Global.setting.windowHeight = this.Height;
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
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", Tools.Global.GetTrueProfilePath() + "user_script_run");
            }
            catch
            {
                MessageBox.Show($"尝试打开文件夹失败，请自行打开该路径：{Tools.Global.GetTrueProfilePath()}user_script_run");
            }
        }

        private void RefreshScriptListButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshScriptList();
        }

        private byte[] toSendData = null;//待发送的数据
        private void openPort()
        {
            Tools.Logger.AddUartLogDebug($"[openPort]{isOpeningPort},{serialPortsListComboBox.SelectedItem}");
            if (isOpeningPort)
                return;
            if (serialPortsListComboBox.SelectedItem != null)
            {
                string[] ports;//获取所有串口列表
                try
                {
                    Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames");
                    ports = SerialPort.GetPortNames();
                    Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames{ports.Length}");
                }
                catch(Exception e)
                {
                    ports = new string[0];
                    Tools.Logger.AddUartLogDebug($"[openPort]GetPortNames Exception:{e.Message}");
                }
                string port = "";//最终串口名
                foreach (string p in ports)//循环查找符合名称串口
                {
                    if ((serialPortsListComboBox.SelectedItem as string).Contains($"({p})"))//如果和选中项目匹配
                    {
                        port = p;
                        break;
                    }
                }
                Tools.Logger.AddUartLogDebug($"[openPort]PortName:{port},isOpeningPort:{isOpeningPort}");
                if (port != "")
                {
                    Task.Run(() =>
                    {
                        isOpeningPort = true;
                        try
                        {
                            forcusClosePort = false;//不再强制关闭串口
                            Tools.Logger.AddUartLogDebug($"[openPort]SetName");
                            Tools.Global.uart.SetName(port);
                            Tools.Logger.AddUartLogDebug($"[openPort]open");
                            Tools.Global.uart.Open();
                            Tools.Logger.AddUartLogDebug($"[openPort]change show");
                            this.Dispatcher.Invoke(new Action(delegate
                            {
                                openClosePortTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                                serialPortsListComboBox.IsEnabled = false;
                                statusTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                            }));
                            Tools.Logger.AddUartLogDebug($"[openPort]check to send");
                            if (toSendData != null)
                            {
                                sendUartData(toSendData);
                                toSendData = null;
                            }
                            Tools.Logger.AddUartLogDebug($"[openPort]done");
                        }
                        catch(Exception e)
                        {
                            Tools.Logger.AddUartLogDebug($"[openPort]open error:{e.Message}");
                            //串口打开失败！
                            MessageBox.Show(TryFindResource("ErrorOpenPort") as string ?? "?!");
                        }
                        isOpeningPort = false;
                        Tools.Logger.AddUartLogDebug($"[openPort]all done");
                    });

                }
            }
        }
        private void OpenClosePortButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]now:{Tools.Global.uart.IsOpen()}");
            if (!Tools.Global.uart.IsOpen())//打开串口逻辑
            {
                openPort();
            }
            else//关闭串口逻辑
            {
                try
                {
                    Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close");
                    forcusClosePort = true;//不再重新开启串口
                    Tools.Global.uart.Close();
                    Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close done");
                }
                catch
                {
                    //串口关闭失败！
                    MessageBox.Show(TryFindResource("ErrorClosePort") as string ?? "?!");
                }
                Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show");
                openClosePortTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show done");
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
                if ((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString() ==
                    (TryFindResource("OtherRate") as string ?? "?!"))
                {
                    int br = 0;
                    Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("ShowBaudRate") as string ?? "?!",
                        "115200", TryFindResource("OtherRate") as string ?? "?!");
                    if (!ret.Item1 || !int.TryParse(ret.Item2,out br))//啥都没选
                    {
                        MessageBox.Show(TryFindResource("OtherRateFail") as string ?? "?!");
                        Task.Run(() =>
                        {
                            this.Dispatcher.Invoke(new Action(delegate {
                                baudRateComboBox.Text = Tools.Global.setting.baudRate.ToString();
                            }));
                        });
                        return;
                    }
                    Tools.Global.setting.baudRate = br;
                    if(Tools.Global.setting.baudRate != br)//说明设置失败了
                        Task.Run(() =>
                        {
                            this.Dispatcher.Invoke(new Action(delegate {
                                baudRateComboBox.Text = Tools.Global.setting.baudRate.ToString();
                            }));
                        });
                }
                else
                {
                    Tools.Global.setting.baudRate =
                        int.Parse((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString());
                }
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
                    MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
                    return;
                }
                try
                {
                    if (Tools.Global.setting.extraEnter)
                        dataConvert += "0D0A";
                    Tools.Global.uart.SendData(Tools.Global.Hex2Byte(dataConvert));
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"{TryFindResource("ErrorSendFail") as string ?? "?!"}\r\n"+ex.ToString());
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
            toSendListItems.Add(new ToSendData() { id = toSendListItems.Count + 1, text = "", hex = false , commit = TryFindResource("QuickSendButton") as string ?? "?!" });
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
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("QuickSendSetButton") as string ?? "?!",
                data.commit, TryFindResource("QuickSendChangeButton") as string ?? "?!");
            if(ret.Item1)
            {
                ((Button)sender).Content = data.commit = ret.Item2;
            }
        }

        /// <summary>
        /// 检查并更正快捷发送区序号
        /// </summary>
        public void CheckToSendListId()
        {
            //当序号不对时，更正序号
            for (int i = 0; i < toSendListItems.Count; i++)
            {
                if (toSendListItems[i].id != i + 1)
                {
                    var item = toSendListItems[i];
                    toSendListItems.RemoveAt(i);//元素删掉重新加进去
                    item.id = i + 1;
                    toSendListItems.Insert(i, item);
                }
            }
        }

        public void SaveSendList(object sender, EventArgs e)
        {
            if (!canSaveSendList)
                return;
            CheckToSendListId();
            //保存当前的所有数据
            var newList = new List<ToSendData>();
            foreach (ToSendData i in toSendListItems)
            {
                newList.Add(i);
            }
            Tools.Global.setting.quickSend = newList;
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
                MessageBox.Show(TryFindResource("LuaNoName") as string ?? "?!");
                return;
            }
            if (File.Exists(Tools.Global.ProfilePath + $"user_script_run/{newLuaFileNameTextBox.Text}.lua"))
            {
                MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                return;
            }

            try
            {
                File.Create(Tools.Global.ProfilePath + $"user_script_run/{newLuaFileNameTextBox.Text}.lua").Close();
                loadLuaFile(newLuaFileNameTextBox.Text);
            }
            catch
            {
                MessageBox.Show(TryFindResource("LuaCreateFail") as string ?? "?!");
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
        //最后打开文件的时间
        private static DateTime lastLuaFileTime = DateTime.Now;
        /// <summary>
        /// 加载lua脚本文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void loadLuaFile(string fileName)
        {
            //检查文件是否存在
            if (!File.Exists(Tools.Global.ProfilePath + $"user_script_run/{fileName}.lua"))
            {
                Tools.Global.setting.runScript = "example";
                if (!File.Exists(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua"))
                {
                    File.Create(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua").Close();
                }
            }
            else
            {
                Tools.Global.setting.runScript = fileName;
            }
            //记录最后时间
            lastLuaFileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua");

            //文件内容显示出来
            textEditor.Text = File.ReadAllText(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua");

            RefreshScriptList();
        }

        /// <summary>
        /// 保存lua文件
        /// </summary>
        /// <param name="fileName">文件名，不带.lua</param>
        private void saveLuaFile(string fileName)
        {
            try
            {
                File.WriteAllText(Tools.Global.ProfilePath + $"user_script_run/{fileName}.lua", textEditor.Text);
                //记录最后时间
                lastLuaFileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $"user_script_run/{fileName}.lua");
            }
            catch { }
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
        private void Window_Deactivated(object sender, EventArgs e)
        {
            //窗口变为后台,可能在切换编辑器,自动保存脚本
            if (lastLuaFile != "")
                saveLuaFile(lastLuaFile);
        }
        private void Window_Activated(object sender, EventArgs e)
        {
            if (lastLuaFile != "")
            {
                //当前文件最后时间
                DateTime fileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $"user_script_run/{lastLuaFile}.lua");
                if (fileTime > lastLuaFileTime)//代码在外部被修改
                {
                    loadLuaFile(lastLuaFile);
                }
            }
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
                        pauseLuaPrintButton.ToolTip = TryFindResource("LuaPause") as string ?? "?!";
                        pauseLuaPrintIcon.Icon = FontAwesomeIcon.Pause;
                    }
                    else
                    {
                        pauseLuaPrintButton.ToolTip = TryFindResource("LuaContinue") as string ?? "?!";
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
                
                stopLuaOrExitIcon.Icon = FontAwesomeIcon.Stop;
                stopLuaButton.ToolTip = TryFindResource("LuaStop") as string ?? "?!";
            }
            else
            {
                stopLuaOrExitIcon.Icon = FontAwesomeIcon.SignOut;
                stopLuaButton.ToolTip = TryFindResource("LuaQuit") as string ?? "?!";
            }
            luaLogPrintable = true;
            LuaEnv.LuaRunEnv.StopLua("");

            pauseLuaPrintButton.ToolTip = TryFindResource("LuaOverload") as string ?? "?!";
            pauseLuaPrintIcon.Icon = FontAwesomeIcon.Refresh;
        }

        private void LuaRunEnv_LuaRunError(object sender, EventArgs e)
        {
            luaLogPrintable = true;
        }

        private void PauseLuaPrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (!LuaEnv.LuaRunEnv.isRunning)
            {
                stopLuaOrExitIcon.Icon = FontAwesomeIcon.Stop;
                stopLuaButton.ToolTip = TryFindResource("LuaStop") as string ?? "?!";
                LuaEnv.LuaRunEnv.New($"user_script_run/{luaFileList.SelectedItem as string}.lua");
                LuaEnv.LuaRunEnv.canRun = true;
            }
            else {
                luaLogPrintable = !luaLogPrintable;
            }
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
            OpenFileDialog.Filter = TryFindResource("QuickSendSSCOMFile") as string ?? "?!";
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    canSaveSendList = false;
                    foreach (var i in Tools.Global.ImportFromSSCOM(OpenFileDialog.FileName))
                    {
                        toSendListItems.Add(new ToSendData()
                        {
                            id = toSendListItems.Count + 1,
                            text = i.text,
                            hex = i.hex,
                            commit = i.commit
                        });
                    }
                    canSaveSendList = true;
                    SaveSendList(0, EventArgs.Empty);//保存并刷新数据列表
                }));
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

        //id序号右击事件
        private void TextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ToSendData data;
            try
            {
                data = ((TextBlock)sender).Tag as ToSendData;
            }
            catch
            {
                data = ((Grid)sender).Tag as ToSendData;
            }
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("QuickSendChangeIdButton") as string ?? "?!",
                data.id.ToString(), (TryFindResource("QuickSendChangeIdTitle") as string ?? "?!") + data.id.ToString());

            if (!ret.Item1)
                return;
            CheckToSendListId();
            if (ret.Item2.Trim().Length == 0)//留空删除该项目
            {
                toSendListItems.RemoveAt(data.id-1);
            }
            else
            {
                int index = -1;
                int.TryParse(ret.Item2, out index);
                if (index == data.id || index <= 0 || index > toSendListItems.Count) return;
                //移动到指定位置
                var item = toSendListItems[data.id-1];
                toSendListItems.RemoveAt(data.id-1);
                toSendListItems.Insert(index - 1, item);
            }
            SaveSendList(null, EventArgs.Empty);
        }

        private void MenuItem_Click_QuickSendList(object sender, RoutedEventArgs e)
        {
            canSaveSendList = false;
            int select = int.Parse((string)((MenuItem)sender).Tag);
            toSendListItems.Clear();
            Global.setting.quickSendSelect = select;
            LoadQuickSendList();
            canSaveSendList = true;
        }

        private void QuickSendImportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog OpenFileDialog = new System.Windows.Forms.OpenFileDialog();
            OpenFileDialog.Filter = TryFindResource("QuickSendLLCOMFile") as string ?? "?!";
            if (OpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<ToSendData> data = null;
                try
                {
                    data = JsonConvert.DeserializeObject<List<ToSendData>>(
                        File.ReadAllText(OpenFileDialog.FileName));
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message);
                    return;
                }
                this.Dispatcher.Invoke(new Action(delegate
                {
                    canSaveSendList = false;
                    foreach(var d in data)
                    {
                        toSendListItems.Add(d);
                    }
                    canSaveSendList = true;
                    SaveSendList(0, EventArgs.Empty);//保存并刷新数据列表
                }));
            }
        }

        private void QuickSendExportButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog SaveFileDialog = new System.Windows.Forms.SaveFileDialog();
            SaveFileDialog.Filter = TryFindResource("QuickSendLLCOMFile") as string ?? "?!";
            if (SaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(SaveFileDialog.FileName, JsonConvert.SerializeObject(toSendListItems));
                    MessageBox.Show(TryFindResource("QuickSendSaveFileDone") as string ?? "?!");
                }
                catch(Exception err)
                {
                    MessageBox.Show(err.Message);
                }
            }
        }

        private void QuickListNameStackPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tuple<bool, string> ret = Tools.InputDialog.OpenDialog("↓↓↓↓↓↓",
                Global.setting.GetQuickListNameNow(), TryFindResource("QuickSendListNameChangeTip") as string ?? "?!");

            if (!ret.Item1)
                return;

            Global.setting.SetQuickListNameNow(ret.Item2);
            QuickListPageTextBlock.Text = ret.Item2;
        }

        private void pauseLuaPrintButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            luaLogTextBox.Clear();
        }
    }
}
