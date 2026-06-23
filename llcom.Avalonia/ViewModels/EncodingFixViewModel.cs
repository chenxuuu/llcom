using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace llcom.Avalonia.ViewModels;

public partial class EncodingFixViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _rawText = "";

    [ObservableProperty]
    private FixResultItem? _selectedResult;

    public ObservableCollection<FixResultItem> FixResults { get; } = new();

    private static readonly string[] EncodingList = { "UTF-8", "GBK", "windows-1252", "Big5", "Shift_Jis", "iso-8859-1" };

    partial void OnRawTextChanged(string value)
    {
        FixResults.Clear();
        if (string.IsNullOrEmpty(value)) return;

        for (int i = 0; i < EncodingList.Length; i++)
        {
            for (int j = 0; j < EncodingList.Length; j++)
            {
                if (i == j) continue;
                try
                {
                    var result = Encoding.GetEncoding(EncodingList[i])
                        .GetString(Encoding.GetEncoding(EncodingList[j]).GetBytes(value));
                    FixResults.Add(new FixResultItem
                    {
                        Raw = EncodingList[i],
                        Target = EncodingList[j],
                        Result = result
                    });
                }
                catch
                {
                    // skip invalid encoding combinations
                }
            }
        }
    }

    /// <summary>Callback for clipboard text copy (set by View layer).</summary>
    public static Func<string, Task>? CopyToClipboardCallback { get; set; }

    [RelayCommand]
    private async Task CopySelected()
    {
        if (SelectedResult == null || string.IsNullOrEmpty(SelectedResult.Result)) return;
        if (CopyToClipboardCallback != null)
            await CopyToClipboardCallback(SelectedResult.Result);
    }
}

public class FixResultItem
{
    public string Raw { get; set; } = "";
    public string Target { get; set; } = "";
    public string Result { get; set; } = "";
}
