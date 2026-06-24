using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace llcom.Avalonia.ViewModels;

public partial class SocketClientViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _server = "";
    [ObservableProperty]
    private int _port = 8888;
    [ObservableProperty]
    private int _selectedProtocolType;
    [ObservableProperty]
    private bool _isConnected;
    [ObservableProperty]
    private bool _isConnecting;
    [ObservableProperty]
    private string _sendText = "";
    [ObservableProperty]
    private bool _hexMode;
    [ObservableProperty]
    private string _receiveText = "";
    [ObservableProperty]
    private bool _autoReconnect;
    [ObservableProperty]
    private int _reconnectInterval = 3;

    public ObservableCollection<string> ProtocolTypes { get; } = new() { "TCP", "UDP", "TCP SSL" };

    private TcpClient? _tcpClient;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _receiveCts;

    [RelayCommand]
    private async Task ToggleConnect()
    {
        if (IsConnected)
        {
            Disconnect();
            return;
        }

        IsConnecting = true;
        try
        {
            if (SelectedProtocolType == 1) // UDP
            {
                _udpClient = new UdpClient();
                _udpClient.Connect(Server, Port);
                IsConnected = true;
                _receiveCts = new CancellationTokenSource();
                _ = UdpReceiveLoop(_receiveCts.Token);
            }
            else
            {
                _tcpClient = new TcpClient();
                await _tcpClient.ConnectAsync(Server, Port);
                IsConnected = true;
                _receiveCts = new CancellationTokenSource();
                _ = TcpReceiveLoop(_receiveCts.Token);
            }
        }
        catch (Exception ex)
        {
            AppendReceive($"Connect error: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task Send()
    {
        if (!IsConnected || string.IsNullOrEmpty(SendText)) return;
        try
        {
            byte[] data = HexMode ? HexToBytes(SendText) : Encoding.UTF8.GetBytes(SendText);
            if (_tcpClient?.Connected == true)
                await _tcpClient.GetStream().WriteAsync(data);
            else if (_udpClient != null)
                await _udpClient.SendAsync(data);
            AppendReceive($"← Sent: {SendText}");
        }
        catch (Exception ex)
        {
            AppendReceive($"Send error: {ex.Message}");
        }
    }

    private async Task TcpReceiveLoop(CancellationToken ct)
    {
        var buffer = new byte[4096];
        try
        {
            while (!ct.IsCancellationRequested && _tcpClient?.Connected == true)
            {
                var count = await _tcpClient.GetStream().ReadAsync(buffer, ct);
                if (count > 0)
                {
                    var data = new byte[count];
                    Array.Copy(buffer, data, count);
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        AppendReceive($"→ {Encoding.UTF8.GetString(data)}"));
                }
                else // count == 0 means graceful close
                {
                    IsConnected = false;
                    break;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch { IsConnected = false; }
    }

    private async Task UdpReceiveLoop(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _udpClient != null)
            {
                var result = await _udpClient.ReceiveAsync(ct);
                global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    AppendReceive($"→ {Encoding.UTF8.GetString(result.Buffer)}"));
            }
        }
        catch (OperationCanceledException) { }
        catch { IsConnected = false; }
    }

    private void Disconnect()
    {
        _receiveCts?.Cancel();
        _tcpClient?.Close();
        _udpClient?.Close();
        _tcpClient = null;
        _udpClient = null;
        IsConnected = false;
    }

    private void AppendReceive(string msg) => ReceiveText += $"[{DateTime.Now:HH:mm:ss}] {msg}\n";

    private static byte[] HexToBytes(string hex)
    {
        hex = System.Text.RegularExpressions.Regex.Replace(hex, "[^0-9A-Fa-f]", "");
        if (hex.Length % 2 != 0) hex = hex[..^1];
        return Convert.FromHexString(hex);
    }

    public void Cleanup() => Disconnect();
}
