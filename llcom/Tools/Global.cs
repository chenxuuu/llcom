using LibUsbDotNet;
using LibUsbDotNet.Main;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                    Logger.CloseUartLog();
                    Logger.CloseLuaLog();
                    if(File.Exists(ProfilePath + "lock"))
                        File.Delete(ProfilePath + "lock");
                }
            }
        }
        //给全局使用的设置参数项
        public static Model.Settings setting;
        public static Model.Uart uart = new Model.Uart();

        //软件根目录
        public static readonly string AppPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName) + "\\";
        //配置文件路径
        public static string ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\llcom\";

        /// <summary>
        /// 获取实际的ProfilePath路径（商店软件里用）
        /// </summary>
        /// <returns></returns>
        public static string GetTrueProfilePath()
        {
            if(IsMSIX())
            {
                //C:\Program Files\WindowsApps\800948F61A16.llcom_1.0.44.0_x86__bd8bdx79914mr\llcom\
                string pkgPath = AppPath.Substring(0,AppPath.LastIndexOf(@"\llcom\"));
                pkgPath = pkgPath.Substring(pkgPath.LastIndexOf("\\") + 1);
                pkgPath = pkgPath.Substring(0, pkgPath.IndexOf("_")) + pkgPath.Substring(pkgPath.LastIndexOf("_"));
                //C:\Users\chenx\AppData\Local\Packages\800948F61A16.llcom_bd8bdx79914mr\LocalCache\Local\llcom
                return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                    @"\Packages\" + pkgPath + @"\LocalCache\Local\llcom\";
            }
            else
            {
                return ProfilePath;
            }
        }

        /// <summary>
        /// 是否为应用商店版本？
        /// </summary>
        /// <returns></returns>
        public static bool IsMSIX()
        {
            return AppPath.ToUpper().Contains(@"C:\PROGRAM FILES\WINDOWSAPPS\");
        }

        /// <summary>
        /// 软件打开后，所有东西的初始化流程
        /// </summary>
        public static void Initial()
        {
            //C:\Users\chenx\AppData\Local\Temp\7zO05433053\user_script_run
            if(AppPath.ToUpper().Contains(@"\APPDATA\LOCAL\TEMP\"))
            {
                System.Windows.MessageBox.Show("请勿在压缩包内直接打开本软件。");
                Environment.Exit(1);
            }

            if(IsMSIX())//商店软件的文件路径改一下
            {
                if (!Directory.Exists(ProfilePath))
                {
                    Directory.CreateDirectory(ProfilePath);
                }
                //升级的时候不会自动升级核心脚本，所以先强制删掉再释放，确保是最新的
                if (Directory.Exists(ProfilePath + "core_script"))
                    Directory.Delete(ProfilePath + "core_script", true);
            }
            else
            {
                ProfilePath = AppPath;//普通exe时，直接用软件路径
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
                if (!Directory.Exists(ProfilePath+"core_script"))
                {
                    Directory.CreateDirectory(ProfilePath+"core_script");
                }
                CreateFile("DefaultFiles/core_script/head.lua", ProfilePath+"core_script/head.lua", false);
                CreateFile("DefaultFiles/core_script/JSON.lua", ProfilePath+"core_script/JSON.lua", false);
                CreateFile("DefaultFiles/core_script/log.lua", ProfilePath+"core_script/log.lua", false);
                CreateFile("DefaultFiles/core_script/once.lua", ProfilePath+"core_script/once.lua", true);
                CreateFile("DefaultFiles/core_script/strings.lua", ProfilePath+"core_script/strings.lua", false);
                CreateFile("DefaultFiles/core_script/sys.lua", ProfilePath+"core_script/sys.lua", true);

                if (!Directory.Exists(ProfilePath+"logs"))
                    Directory.CreateDirectory(ProfilePath+"logs");
                if (!Directory.Exists(ProfilePath+"user_script_run"))
                {
                    Directory.CreateDirectory(ProfilePath+"user_script_run");
                    CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-快发模式.lua", ProfilePath+"user_script_run/AT控制TCP连接-快发模式.lua");
                    CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-慢发模式.lua", ProfilePath+"user_script_run/AT控制TCP连接-慢发模式.lua");
                    CreateFile("DefaultFiles/user_script_run/example.lua", ProfilePath+"user_script_run/example.lua");
                    CreateFile("DefaultFiles/user_script_run/循环发送快捷发送区数据.lua", ProfilePath+"user_script_run/循环发送快捷发送区数据.lua");
                }
                if (!Directory.Exists(ProfilePath+"user_script_run/requires"))
                    Directory.CreateDirectory(ProfilePath+"user_script_run/requires");
                if (!Directory.Exists(ProfilePath+"user_script_run/logs"))
                    Directory.CreateDirectory(ProfilePath+"user_script_run/logs");

                if (!Directory.Exists(ProfilePath+"user_script_send_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script_send_convert");
                    CreateFile("DefaultFiles/user_script_send_convert/16进制数据.lua", ProfilePath+"user_script_send_convert/16进制数据.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/GPS NMEA.lua", ProfilePath+"user_script_send_convert/GPS NMEA.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/加上换行回车.lua", ProfilePath+"user_script_send_convert/加上换行回车.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/解析换行回车的转义字符.lua", ProfilePath+"user_script_send_convert/解析换行回车的转义字符.lua");
                    CreateFile("DefaultFiles/user_script_send_convert/默认.lua", ProfilePath+"user_script_send_convert/默认.lua");
                }
                if (!Directory.Exists(ProfilePath + "user_script_recv_convert"))
                {
                    Directory.CreateDirectory(ProfilePath + "user_script_recv_convert");
                }
                if (!File.Exists(ProfilePath + "user_script_recv_convert/默认.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/默认.lua", ProfilePath + "user_script_recv_convert/默认.lua");
                if (!File.Exists(ProfilePath + "user_script_recv_convert/绘制曲线.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线.lua", ProfilePath + "user_script_recv_convert/绘制曲线.lua");
                if (!File.Exists(ProfilePath + "user_scrispt_recv_convert/绘制曲线-多条.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-多条.lua", ProfilePath + "user_script_recv_convert/绘制曲线-多条.lua");
                if (!File.Exists(ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua"))
                    CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-解析结构体.lua", ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua");

                CreateFile("DefaultFiles/LICENSE", ProfilePath+"LICENSE", false);
                CreateFile("DefaultFiles/反馈网址.txt", ProfilePath+"反馈网址.txt", false);
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show("生成文件结构失败，请确保本软件处于有读写权限的目录下再打开。\r\n错误信息："+e.Message);
                Environment.Exit(1);
            }

            //配置文件
            if(File.Exists(ProfilePath+"settings.json"))
            {
                //cost 309ms
                setting = JsonConvert.DeserializeObject<Model.Settings>(File.ReadAllText(ProfilePath+"settings.json"));
                setting.SentCount = 0;
                setting.ReceivedCount = 0;
            }
            else
            {
                setting = new Model.Settings();
            }


            uart.serial.BaudRate = setting.baudRate;
            uart.serial.Parity = (Parity)setting.parity;
            uart.serial.DataBits = setting.dataBits;
            uart.serial.StopBits = (StopBits)setting.stopBit;
            uart.UartDataRecived += Uart_UartDataRecived;
            uart.UartDataSent += Uart_UartDataSent;
            LuaEnv.LuaRunEnv.init();
        }

        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"<-{Byte2String((byte[])sender)}");
            Logger.AddUartLogDebug($"[HEX]{Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"->{Byte2String((byte[])sender)}");
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

    }
}
