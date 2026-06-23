using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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

    private static string ListFilePath(int index) =>
        Path.Combine(PlatformHelper.ProfilePath, $"quicksend_{index}.json");

    partial void OnCurrentListIndexChanged(int value)
    {
        SaveCurrentList();
        LoadList(value);
    }

    public QuickSendViewModel()
    {
        System.IO.Directory.CreateDirectory(PlatformHelper.ProfilePath);
        for (int i = 0; i < 15; i++)
        {
            var item = new QuickSendItem { Id = i, SendItemCommand = SendItemCommand };
            Items.Add(item);
        }
        LoadList(0);
    }

    // ── Core commands ──────────────────────────────────────────────────

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
    private void SwitchList(string param)
    {
        if (int.TryParse(param, out var index) && index >= 0 && index < ListNames.Count)
            CurrentListIndex = index;
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

    // ── Import / Export ────────────────────────────────────────────────

    [RelayCommand]
    private async Task ImportData()
    {
        try
        {
            var callback = PlatformHelper.OpenFilePickerCallback;
            string? path;
            if (callback != null)
            {
                path = await callback("LLCOM列表文件|*.lclst|所有文件|*.*");
            }
            else
            {
                path = ListFilePath(CurrentListIndex);
                if (!File.Exists(path)) return;
            }
            if (string.IsNullOrEmpty(path)) return;

            var json = await File.ReadAllTextAsync(path);
            var data = JsonSerializer.Deserialize<List<QuickSendItemData>>(json);
            if (data != null)
            {
                Items.Clear();
                foreach (var d in data)
                {
                    Items.Add(new QuickSendItem
                    {
                        Id = d.Id, Text = d.Text ?? "", Hex = d.Hex,
                        Commit = d.Commit ?? "发送",
                        RecvScriptPath = d.RecvScriptPath ?? "",
                        RecvScriptPara = d.RecvScriptPara ?? "",
                        SendItemCommand = SendItemCommand
                    });
                }
                SaveCurrentList();
                PlatformHelper.ShowMessage("数据导入成功");
            }
        }
        catch (Exception ex) { PlatformHelper.ShowMessage($"导入失败: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ExportData()
    {
        try
        {
            var data = Items.Select(item => new QuickSendItemData
            {
                Id = item.Id, Text = item.Text, Hex = item.Hex, Commit = item.Commit,
                RecvScriptPath = item.RecvScriptPath, RecvScriptPara = item.RecvScriptPara
            }).ToList();

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            var callback = PlatformHelper.SaveFilePickerCallback;
            if (callback != null)
            {
                var path = await callback("LLCOM列表文件|*.lclst|所有文件|*.*", $"quicksend_{CurrentListIndex}.lclst");
                if (!string.IsNullOrEmpty(path))
                {
                    await File.WriteAllTextAsync(path, json);
                    PlatformHelper.ShowMessage("数据导出成功！");
                }
            }
            else
            {
                await File.WriteAllTextAsync(ListFilePath(CurrentListIndex), json);
                PlatformHelper.ShowMessage("数据已保存到配置目录");
            }
        }
        catch (Exception ex) { PlatformHelper.ShowMessage($"导出失败: {ex.Message}"); }
    }

    [RelayCommand]
    private async Task ImportSSCOM()
    {
        try
        {
            var callback = PlatformHelper.OpenFilePickerCallback;
            string? path;
            if (callback != null)
                path = await callback("SSCOM配置文件|sscom51.ini;sscom.ini|所有文件|*.*");
            else
            {
                PlatformHelper.ShowMessage("请在 UI 中启用文件选择器");
                return;
            }
            if (string.IsNullOrEmpty(path)) return;

            var lines = await File.ReadAllLinesAsync(path);
            Items.Clear();
            int id = 0;
            foreach (var line in lines)
            {
                var parts = line.Split('=', 2);
                if (parts.Length < 2) continue;
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                if (key.StartsWith("S") && int.TryParse(key[1..], out _) && !string.IsNullOrEmpty(value))
                {
                    Items.Add(new QuickSendItem
                    {
                        Id = id++, Text = value, Hex = false,
                        Commit = "发送", SendItemCommand = SendItemCommand
                    });
                }
            }
            SaveCurrentList();
            PlatformHelper.ShowMessage($"已导入 SSCOM {Items.Count} 条数据");
        }
        catch (Exception ex) { PlatformHelper.ShowMessage($"导入SSCOM失败: {ex.Message}"); }
    }

    // ── Persistence ────────────────────────────────────────────────────

    private void SaveCurrentList()
    {
        try
        {
            var data = Items.Select(item => new QuickSendItemData
            {
                Id = item.Id, Text = item.Text, Hex = item.Hex, Commit = item.Commit,
                RecvScriptPath = item.RecvScriptPath, RecvScriptPara = item.RecvScriptPara
            }).ToList();
            var json = JsonSerializer.Serialize(data);
            File.WriteAllText(ListFilePath(CurrentListIndex), json);
        }
        catch { /* ignore persistence errors */ }
    }

    private void LoadList(int index)
    {
        try
        {
            var path = ListFilePath(index);
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<QuickSendItemData>>(json);
            if (data == null) return;

            Items.Clear();
            foreach (var d in data)
            {
                Items.Add(new QuickSendItem
                {
                    Id = d.Id, Text = d.Text ?? "", Hex = d.Hex,
                    Commit = d.Commit ?? "发送",
                    RecvScriptPath = d.RecvScriptPath ?? "",
                    RecvScriptPara = d.RecvScriptPara ?? "",
                    SendItemCommand = SendItemCommand
                });
            }
        }
        catch { /* ignore load errors, use defaults */ }
    }

    private class QuickSendItemData
    {
        public int Id { get; set; }
        public string Text { get; set; } = "";
        public bool Hex { get; set; }
        public string Commit { get; set; } = "发送";
        public string RecvScriptPath { get; set; } = "";
        public string RecvScriptPara { get; set; } = "";
    }
}
