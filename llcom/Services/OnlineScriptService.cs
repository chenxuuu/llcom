using llcom.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace llcom.Tools
{
    /// <summary>
    /// 在线脚本服务。
    /// 通过 GitHub GraphQL API 拉取仓库讨论区（Discussions）中的脚本分享帖，
    /// 解析为 OnlineScript 列表。Token 从 llcom.papapoi.com 获取。
    /// 从原 Tools/Global.cs 拆出（Step 2）。
    /// </summary>
    internal static class OnlineScriptService
    {
        private static string GitHubToken = null;

        /// <summary>
        /// 获取在线脚本列表（分页拉取全部）
        /// </summary>
        /// <param name="callback">进度回调（当前页, 总页数）</param>
        /// <returns>脚本列表；获取 Token 失败时返回 null</returns>
        public static List<OnlineScript> GetOnlineScripts(Action<int, int> callback = null)
        {
            if (GitHubToken == null)
            {
                try
                {
                    var client = new RestClient("https://llcom.papapoi.com/token.txt");
                    var request = new RestRequest();
                    request.Timeout = 10000;
                    var response = client.Get(request);
                    GitHubToken = response.Content;
                }
                catch
                {
                    return null;
                }
            }
            //请求函数
            var req = (string after) =>
            {
                var client = new RestClient();
                client.BaseUrl = new Uri("https://api.github.com/graphql");
                var request = new RestRequest(RestSharp.Method.POST);
                request.AddHeader("user-agent", "llcom");
                request.AddHeader("Authorization", $"bearer {GitHubToken}");
                request.AddParameter("application/json",
                    "{\r\n  \"query\": \"query {repository(owner: \\\"chenxuuu\\\", name: \\\"llcom\\\") {discussions(categoryId:\\\"DIC_kwDOCtNzks4CSz35\\\"," +
                    (after == null ? "" : $"after: \"{after}\"") + "first: 100" +
                    ") {totalCount,pageInfo {startCursor,endCursor,hasNextPage,hasPreviousPage},nodes {body,url}}}}\"\r\n}", ParameterType.RequestBody);
                var response = client.Execute(request);
                var j = JsonConvert.DeserializeObject<JObject>(response.Content);
                var bodys = from i in j["data"]["repository"]["discussions"]["nodes"]
                            select ((string)i["body"], (string)i["url"]);
                return (
                    bodys.ToList(),
                    (int)j["data"]["repository"]["discussions"]["totalCount"],
                    (string)j["data"]["repository"]["discussions"]["pageInfo"]["endCursor"],
                    (bool)j["data"]["repository"]["discussions"]["pageInfo"]["hasNextPage"]
                );
            };

            string lastPage = null;
            var scripts = new List<OnlineScript>();
            var pages = 0;
            while (true)
            {
                var (data, total, endCursor, hasNextPage) = req(lastPage);
                foreach (var (s, u) in data)
                {
                    try
                    {
                        var n = new OnlineScript(s);
                        n.Url = u;
                        scripts.Add(n);
                    }
                    catch { }
                }
                callback?.Invoke(pages, total / 100);//回调，上报进度
                if (!hasNextPage)
                    break;
                pages++;
                lastPage = endCursor;
            }
            return scripts;
        }
    }
}
