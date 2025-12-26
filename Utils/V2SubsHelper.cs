using V2SubsCombinator.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace V2SubsCombinator.Utils
{
    public class ClashConfig
    {
        public List<SupportedNode>? Proxies { get; set; }

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

        private static bool IsClashYaml(string content)
        {
            var trimmed = content.TrimStart();
            return trimmed.StartsWith("proxies:") ||
                   trimmed.Contains("\nproxies:") ||
                   trimmed.StartsWith("port:") ||
                   trimmed.StartsWith("mixed-port:");
        }

        private static List<SupportedNode> ParseYamlToNodes(string yamlContent, string remarkPrefix)
        {
            try
            {
                var config = yamlDeserializer.Deserialize<ClashConfig>(yamlContent);
                if (config?.Proxies == null) return [];

                foreach (var node in config.Proxies)
                    node.Name = remarkPrefix + node.Name;

                return config.Proxies.Where(n => !string.IsNullOrEmpty(n.Server)).ToList();
            }
            catch { return []; }
        }

        private static SupportedNode? ParseSingleNode(string url, string remarkPrefix)
        {
            var node = new SupportedNode(url, remarkPrefix);
            return !string.IsNullOrEmpty(node.Server) ? node : null;
        }

        private static List<SupportedNode> ParseV2rayToNodes(string content, string remarkPrefix)
        {
            var nodes = new List<SupportedNode>();
            var decoded = SupportedNetworkNodeHelper.DecodeBase64(content);
            foreach (var line in decoded.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = line.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    var node = ParseSingleNode(trimmed, remarkPrefix);
                    if (node != null) nodes.Add(node);
                }
            }
            return nodes;
        }

        private static string GenerateYaml(List<SupportedNode> nodes)
        {
            var validNodes = nodes.Where(n => !string.IsNullOrEmpty(n.Server) && n.Port != null).ToList();
            var proxyNames = validNodes.Select(n => n.Name).ToList();

            var config = new ClashConfig
            {
                Proxies = validNodes,
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

        private static string GenerateV2ray(List<SupportedNode> nodes)
        {
            var urls = nodes
                .Select(n => n.ToUrl())
                .Where(url => !string.IsNullOrEmpty(url))
                .ToList();
            return SupportedNetworkNodeHelper.EncodeBase64(string.Join("\n", urls));
        }

        public static async Task<string> FetchAndCombineSubscriptionsAsync(
            IEnumerable<(string url, string remarkPrefix)> subscriptions,
            bool isClash)
        {
            var subList = subscriptions.ToList();
            var tasks = subList.Select(async sub =>
            {
                var (url, remarkPrefix) = sub;
                var nodes = new List<SupportedNode>();

                if (SupportedNetworkNodeHelper.TryGetNodeType(url, out _))
                {
                    var node = ParseSingleNode(url, remarkPrefix);
                    if (node != null) nodes.Add(node);
                    return nodes;
                }

                for (var retry = 0; retry < 5; retry++)
                {
                    try
                    {
                        var content = await httpClient.GetStringAsync(url);

                        if (IsClashYaml(content))
                        {
                            nodes.AddRange(ParseYamlToNodes(content, remarkPrefix));
                        }
                        else
                        {
                            nodes.AddRange(ParseV2rayToNodes(content, remarkPrefix));
                        }
                        break;
                    }
                    catch { }
                }
                return nodes;
            });

            var results = await Task.WhenAll(tasks);
            var allNodes = results.SelectMany(x => x).ToList();

            return isClash ? GenerateYaml(allNodes) : GenerateV2ray(allNodes);
        }
    }
}
