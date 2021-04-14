using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// MqttTestPage.xaml 的交互逻辑
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public partial class MqttTestPage : Page
    {
        public MqttTestPage()
        {
            InitializeComponent();
        }

        private bool initial = false;
        private static MqttFactory factory = new MqttFactory();
        private MQTTnet.Client.IMqttClient mqttClient = factory.CreateMqttClient();
        public bool MqttIsConnected { get; set; } = false;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (initial)
                return;

            BrokerTextBox.DataContext = Tools.Global.setting;
            PortTextBox.DataContext = Tools.Global.setting;
            ClientTextBox.DataContext = Tools.Global.setting;
            TLSCheckBox.DataContext = Tools.Global.setting;
            UserTextBox.DataContext = Tools.Global.setting;
            PasswordTextBox.DataContext = Tools.Global.setting;
            KeepAliveTextBox.DataContext = Tools.Global.setting;
            CleanTextBox.DataContext = Tools.Global.setting;
            ConnectButton.DataContext = this;
            SettingStackPanel.DataContext = this;

            mqttClient.UseConnectedHandler(async e =>
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    MqttIsConnected = mqttClient.IsConnected;
                    subListBox.Items.Clear();
                }));
            });
            mqttClient.UseDisconnectedHandler(async e =>
            {
                this.Dispatcher.Invoke(new Action(delegate
                {
                    MqttIsConnected = mqttClient.IsConnected;
                    subListBox.Items.Clear();
                    subListBox.Items.Add("not connect to server");
                }));
            });
            //
            mqttClient.UseApplicationMessageReceivedHandler(e =>
            {
                var s = $"Topic:{e.ApplicationMessage.Topic},QOS:{e.ApplicationMessage.QualityOfServiceLevel}\r\n" +
                $"{Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}";
                this.Dispatcher.Invoke(new Action(delegate
                {
                    Tools.Logger.ShowDataRaw(s);
                }));
            });


            initial = true;
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if(mqttClient.IsConnected)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await mqttClient.DisconnectAsync();
                    }
                    catch { }
                });
            }
            else
            {
                MqttIsConnected = true;
                var temp = new MqttClientOptionsBuilder()
                    .WithClientId(Tools.Global.setting.mqttClientID)
                    .WithTcpServer(Tools.Global.setting.mqttServer, Tools.Global.setting.mqttPort)
                    .WithCredentials(Tools.Global.setting.mqttUser, Tools.Global.setting.mqttPassword)
                    .WithKeepAlivePeriod(new TimeSpan(0, 0, Tools.Global.setting.mqttKeepAlive));
                if (Tools.Global.setting.mqttTLS)
                    temp.WithTls();
                if (Tools.Global.setting.mqttCleanSession)
                    temp.WithCleanSession();
                var options = temp.Build();
                Task.Run(async () =>
                {
                    try
                    {
                        await mqttClient.ConnectAsync(options, CancellationToken.None);
                    }
                    catch { }
                });
            }
        }

        private void subcribeButton_Click(object sender, RoutedEventArgs e)
        {
            if(mqttClient.IsConnected)
            {
                var topic = subcribeTextBox.Text;
                var qos = int.Parse(subQOSComboBox.Text);
                Task.Run(async () =>
                {
                    try
                    {
                        var r = await mqttClient.SubscribeAsync(
                            new MqttTopicFilterBuilder()
                            .WithTopic(topic)
                            .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)qos)
                            .Build());
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            foreach(string i in subListBox.Items)
                            {
                                if (i == topic)
                                    return;
                            }
                            subListBox.Items.Add(topic);
                        }));
                    }
                    catch (Exception ee)
                    {
                        if(mqttClient.IsConnected)
                            MessageBox.Show($"订阅失败：{ee}");
                    }
                });
            }
        }

        private void publishButton_Click(object sender, RoutedEventArgs e)
        {
            if (mqttClient.IsConnected)
            {
                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(publishTopicTextBox.Text)
                    .WithPayload(PublishTextBox.Text)
                    .WithQualityOfServiceLevel(int.Parse(publishQOSComboBox.Text))
                    .WithRetainFlag()
                    .Build();
                Task.Run(async () =>
                {
                    try
                    {
                        await mqttClient.PublishAsync(message, CancellationToken.None);
                    }
                    catch { }
                });
            }
        }
    }
}
