using System.IO.Ports;

namespace llcom.Tools;

/// <summary>
/// Singleton UART manager for cross-platform serial port operations.
/// Replaces the original llcom.Model.Uart which was tightly coupled to WPF.
/// </summary>
public class UartManager
{
    private static readonly Lazy<UartManager> _instance = new(() => new UartManager());
    public static UartManager Instance => _instance.Value;

    private SerialPort? _serial;
    private Stream? _lastPortBaseStream;
    private readonly List<SerialPort> _disposed = new();
    private readonly object _lock = new();

    public SerialPort Serial => _serial ??= CreateSerial();

    public event EventHandler<byte[]>? UartDataReceived;
    public event EventHandler<byte[]>? UartDataSent;
    public event EventHandler<byte[]>? UartDataRawSent;

    private bool _rts;
    private bool _dtr = true;
    private readonly EventWaitHandle _waitUartReceive = new AutoResetEvent(true);
    private bool _isRunning;

    public bool Rts
    {
        get => _rts;
        set { Serial.RtsEnable = _rts = value; }
    }

    public bool Dtr
    {
        get => _dtr;
        set { Serial.DtrEnable = _dtr = value; }
    }

    public bool IsOpen => _serial?.IsOpen ?? false;
    public string PortName { get => Serial.PortName; set => Serial.PortName = value; }

    private UartManager()
    {
        _serial = CreateSerial();
        _isRunning = true;
        new Thread(ReadLoop) { IsBackground = true }.Start();
    }

    private SerialPort CreateSerial()
    {
        var sp = new SerialPort();
        sp.DataReceived += (_, _) => _waitUartReceive.Set();
        return sp;
    }

    public void SetName(string name) => Serial.PortName = name;
    public string GetName() => Serial.PortName;

    public void Open()
    {
        var temp = Serial.PortName;
        RefreshSerialDevice();
        Serial.PortName = temp;
        Serial.Open();
        _lastPortBaseStream = Serial.BaseStream;
    }

    public void Close()
    {
        RefreshSerialDevice();
        Serial.Close();
    }

    public void SendData(byte[] data, byte[]? dataRaw = null)
    {
        if (data.Length == 0) return;
        Serial.Write(data, 0, data.Length);

        if (dataRaw != null && dataRaw.Length == data.Length)
        {
            bool same = true;
            for (int i = 0; i < data.Length; i++)
                if (data[i] != dataRaw[i]) { same = false; break; }
            if (same) dataRaw = null;
        }

        UartDataRawSent?.Invoke(this, dataRaw ?? data);
        UartDataSent?.Invoke(this, data);
    }

    private void RefreshSerialDevice()
    {
        try { _lastPortBaseStream?.Dispose(); } catch { }
        try { _serial?.BaseStream.Dispose(); } catch { }
        try { _serial?.Dispose(); } catch { }

        lock (_lock) { if (_serial != null) _disposed.Add(_serial); }
        _serial = new SerialPort();
        _serial.RtsEnable = _rts;
        _serial.DtrEnable = _dtr;
        _serial.DataReceived += (_, _) => _waitUartReceive.Set();
    }

    private void ReadLoop()
    {
        _waitUartReceive.Reset();
        while (_isRunning)
        {
            _waitUartReceive.WaitOne();
            if (!_isRunning) return;

            Thread.Sleep(10);
            var result = new List<byte>();
            while (true)
            {
                if (_serial == null || !_serial.IsOpen) break;
                try
                {
                    int length = _serial.BytesToRead;
                    if (length == 0) break;
                    byte[] rev = new byte[length];
                    _serial.Read(rev, 0, length);
                    if (rev.Length == 0) break;
                    result.AddRange(rev);
                }
                catch { break; }
                if (result.Count > 10240) break;
            }

            if (result.Count > 0)
            {
                try { UartDataReceived?.Invoke(this, result.ToArray()); }
                catch { }
            }
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _waitUartReceive.Set();
        try { _serial?.Close(); } catch { }
    }
}
