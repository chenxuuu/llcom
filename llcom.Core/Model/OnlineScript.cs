using System.Text.RegularExpressions;

namespace llcom.Model;

public class OnlineScript
{
    /// <summary>作者</summary>
    public string Author { get; set; } = "";
    /// <summary>脚本名</summary>
    public string Name { get; set; } = "";
    /// <summary>简介</summary>
    public string Description { get; set; } = "";
    /// <summary>版本</summary>
    public int Version { get; set; }
    /// <summary>备注</summary>
    public string Note { get; set; } = "";
    /// <summary>脚本内容</summary>
    public string Script { get; set; } = "";
    /// <summary>脚本网址</summary>
    public string? Url { get; set; }

    /// <summary>从GitHub markdown数据解析脚本</summary>
    public OnlineScript(string body)
    {
        body = body.Replace("\r", "");
        var regStr =
            "- *(?<author>.+?)\n" +
            "- *(?<name>.+?)\n" +
            "- *(?<description>.+?)\n" +
            "- *(?<version>.+?)\n" +
            "- *(?<note>.+?)\n" +
            "\n*" +
            "```lua\n(?<script>.+)\n```";
        var match = Regex.Match(body, regStr, RegexOptions.Singleline);
        if (!match.Success)
            throw new Exception("can not match format");
        Author = match.Groups["author"].Value;
        Name = match.Groups["name"].Value;
        Description = match.Groups["description"].Value;
        Version = int.Parse(match.Groups["version"].Value);
        Note = match.Groups["note"].Value;
        Script = match.Groups["script"].Value;
    }

    public OnlineScript() { }

    public override string ToString()
    {
        return $"{Name} - {Version} {Author}\r\n{Description}";
    }
}
