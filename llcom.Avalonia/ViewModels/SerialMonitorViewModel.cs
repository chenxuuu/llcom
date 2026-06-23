using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Avalonia.Helpers;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class SerialMonitorViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<string> _processList = new();

    [ObservableProperty]
    private string? _selectedProcess;

    [ObservableProperty]
    private ObservableCollection<string> _comPortList = new();

    [ObservableProperty]
    private string? _selectedComPort;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _monitorButtonText = LocaleHelper.Get("SerialMonitorStart");

    [ObservableProperty]
    private string _statusText = LocaleHelper.Get("StatusReady");

    [ObservableProperty]
    private string _receivedData = "";

    [ObservableProperty]
    private bool _isAvailable;

    [ObservableProperty]
    private string _availabilityMessage = "";

    public SerialMonitorViewModel()
    {
        IsAvailable = NativeInterop.IsAvailable;
        AvailabilityMessage = NativeInterop.AvailabilityMessage;
        if (IsAvailable)
        {
            Refresh();
        }
        else
        {
            StatusText = AvailabilityMessage;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        // Refresh process list
        string lastProc = SelectedProcess ?? "";
        ProcessList.Clear();
        var procs = new List<string>();
        try
        {
            foreach (var p in Process.GetProcesses())
            {
                try { procs.Add($"{p.ProcessName}[{p.Id}]"); }
                catch { }
            }
        }
        catch { }
        procs.Sort();
        foreach (var p in procs) ProcessList.Add(p);

        if (ProcessList.Count > 0)
        {
            SelectedProcess = !string.IsNullOrWhiteSpace(lastProc) && procs.Contains(lastProc)
                ? lastProc : ProcessList[0];
        }

        // Refresh COM port list (Windows: only COMx; Linux: all)
        string lastCom = SelectedComPort ?? "";
        ComPortList.Clear();
        try
        {
            foreach (var p in SerialPort.GetPortNames())
            {
                ComPortList.Add(p);
            }
        }
        catch { }

        if (ComPortList.Count > 0)
        {
            SelectedComPort = !string.IsNullOrWhiteSpace(lastCom) && ComPortList.Contains(lastCom)
                ? lastCom : ComPortList[0];
        }
    }

    [RelayCommand]
    private void ToggleMonitor()
    {
        if (!IsAvailable)
        {
            StatusText = AvailabilityMessage;
            return;
        }

        if (IsMonitoring)
        {
            // Stop monitoring
            NativeInterop.UnMonitorComm();
            IsMonitoring = false;
            MonitorButtonText = LocaleHelper.Get("SerialMonitorStart");
            StatusText = LocaleHelper.Get("SerialMonitorStopped");
        }
        else
        {
            // Start monitoring
            if (SelectedProcess == null || SelectedComPort == null)
            {
                StatusText = LocaleHelper.Get("SerialMonitorSelectBoth");
                return;
            }

            // Parse PID from format "name[pid]"
            var start = SelectedProcess.IndexOf('[');
            if (start < 0 || !uint.TryParse(
                SelectedProcess.Substring(start + 1, SelectedProcess.Length - start - 2),
                out var pid))
            {
                StatusText = LocaleHelper.Get("SerialMonitorInvalidPid");
                return;
            }

            // Parse COM index
            uint comIndex = 1;
            try
            {
                var digits = new string(SelectedComPort.Where(char.IsDigit).ToArray());
                if (digits.Length > 0) comIndex = uint.Parse(digits);
            }
            catch { }

            NativeInterop.MonitorCallback callback = (IntPtr param) =>
            {
                var d = Marshal.PtrToStructure<NativeInterop.Udata>(param);
                byte[] b = new byte[d.DataSize];
                for (int i = 0; i < d.DataSize; i++) b[i] = d.Data[i];

                string prefix = d.CommState switch
                {
                    (byte)NativeInterop.CommState.Send => "→",
                    (byte)NativeInterop.CommState.Receive => "←",
                    (byte)NativeInterop.CommState.Disconnect => "❌",
                    _ => "?"
                };
                AppendReceived($"monitor COM{d.ComPort} {prefix}: {BitConverter.ToString(b)}\n");
                return 1;
            };

            try
            {
                IsMonitoring = NativeInterop.MonitorComm(pid, comIndex, callback);
                if (IsMonitoring)
                {
                    MonitorButtonText = LocaleHelper.Get("SerialMonitorStop");
                    StatusText = LocaleHelper.Get("SerialMonitorMonitoring");
                }
                else
                {
                    StatusText = LocaleHelper.Get("SerialMonitorStartFailed");
                }
            }
            catch (Exception ex)
            {
                StatusText = LocaleHelper.Format("SerialMonitorStartFailed", ex.Message);
            }
        }
    }

    private int _maxLen = 20000;
    private void AppendReceived(string text)
    {
        ReceivedData = (ReceivedData + text)[..Math.Min(ReceivedData.Length + text.Length, _maxLen)];
    }

    [RelayCommand]
    private void ClearData()
    {
        ReceivedData = "";
    }

    public void Cleanup()
    {
        if (IsMonitoring)
        {
            try { NativeInterop.UnMonitorComm(); } catch { }
        }
    }
}
