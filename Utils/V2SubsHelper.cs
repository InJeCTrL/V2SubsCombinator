using System.Text;
using System.Text.Json.Nodes;

namespace V2SubsCombinator.Utils
{
    public static class V2SubsHelper
    {
        private static readonly HttpClient httpClient = new();

        private static readonly Dictionary<string, Func<string, string, string>> protocolHandlers =
        new()
        {
            { "vmess", ProcessVmess },
            { "vless", ProcessVless },
            { "trojan", ProcessTrojan },
            { "ss", ProcessShadowsocks },
            { "ssr", ProcessShadowsocksR }
        };

        public static string DecodeBase64(string content)
        {
            try
            {
                var bytes = Convert.FromBase64String(content.Trim());
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return content;
            }
        }

        public static string EncodeBase64(string content)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        }

        public static bool IsSingleNode(string subUrl, out string protocol)
        {
            var protocolEndIndex = subUrl.IndexOf("://", StringComparison.Ordinal);
            if (protocolEndIndex == -1)
            {
                protocol = string.Empty;
                return false;
            }

            var currentProtocol = subUrl[..protocolEndIndex];
            if (!protocolHandlers.ContainsKey(currentProtocol.Trim()))
            {
                protocol = string.Empty;
                return false;
            }

            protocol = currentProtocol;

            return true;
        }

        public static string AddRemarkPrefixToSub(string subUrl, string remarkPrefix)
        {
            if (string.IsNullOrEmpty(remarkPrefix) || string.IsNullOrEmpty(subUrl))
                return subUrl;

            subUrl = subUrl.Trim();

            if (!IsSingleNode(subUrl, out var protocol))
                return subUrl;

            if (!protocolHandlers.TryGetValue(protocol, out var handler))
                return subUrl;

            return handler(subUrl, remarkPrefix);
        }

        private static string ProcessVmess(string subUrl, string remarkPrefix)
        {
            try
            {
                var base64Part = subUrl[8..];
                var decoded = DecodeBase64(base64Part);
                var json = JsonNode.Parse(decoded);
                
                if (json != null && json["ps"] != null)
                {
                    json["ps"] = remarkPrefix + json["ps"]?.GetValue<string>();
                    var newJson = json.ToJsonString();
                    return "vmess://" + EncodeBase64(newJson);
                }
            }
            catch { }
            return subUrl;
        }

        private static string ProcessVless(string subUrl, string remarkPrefix)
        {
            try
            {
                var hashIndex = subUrl.LastIndexOf('#');
                if (hashIndex > 0)
                {
                    var originRemark = subUrl[(hashIndex + 1)..];
                    return subUrl[..hashIndex] + "#" + Uri.EscapeDataString(remarkPrefix) + originRemark;
                }
            }
            catch { }
            return subUrl;
        }

        private static string ProcessTrojan(string subUrl, string remarkPrefix)
        {
            return ProcessVless(subUrl, remarkPrefix);
        }

        private static string ProcessShadowsocks(string subUrl, string remarkPrefix)
        {
            try
            {
                var hashIndex = subUrl.LastIndexOf('#');
                if (hashIndex > 0)
                {
                    var originRemark = subUrl[(hashIndex + 1)..];
                    return subUrl[..hashIndex] + "#" + Uri.EscapeDataString(remarkPrefix) + originRemark;
                }
            }
            catch { }
            return subUrl;
        }

        private static string ProcessShadowsocksR(string subUrl, string remarkPrefix)
        {
            try
            {
                var base64Part = subUrl[6..];
                var decoded = DecodeBase64(base64Part);
                
                var remarksIndex = decoded.IndexOf("remarks=", StringComparison.OrdinalIgnoreCase);
                if (remarksIndex > 0)
                {
                    var endIndex = decoded.IndexOf('&', remarksIndex);
                    var originRemark = endIndex > 0 
                        ? decoded[(remarksIndex + 8)..endIndex]
                        : decoded[(remarksIndex + 8)..];
                    
                    decoded = endIndex > 0
                        ? decoded[..(remarksIndex + 8)] + Uri.EscapeDataString(remarkPrefix) + originRemark + decoded[endIndex..]
                        : decoded[..(remarksIndex + 8)] + Uri.EscapeDataString(remarkPrefix) + originRemark;
                    
                    return "ssr://" + EncodeBase64(decoded);
                }
            }
            catch { }
            return subUrl;
        }

        public static async Task<string> FetchAndCombineSubscriptionsAsync(
            IEnumerable<(string url, string remarkPrefix)> subscriptions)
        {
            var subList = subscriptions.ToList();
            return await httpClient.GetStringAsync(subList[0].url);
            var tasks = subList.Select(async sub =>
            {
                var (url, remarkPrefix) = sub;
                var lines = new List<string>();

                if (IsSingleNode(url, out var protocol))
                {
                    lines.Add(AddRemarkPrefixToSub(url, remarkPrefix));
                    return lines;
                }

                for (var retry = 0; retry < 5; retry++)
                {
                    try
                    {
                        var content = await httpClient.GetStringAsync(url);
                        var decoded = DecodeBase64(content);
                        foreach (var line in decoded.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                        {
                            var trimmed = line.Trim();
                            if (!string.IsNullOrEmpty(trimmed))
                                lines.Add(AddRemarkPrefixToSub(trimmed, remarkPrefix));
                        }
                        break;
                    }
                    catch { }
                }
                return lines;
            });

            var results = await Task.WhenAll(tasks);
            var allLines = results.SelectMany(x => x).ToList();
            
            return EncodeBase64(string.Join("\n", allLines));
        }
    }
}
