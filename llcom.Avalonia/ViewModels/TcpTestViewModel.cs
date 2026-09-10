using System;
using System.Collections.ObjectModel;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace llcom.Avalonia.ViewModels;

public partial class TcpTestViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isConnected;
    [ObservableProperty]
    private bool _isConnecting;
    [ObservableProperty]
    private string _address = "loading...";
    [ObservableProperty]
    private string _addressV6 = "loading...";
    [ObservableProperty]
    private string _sendText = "";
    [ObservableProperty]
    private bool _hexMode;
    [ObservableProperty]
    private string _selectedClient = "";
    [ObservableProperty]
    private string _log = "";

    public ObservableCollection<string> Clients { get; } = new();

    private ClientWebSocket? _ws;
    private ClientWebSocket? _wsV6;
    private CancellationTokenSource? _cts;

    private string ConnectionType = "tcp";

    [RelayCommand]
    private async Task CreateTcp() => await ConnectWebSocket("tcp");

    [RelayCommand]
    private async Task CreateTcpSsl() => await ConnectWebSocket("ssl", "ssl-tcp");

    [RelayCommand]
    private async Task CreateUdp() => await ConnectWebSocket("udp");

    [RelayCommand]
    private async Task CreateTcpIpv6() => await ConnectWebSocket("tcpv6");

    [RelayCommand]
    private async Task Disconnect()
    {
        try
        {
            _cts?.Cancel();
            if (_ws?.State == WebSocketState.Open) await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            if (_wsV6?.State == WebSocketState.Open) await _wsV6.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            IsConnected = false;
            Address = "loading...";
            AddressV6 = "loading...";
            AppendLog("Server closed.");
        }
        catch (Exception ex)
        {
            AppendLog($"Disconnect error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task Send()
    {
        if (!IsConnected || string.IsNullOrEmpty(SendText) || string.IsNullOrEmpty(SelectedClient)) return;
        try
        {
            var ws = (_ws?.State == WebSocketState.Open) ? _ws : _wsV6;
            var msg = JsonSerializer.Serialize(new
            {
                action = "sendc",
                data = SendText,
                hex = HexMode,
                client = SelectedClient
            });
            if (ws != null)
                await ws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            AppendLog($"← send to [{SelectedClient}]: {SendText}");
        }
        catch (Exception ex)
        {
            AppendLog($"Send error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task KickClient()
    {
        if (!IsConnected || string.IsNullOrEmpty(SelectedClient)) return;
        try
        {
            var ws = (_ws?.State == WebSocketState.Open) ? _ws : _wsV6;
            var msg = JsonSerializer.Serialize(new { action = "closec", client = SelectedClient });
            if (ws != null)
                await ws.SendAsync(Encoding.UTF8.GetBytes(msg), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppendLog($"Kick error: {ex.Message}");
        }
    }

    private async Task ConnectWebSocket(string ctype, string? stype = null)
    {
        if (IsConnecting) return;
        IsConnecting = true;
        ConnectionType = ctype switch
        {
            "tcpv6" => "tcp",
            "ssl" => "ssl",
            _ => ctype
        };
        AppendLog("Server is creating...");

        var isV6 = ctype == "tcpv6";
        var uri = isV6 ? "wss://netlab.luatos.org/ws/netlab" : "wss://gps.openluat.com/netlab/ws/netlab";
        var ws = new ClientWebSocket();
        if (isV6) _wsV6 = ws; else _ws = ws;

        try
        {
            _cts = new CancellationTokenSource();
            await ws.ConnectAsync(new Uri(uri), _cts.Token);
            IsConnected = true;
            var newAction = JsonSerializer.Serialize(new { action = "newp", type = stype ?? (ctype == "tcpv6" ? "tcp" : ctype) });
            await ws.SendAsync(Encoding.UTF8.GetBytes(newAction), WebSocketMessageType.Text, true, _cts.Token);
            _ = ReceiveLoop(ws, isV6, _cts.Token);
        }
        catch (Exception ex)
        {
            AppendLog($"Create failed: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    private async Task ReceiveLoop(ClientWebSocket ws, bool isV6, CancellationToken ct)
    {
        var buffer = new byte[8192];
        try
        {
            while (!ct.IsCancellationRequested && ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close) break;

                var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                try
                {
                    var obj = JsonNode.Parse(text);
                    var action = obj?["action"]?.ToString();
                    switch (action)
                    {
                        case "port":
                            var port = obj!["port"]!.ToString();
                            if (isV6)
                            {
                                Address = $"tcp://152.70.80.204:{port}";
                                AddressV6 = $"tcp://[2603:c023:1:5fcc:c028:8ed:49a7:6e08]:{port}";
                            }
                            else
                                Address = $"{ConnectionType}://115.120.239.161:{port}";
                            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => { Address = Address; AddressV6 = AddressV6; });
                            AppendLog($"Created a {ConnectionType} server.");
                            break;
                        case "client":
                        case "connected":
                            var clientAddr = $"[{obj!["client"]}]{obj["addr"]}";
                            AppendLog($"✔ {clientAddr} connected.");
                            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                Clients.Add(obj["client"]!.ToString());
                                if (string.IsNullOrEmpty(SelectedClient) && Clients.Count > 0)
                                    SelectedClient = Clients[0];
                            });
                            break;
                        case "closed":
                            AppendLog($"❌ [{obj!["client"]}] disconnected.");
                            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                            {
                                Clients.Remove(obj["client"]!.ToString());
                            });
                            break;
                        case "data":
                            AppendLog($" → receive from [{obj!["client"]}]: {obj["data"]}");
                            break;
                        case "error":
                            AppendLog($"❔ error: {obj!["msg"]}");
                            break;
                    }
                }
                catch { }
            }
        }
        catch (OperationCanceledException) { }
        catch { IsConnected = false; }
    }

    private void AppendLog(string msg) => Log += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";

    public void Cleanup() => _cts?.Cancel();
}
