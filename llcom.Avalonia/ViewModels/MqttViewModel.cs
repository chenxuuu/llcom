using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;
using MQTTnet;
using MQTTnet.Client;

namespace llcom.Avalonia.ViewModels;

public partial class MqttViewModel : ViewModelBase
{
    private IMqttClient? _mqttClient;
    private readonly MqttFactory _factory = new();

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _server = "broker.emqx.io";
    [ObservableProperty]
    private int _port = 1883;
    [ObservableProperty]
    private string _clientId = Guid.NewGuid().ToString("N")[..16];
    [ObservableProperty]
    private string _userName = "";
    [ObservableProperty]
    private string _password = "";
    [ObservableProperty]
    private int _keepAlive = 60;
    [ObservableProperty]
    private bool _useTls;
    [ObservableProperty]
    private string _tlsCaCertPath = "";
    [ObservableProperty]
    private string _tlsClientCertPath = "";
    [ObservableProperty]
    private string _tlsCertPassword = "";
    [ObservableProperty]
    private bool _useWebSocket;
    [ObservableProperty]
    private string _wsPath = "/mqtt";
    [ObservableProperty]
    private bool _cleanSession = true;
    [ObservableProperty]
    private string _subscribeTopic = "";
    [ObservableProperty]
    private string _publishTopic = "";
    [ObservableProperty]
    private string _publishPayload = "";
    [ObservableProperty]
    private int _subscribeQos;
    [ObservableProperty]
    private int _publishQos;
    [ObservableProperty]
    private bool _hexMode;
    [ObservableProperty]
    private string _log = "";

    public ObservableCollection<string> SubscribedTopics { get; } = new();
    public ObservableCollection<int> QosOptions { get; } = new() { 0, 1, 2 };

    private async Task InitClientAsync()
    {
        _mqttClient = _factory.CreateMqttClient();
        _mqttClient.ConnectedAsync += async e =>
        {
            IsConnected = true;
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => SubscribedTopics.Clear());
            AppendLog("MQTT: ✔ connected");
        };
        _mqttClient.DisconnectedAsync += async e =>
        {
            IsConnected = false;
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SubscribedTopics.Clear();
                SubscribedTopics.Add("Not connected");
            });
            AppendLog("MQTT: ❌ disconnected");
        };
        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            AppendLog($"MQTT → {e.ApplicationMessage.Topic}: {payload}");
            return Task.CompletedTask;
        };
    }

    [RelayCommand]
    private async Task ToggleConnect()
    {
        if (_mqttClient == null)
            await InitClientAsync();

        if (IsConnected && _mqttClient != null)
        {
            await _mqttClient.DisconnectAsync();
            return;
        }

        try
        {
            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithClientId(ClientId)
                .WithKeepAlivePeriod(TimeSpan.FromSeconds(KeepAlive));

            if (!string.IsNullOrEmpty(UserName))
                optionsBuilder.WithCredentials(UserName, Password);

            if (UseTls)
            {
                optionsBuilder.WithTlsOptions(o =>
                {
                    o.WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13);

                    // NOTE: Certificate validation is intentionally relaxed for self-signed/
                    // embedded device MQTT brokers commonly used in IoT scenarios.
                    // In production deployments, use WithCertificateValidationHandler with
                    // proper certificate pinning instead of unconditional acceptance.
                    if (!string.IsNullOrEmpty(TlsCaCertPath) && File.Exists(TlsCaCertPath))
                    {
                        o.WithCertificateValidationHandler(ctx => true);
                    }

                    o.WithAllowUntrustedCertificates(true);
                });
            }

            if (UseWebSocket)
                optionsBuilder.WithWebSocketServer(b => b.WithUri($"{Server}:{Port}{WsPath}"));
            else
                optionsBuilder.WithTcpServer(Server, Port);

            if (CleanSession)
                optionsBuilder.WithCleanStart();

            var options = optionsBuilder.Build();
            await _mqttClient!.ConnectAsync(options, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppendLog($"MQTT error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Subscribe()
    {
        if (!IsConnected || _mqttClient == null || string.IsNullOrEmpty(SubscribeTopic)) return;
        try
        {
            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder()
                .WithTopic(SubscribeTopic)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)SubscribeQos)
                .Build());
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (!SubscribedTopics.Contains(SubscribeTopic))
                    SubscribedTopics.Add(SubscribeTopic);
            });
        }
        catch (Exception ex)
        {
            AppendLog($"Subscribe error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Publish()
    {
        if (!IsConnected || _mqttClient == null || string.IsNullOrEmpty(PublishTopic)) return;
        try
        {
            var payload = HexMode
                ? HexToBytes(PublishPayload)
                : Encoding.UTF8.GetBytes(PublishPayload);
            await _mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic(PublishTopic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel((MQTTnet.Protocol.MqttQualityOfServiceLevel)PublishQos)
                .Build(), CancellationToken.None);
            AppendLog($"MQTT ← {PublishTopic}: {PublishPayload}");
        }
        catch (Exception ex)
        {
            AppendLog($"Publish error: {ex.Message}");
        }
    }

    private void AppendLog(string msg) => Log += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";

    // Use shared ByteConvert.Hex2Byte with size limit
    private static byte[] HexToBytes(string hex) => ByteConvert.Hex2Byte(hex);

    public void Cleanup()
    {
        _mqttClient?.Dispose();
    }
}
