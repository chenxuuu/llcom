using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using llcom.Tools;

namespace llcom.Avalonia.ViewModels;

public partial class QuickSendItem : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _text = "";
    [ObservableProperty] private bool _hex;
    [ObservableProperty] private string _commit = "发送";
    [ObservableProperty] private string _recvScriptPath = "";
    [ObservableProperty] private string _recvScriptPara = "";
    public ICommand? SendItemCommand { get; set; }
}

public partial class QuickSendViewModel : ViewModelBase
{
    [ObservableProperty]
    private ObservableCollection<QuickSendItem> _items = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentListNameDisplay))]
    private int _currentListIndex;

    [ObservableProperty]
    private ObservableCollection<string> _listNames = new(Enumerable.Range(0, 10).Select(i => $"列表 {i}"));

    public string CurrentListNameDisplay => $"{ListNames[CurrentListIndex]} ({CurrentListIndex})";

    partial void OnCurrentListIndexChanged(int value)
    {
        SaveCurrentList();
        LoadList(value);
        OnPropertyChanged(nameof(CurrentListNameDisplay));
    }

    public QuickSendViewModel()
    {
        for (int i = 0; i < 15; i++)
        {
            var item = new QuickSendItem { Id = i, SendItemCommand = SendItemCommand };
            Items.Add(item);
        }
        LoadList(0);
    }

    [RelayCommand]
    private void SendItem(QuickSendItem? item)
    {
        if (item == null || string.IsNullOrEmpty(item.Text)) return;
        try
        {
            byte[] data = item.Hex
                ? ByteConvert.Hex2Byte(item.Text)
                : System.Text.Encoding.UTF8.GetBytes(item.Text);
            UartManager.Instance.SendData(data);
        }
        catch (Exception) { /* handle in UI */ }
    }

    [RelayCommand]
    private void AddItem()
    {
        int newId = Items.Count > 0 ? Items.Max(x => x.Id) + 1 : 0;
        Items.Add(new QuickSendItem { Id = newId, SendItemCommand = SendItemCommand });
    }

    [RelayCommand]
    private void RemoveLastItem()
    {
        if (Items.Count > 0)
            Items.RemoveAt(Items.Count - 1);
    }

    [RelayCommand]
    private void ClearAll()
    {
        Items.Clear();
    }

    private void SaveCurrentList() { /* TODO: persist to file */ }
    private void LoadList(int index) { /* TODO: load from file */ }
}
