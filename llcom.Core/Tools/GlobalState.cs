using Newtonsoft.Json;
using llcom.Model;

namespace llcom.Tools;

/// <summary>
/// Global application state singleton. Holds settings, and utility methods.
/// Cross-platform: replaces the old llcom.Tools.Global from the WPF project.
/// </summary>
public class GlobalState
{
    private static GlobalState? _instance;
    public static GlobalState Instance => _instance ??= Load();

    public Settings Settings { get; set; } = new();
    public bool IsMainWindowClosed { get; set; }

    /// <summary>Profile path (matches PlatformHelper.ProfilePath).</summary>
    public string ProfilePath => PlatformHelper.ProfilePath;

    /// <summary>Encoding used for string/byte conversion.</summary>
    public System.Text.Encoding GetEncoding() => System.Text.Encoding.GetEncoding(Settings.encoding);

    /// <summary>Callback to refresh the Lua script list in UI.</summary>
    public static event Action? RefreshLuaScriptListCallback;
    public static void RefreshLuaScriptList() => RefreshLuaScriptListCallback?.Invoke();

    #region Online Scripts (GitHub GraphQL)

    private static string? _githubToken;
    public static List<OnlineScript> GetOnlineScripts(Action<int, int>? callback = null)
    {
        if (_githubToken == null)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                var response = client.GetStringAsync("https://llcom.papapoi.com/token.txt").Result;
                _githubToken = response.Trim();
            }
            catch { return new(); }
        }

        var scripts = new List<OnlineScript>();
        string? lastPage = null;
        var pages = 0;

        while (true)
        {
            var (data, total, endCursor, hasNextPage) = FetchPage(lastPage);
            foreach (var (body, url) in data)
            {
                try
                {
                    var n = new OnlineScript(body);
                    n.Url = url;
                    scripts.Add(n);
                }
                catch { }
            }
            callback?.Invoke(scripts.Count, total);
            if (!hasNextPage) break;
            lastPage = endCursor;
            pages++;
        }
        return scripts;
    }

    private static (List<(string body, string url)> data, int total, string? endCursor, bool hasNextPage)
        FetchPage(string? after)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "llcom");
        client.DefaultRequestHeaders.Add("Authorization", $"bearer {_githubToken}");

        var afterStr = after == null ? "" : $"after: \"{after}\",";
        var query = "{\"query\": \"query {repository(owner: \\\"chenxuuu\\\", name: \\\"llcom\\\") " +
            $"{{\"discussions(categoryId:\\\"DIC_kwDOCtNzks4CSz35\\\",{afterStr}first: 100) " +
            "{totalCount,pageInfo {startCursor,endCursor,hasNextPage,hasPreviousPage},nodes {body,url}}}}\"}";

        var content = new StringContent(query, System.Text.Encoding.UTF8, "application/json");
        var response = client.PostAsync("https://api.github.com/graphql", content).Result;
        var json = response.Content.ReadAsStringAsync().Result;
        var j = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(json)!;

        var bodys = j["data"]!["repository"]!["discussions"]!["nodes"]!
            .Select(n => ((string)n!["body"]!, (string)n!["url"]!)).ToList();

        var total = (int)j["data"]!["repository"]!["discussions"]!["totalCount"]!;
        var endCursor = (string?)j["data"]!["repository"]!["discussions"]!["pageInfo"]!["endCursor"];
        var hasNextPage = (bool)j["data"]!["repository"]!["discussions"]!["pageInfo"]!["hasNextPage"]!;

        return (bodys, total, endCursor, hasNextPage);
    }

    #endregion

    #region Hex conversion helpers

    public static byte[] Hex2Byte(string hex)
    {
        hex = hex.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Replace("-", "");
        if (hex.Length % 2 != 0) hex = "0" + hex;
        var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    public static string Byte2Hex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", " ");
    }

    public static string String2Hex(string s)
    {
        return BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(s)).Replace("-", " ");
    }

    public static string Hex2String(string hex)
    {
        return System.Text.Encoding.UTF8.GetString(Hex2Byte(hex));
    }

    #endregion

    private static GlobalState Load()
    {
        var state = new GlobalState();
        try
        {
            var path = PlatformHelper.ProfilePath + "settings.json";
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var settings = JsonConvert.DeserializeObject<Settings>(json);
                if (settings != null) state.Settings = settings;
            }
        }
        catch { }
        return state;
    }
}
