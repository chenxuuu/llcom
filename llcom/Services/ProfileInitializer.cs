using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using llcom.Model;

namespace llcom.Tools
{
    /// <summary>
    /// 配置文件与环境初始化服务。
    /// 负责：加载/保存设置（settings.json）、软件首次运行的文件结构生成、
    /// 多开检测、环境检查（.net 版本 / 文件名 / 压缩包内运行）、串口事件日志挂接。
    /// 从原 Tools/Global.cs 拆出（Step 2）。
    /// </summary>
    internal static class ProfileInitializer
    {
        /// <summary>
        /// 加载配置文件
        /// </summary>
        public static void LoadSetting()
        {
            if (AppPaths.IsMSIX())
            {
                if (Directory.Exists(AppPaths.ProfilePath))
                {
                    //已经开过一次了，那就继续用之前的路径
                }
                else
                {
                    //appdata路径不可靠，用文档路径替代
                    AppPaths.ProfilePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\llcom\\";
                    if (!Directory.Exists(AppPaths.ProfilePath))
                        Directory.CreateDirectory(AppPaths.ProfilePath);
                }
            }
            else
            {
                AppPaths.ProfilePath = AppPaths.AppPath;//普通exe时，直接用软件路径
            }
            //配置文件
            if (File.Exists(AppPaths.ProfilePath + "settings.json"))
            {
                try
                {
                    //cost 309ms
                    Tools.Global.setting = JsonConvert.DeserializeObject<Model.Settings>(File.ReadAllText(AppPaths.ProfilePath + "settings.json"));
                    Tools.Global.setting.SentCount = 0;
                    Tools.Global.setting.ReceivedCount = 0;
                    Tools.Global.setting.DisableLog = false;
                }
                catch
                {
                    Tools.MessageBox.Show($"配置文件加载失败！\r\n" +
                        $"如果是配置文件损坏，可前往{AppPaths.ProfilePath}settings.json.bakup查找备份文件\r\n" +
                        $"并使用该文件替换{AppPaths.ProfilePath}settings.json文件恢复配置");
                    Environment.Exit(1);
                }
            }
            else
            {
                if (Directory.GetFiles(AppPaths.ProfilePath).Length > 10)
                {
                    var r = Tools.InputDialog.OpenDialog("检测到当前文件夹有其他文件\r\n" +
                        "建议新建一个文件夹给llcom，并将llcom.exe放入其中\r\n" +
                        "不然当前文件夹会显得很乱哦~\r\n" +
                        "是否想要继续运行呢？", null, "温馨提示");
                    if (!r.Item1)
                        Environment.Exit(1);
                }
                Tools.Global.setting = new Model.Settings();
            }
            LocalizationService.LoadLanguageFile(Tools.Global.setting.language);
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
                Tools.MessageBox.Show($"本软件仅支持.net framework 4.6.2以上版本，该计算机上的最高版本为{currentVersion}\r\n" +
                    $"你可以选择继续使用，但若运行途中遇到bug，将不会上报给开发者。\r\n" +
                    $"建议升级到最新.net framework版本");
                Tools.Global.ReportBug = false;
            }
            //文件名不能改！
            if (AppPaths.FileName.ToUpper() != "LLCOM.EXE")
            {
                Tools.MessageBox.Show("啊呀呀，软件文件名被改了。。。\r\n" +
                    "为了保证软件功能的正常运行，请将exe名改回llcom.exe");
                Environment.Exit(1);
            }
            //C:\Users\chenx\AppData\Local\Temp\7zO05433053\user_script_run
            if (AppPaths.AppPath.ToUpper().Contains(@"\APPDATA\LOCAL\TEMP\") ||
                AppPaths.AppPath.ToUpper().Contains(@"\WINDOWS\TEMP\"))
            {
                Tools.MessageBox.Show("请勿在压缩包内直接打开本软件。");
                Environment.Exit(1);
            }

            if (AppPaths.IsMSIX())//商店软件的文件路径需要手动新建文件夹
            {
                if (!Directory.Exists(AppPaths.ProfilePath))
                {
                    Directory.CreateDirectory(AppPaths.ProfilePath);
                }
                //升级的时候不会自动升级核心脚本，所以先强制删掉再释放，确保是最新的
                if (Directory.Exists(AppPaths.ProfilePath + "core_script"))
                    Directory.Delete(AppPaths.ProfilePath + "core_script", true);
            }

            //检测多开
            string processName = Process.GetCurrentProcess().ProcessName;
            Process[] processes = Process.GetProcessesByName(processName);
            //如果该数组长度大于1，说明多次运行
            if (processes.Length > 1 && File.Exists(AppPaths.ProfilePath + "lock"))
            {
                Tools.MessageBox.Show("不支持同文件夹多开！\r\n如需多开，请在多个文件夹分别存放llcom.exe后，分别运行。");
                Environment.Exit(1);
            }
            File.Create(AppPaths.ProfilePath + "lock").Close();
            try
            {
                if (!Directory.Exists(AppPaths.ProfilePath + "core_script"))
                {
                    Directory.CreateDirectory(AppPaths.ProfilePath + "core_script");
                }
                FileUtils.CreateFile("DefaultFiles/core_script/head.lua", AppPaths.ProfilePath + "core_script/head.lua", true);
                FileUtils.CreateFile("DefaultFiles/core_script/JSON.lua", AppPaths.ProfilePath + "core_script/JSON.lua", false);
                FileUtils.CreateFile("DefaultFiles/core_script/log.lua", AppPaths.ProfilePath + "core_script/log.lua", false);
                FileUtils.CreateFile("DefaultFiles/core_script/strings.lua", AppPaths.ProfilePath + "core_script/strings.lua", false);
                FileUtils.CreateFile("DefaultFiles/core_script/sys.lua", AppPaths.ProfilePath + "core_script/sys.lua", true);

                if (!Directory.Exists(AppPaths.ProfilePath + "logs"))
                    Directory.CreateDirectory(AppPaths.ProfilePath + "logs");
                if (!Directory.Exists(AppPaths.ProfilePath + "user_script_run"))
                {
                    Directory.CreateDirectory(AppPaths.ProfilePath + "user_script_run");
                    FileUtils.CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-快发模式.lua", AppPaths.ProfilePath + "user_script_run/AT控制TCP连接-快发模式.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_run/AT控制TCP连接-慢发模式.lua", AppPaths.ProfilePath + "user_script_run/AT控制TCP连接-慢发模式.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_run/example.lua", AppPaths.ProfilePath + "user_script_run/example.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_run/循环发送快捷发送区数据.lua", AppPaths.ProfilePath + "user_script_run/循环发送快捷发送区数据.lua");
                }
                //通用消息通道的demo
                if (!File.Exists(AppPaths.ProfilePath + "user_script_run/channel-demo.lua"))
                    FileUtils.CreateFile("DefaultFiles/user_script_run/channel-demo.lua", AppPaths.ProfilePath + "user_script_run/channel-demo.lua");

                if (!Directory.Exists(AppPaths.ProfilePath + "user_script_run/requires"))
                    Directory.CreateDirectory(AppPaths.ProfilePath + "user_script_run/requires");
                if (!Directory.Exists(AppPaths.ProfilePath + "user_script_run/logs"))
                    Directory.CreateDirectory(AppPaths.ProfilePath + "user_script_run/logs");

                if (!Directory.Exists(AppPaths.ProfilePath + "user_script_send_convert"))
                {
                    Directory.CreateDirectory(AppPaths.ProfilePath + "user_script_send_convert");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/checksum.lua", AppPaths.ProfilePath + "user_script_send_convert/checksum.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/16进制数据.lua", AppPaths.ProfilePath + "user_script_send_convert/16进制数据.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/GPS NMEA.lua", AppPaths.ProfilePath + "user_script_send_convert/GPS NMEA.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/加上换行回车.lua", AppPaths.ProfilePath + "user_script_send_convert/加上换行回车.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/解析换行回车的转义字符.lua", AppPaths.ProfilePath + "user_script_send_convert/解析换行回车的转义字符.lua");
                    FileUtils.CreateFile("DefaultFiles/user_script_send_convert/default.lua", AppPaths.ProfilePath + "user_script_send_convert/default.lua");
                }
                if (!Directory.Exists(AppPaths.ProfilePath + "user_script_recv_convert"))
                {
                    Directory.CreateDirectory(AppPaths.ProfilePath + "user_script_recv_convert");
                }
                if (!File.Exists(AppPaths.ProfilePath + "user_script_recv_convert/default.lua"))
                    FileUtils.CreateFile("DefaultFiles/user_script_recv_convert/default.lua", AppPaths.ProfilePath + "user_script_recv_convert/default.lua");
                if (!File.Exists(AppPaths.ProfilePath + "user_script_recv_convert/绘制曲线.lua"))
                    FileUtils.CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线.lua", AppPaths.ProfilePath + "user_script_recv_convert/绘制曲线.lua");
                if (!File.Exists(AppPaths.ProfilePath + "user_scrispt_recv_convert/绘制曲线-多条.lua"))
                    FileUtils.CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-多条.lua", AppPaths.ProfilePath + "user_script_recv_convert/绘制曲线-多条.lua");
                if (!File.Exists(AppPaths.ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua"))
                    FileUtils.CreateFile("DefaultFiles/user_script_recv_convert/绘制曲线-解析结构体.lua", AppPaths.ProfilePath + "user_script_recv_convert/绘制曲线-解析结构体.lua");

                FileUtils.CreateFile("DefaultFiles/LICENSE", AppPaths.ProfilePath + "LICENSE", false);
                FileUtils.CreateFile("DefaultFiles/反馈网址.txt", AppPaths.ProfilePath + "反馈网址.txt", false);

                if (IntPtr.Size == 8)
                    FileUtils.CreateFile("DefaultFiles/libusb-1.0-x64.dll", AppPaths.ProfilePath + "libusb-1.0", false);
                else
                    FileUtils.CreateFile("DefaultFiles/libusb-1.0-x86.dll", AppPaths.ProfilePath + "libusb-1.0", false);
            }
            catch (Exception e)
            {
                Tools.MessageBox.Show("生成文件结构失败，请确保本软件处于有读写权限的目录下再打开。\r\n错误信息：" + e.Message);
                Environment.Exit(1);
            }

            //加载配置文件改成单独拎出来了

            //备份一下文件好了（心理安慰）
            if (File.Exists(AppPaths.ProfilePath + "settings.json"))
            {
                if (File.Exists(AppPaths.ProfilePath + "settings.json.bakup"))
                    File.Delete(AppPaths.ProfilePath + "settings.json.bakup");
                File.Copy(AppPaths.ProfilePath + "settings.json", AppPaths.ProfilePath + "settings.json.bakup");
            }

            Tools.Global.uart.serial.BaudRate = Tools.Global.setting.baudRate;
            Tools.Global.uart.serial.Parity = (Parity)Tools.Global.setting.parity;
            Tools.Global.uart.serial.DataBits = Tools.Global.setting.dataBits;
            Tools.Global.uart.serial.StopBits = (StopBits)Tools.Global.setting.stopBit;
            Tools.Global.uart.UartDataRecived += Uart_UartDataRecived;
            Tools.Global.uart.UartDataSent += Uart_UartDataSent;
            Tools.Global.uart.UartDataRawSent += Uart_UartDataRawSent;
        }

        /// <summary>
        /// 已发送记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataSent(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"<-{EncodingHelper.Byte2Readable((byte[])sender)}");
            Logger.AddUartLogDebug($"[HEX]{EncodingHelper.Byte2Hex((byte[])sender, " ")}");
        }
        private static void Uart_UartDataRawSent(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"Raw<-{EncodingHelper.Byte2Readable((byte[])sender)}");
            Logger.AddUartLogDebug($"[Raw HEX]{EncodingHelper.Byte2Hex((byte[])sender, " ")}");
        }

        /// <summary>
        /// 收到的数据记录到日志
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Uart_UartDataRecived(object sender, EventArgs e)
        {
            Logger.AddUartLogInfo($"->{EncodingHelper.Byte2Readable((byte[])sender)}");
            Logger.AddUartLogDebug($"[HEX]{EncodingHelper.Byte2Hex((byte[])sender, " ")}");
        }
    }
}
