using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace llcom.Tools;

/// <summary>
/// 文件工具服务。
/// 负责：释放软件内嵌资源到磁盘、读取嵌入资源内容、导入 SSCOM 配置文件。
/// 从原 Tools/Global.cs 拆出（Step 2）。
/// </summary>
internal static class FileUtils
{
    /// <summary>
    /// 导入SSCOM配置文件数据
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static List<Model.ToSendData> ImportFromSSCOM(string path)
    {
        var lines = File.ReadAllLines(path, Encoding.GetEncoding("GB2312"));
        var r = new List<Model.ToSendData>();
        Regex title = new Regex(@"N1\d\d=\d*,");
        for (int i = 0; i < lines.Length; i++)
        {
            try
            {
                var temp = new Model.ToSendData();
                //Console.WriteLine(lines[i]);
                if (title.IsMatch(lines[i])) //匹配上了
                {
                    var strs = lines[i].Split(",".ToCharArray()[0]);
                    temp.commit = strs[1].Replace(((char)2).ToString(), ",");
                    if (string.IsNullOrWhiteSpace(temp.commit))
                        temp.commit = "发送";
                    //Console.WriteLine(temp.commit);

                    int dot = lines[i + 1].IndexOf(",");
                    temp.hex = lines[i + 1].Substring(dot - 1, 1) == "H";
                    //Console.WriteLine(strs[0].Substring(strs[0].Length - 1));

                    string text = lines[i + 1].Substring(dot + 1);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        temp.text = text.Replace(((char)2).ToString(), ",");
                        r.Add(temp);
                    }
                }
            }
            catch
            {
                //先不处理
            }
        }
        return r;
    }

    /// <summary>
    /// 读取软件资源文件内容
    /// </summary>
    /// <param name="path">路径</param>
    /// <returns>内容字节数组</returns>
    public static byte[] GetAssetsFileContent(string path)
    {
        Uri uri = new Uri(path, UriKind.Relative);
        var source = Application.GetResourceStream(uri).Stream;
        byte[] f = new byte[source.Length];
        source.Read(f, 0, (int)source.Length);
        return f;
    }

    /// <summary>
    /// 取出文件（将内嵌资源释放到磁盘）
    /// </summary>
    /// <param name="insidePath">软件内部的路径</param>
    /// <param name="outPath">需要释放到的路径</param>
    /// <param name="d">是否覆盖</param>
    public static void CreateFile(string insidePath, string outPath, bool d = true)
    {
        if (!File.Exists(outPath) || d)
            File.WriteAllBytes(outPath, GetAssetsFileContent(insidePath));
    }
}
