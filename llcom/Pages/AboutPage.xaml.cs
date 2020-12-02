using System;
using System.Collections.Generic;
using System.Linq;
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

namespace llcom.Pages
{
    /// <summary>
    /// AboutPage.xaml 的交互逻辑
    /// </summary>
    public partial class AboutPage : Page
    {
        public AboutPage()
        {
            InitializeComponent();
        }

        private static bool loaded = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (loaded)
                return;
            loaded = true;
            this.DataContext = Tools.Global.setting;
            aboutScrollViewer.ScrollToTop();
            versionTextBlock.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();


            //设置为手动检查
            AutoUpdaterDotNET.AutoUpdater.CheckForUpdateEvent += checkUpdateEvent;
            checkUpdate();
        }

        private AutoUpdaterDotNET.UpdateInfoEventArgs G_args;
        private void checkUpdateEvent(AutoUpdaterDotNET.UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                G_args = args;
                if (args.IsUpdateAvailable)
                {
                    if(Tools.Global.IsMSIX())
                    {
                        this.Dispatcher.Invoke(new Action(delegate {
                            CheckUpdateButton.Content = "检测到有更新，请前往微软商店更新";
                        }));
                    }
                    else if (Tools.Global.setting.autoUpdate)
                    {
                        this.Dispatcher.Invoke(new Action(delegate {
                            CheckUpdateButton.Content = "检测到有更新，获取中";
                            AutoUpdaterDotNET.AutoUpdater.ShowUpdateForm(G_args);
                        }));
                    }
                    else
                    {
                        this.Dispatcher.Invoke(new Action(delegate {
                            CheckUpdateButton.IsEnabled = true;
                            CheckUpdateButton.Content = "检测到有更新，立即更新";
                        }));
                    }
                }
                else
                {
                    this.Dispatcher.Invoke(new Action(delegate {
                        CheckUpdateButton.Content = "已是最新版，无需更新";
                    }));
                }
            }
            else
            {
                this.Dispatcher.Invoke(new Action(delegate {
                    CheckUpdateButton.Content = "检查更新失败，请检查网络";
                }));
            }
        }

        private void checkUpdate()
        {
            try
            {
                Random r = new Random();//加上随机参数，确保获取的是最新数据
                this.Dispatcher.Invoke(new Action(delegate
                {
                    AutoUpdaterDotNET.AutoUpdater.Start("https://llcom.papapoi.com/autoUpdate.xml?" + r);
                }));
            }
            catch
            {
                this.Dispatcher.Invoke(new Action(delegate {
                    CheckUpdateButton.Content = "检查更新失败，请检查网络";
                }));
            }
        }

        private void NewissueButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/chenxuuu/llcom/issues/new/choose");
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void OpenSourceButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://llcom.papapoi.com/");
        }

        private void CheckUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateButton.IsEnabled = false;
            CheckUpdateButton.Content = "获取更新信息中";
            AutoUpdaterDotNET.AutoUpdater.ShowUpdateForm(G_args);
        }
    }
}
