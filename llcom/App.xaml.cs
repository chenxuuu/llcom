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
            TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
#endif
        }

        private void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArgs)
        {
            SendReport(unobservedTaskExceptionEventArgs.Exception);
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
                MessageBox.Show("检测到不支持的.net版本，停止上报bug逻辑");
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
