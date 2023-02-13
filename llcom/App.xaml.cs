using CrashReporterDotNET;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace llcom
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
#if DEBUG
#else
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            Application.Current.DispatcherUnhandledException += DispatcherOnUnhandledException;
#endif
        }

        private void DispatcherOnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs dispatcherUnhandledExceptionEventArgs)
        {
            SendReport(dispatcherUnhandledExceptionEventArgs.Exception);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs unhandledExceptionEventArgs)
        {
            SendReport((Exception)unhandledExceptionEventArgs.ExceptionObject);
        }

        public static void SendReport(Exception exception, string developerMessage = "", bool silent = true)
        {
            if(Tools.Global.setting.language == "zh-CN")
                MessageBox.Show("恭喜你触发了一个BUG！\r\n" +
                    "如果条件允许，请点击“Send Report”来上报这个BUG\r\n" +
                    $"报错信息：{exception.Message}");
            if(!Tools.Global.ReportBug)
            {
                MessageBox.Show("检测到不支持的.net版本，禁止上报bug");
                return;
            }
            if(Tools.Global.HasNewVersion)
            {
                MessageBox.Show("检测到该软件不是最新版，禁止上报bug\r\n请保证软件是最新版");
                return;
            }
            var reportCrash = new ReportCrash("lolicon@papapoi.com")
            {
                DeveloperMessage = developerMessage
            };
            //reportCrash.Silent = silent;
            reportCrash.CaptureScreen = true;
            reportCrash.Send(exception);
        }
    }
}
