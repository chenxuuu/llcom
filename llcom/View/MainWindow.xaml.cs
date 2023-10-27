using FontAwesome.WPF;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
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
using RestSharp;
using System.Threading;
using System.Windows.Interop;

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
            Tools.Global.LoadSetting();
            if (Tools.Global.setting.windowHeight != 0 &&
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
        }
        ObservableCollection<ToSendData> toSendListItems = new ObservableCollection<ToSendData>();
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
                    Tools.Global.Initial();

                    //重写关闭窗口代码
                    this.Closing += MainWindow_Closing;

                    //窗口置顶事件
                    Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);

                    //收发数据显示页面
                    dataShowFrame.Navigate(new Uri("Pages/DataShowPage.xaml", UriKind.Relative));

                    //加载初始波特率
                    var br = Tools.Global.setting.baudRate.ToString();
                    if(baudRateComboBox.Items.Contains(br))
                        baudRateComboBox.Text = Tools.Global.setting.baudRate.ToString();
                    else
                    {
                        lastBaudRateSelectedIndex = baudRateComboBox.Items.Count - 1;//防止弹窗提示
                        baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = br;
                        baudRateComboBox.Text = br;
                    }

                    // 绑定事件监听,用于监听HID设备插拔
                    (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);
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

                    //在线脚本列表
                    OnlineScriptsFrame.Navigate(new Uri("Pages/OnlineScriptsPage.xaml", UriKind.Relative));

                    //关于页面
                    aboutFrame.Navigate(new Uri("Pages/AboutPage.xaml", UriKind.Relative));

                    //tcp测试页面
                    tcpTestFrame.Navigate(new Uri("Pages/tcpTest.xaml", UriKind.Relative));

                    //tcp客户端页面
                    tcpClientFrame.Navigate(new Uri("Pages/SocketClientPage.xaml", UriKind.Relative));

                    //本地tcp服务器
                    tcpLocalTestFrame.Navigate(new Uri("Pages/TcpLocalPage.xaml", UriKind.Relative));

                    //本地udp服务器
                    udpLocalTestFrame.Navigate(new Uri("Pages/UdpLocalPage.xaml", UriKind.Relative));

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

                    //WinUSB
                    WinUSBFrame.Navigate(new Uri("Pages/WinUSBPage.xaml", UriKind.Relative));

                    this.Title += $" - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";

                    TongjiWebBrowser.Source = new Uri(
                            $"https://llcom.papapoi.com/tongji.html?{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}"
                        );

                    new Thread(LuaLogPrintTask).Start();

                    //加载完了，可以允许点击
                    MainGrid.IsEnabled = true;

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

                    //更换标题栏
                    var title = "";
                    title = this.Title;
                    Tools.Global.ChangeTitleEvent += (n, s) =>
                    {
                        this.Dispatcher.Invoke(() => this.Title = title + s);
                    };

                    //热更，防止恶性bug，及时修复
                    new Thread(() =>
                    {
                        try
                        {
                            Random r = new Random();//加上随机参数，确保获取的是最新数据
                            var client = new RestClient("https://llcom.papapoi.com/hotfix.lua?" + r.Next());
                            var request = new RestRequest();
                            var response = client.Get(request);
                            var lua = new LuaEnv.LuaEnv();
                            lua.DoString(response.Content);
                        }
                        catch { }
                    }).Start();


                    Tools.Global.RefreshLuaScriptListEvent += (n, s) =>
                    {
                        this.Dispatcher.Invoke(() => RefreshScriptList());
                    };

                }));
            });
        }

        private bool DoInvoke(Action action)
        {
            if (Tools.Global.isMainWindowsClosed)
                return false;
            Dispatcher.Invoke(action);
            return true;
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
            Tools.Logger.ShowData(sender as byte[], true);
        }

        private void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Tools.Logger.ShowData(sender as byte[], false);
        }

        private bool refreshLock = false;
        /// <summary>
        /// 刷新设备列表
        /// </summary>
        private void refreshPortList(string lastPort = null)
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
                        //有些人遇到了微软库的bug，所以需要手动从0x00截断
                        var pp = p;
                        if (p.IndexOf("\0") > 0)
                            pp = p.Substring(0, p.IndexOf("\0"));
                        bool notMatch = true;
                        foreach (string n in strs)
                        {
                            if (n.Contains($"({pp})"))//如果和选中项目匹配
                            {
                                notMatch = false;
                                break;
                            }
                        }
                        if (notMatch)
                            strs.Add($"Serial Port {pp} ({pp})");//如果列表中没有，就自己加上
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

                    if (string.IsNullOrEmpty(lastPort))
                        lastPort = Tools.Global.uart.GetName();
                    //选定上次的com口
                    foreach (string c in serialPortsListComboBox.Items)
                    {
                        if (c.Contains($"({lastPort})"))
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
                    string name = file.Name.Substring(0, file.Name.Length - 4);
                    luaFileList.Items.Add(name);
                    if (name== Tools.Global.setting.runScript)
                    {
                        luaFileList.SelectedIndex = luaFileList.Items.Count - 1;
                    }
                }
            }
            lastLuaFile = Tools.Global.setting.runScript;
            fileLoading = false;
        }

        private static int UsbPluginDeley = 0;
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x219 && !Tools.Global.uart.IsOpen())// 监听USB设备插拔消息
            {
                if (UsbPluginDeley == 0)
                {
                    ++UsbPluginDeley;   // Task启动需要准备时间,这里提前对公共变量加一
                    Task.Run(() =>
                    {
                        do Task.Delay(100).Wait();
                        while (++UsbPluginDeley < 10);
                        UsbPluginDeley = 0;
                        Dispatcher.Invoke(() =>
                        {
                            UsbDeviceNotifier_OnDeviceNotify();
                        });
                        Logger.AddUartLogInfo($"[USB拔插事件] {DateTime.Now:HH:mm:ss.fff}");
                    });
                }
                else UsbPluginDeley = 1;
                handled = true;
            }
            return IntPtr.Zero;
        }
        private void UsbDeviceNotifier_OnDeviceNotify()
        {
            if (Tools.Global.uart.IsOpen())
            {
                refreshPortList();
                foreach (string c in serialPortsListComboBox.Items)
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
                Tools.MessageBox.Show($"尝试打开文件夹失败，请自行打开该路径：{Tools.Global.GetTrueProfilePath()}user_script_run");
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
                    //有些人遇到了微软库的bug，所以需要手动从0x00截断
                    var pp = p;
                    if (p.IndexOf("\0") > 0)
                        pp = p.Substring(0, p.IndexOf("\0"));
                    if ((serialPortsListComboBox.SelectedItem as string).Contains($"({pp})"))//如果和选中项目匹配
                    {
                        port = pp;
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
                            Tools.MessageBox.Show(TryFindResource("ErrorOpenPort") as string ?? "?!");
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
                string lastPort = null;//记录一下上次的串口号
                try
                {
                    Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close");
                    forcusClosePort = true;//不再重新开启串口
                    lastPort = Tools.Global.uart.GetName();//串口号
                    Tools.Global.uart.Close();
                    Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]close done");
                }
                catch
                {
                    //串口关闭失败！
                    Tools.MessageBox.Show(TryFindResource("ErrorClosePort") as string ?? "?!");
                }
                Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show");
                openClosePortTextBlock.Text = (TryFindResource("OpenPort_open") as string ?? "?!");
                serialPortsListComboBox.IsEnabled = true;
                statusTextBlock.Text = (TryFindResource("OpenPort_close") as string ?? "?!");
                Tools.Logger.AddUartLogDebug($"[OpenClosePortButton]change show done");
                refreshPortList(lastPort);
            }

        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            Tools.Logger.ClearData();
        }

        private int lastBaudRateSelectedIndex = -1;
        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //选的没变
            if(lastBaudRateSelectedIndex == baudRateComboBox.SelectedIndex)
                return;

            if (baudRateComboBox.SelectedItem != null)
            {
                lastBaudRateSelectedIndex = baudRateComboBox.SelectedIndex;
                if (baudRateComboBox.SelectedIndex == baudRateComboBox.Items.Count - 1)
                {
                    int br = 0;
                    Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(TryFindResource("ShowBaudRate") as string ?? "?!",
                        "115200", TryFindResource("OtherRate") as string ?? "?!");
                    if (!ret.Item1 || !int.TryParse(ret.Item2,out br))//啥都没选
                    {
                        Tools.MessageBox.Show(TryFindResource("OtherRateFail") as string ?? "?!");
                    }
                    Tools.Global.setting.baudRate = br;
                    Task.Run(() =>
                    {
                        this.Dispatcher.Invoke(new Action(delegate {
                            var text = Tools.Global.setting.baudRate.ToString();
                            baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = text;
                            baudRateComboBox.Text = text;
                        }));
                    });
                }
                else
                {
                    Tools.Global.setting.baudRate =
                        int.Parse((baudRateComboBox.SelectedItem as ComboBoxItem).Content.ToString());
                    baudRateComboBox.Items[baudRateComboBox.Items.Count - 1] = TryFindResource("OtherRate") as string ?? "?!";
                }
            }
        }

        /// <summary>
        /// 发串口数据
        /// </summary>
        /// <param name="data"></param>
        private void sendUartData(byte[] data, bool? is_hex = null)
        {
            if (!Tools.Global.uart.IsOpen())
            {
                openPort();
                toSendData = (byte[])data.Clone();//带发送数据缓存起来，连上串口后发出去
            }

            if (Tools.Global.uart.IsOpen())
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
                    Tools.MessageBox.Show($"{TryFindResource("ErrorScript") as string ?? "?!"}\r\n" + ex.ToString());
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
                    Tools.Global.uart.SendData(dataConvert);
                }
                catch(Exception ex)
                {
                    Tools.MessageBox.Show($"{TryFindResource("ErrorSendFail") as string ?? "?!"}\r\n"+ ex.ToString());
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
                sendUartData(Tools.Global.Hex2Byte(data.text), true);
            else
                sendUartData(Global.GetEncoding().GetBytes(data.text), false);
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
                Tools.MessageBox.Show(TryFindResource("LuaNoName") as string ?? "?!");
                return;
            }
            if (File.Exists(Tools.Global.ProfilePath + $"user_script_run/{newLuaFileNameTextBox.Text}.lua"))
            {
                Tools.MessageBox.Show(TryFindResource("LuaExist") as string ?? "?!");
                return;
            }

            try
            {
                File.Create(Tools.Global.ProfilePath + $"user_script_run/{newLuaFileNameTextBox.Text}.lua").Close();
                loadLuaFile(newLuaFileNameTextBox.Text);
            }
            catch
            {
                Tools.MessageBox.Show(TryFindResource("LuaCreateFail") as string ?? "?!");
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
        //最后修改文件的时间
        private static DateTime lastLuaChangeTime = DateTime.Now;
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

            //文件内容显示出来
            try
            {
                textEditor.Text = File.ReadAllText(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua");
            }
            catch
            {
                Tools.MessageBox.Show("File load failed.\r\n" +
                    "Do not open this file in other application!");
                return;
            }
            
            //记录最后时间
            lastLuaFileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $"user_script_run/{Tools.Global.setting.runScript}.lua");
            //加载文件,修改时间使用文件时间
            lastLuaChangeTime = lastLuaFileTime;

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
                //如果修改时间大于文件时间才执行保存操作
                if (lastLuaChangeTime > lastLuaFileTime)
                {
                    File.WriteAllText(Tools.Global.ProfilePath + $"user_script_run/{fileName}.lua", textEditor.Text);
                    //记录最后时间
                    lastLuaFileTime = File.GetLastWriteTime(Tools.Global.ProfilePath + $"user_script_run/{fileName}.lua");
                }
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

        //lua日志打印次数
        private int luaLogCount = 0;
        /// <summary>
        /// 消息来的信号量
        /// </summary>
        private EventWaitHandle luaWaitQueue = new AutoResetEvent(false);
        private List<string> luaLogsBuff = new List<string>();
        private void LuaApis_PrintLuaLog(object sender, EventArgs e)
        {
            if(sender is string && sender != null)
            { 
                lock(luaLogsBuff)
                {
                    if (luaLogsBuff.Count > 500)
                    {
                        luaLogsBuff.Clear();
                        luaLogsBuff.Add("too many logs!");
                        //延时0.5秒，防止卡住ui线程
                        Thread.Sleep(500);
                    }
                    else
                        luaLogsBuff.Add(sender as string);
                }
                luaWaitQueue.Set();
            }
        }

        private void LuaLogPrintTask()
        {
            luaWaitQueue.Reset();
            Tools.Global.ProgramClosedEvent += (_, _) =>
            {
                luaWaitQueue.Set();
            };
            while (true)
            {
                luaWaitQueue.WaitOne();
                if (Tools.Global.isMainWindowsClosed)
                    return;
                var logsb = new StringBuilder();
                lock (luaLogsBuff)
                {
                    for(int i=0;i<luaLogsBuff.Count;i++)
                    {
                        logsb.AppendLine(luaLogsBuff[i]);
                        luaLogCount++;
                    }
                    luaLogsBuff.Clear();
                }

                if (!luaLogPrintable)
                    continue;
                if (logsb.Length == 0)
                    continue;
                var logs = logsb.ToString();
                DoInvoke(()=>
                {
                    luaLogTextBox.IsEnabled = false;//确保文字不再被选中，防止wpf卡死
                    if (luaLogCount >= 1000)
                    {
                        luaLogTextBox.Clear();
                        luaLogTextBox.AppendText("Lua log too long, auto clear.\r\n" +
                            "more logs see lua log file.\r\n");
                        luaLogCount = 0;
                    }
                    luaLogTextBox.AppendText(logs);
                    luaLogTextBox.ScrollToEnd();
                    if (!luaLogTextBox.IsMouseOver)
                        luaLogTextBox.IsEnabled = true;
                });
                //正常就延时10ms，防止卡住ui线程
                Thread.Sleep(10);
            }
        }


        private void luaLogTextBox_MouseLeave(object sender, MouseEventArgs e)
        {
            luaLogTextBox.IsEnabled = true;
        }

        private void StopLuaButton_Click(object sender, RoutedEventArgs e)
        {
            luaLogCount = 0;
            lock(luaLogsBuff)
                luaLogsBuff.Clear();
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
                luaLogPrintable = true;
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
                    Tools.MessageBox.Show(err.Message);
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
            SaveFileDialog.FileName = QuickListPageTextBlock.Text;
            SaveFileDialog.Filter = TryFindResource("QuickSendLLCOMFile") as string ?? "?!";
            if (SaveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(SaveFileDialog.FileName, JsonConvert.SerializeObject(toSendListItems));
                    Tools.MessageBox.Show(TryFindResource("QuickSendSaveFileDone") as string ?? "?!");
                }
                catch(Exception err)
                {
                    Tools.MessageBox.Show(err.Message);
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

        private void textEditor_TextChanged(object sender, EventArgs e)
        {
            lastLuaChangeTime = DateTime.Now;
        }

        private void removeAllButton_Click(object sender, RoutedEventArgs e)
        {
            (bool r,string s) = Tools.InputDialog.OpenDialog(TryFindResource("DeleteConfirmationMsg") as string ?? "?!",
                "", TryFindResource("DeleteConfirmation") as string ?? "?!");
            if (r && s == "YES")
            {
                toSendListItems.Clear();
                SaveSendList(null, EventArgs.Empty);
            }
        }

        private void uartDataFlowDocument_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Tools.Global.setting.terminal)
                dataShowFrame.BorderThickness = new Thickness(0.5);
        }

        private void uartDataFlowDocument_LostFocus(object sender, RoutedEventArgs e)
        {
            dataShowFrame.BorderThickness = new Thickness(0);
        }

        private void uartDataFlowDocument_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.TextComposition.Text.Length < 1 || !Tools.Global.setting.terminal)
                return;
            if (Tools.Global.uart.IsOpen())
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
            if (e.Key >= Key.A && e.Key <= Key.Z && Tools.Global.uart.IsOpen())
                try
                {
                    Tools.Global.uart.SendData(new byte[] { (byte)((int)e.Key - (int)Key.A + 1) });
                }
                catch { }
        }
    }
}
