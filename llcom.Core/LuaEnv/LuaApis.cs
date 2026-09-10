using System.Text;
using llcom.Model;

namespace llcom.LuaEnv;

/// <summary>
/// C# API methods exposed to Lua scripts.
/// Cross-platform: no XLua dependency.
/// </summary>
public static class LuaApis
{
    public static event EventHandler? PrintLuaLog;
    public static event EventHandler<LinePlotPoint>? LinePlotAdd;

    /// <summary>Print a log message to the Lua log file.</summary>
    public static void PrintLog(string log)
    {
        Tools.Logger.AddLuaLog(log);
        PrintLuaLog?.Invoke(DateTime.Now.ToString("[HH:mm:ss:ffff]") + log, EventArgs.Empty);
    }

    /// <summary>Convert UTF-8 string to GBK Hex encoding.</summary>
    public static string Utf8ToAsciiHex(string input)
    {
        return BitConverter.ToString(Encoding.GetEncoding("GB2312").GetBytes(input)).Replace("-", "");
    }

    /// <summary>Convert GBK-encoded bytes to UTF-8 bytes.</summary>
    public static byte[] Ascii2Utf8(byte[] input)
    {
        return Encoding.UTF8.GetBytes(Encoding.Default.GetString(input));
    }

    /// <summary>Get the application profile/data directory path.</summary>
    public static string GetPath()
    {
        return Tools.PlatformHelper.ProfilePath;
    }

    /// <summary>Get quick-send list entry by index (1-based).</summary>
    public static string QuickSendList(int id)
    {
        if (Tools.GlobalState.Instance.Settings.quickSend.Count < id || id <= 0)
            return "";
        var item = Tools.GlobalState.Instance.Settings.quickSend[id - 1];
        return (item.hex ? "H" : "S") + item.text;
    }

    /// <summary>
    /// Show an input dialog. Returns a tuple of (ok, value).
    /// The value is stored in a static field for Lua interop.
    /// </summary>
    public static string? LastInputResult { get; private set; }
    public static bool InputBox(string prompt, string defaultInput, string? title)
    {
        var callback = Tools.PlatformHelper.InputDialogCallback;
        if (callback == null)
        {
            LastInputResult = defaultInput;
            return true;
        }
        var result = callback(prompt, defaultInput, title ?? "");
        LastInputResult = result.Item2;
        return result.Item1;
    }

    /// <summary>Add a data point to the plot.</summary>
    public static void AddPoint(double n, int l)
    {
        LinePlotAdd?.Invoke(null, new LinePlotPoint { N = n, Line = l });
    }

    /// <summary>Send channels: registered callbacks keyed by channel name.</summary>
    private static readonly Dictionary<string, Func<byte[], Dictionary<string, object?>, bool>> SendChannels = new();

    public static void SendChannelsRegister(string channel, Func<byte[], Dictionary<string, object?>, bool> cb) =>
        SendChannels[channel] = cb;

    /// <summary>Send data to a registered channel.</summary>
    public static bool Send(string channel, byte[] data, Dictionary<string, object?> table)
    {
        if (SendChannels.TryGetValue(channel, out var cb))
            return cb(data, table);
        return false;
    }

    /// <summary>Notify Lua that a channel received data.</summary>
    public static void SendChannelsReceived(string channel, object data) =>
        LuaRunEnv.ChannelReceived(channel, data);
}
