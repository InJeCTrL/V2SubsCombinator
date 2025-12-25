using V2SubsCombinator.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace V2SubsCombinator.Utils
{
    public class ClashConfig
    {
        public List<ISupportedNode>? Proxies { get; set; }

        [YamlMember(Alias = "proxy-groups", ApplyNamingConventions = false)]
        public List<ProxyGroup>? ProxyGroups { get; set; }

        public List<string>? Rules { get; set; }
    }

    public class ProxyGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "select";
        public List<string>? Proxies { get; set; }
    }

    public static class V2SubsHelper
    {
        private static readonly HttpClient httpClient = new();

        private static readonly IDeserializer yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        private static readonly ISerializer yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
            .Build();

        private static readonly ISerializer yamlSerializerForDict = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();

        private static readonly Dictionary<string, Type> NodeTypes = new()
        {
            ["vmess"] = typeof(Vmess),
            ["vless"] = typeof(Vless),
            ["trojan"] = typeof(Trojan),
            ["ss"] = typeof(Shadowsocks),
            ["ssr"] = typeof(ShadowsocksR)
        };

        private static bool IsClashYaml(string content)
        {
            var trimmed = content.TrimStart();
            return trimmed.StartsWith("proxies:") ||
                   trimmed.Contains("\nproxies:") ||
                   trimmed.StartsWith("port:") ||
                   trimmed.StartsWith("mixed-port:");
        }

        private static List<ISupportedNode> ParseClashYamlToModels(string yamlContent, string remarkPrefix)
        {
            var models = new List<ISupportedNode>();
            try
            {
                var dict = yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                if (dict == null || !dict.TryGetValue("proxies", out var proxiesObj) || proxiesObj is not List<object> proxies)
                    return models;

                foreach (var proxy in proxies)
                {
                    var proxyYaml = yamlSerializerForDict.Serialize(proxy);

                    if (proxy is not Dictionary<object, object> proxyDict ||
                        !proxyDict.TryGetValue("type", out var typeObj))
                        continue;

                    var type = typeObj?.ToString();
                    if (string.IsNullOrEmpty(type) || !NodeTypes.TryGetValue(type, out var nodeType))
                        continue;

                    var node = yamlDeserializer.Deserialize(proxyYaml, nodeType) as ISupportedNode;
                    if (node != null)
                    {
                        node.Name = remarkPrefix + node.Name;
                        models.Add(node);
                    }
                }
            }
            catch { }
            return models;
        }

        private static string GenerateClashYaml(List<ISupportedNode> models)
        {
            var proxyNames = models.Select(m => m.Name).ToList();

            var config = new ClashConfig
            {
                Proxies = models,
                ProxyGroups =
                [
                    new ProxyGroup
                    {
                        Name = "PROXY",
                        Type = "select",
                        Proxies = proxyNames
                    }
                ],
                Rules =
                [
                    "GEOIP,CN,DIRECT",
                    "MATCH,PROXY"
                ]
            };
            return yamlSerializer.Serialize(config);
        }

        private static string GenerateBase64(List<ISupportedNode> models)
        {
            var lines = models.Select(m => m.ToUrl()).ToList();
            return SupportedNetworkNodeParser.EncodeBase64(string.Join("\n", lines));
        }

        public static async Task<string> FetchAndCombineSubscriptionsAsync(
            IEnumerable<(string url, string remarkPrefix)> subscriptions,
            bool isClash)
        {
            var subList = subscriptions.ToList();
            var tasks = subList.Select(async sub =>
            {
                var (url, remarkPrefix) = sub;
                var models = new List<ISupportedNode>();

                if (SupportedNetworkNodeParser.TryGetNodeType(url, out _))
                {
                    var model = SupportedNetworkNodeParser.Parse(url, remarkPrefix);
                    if (model != null)
                        models.Add(model);
                    return models;
                }

                for (var retry = 0; retry < 5; retry++)
                {
                    try
                    {
                        var content = await httpClient.GetStringAsync(url);

                        if (IsClashYaml(content))
                        {
                            models.AddRange(ParseClashYamlToModels(content, remarkPrefix));
                        }
                        else
                        {
                            var decoded = SupportedNetworkNodeParser.DecodeBase64(content);
                            foreach (var line in decoded.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                            {
                                var trimmed = line.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    var model = SupportedNetworkNodeParser.Parse(trimmed, remarkPrefix);
                                    if (model != null)
                                        models.Add(model);
                                }
                            }
                        }
                        break;
                    }
                    catch { }
                }
                return models;
            });

            var results = await Task.WhenAll(tasks);
            var allModels = results.SelectMany(x => x).ToList();

            return isClash ? GenerateClashYaml(allModels) : GenerateBase64(allModels);
        }
    }
}
