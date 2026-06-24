using System;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace llcom.Avalonia.ViewModels;

public partial class PlotViewModel : ViewModelBase
{
    private const int MaxPoints = 1000;
    private double[][] _data = new double[10][];
    private double[] _dataX = new double[MaxPoints];
    private int _styleIndex;
    private bool _needsRender;

    [ObservableProperty]
    private string? _plotImageSource;

    private CancellationTokenSource? _renderCts;

    public PlotViewModel()
    {
        for (int i = 0; i < MaxPoints; i++)
            _dataX[i] = i - MaxPoints + 1;
        for (int i = 0; i < 10; i++)
            _data[i] = new double[MaxPoints];

        _renderCts = new CancellationTokenSource();
        _ = RenderLoop(_renderCts.Token);
    }

    public void AddPoint(double value, int line)
    {
        if (line >= 10 || line < 0) return;
        var arr = _data[line];
        for (int i = 0; i < MaxPoints - 1; i++)
            arr[i] = arr[i + 1];
        arr[MaxPoints - 1] = value;
        _needsRender = true;
    }

    [RelayCommand]
    private void Fit()
    {
        double min = double.MaxValue, max = double.MinValue;
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < MaxPoints; j++)
            {
                if (_data[i][j] < min) min = _data[i][j];
                if (_data[i][j] > max) max = _data[i][j];
            }
        }
        // In Avalonia, we can use ScottPlot.Avalonia's Plot control directly
        _needsRender = true;
    }

    [RelayCommand]
    private void Clear()
    {
        for (int i = 0; i < 10; i++)
            for (int j = 0; j < MaxPoints; j++)
                _data[i][j] = 0;
        _needsRender = true;
    }

    [RelayCommand]
    private void CycleTheme()
    {
        _styleIndex = (_styleIndex + 1) % 5;
        _needsRender = true;
    }

    private async Task RenderLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (_needsRender)
            {
                _needsRender = false;
                // Plot rendering handled by ScottPlot control
            }
            await Task.Delay(100, ct);
        }
    }

    public double[][] GetData() => _data;
    public double[] GetDataX() => _dataX;
    public bool NeedsRender() => _needsRender;
    public void MarkRendered() => _needsRender = false;

    public void Cleanup() => _renderCts?.Cancel();
}
