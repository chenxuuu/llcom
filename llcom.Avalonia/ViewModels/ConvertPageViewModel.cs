using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace llcom.Avalonia.ViewModels;

public partial class ConvertPageViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _rawText = "";

    [ObservableProperty]
    private string _resultText = "";

    [ObservableProperty]
    private int _selectedConverterIndex = -1;

    public ObservableCollection<string> ConverterNames { get; } = new();
    public ObservableCollection<string> ConvertJobs { get; } = new();

    private readonly Dictionary<string, Func<byte[], byte[]>> _converters = new()
    {
        ["String to Hex(with space)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(e).Replace("-", " ")),
        ["String to Hex(without space)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(e).Replace("-", "")),
        ["Hex to String"] = e => Hex2Byte(Encoding.Default.GetString(e)),
        ["String to Base64"] = e => { try { return Encoding.Default.GetBytes(Convert.ToBase64String(e)); } catch (Exception ex) { return Encoding.Default.GetBytes(ex.Message); } },
        ["Base64 to String"] = e => { try { return Convert.FromBase64String(Encoding.Default.GetString(e)); } catch (Exception ex) { return Encoding.Default.GetBytes(ex.Message); } },
        ["URL encode"] = e => Encoding.Default.GetBytes(HttpUtility.UrlEncode(Encoding.Default.GetString(e))),
        ["URL decode"] = e => Encoding.Default.GetBytes(HttpUtility.UrlDecode(Encoding.Default.GetString(e))),
        ["HTML encode"] = e => Encoding.Default.GetBytes(HttpUtility.HtmlEncode(Encoding.Default.GetString(e))),
        ["HTML decode"] = e => Encoding.Default.GetBytes(HttpUtility.HtmlDecode(Encoding.Default.GetString(e))),
        ["String to Unicode"] = e => Encoding.Default.GetBytes(String2Unicode(Encoding.Default.GetString(e))),
        ["Unicode to String"] = e => Encoding.Default.GetBytes(Unicode2String(Encoding.Default.GetString(e))),
        ["String to MD5 (Hex)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(MD5Encrypt(e)).Replace("-", "")),
        ["String to SHA-1 (Hex)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(SHA1Encrypt(e)).Replace("-", "")),
        ["String to SHA-256 (Hex)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(SHA256Encrypt(e)).Replace("-", "")),
        ["String to SHA-512 (Hex)"] = e => Encoding.Default.GetBytes(BitConverter.ToString(SHA512Encrypt(e)).Replace("-", "")),
    };

    public ConvertPageViewModel()
    {
        foreach (var key in _converters.Keys)
            ConverterNames.Add(key);
        ConvertJobs.CollectionChanged += (_, _) => DoConvert();
    }

    partial void OnRawTextChanged(string value) => DoConvert();

    private void DoConvert()
    {
        byte[] row = Encoding.Default.GetBytes(RawText);
        foreach (var job in ConvertJobs)
        {
            if (_converters.ContainsKey(job))
                row = _converters[job](row);
        }
        ResultText = Encoding.Default.GetString(row);
    }

    [RelayCommand]
    private void AddJob()
    {
        if (SelectedConverterIndex < 0 || SelectedConverterIndex >= ConverterNames.Count)
            return;
        ConvertJobs.Add(ConverterNames[SelectedConverterIndex]);
    }

    [RelayCommand]
    private void RemoveLastJob()
    {
        if (ConvertJobs.Count == 0) return;
        ConvertJobs.RemoveAt(ConvertJobs.Count - 1);
    }

    private static byte[] Hex2Byte(string hex)
    {
        hex = Regex.Replace(hex, "[^0-9A-Fa-f]", "");
        if (hex.Length % 2 != 0)
            hex = hex.Remove(hex.Length - 1, 1);
        if (hex.Length <= 0) return Array.Empty<byte>();
        byte[] vBytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
            if (!byte.TryParse(hex.Substring(i, 2), NumberStyles.HexNumber, null, out vBytes[i / 2]))
                vBytes[i / 2] = 0;
        return vBytes;
    }

    private static byte[] MD5Encrypt(byte[] b) => MD5.HashData(b);
    private static byte[] SHA1Encrypt(byte[] b) => SHA1.HashData(b);
    private static byte[] SHA256Encrypt(byte[] b) => SHA256.HashData(b);
    private static byte[] SHA512Encrypt(byte[] b) => SHA512.HashData(b);

    private static string String2Unicode(string source)
    {
        var bytes = Encoding.Unicode.GetBytes(source);
        var sb = new StringBuilder();
        for (var i = 0; i < bytes.Length; i += 2)
            sb.AppendFormat("\\u{0}{1}", bytes[i + 1].ToString("x").PadLeft(2, '0'), bytes[i].ToString("x").PadLeft(2, '0'));
        return sb.ToString();
    }

    private static string Unicode2String(string source) =>
        new Regex(@"\\u([0-9a-fA-F]{4})", RegexOptions.IgnoreCase | RegexOptions.Compiled)
            .Replace(source, x => Convert.ToChar(Convert.ToUInt16(x.Result("$1"), 16)).ToString());
}
