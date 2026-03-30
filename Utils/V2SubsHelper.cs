using V2SubsCombinator.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace V2SubsCombinator.Utils
{
    public class ClashConfig
    {
        [YamlMember(Alias = "tcp-concurrent", ApplyNamingConventions = false)]
        public bool TcpConcurrent { get; set; } = true;

        public string Secret { get; set; } = "";

        [YamlMember(Alias = "global-client-fingerprint", ApplyNamingConventions = false)]
        public string GlobalClientFingerprint { get; set; } = "chrome";

        [YamlMember(Alias = "allow-lan", ApplyNamingConventions = false)]
        public bool AllowLan { get; set; } = false;

        [YamlMember(Alias = "bind-address", ApplyNamingConventions = false)]
        public string BindAddress { get; set; } = "*";

        public string Mode { get; set; } = "rule";

        [YamlMember(Alias = "log-level", ApplyNamingConventions = false)]
        public string LogLevel { get; set; } = "info";

        [YamlMember(Alias = "external-controller", ApplyNamingConventions = false)]
        public string ExternalController { get; set; } = "127.0.0.1:9090";

        [YamlMember(Alias = "find-process-mode", ApplyNamingConventions = false)]
        public string FindProcessMode { get; set; } = "always";

        [YamlMember(Alias = "keep-alive-interval", ApplyNamingConventions = false)]
        public int KeepAliveInterval { get; set; } = 30;

        [YamlMember(Alias = "geo-auto-update", ApplyNamingConventions = false)]
        public bool GeoAutoUpdate { get; set; } = false;

        [YamlMember(Alias = "unified-delay", ApplyNamingConventions = false)]
        public bool UnifiedDelay { get; set; } = true;

        public DnsConfig? Dns { get; set; }

        public List<SupportedNode>? Proxies { get; set; }

        [YamlMember(Alias = "proxy-groups", ApplyNamingConventions = false)]
        public List<ProxyGroup>? ProxyGroups { get; set; }

        public List<string>? Rules { get; set; }
    }

    public class DnsConfig
    {
        public bool Enable { get; set; } = true;
        public string Listen { get; set; } = "127.0.0.1:53";

        [YamlMember(Alias = "default-nameserver", ApplyNamingConventions = false)]
        public List<string>? DefaultNameserver { get; set; }

        [YamlMember(Alias = "enhanced-mode", ApplyNamingConventions = false)]
        public string EnhancedMode { get; set; } = "fake-ip";

        [YamlMember(Alias = "fake-ip-range", ApplyNamingConventions = false)]
        public string FakeIpRange { get; set; } = "198.18.0.1/16";

        public List<string>? Nameserver { get; set; }

        [YamlMember(Alias = "nameserver-policy", ApplyNamingConventions = false)]
        public Dictionary<string, string>? NameserverPolicy { get; set; }

        public List<string>? Fallback { get; set; }

        [YamlMember(Alias = "fallback-filter", ApplyNamingConventions = false)]
        public FallbackFilterConfig? FallbackFilter { get; set; }
    }

    public class FallbackFilterConfig
    {
        public bool Geoip { get; set; } = true;
        public List<string>? Ipcidr { get; set; }
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
                Dns = new DnsConfig
                {
                    Enable = true,
                    Listen = "127.0.0.1:53",
                    DefaultNameserver = ["114.114.114.114", "223.5.5.5", "8.8.8.8"],
                    EnhancedMode = "fake-ip",
                    FakeIpRange = "198.18.0.1/16",
                    Nameserver =
                    [
                        "https://doh.pub/dns-query",
                        "https://dns.alidns.com/dns-query",
                        "https://1.1.1.1/dns-query",
                        "223.5.5.5",
                        "119.29.29.29",
                        "114.114.114.114",
                        "tcp://223.5.5.5"
                    ],
                    NameserverPolicy = new Dictionary<string, string>
                    {
                        ["*.digital-nvme.com"] = "8.138.94.132:8053",
                        ["geoip:cn"] = "223.5.5.5,114.114.114.114,119.29.29.29"
                    },
                    Fallback =
                    [
                        "https://doh.dns.sb/dns-query",
                        "https://dns.cloudflare.com/dns-query",
                        "https://dns.twnic.tw/dns-query",
                        "tls://8.8.4.4:853"
                    ],
                    FallbackFilter = new FallbackFilterConfig
                    {
                        Geoip = true,
                        Ipcidr = ["240.0.0.0/4", "0.0.0.0/32"]
                    }
                },
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
                    "DOMAIN-KEYWORD,localhost,DIRECT",
                    "IP-CIDR,127.0.0.0/8,DIRECT",
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

        public static async Task<List<SupportedNode>> FetchSubscriptionNodesAsync(string url, string remarkPrefix)
        {
            var nodes = new List<SupportedNode>();

            if (SupportedNetworkNodeHelper.TryGetNodeType(url, out _))
            {
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
        }

        public static async Task<string> FetchAndCombineSubscriptionsAsync(
            IEnumerable<(string url, string remarkPrefix, List<SupportedNode>? cachedNodes)> subscriptions,
            bool isClash)
        {
            var subList = subscriptions.ToList();
            var tasks = subList.Select(async sub =>
            {
                var (url, remarkPrefix, cachedNodes) = sub;
                
                if (cachedNodes != null && cachedNodes.Count > 0)
                {
                    return cachedNodes;
                }

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
