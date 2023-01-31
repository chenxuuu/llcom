using LibUsbDotNet.Info;
using LibUsbDotNet.LibUsb;
using llcom.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Shapes;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace llcom.Tools
{
    class Global
    {
        //api接口文档网址
        public static string apiDocumentUrl = "https://github.com/chenxuuu/llcom/blob/master/LuaApi.md";
        //主窗口是否被关闭？
        private static bool _isMainWindowsClosed = false;
        public static bool isMainWindowsClosed
        {
            get
            {
                return _isMainWindowsClosed;
            }
            set
            {
                _isMainWindowsClosed = value;
                if (value)
                {
                    uart.WaitUartReceive.Set();
                    Logger.CloseUartLog();
                    Logger.CloseLuaLog();
                    if (File.Exists(ProfilePath + "lock"))
                        File.Delete(ProfilePath + "lock");
                }
            }
        }
        //给全局使用的设置参数项
        public static Model.Settings setting;
        public static Model.Uart uart = new Model.Uart();

        //软件文件名
        private static string _fileName = "";
        public static string FileName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(_fileName))
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _fileName = System.IO.Path.GetFileName(processModule?.FileName);
                    }
                }
                return _fileName;
            }
        }

        //软件根目录
        private static string _appPath = null;
        /// <summary>
        /// 软件根目录（末尾带\）
        /// </summary>
        public static string AppPath
        {
            get
            {
                if (_appPath == null)
                {
                    using (var processModule = Process.GetCurrentProcess().MainModule)
                    {
                        _appPath = System.IO.Path.GetDirectoryName(processModule?.FileName);
                    }
                    if (!_appPath.EndsWith("\\"))
                        _appPath = _appPath + "\\";
                }
                return _appPath;
            }
        }

        //配置文件路径（普通exe时，会被替换为AppPath）
        public static string ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\llcom\";

        /// <summary>
        /// 获取实际的ProfilePath路径（目前没啥用了）
        /// </summary>
        /// <returns></returns>
        public static string GetTrueProfilePath()
        {
            return ProfilePath;
        }

        /// <summary>
        /// 是否为应用商店版本？
        /// </summary>
        /// <returns></returns>
        public static bool IsMSIX()
        {
            return AppPath.ToUpper().Contains(@"\PROGRAM FILES\WINDOWSAPPS\");
        }

        /// <summary>
        /// 是否上报bug？低版本.net框架的上报行为将被限制
        /// </summary>
        public static bool ReportBug { get; set; } = true;

        /// <summary>
        /// 是否有新版本？
        /// </summary>
        public static bool HasNewVersion { get; set; } = false;


        /// <summary>
        /// 更换软件标题栏文字
        /// </summary>
        public static event EventHandler<string> ChangeTitleEvent;
        public static void ChangeTitle(string s) => ChangeTitleEvent?.Invoke(null, s);

        /// <summary>
        /// 刷新lua脚本列表
        /// </summary>
        public static event EventHandler RefreshLuaScriptListEvent;
        public static void RefreshLuaScriptList() => RefreshLuaScriptListEvent?.Invoke(null, null);

        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadSetting()
        {
            if (IsMSIX())
            {
                if (Directory.Exists(ProfilePath))
                {
                    //已经开过一次了，那就继续用之前的路径
                }
                else
                {
                    //appdata路径不可靠，用文档路径替代
                    ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\llcom\\";
                    if (!Directory.Exists(ProfilePath))
                        Directory.CreateDirectory(ProfilePath);
                }
            }
            else
            {
                ProfilePath = AppPath;//普通exe时，直接用软件路径
            }
            //配置文件
            if (File.Exists(ProfilePath + "settings.json"))
            {
                try
                {
                    //cost 309ms
                    setting = JsonConvert.DeserializeObject<Model.Settings>(File.ReadAllText(ProfilePath + "settings.json"));
                    setting.SentCount = 0;
                    setting.ReceivedCount = 0;
                    setting.DisableLog = false;
                }
                catch
                {
                    MessageBox.Show($"配置文件加载失败！\r\n" +
                        $"如果是配置文件损坏，可前往{ProfilePath}settings.json.bakup查找备份文件\r\n" +
                        $"并使用该文件替换{ProfilePath}settings.json文件恢复配置");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (Directory.GetFiles(ProfilePath).Length > 10)
                {
                    var r = Tools.InputDialog.OpenDialog("检测到当前文件夹有其他文件\r\n" +
                        "建议新建一个文件夹给llcom，并将llcom.exe放入其中\r\n" +
                        "不然当前文件夹会显得很乱哦~\r\n" +
                        "是否想要继续运行呢？", null, "温馨提示");
                    if (!r.Item1)
                        Environment.Exit(1);
                }
                setting = new Model.Settings();
            }
        }

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial()
        {
            //检查.net版本
            var currentVersion = Walterlv.NdpInfo.GetCurrentVersionName();
            try
            {
                if (currentVersion.StartsWith("4."))
                {
                    var sv = int.Parse(currentVersion.Substring(2, 1));
                    if (sv < 6)
                        throw new Exception();
                }
                else
                {
                    throw new Exception();
                }
            }
            catch
            {
                MessageBox.Show($"本软件仅支持.net framework 4.6.2以上版本，该计算机上的最高版本为{currentVersion}\r\n" +
                    $"你可以选择继续使用，但若运行途中遇到bug，将不会上报给开发者。\r\n" +
                    $"建议升级到最新.net framework版本");
                ReportBug = false;
            }
            //文件名不能改！
            if (FileName.ToUpper() != "LLCOM.EXE")
            {
                MessageBox.Show("啊呀呀，软件文件名被改了。。。\r\n" +
                    "为了保证软件功能的正常运行，请将exe名改回llcom.exe");
                Environment.Exit(1);
            }
            //C:\Users\chenx\AppData\Local\Temp\7zO05433053\user_script_run
            if (AppPath.ToUpper().Contains(@"\APPDATA\LOCAL\TEMP\") ||
                AppPath.ToUpper().Contains(@"\WINDOWS\TEMP\"))
            {
                System.Windows.MessageBox.Show("请勿在压缩包内直接打开本软件。");
                Environment.Exit(1);
            }

            if (IsMSIX())//商店软件的文件路径需要手动新建文件夹
            {
                if (!Directory.Exists(ProfilePath))
                {
                    Directory.CreateDirectory(ProfilePath);
                }
                //升级的时候不会自动升级核心脚本，所以先强制删掉再释放，确保是最新的
                if (Directory.Exists(ProfilePath + "core_script"))
                    Directory.Delete(ProfilePath + "core_script", true);
            }

            //检测多开
            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            if (processes.Length > 1 && File.Exists(ProfilePath + "lock"))
            {
                MessageBox.Show("不支持同文件夹多开！\r\n如需多开，请在多个文件夹分别存放llcom.exe后，分别运行。");
                Environment.Exit(1);
            }
            File.Create(ProfilePath + "lock").Close();
            try
            {
                if (!Directory.Exists(ProfilePath + "core_script"))
                {
                    Directory.CreateDirectory(ProfilePath + "core_script");
                }
                CreateFile("DefaultFiles/core_script/head.lua", ProfilePath + "core_script/head.lua", true);
                CreateFile("DefaultFiles/core_script/JSON.lua", ProfilePath + "core_script/JSON.lua", false);
                CreateFile("DefaultFiles/core_script/log.lua", ProfilePath + "core_script/log.lua", false);
                CreateFile("DefaultFiles/core_script/once.lua", ProfilePath + "core_script/once.lua", true);
                CreateFile("DefaultFiles/core_script/strings.lua", ProfilePath + "core_script/strings.lua", false);
                CreateFile("DefaultFiles/core_script/sys.lua", ProfilePath + "core_script/sys.lua", true);

                if (!Directory.Exists(ProfilePath + "logs"))
                    Directory.CreateDirectory(ProfilePath + "logs");
                if (!Directory.Exists(ProfilePath + "user_script_run"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script_run");
                    CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-快发模式.lua", ProfilePath + "user_script_run/AT控制TCP连接-快发模式.lua");
                    CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-慢发模式.lua", ProfilePath + "user_script_run/AT控制TCP连接-慢发模式.lua");
                    CreateFile("DefaultFiles/user_script_run/example.lua", ProfilePath + "user_script_run/example.lua");
                    CreateFile("DefaultFiles/user_script_run/循环发送快捷发送区数据.lua", ProfilePath + "user_script_run/循环发送快捷发送区数据.lua");
                }
                //通用消息通道的demo
                if (!File.Exists(ProfilePath + "user_script_run/channel-demo.lua"))
                    CreateFile("DefaultFiles/user_script_run/channel-demo.lua", ProfilePath + "user_script_run/channel-demo.lua");

                if (!Directory.Exists(ProfilePath + "user_script_run/requires"))
                    Directory.CreateDirectory(ProfilePath + "user_script_run/requires");
                if (!Directory.Exists(ProfilePath + "user_script_run/logs"))
                    Directory.CreateDirectory(ProfilePath + "user_script_run/logs");

                if (!Directory.Exists(ProfilePath + "user_script_send_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script_send_convert");
                    CreateFile("DefaultFiles/user_script_send_convert/16进制数据.lua", ProfilePath + "user_script_send_convert/16进制数据.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/GPS NMEA.lua", ProfilePath + "user_script_send_convert/GPS NMEA.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/加上换行回车.lua", ProfilePath + "user_script_send_convert/加上换行回车.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/解析换行回车的转义字符.lua", ProfilePath + "user_script_send_convert/解析换行回车的转义字符.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/default.lua", ProfilePath + "user_script_send_convert/default.lua");
                }
                if (!Directory.Exists(ProfilePath + "user_script_recv_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script_recv_convert");
                }
                if (!File.Exists(ProfilePath + "user_script_recv_convert/default.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/default.lua", ProfilePath + "user_script_recv_convert/default.lua");
                if (!File.Exists(ProfilePath + "user_script_recv_convert/绘制曲线.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线.lua", ProfilePath + "user_script_recv_convert/绘制曲线.lua");
                if (!File.Exists(ProfilePath + "user_scrispt_recv_convert/绘制曲线-多条.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-多条.lua", ProfilePath + "user_script_recv_convert/绘制曲线-多条.lua");
                if (!File.Exists(ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-解析结构体.lua", ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua");

                CreateFile("DefaultFiles/LICENSE", ProfilePath + "LICENSE", false);
                CreateFile("DefaultFiles/反馈网址.txt", ProfilePath + "反馈网址.txt", false);

                if (IntPtr.Size == 8)
                    CreateFile("DefaultFiles/libusb-1.0-x64.dll", ProfilePath + "libusb-1.0", false);
                else
                    CreateFile("DefaultFiles/libusb-1.0-x86.dll", ProfilePath + "libusb-1.0", false);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("生成文件结构失败，请确保本软件处于有读写权限的目录下再打开。\r\n错误信息：" + e.Message);
                Environment.Exit(1);
            }

            //加载配置文件改成单独拎出来了

            //备份一下文件好了（心理安慰）
            if (File.Exists(ProfilePath + "settings.json"))
            {
                if (File.Exists(ProfilePath + "settings.json.bakup"))
                    File.Delete(ProfilePath + "settings.json.bakup");
                File.Copy(ProfilePath + "settings.json", ProfilePath + "settings.json.bakup");
            }

            uart.serial.BaudRate = setting.baudRate;
            uart.serial.Parity = (Parity)setting.parity;
            uart.serial.DataBits = setting.dataBits;
            uart.serial.StopBits = (StopBits)setting.stopBit;
            uart.UartDataRecived += Uart_UartDataRecived;
            uart.UartDataSent += Uart_UartDataSent;
        }

        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"<-{Byte2Readable((byte[])sender)}");
            Logger.AddUartLogDebug($"[HEX]{Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"->{Byte2Readable((byte[])sender)}");
            Logger.AddUartLogDebug($"[HEX]{Byte2Hex((byte[])sender, " ")}");
        }

        public static Encoding GetEncoding() => Encoding.GetEncoding(setting.encoding);

        /// <summary>
        /// 字符串转hex值
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="space">间隔符号</param>
        /// <returns>结果</returns>
        public static string String2Hex(string str, string space)
        {
            return BitConverter.ToString(GetEncoding().GetBytes(str)).Replace("-", space);
        }


        /// <summary>
        /// hex值转字符串
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static string Hex2String(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return "";
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return GetEncoding().GetString(vBytes);
        }


        /// <summary>
        /// byte转string
        /// </summary>
        /// <param name="mHex"></param>
        /// <returns></returns>
        public static string Byte2String(byte[] vBytes)
        {
            var br = from e in vBytes
                     where e != 0
                     select e;
            return GetEncoding().GetString(br.ToArray());
        }

        private static byte[] b_del = Encoding.GetEncoding(65001).GetBytes("␡");

        private static byte[][] symbols = null;
        /// <summary>
        /// byte转string（可读）
        /// </summary>
        /// <param name="vBytes"></param>
        /// <returns></returns>
        public static string Byte2Readable(byte[] vBytes)
        {
            //没开这个功能/非utf8就别搞了
            if (!setting.EnableSymbol || setting.encoding != 65001)
                return Byte2String(vBytes);
            //初始化一下这个数组
            if (symbols == null)
            {
                symbols = new byte[32][];
                string[] tc = { "␀", "␁", "␂", "␃", "␄", "␅", "␆", "␇", "␈", "␉", "␊", "␋", "␌", "␍",
                    "␎", "␏", "␐", "␑", "␒", "␓", "␔", "␕", "␖", "␗", "␘", "␙", "␚", "␛", "␜", "␝", "␞", "␟" };
                for (int i = 0; i < 32; i++)
                    symbols[i] = Encoding.GetEncoding(65001).GetBytes(tc[i]);
            }
            var tb = new List<byte>();
            for (int i = 0; i < vBytes.Length; i++)
            {
                switch(vBytes[i])
                {
                    case 0x0d:
                        //遇到成对出现
                        if(i < vBytes.Length-1 && vBytes[i+1] == 0x0a)
                        {
                            tb.AddRange(symbols[0x0d]);
                            tb.AddRange(symbols[0x0a]);
                            tb.Add(0x0d);
                            tb.Add(0x0a);
                            i++;
                        }
                        else
                        {
                            tb.AddRange(symbols[0x0d]);
                            tb.Add(vBytes[i]);
                        }
                        break;
                    case 0x0a:
                    case 0x09://tab字符
                        tb.AddRange(symbols[vBytes[i]]);
                        tb.Add(vBytes[i]);
                        break;
                    default:
                        //普通的字符
                        if(vBytes[i] <= 0x1f)
                            tb.AddRange(symbols[vBytes[i]]);
                        else if (vBytes[i] == 0x7f)//del
                            tb.AddRange(b_del);
                        else
                            tb.Add(vBytes[i]);
                        break;
                }
            }
            return GetEncoding().GetString(tb.ToArray());
        }

        /// <summary>
        /// hex转byte
        /// </summary>
        /// <param name="mHex">hex值</param>
        /// <returns>原始字符串</returns>
        public static byte[] Hex2Byte(string mHex)
        {
            mHex = Regex.Replace(mHex, "[^0-9A-Fa-f]", "");
            if (mHex.Length % 2 != 0)
                mHex = mHex.Remove(mHex.Length - 1, 1);
            if (mHex.Length <= 0) return new byte[0];
            byte[] vBytes = new byte[mHex.Length / 2];
            for (int i = 0; i < mHex.Length; i += 2)
                if (!byte.TryParse(mHex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                    vBytes[i / 2] = 0;
            return vBytes;
        }


        public static string Byte2Hex(byte[] d, string s = "")
        {
            return BitConverter.ToString(d).Replace("-", s);
        }


        /// <summary>
        /// 导入SSCOM配置文件数据
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Model.ToSendData> ImportFromSSCOM(string path)
        {
            var lines = File.ReadAllLines(path, Encoding.GetEncoding("GB2312"));
            var r = new List<Model.ToSendData>();
            Regex title = new Regex(@"N1\d\d=\d*,");
            for (int i = 0; i < lines.Length; i++)
            {
                try
                {
                    var temp = new Model.ToSendData();
                    //Console.WriteLine(lines[i]);
                    if (title.IsMatch(lines[i]))//匹配上了
                    {
                        var strs = lines[i].Split(",".ToCharArray()[0]);
                        temp.commit = strs[1].Replace(((char)2).ToString(), ",");
                        if (string.IsNullOrWhiteSpace(temp.commit))
                            temp.commit = "发送";
                        //Console.WriteLine(temp.commit);

                        int dot = lines[i + 1].IndexOf(",");
                        temp.hex = lines[i + 1].Substring(dot - 1, 1) == "H";
                        //Console.WriteLine(strs[0].Substring(strs[0].Length - 1));

                        string text = lines[i + 1].Substring(dot + 1);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            temp.text = text.Replace(((char)2).ToString(), ",");
                            r.Add(temp);
                        }
                    }
                }
                catch
                {
                    //先不处理
                }
            }
            return r;
        }

        /// <summary>
        /// 读取软件资源文件内容
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>内容字节数组</returns>
        public static byte[] GetAssetsFileContent(string path)
        {
            Uri uri = new Uri(path, UriKind.Relative);
            var source = System.Windows.Application.GetResourceStream(uri).Stream;
            byte[] f = new byte[source.Length];
            source.Read(f, 0, (int)source.Length);
            return f;
        }

        /// <summary>
        /// 取出文件
        /// </summary>
        /// <param name="insidePath">软件内部的路径</param>
        /// <param name="outPath">需要释放到的路径</param>
        /// <param name="d">是否覆盖</param>
        public static void CreateFile(string insidePath, string outPath, bool d = true)
        {
            if(!File.Exists(outPath) || d)
                File.WriteAllBytes(outPath, GetAssetsFileContent(insidePath));
        }

        /// <summary>
        /// 更换语言文件
        /// </summary>
        /// <param name="languagefileName"></param>
        public static void LoadLanguageFile(string languagefileName)
        {
            try
            {
                System.Windows.Application.Current.Resources.MergedDictionaries[0] = new System.Windows.ResourceDictionary()
                {
                    Source = new Uri($"pack://application:,,,/languages/{languagefileName}.xaml", UriKind.RelativeOrAbsolute)
                };
            }
            catch
            {
                System.Windows.Application.Current.Resources.MergedDictionaries[0] = new System.Windows.ResourceDictionary()
                {
                    Source = new Uri("pack://application:,,,/languages/en-US.xaml", UriKind.RelativeOrAbsolute)
                };
            }

        }

        private static string GitHubToken = null;
        /// <summary>
        /// 获取
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static List<OnlineScript> GetOnlineScripts(Action<int,int> callback = null)
        {
            if(GitHubToken == null)
            {
                try
                {
                    var client = new RestClient("https://llcom.papapoi.com/token.txt");
                    var request = new RestRequest();
                    request.Timeout = 10000;
                    var response = client.Get(request);
                    GitHubToken = response.Content;
                }
                catch
                {
                    return null;
                }
            }
            //请求函数
            var req = (string after) =>
            {
                var client = new RestClient();
                client.BaseUrl = new Uri("https://api.github.com/graphql");
                var request = new RestRequest(RestSharp.Method.POST);
                request.AddHeader("user-agent", "llcom");
                request.AddHeader("Authorization", $"bearer {GitHubToken}");
                request.AddParameter("application/json", 
                    "{\r\n  \"query\": \"query {repository(owner: \\\"chenxuuu\\\", name: \\\"llcom\\\") {discussions(categoryId:\\\"DIC_kwDOCtNzks4CSz35\\\"," +
                    (after == null ? "" : $"after: \"{after}\"") + "first: 100" +
                    ") {totalCount,pageInfo {startCursor,endCursor,hasNextPage,hasPreviousPage},nodes {body,url}}}}\"\r\n}", ParameterType.RequestBody);
                var response = client.Execute(request);
                var j = JsonConvert.DeserializeObject<JObject>(response.Content);
                var bodys = from i in j["data"]["repository"]["discussions"]["nodes"]
                            select ((string)i["body"],(string)i["url"]);
                return (
                    bodys.ToList(),
                    (int)j["data"]["repository"]["discussions"]["totalCount"],
                    (string)j["data"]["repository"]["discussions"]["pageInfo"]["endCursor"],
                    (bool)j["data"]["repository"]["discussions"]["pageInfo"]["hasNextPage"]
                );
            };

            string lastPage = null;
            var scripts = new List<OnlineScript>();
            var pages = 0;
            while(true)
            {
                var (data, total, endCursor, hasNextPage) = req(lastPage);
                foreach (var (s,u) in data)
                {
                    try
                    {
                        var n = new OnlineScript(s);
                        n.Url = u;
                        scripts.Add(n);
                    }
                    catch { }
                }
                callback?.Invoke(pages, total/100);//回调，上报进度
                if (!hasNextPage)
                    break;
                pages++;
                lastPage = endCursor;
            }
            return scripts;
        }

    }
}
