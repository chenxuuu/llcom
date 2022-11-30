using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace llcom.Model
{
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class OnlineScript
    {
        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; }
        /// <summary>
        /// 脚本名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 简介
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Note { get; set; }
        /// <summary>
        /// 脚本内容
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// 脚本网址
        /// </summary>
        public string Url { get; set; } = null;

        /// <summary>
        /// 导入来自GitHub的原始数据
        /// </summary>
        /// <param name="body">markdown原始数据</param>
        public OnlineScript(string body)
        {
            //- 作者 author
            //- 脚本名 script name
            //- 脚本功能 script function
            //- 版本 vesion （0~2147483647）
            //- 备注 notes

            //```
            //你的脚本
            //your script
            //```

            //只留下\n换行符，好处理
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

        public OnlineScript()
        {

        }

        public override string ToString()
        {
            return $"{Name} - {Version} {Author}\r\n{Description}";
        }
    }
}
