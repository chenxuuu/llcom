using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls.WebParts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using FontAwesome.WPF;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using llcom.LuaEnv;
using llcom.Model;
using llcom.Tools;
using llcom.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Color = System.Windows.Media.Color;

namespace llcom;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>串口控制区 ViewModel（Step 5 MVVM 化）</summary>
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        Tools.Global.LoadSetting();
        _vm = new MainViewModel();
        DataContext = _vm;
        if (
            Tools.Global.setting.windowHeight != 0
            && Tools.Global.setting.windowWidth != 0
            && Tools.Global.setting.windowLeft >= SystemParameters.VirtualScreenLeft
            && Tools.Global.setting.windowTop >= SystemParameters.VirtualScreenTop
            && Tools.Global.setting.windowLeft
                < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
            && Tools.Global.setting.windowTop
                < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight
        )
        {
            this.Left = Tools.Global.setting.windowLeft;
            this.Top = Tools.Global.setting.windowTop;
            this.Width = Tools.Global.setting.windowWidth;
            this.Height = Tools.Global.setting.windowHeight;
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        //延迟启动，加快软件第一屏出现速度
        Task.Run(() =>
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        //接收到、发送数据成功回调
                        Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
                        Tools.Global.uart.UartDataSent += Uart_UartDataSent;
                        Tools.Global.uart.UartDataRawSent += Uart_UartDataRawSent;

                        //初始化所有数据
                        Tools.Global.Initial();

                        //重写关闭窗口代码
                        this.Closing += MainWindow_Closing;

                        //窗口置顶事件
                        Tools.Global.setting.MainWindowTop += new EventHandler(topEvent);
                        if (Tools.Global.setting.topmost) //设置窗口置顶
                            this.Topmost = true;

                        //收发数据显示页面
                        dataShowFrame.Navigate(
                            new Uri("Pages/DataShowPage.xaml", UriKind.Relative)
                        );

                        // 绑定事件监听,用于监听HID设备插拔
                        //Lua 编辑器文本桥接（AvalonEdit Text 不可绑定，经回调访问）
                        _vm.LuaEditor.SetTextBridge(
                            () => textEditor.Text,
                            t => textEditor.Text = t
                        );

                        (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(WndProc);
                        //刷新设备列表
                        _vm.RefreshPorts();

                        //绑定数据（快捷发送/串口区均通过 MainViewModel 绑定）

                        //快速搜索
                        SearchPanel.Install(textEditor.TextArea);

                        var foldingManager = FoldingManager.Install(textEditor.TextArea);
                        var foldingStrategy = new Model.LuaFolding();

                        Task.Run(() =>
                        {
                            while (true)
                            {
                                Task.Delay(1000).Wait();
                                this.Dispatcher.Invoke(
                                    new Action(
                                        delegate
                                        {
                                            try
                                            {
                                                foldingStrategy.UpdateFoldings(
                                                    foldingManager,
                                                    textEditor.Document
                                                );
                                            }
                                            catch { }
                                        }
                                    )
                                );
                            }
                        });

                        string name =
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name
                            + ".Lua.xshd";
                        System.Reflection.Assembly assembly =
                            System.Reflection.Assembly.GetExecutingAssembly();
                        using (System.IO.Stream s = assembly.GetManifestResourceStream(name))
                        {
                            using (XmlTextReader reader = new XmlTextReader(s))
                            {
                                var xshd = HighlightingLoader.LoadXshd(reader);
                                textEditor.SyntaxHighlighting = HighlightingLoader.Load(
                                    xshd,
                                    HighlightingManager.Instance
                                );
                            }
                        }

                        //加载lua日志打印事件
                        LuaEnv.LuaApis.PrintLuaLog += LuaApis_PrintLuaLog;
                        //lua代码出错/结束运行事件
                        LuaEnv.LuaRunEnv.LuaRunError += LuaRunEnv_LuaRunError;

                        //在线脚本列表
                        OnlineScriptsFrame.Navigate(
                            new Uri("Pages/OnlineScriptsPage.xaml", UriKind.Relative)
                        );

                        //关于页面
                        aboutFrame.Navigate(new Uri("Pages/AboutPage.xaml", UriKind.Relative));

                        //tcp测试页面
                        tcpTestFrame.Navigate(new Uri("Pages/tcpTest.xaml", UriKind.Relative));

                        //tcp客户端页面
                        tcpClientFrame.Navigate(
                            new Uri("Pages/SocketClientPage.xaml", UriKind.Relative)
                        );

                        //本地tcp服务器
                        tcpLocalTestFrame.Navigate(
                            new Uri("Pages/TcpLocalPage.xaml", UriKind.Relative)
                        );

                        //本地udp服务器
                        udpLocalTestFrame.Navigate(
                            new Uri("Pages/UdpLocalPage.xaml", UriKind.Relative)
                        );

                        //mqtt测试页面
                        MqttTestFrame.Navigate(
                            new Uri("Pages/MqttTestPage.xaml", UriKind.Relative)
                        );

                        //编码转换工具页面
                        EncodingToolsFrame.Navigate(
                            new Uri("Pages/ConvertPage.xaml", UriKind.Relative)
                        );

                        //乱码修复
                        EncodingFixFrame.Navigate(
                            new Uri("Pages/EncodingFixPage.xaml", UriKind.Relative)
                        );

                        //串口监听
                        SerialMonitorFrame.Navigate(
                            new Uri("Pages/SerialMonitorPage.xaml", UriKind.Relative)
                        );

                        //绘制曲线
                        PlotFrame.Navigate(new Uri("Pages/PlotPage.xaml", UriKind.Relative));

                        //WinUSB
                        WinUSBFrame.Navigate(new Uri("Pages/WinUSBPage.xaml", UriKind.Relative));

                        this.Title +=
                            $" - {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}";

                        TongjiWebBrowser.Source = new Uri(
                            $"https://llcom.papapoi.com/tongji.html?{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}"
                        );

                        new Thread(LuaLogPrintTask).Start();

                        //加载完了，可以允许点击
                        MainGrid.IsEnabled = true;

                        //检查更新
                        if (!Tools.Global.IsMSIX())
                        {
                            Task.Run(() =>
                            {
                                bool runed = false;
                                AutoUpdaterDotNET.AutoUpdater.CheckForUpdateEvent += (args) =>
                                {
                                    if (runed)
                                        return;
                                    runed = true;
                                    if (args.IsUpdateAvailable)
                                    {
                                        Global.HasNewVersion = true; //有新版本
                                        if (Tools.Global.setting.autoUpdate) //开了自动升级功能再开
                                        {
                                            this.Dispatcher.Invoke(
                                                new Action(
                                                    delegate
                                                    {
                                                        AutoUpdaterDotNET.AutoUpdater.ShowUpdateForm(
                                                            args
                                                        );
                                                    }
                                                )
                                            );
                                        }
                                    }
                                };
                                Random r = new Random(); //加上随机参数，确保获取的是最新数据
                                try
                                {
                                    AutoUpdaterDotNET.AutoUpdater.Start(
                                        "https://llcom.papapoi.com/autoUpdate.xml?" + r
                                    );
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
                                Random r = new Random(); //加上随机参数，确保获取的是最新数据
                                var client = new RestClient(
                                    "https://llcom.papapoi.com/hotfix.lua?" + r.Next()
                                );
                                var request = new RestRequest();
                                var response = client.Get(request);
                                var lua = new LuaEnv.LuaEnv();
                                lua.DoString(response.Content);
                            }
                            catch { }
                        }).Start();

                        Tools.Global.RefreshLuaScriptListEvent += (n, s) =>
                        {
                            this.Dispatcher.Invoke(() => _vm.LuaEditor.RefreshList());
                        };
                    }
                )
            );
        });
        Tools.Global.recvScriptBackup = Tools.Global.setting.recvScript;
        if (string.IsNullOrEmpty(Tools.Global.recvScriptBackup))
            Tools.Global.recvScriptBackup = "default";
    }

    private bool DoInvoke(Action action)
    {
        if (Tools.Global.isMainWindowsClosed)
            return false;
        Dispatcher.Invoke(action);
        return true;
    }

    private void Uart_UartDataSent(object sender, EventArgs e)
    {
        Tools.Logger.ShowData(sender as byte[], true);
    }

    private string RawSentTitle = null;

    private void Uart_UartDataRawSent(object sender, EventArgs e)
    {
        if (RawSentTitle is null)
            RawSentTitle = TryFindResource("RawDataSentTitle") as string ?? "?!";
        Tools.Logger.ShowRawData(RawSentTitle, sender as byte[], true);
    }

    private void Uart_UartDataRecived(object sender, EventArgs e)
    {
        Tools.Logger.ShowData(sender as byte[], false);
    }

    private static int UsbPluginDeley = 0;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x219 && !Tools.Global.uart.IsOpen()) // 监听USB设备插拔消息
        {
            if (UsbPluginDeley == 0)
            {
                ++UsbPluginDeley; // Task启动需要准备时间,这里提前对公共变量加一
                Task.Run(() =>
                {
                    do Task.Delay(100).Wait();
                    while (++UsbPluginDeley < 10);
                    UsbPluginDeley = 0;
                    Dispatcher.Invoke(() =>
                    {
                        _vm.OnUsbDeviceChanged();
                    });
                    Logger.AddUartLogInfo($"[USB拔插事件] {DateTime.Now:HH:mm:ss.fff}");
                });
            }
            else
                UsbPluginDeley = 1;
            handled = true;
        }
        return IntPtr.Zero;
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
        _vm.LuaEditor.OnAutoSave();
        Tools.Global.isMainWindowsClosed = true;
        foreach (Window win in App.Current.Windows)
        {
            if (win != this)
            {
                win.Close();
            }
        }
        e.Cancel = false; //正常关闭
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
            System.Diagnostics.Process.Start(
                "explorer.exe",
                Tools.Global.GetTrueProfilePath() + "user_script_run"
            );
        }
        catch
        {
            Tools.MessageBox.Show(
                $"尝试打开文件夹失败，请自行打开该路径：{Tools.Global.GetTrueProfilePath()}user_script_run"
            );
        }
    }

    private void RefreshScriptListButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.LuaEditor.RefreshList();
    }

    /// <summary>Lua 编辑器失焦自动保存</summary>
    private void TextEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        _vm.LuaEditor.OnAutoSave();
    }

    /// <summary>窗口切换后台自动保存</summary>
    private void Window_Deactivated(object sender, EventArgs e)
    {
        _vm.LuaEditor.OnAutoSave();
    }

    /// <summary>窗口激活时检测外部修改（其他编辑器改过则重新加载）</summary>
    private void Window_Activated(object sender, EventArgs e)
    {
        _vm.LuaEditor.CheckExternalChange();
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        Tools.Logger.ClearData();
    }

    private void knowSendDataButton_click(object sender, RoutedEventArgs e)
    {
        ToSendData data = ((Button)sender).Tag as ToSendData;

        // 如果有指定接收脚本，则切换
        if (!string.IsNullOrEmpty(data.recvScriptPath))
        {
            //检查文件是否存在
            if (
                !File.Exists(
                    Tools.Global.ProfilePath + $"user_script_recv_convert/{data.recvScriptPath}.lua"
                )
            )
            {
                Tools.Global.setting.recvScript = "default";
                data.recvScriptPath = "";
                if (
                    !File.Exists(
                        Tools.Global.ProfilePath
                            + $"user_script_recv_convert/{Tools.Global.setting.recvScript}.lua"
                    )
                )
                {
                    File.Create(
                            Tools.Global.ProfilePath
                                + $"user_script_recv_convert/{Tools.Global.setting.recvScript}.lua"
                        )
                        .Close();
                }
            }
            else
            {
                Tools.Global.setting.recvScript = data.recvScriptPath;
            }
        }
        else
        {
            Tools.Global.setting.recvScript = Tools.Global.recvScriptBackup;
        }

        var sendData = data.hex
            ? Global.Hex2Byte(data.text)
            : Global.GetEncoding().GetBytes(data.text);
        _vm.SendUartData(sendData, true);
    }

    private void Button_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // 恢复原有的双击改名功能
        ToSendData data = ((Button)sender).Tag as ToSendData;
        Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(
            TryFindResource("QuickSendSetButton") as string ?? "?!",
            data.commit,
            TryFindResource("QuickSendChangeButton") as string ?? "?!"
        );
        if (ret.Item1)
        {
            ((Button)sender).Content = data.commit = ret.Item2;
        }
    }

    private void NewScriptButton_Click(object sender, RoutedEventArgs e)
    {
        newLuaFileWrapPanel.Visibility = Visibility.Visible;
    }

    private void RunScriptButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_vm.LuaEditor.SelectedFile))
        {
            luaLogTextBox.Clear();
            LuaEnv.LuaRunEnv.New($"user_script_run/{_vm.LuaEditor.SelectedFile}.lua");
            luaScriptEditorGrid.Visibility = Visibility.Collapsed;
            luaLogShowGrid.Visibility = Visibility.Visible;
            luaLogPrintable = true;
        }
        LuaEnv.LuaRunEnv.canRun = true;
    }

    private void NewLuaFilebutton_Click(object sender, RoutedEventArgs e)
    {
        //创建/重名校验与加载都在 VM.CreateNew 内完成
        _vm.LuaEditor.CreateNew(newLuaFileNameTextBox.Text);
        newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
    }

    private void NewLuaFileCancelbutton_Click(object sender, RoutedEventArgs e)
    {
        newLuaFileWrapPanel.Visibility = Visibility.Collapsed;
    }

    //是否可打印标记
    private bool _luaLogPrintable = true;
    private bool luaLogPrintable
    {
        get { return _luaLogPrintable; }
        set
        {
            this.Dispatcher.Invoke(
                new Action(
                    delegate
                    {
                        if (value)
                        {
                            pauseLuaPrintButton.ToolTip =
                                TryFindResource("LuaPause") as string ?? "?!";
                            pauseLuaPrintIcon.Icon = FontAwesomeIcon.Pause;
                        }
                        else
                        {
                            pauseLuaPrintButton.ToolTip =
                                TryFindResource("LuaContinue") as string ?? "?!";
                            pauseLuaPrintIcon.Icon = FontAwesomeIcon.Play;
                        }
                    }
                )
            );
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
        if (sender is string && sender != null)
        {
            lock (luaLogsBuff)
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
                for (int i = 0; i < luaLogsBuff.Count; i++)
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
            DoInvoke(() =>
            {
                luaLogTextBox.IsEnabled = false; //确保文字不再被选中，防止wpf卡死
                if (luaLogCount >= 1000)
                {
                    luaLogTextBox.Clear();
                    luaLogTextBox.AppendText(
                        "Lua log too long, auto clear.\r\n" + "more logs see lua log file.\r\n"
                    );
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
        lock (luaLogsBuff)
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
            LuaEnv.LuaRunEnv.New($"user_script_run/{_vm.LuaEditor.SelectedFile}.lua");
            LuaEnv.LuaRunEnv.canRun = true;
            luaLogPrintable = true;
        }
        else
        {
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
        if (e.Key == Key.Enter)
            LuaEnv.LuaRunEnv.RunCommand(runOneLineLuaTextBox.Text);
    }

    private void ScriptShareButton_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom/blob/master/scripts");
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
        Tuple<bool, string> ret = Tools.InputDialog.OpenDialog(
            TryFindResource("QuickSendChangeIdButton") as string ?? "?!",
            data.id.ToString(),
            (TryFindResource("QuickSendChangeIdTitle") as string ?? "?!") + data.id.ToString()
        );

        if (!ret.Item1)
            return;
        var qs = _vm.QuickSend;
        qs.CheckIds();
        if (ret.Item2.Trim().Length == 0) //留空删除该项目
        {
            qs.Items.RemoveAt(data.id - 1);
        }
        else
        {
            int index = -1;
            int.TryParse(ret.Item2, out index);
            if (index == data.id || index <= 0 || index > qs.Items.Count)
                return;
            //移动到指定位置
            var item = qs.Items[data.id - 1];
            qs.Items.RemoveAt(data.id - 1);
            qs.Items.Insert(index - 1, item);
        }
        qs.Save();
    }

    private void sentCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Tools.Global.setting.SentCount = 0;
    }

    private void receivedCountTextBlock_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        Tools.Global.setting.ReceivedCount = 0;
    }

    private void QuickListNameStackPanel_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _vm.QuickSend.RenamePageCommand.Execute(null);
    }

    private void pauseLuaPrintButton_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
    {
        luaLogTextBox.Clear();
    }

    private void textEditor_TextChanged(object sender, EventArgs e)
    {
        _vm.LuaEditor.OnTextChanged();
    }

    private void uartDataFlowDocument_GotFocus(object sender, RoutedEventArgs e)
    {
        if (Tools.Global.setting.terminal)
            dataShowFrame.BorderBrush = new SolidColorBrush(Color.FromRgb(0, 148, 0));
    }

    private void uartDataFlowDocument_LostFocus(object sender, RoutedEventArgs e)
    {
        dataShowFrame.BorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
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
        if (
            !(Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            || !Tools.Global.setting.terminal
        )
            return;
        if (e.Key >= Key.A && e.Key <= Key.Z && Tools.Global.uart.IsOpen())
            try
            {
                Tools.Global.uart.SendData(new byte[] { (byte)((int)e.Key - (int)Key.A + 1) });
            }
            catch { }
    }

    private void ScriptIcon_Click(object sender, MouseButtonEventArgs e)
    {
        // 点击📜图标时配置接收脚本
        TextBlock icon = sender as TextBlock;
        ToSendData data = icon.Tag as ToSendData;
        recvScriptCombo.ItemsSource = Directory
            .GetFiles(Global.ProfilePath + "user_script_recv_convert", "*.lua")
            .Select(System.IO.Path.GetFileNameWithoutExtension)
            .ToList();
        recvScriptPopup.PlacementTarget = icon;
        recvScriptCombo.Tag = data;
        recvScriptCombo.SelectedItem = data.recvScriptPath ?? "";
        recvScriptCombo.IsDropDownOpen = true;
        recvScriptPopup.IsOpen = false;
        recvScriptPopup.IsOpen = true;

        // 打开对话框，选择接收脚本
        //System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
        //dialog.Filter = "Lua脚本文件 (*.lua)|*.lua|所有文件 (*.*)|*.*";
        //dialog.InitialDirectory = System.IO.Path.Combine(Tools.Global.ProfilePath, "user_script_recv_convert");

        //if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        //{
        //    data.recvScriptPath = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
        //    //SaveSendList(null, EventArgs.Empty);
        //}
    }

    private void ScriptIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        // 右击📜图标时清除接收脚本
        TextBlock icon = sender as TextBlock;
        ToSendData data = icon.Tag as ToSendData;

        // 清除接收脚本项
        if (!string.IsNullOrEmpty(data.recvScriptPath))
        {
            data.recvScriptPath = "";
            //SaveSendList(null, EventArgs.Empty);
        }
    }

    private void recvScriptCombo_DropDownClosed(object sender, EventArgs e)
    {
        ComboBox me = sender as ComboBox;
        ToSendData data = me.Tag as ToSendData;
        string newItem = me.SelectedItem as string;
        if (data.recvScriptPath != newItem)
            data.recvScriptPath = newItem;
        recvScriptPopup.IsOpen = false;
        me.SelectedItem = null;
    }

    [DllImport("user32")]
    public static extern IntPtr SetFocus(IntPtr hWnd);

    private async void ScriptParaIcon_Click(object sender, MouseButtonEventArgs e)
    {
        TextBlock icon = sender as TextBlock;
        ToSendData data = icon.Tag as ToSendData;

        recvScriptParaBox.Tag = data;
        recvScriptParaBox.Text = data.recvScriptPara;
        recvScriptParaBox.ScrollToEnd();
        recvScriptParaPopup.PlacementTarget = icon;
        recvScriptParaPopup.IsOpen = false;
        await Task.Yield();
        recvScriptParaPopup.IsOpen = true;
        await Task.Yield();
        var source = (HwndSource)PresentationSource.FromVisual(recvScriptParaPopup.Child);
        SetFocus(source.Handle);
        await Task.Yield();
        Keyboard.Focus(recvScriptParaBox);
    }

    private void ScriptParaIcon_RightClick(object sender, MouseButtonEventArgs e)
    {
        TextBlock icon = sender as TextBlock;
        ToSendData data = icon.Tag as ToSendData;

        if (!string.IsNullOrEmpty(data.recvScriptPara))
        {
            data.recvScriptPara = "";
            //SaveSendList(null, EventArgs.Empty);
        }
    }

    private void ScriptParaConfirm_Click(object sender, MouseButtonEventArgs e)
    {
        TextBlock icon = sender as TextBlock;
        TextEditor t = icon.Tag as TextEditor;
        ToSendData data = t.Tag as ToSendData;

        data.recvScriptPara = t.Text;
        //SaveSendList(null, EventArgs.Empty);
        recvScriptParaPopup.IsOpen = false;
    }

    private void ScriptParaCancel_Click(object sender, MouseButtonEventArgs e)
    {
        recvScriptParaPopup.IsOpen = false;
    }
}
