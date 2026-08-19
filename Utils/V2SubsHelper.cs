using V2SubsCombinator.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace V2SubsCombinator.Utils
{
    public class ClashConfig
    {
        [YamlMember(Alias = "mixed-port", ApplyNamingConventions = false)]
        public int MixedPort { get; set; } = 7890;

        [YamlMember(Alias = "allow-lan", ApplyNamingConventions = false)]
        public bool AllowLan { get; set; } = true;

        [YamlMember(Alias = "bind-address", ApplyNamingConventions = false)]
        public string BindAddress { get; set; } = "*";

        public string Mode { get; set; } = "rule";

        [YamlMember(Alias = "log-level", ApplyNamingConventions = false)]
        public string LogLevel { get; set; } = "info";

        [YamlMember(Alias = "external-controller", ApplyNamingConventions = false)]
        public string ExternalController { get; set; } = "127.0.0.1:9090";

        public DnsConfig? Dns { get; set; }

        public List<SupportedNode>? Proxies { get; set; }

        [YamlMember(Alias = "proxy-groups", ApplyNamingConventions = false)]
        public List<ProxyGroup>? ProxyGroups { get; set; }

        public List<string>? Rules { get; set; }
    }

    public class DnsConfig
    {
        public bool Enable { get; set; } = true;
        
        [YamlMember(Alias = "ipv6", ApplyNamingConventions = false)]
        public bool Ipv6 { get; set; } = false;

        [YamlMember(Alias = "default-nameserver", ApplyNamingConventions = false)]
        public List<string>? DefaultNameserver { get; set; }

        [YamlMember(Alias = "enhanced-mode", ApplyNamingConventions = false)]
        public string EnhancedMode { get; set; } = "fake-ip";

        [YamlMember(Alias = "fake-ip-range", ApplyNamingConventions = false)]
        public string FakeIpRange { get; set; } = "198.18.0.1/16";
        
        [YamlMember(Alias = "use-hosts", ApplyNamingConventions = false)]
        public bool UseHosts { get; set; } = true;
        
        [YamlMember(Alias = "respect-rules", ApplyNamingConventions = false)]
        public bool RespectRules { get; set; } = true;
        
        [YamlMember(Alias = "proxy-server-nameserver", ApplyNamingConventions = false)]
        public List<string>? ProxyServerNameserver { get; set; }

        public List<string>? Nameserver { get; set; }

        public List<string>? Fallback { get; set; }

        [YamlMember(Alias = "fallback-filter", ApplyNamingConventions = false)]
        public FallbackFilterConfig? FallbackFilter { get; set; }
    }

    public class FallbackFilterConfig
    {
        public bool Geoip { get; set; } = true;
        
        [YamlMember(Alias = "geoip-code", ApplyNamingConventions = false)]
        public string GeoipCode { get; set; } = "CN";
        
        public List<string>? Geosite { get; set; }
        
        public List<string>? Ipcidr { get; set; }
        
        public List<string>? Domain { get; set; }
    }

    public class ProxyGroup
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "select";
        public List<string>? Proxies { get; set; }
        public string? Url { get; set; }
        public int? Interval { get; set; }
        public int? Tolerance { get; set; }
        public bool? Lazy { get; set; }

        [YamlMember(Alias = "expected-status", ApplyNamingConventions = false)]
        public int? ExpectedStatus { get; set; }

        public int? Timeout { get; set; }
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

        private static List<SupportedNode> ParseContentToNodes(string content, string remarkPrefix)
        {
            if (SupportedNetworkNodeHelper.TryGetNodeType(content.Trim(), out _))
            {
                var node = ParseSingleNode(content.Trim(), remarkPrefix);
                return node == null ? [] : [node];
            }

            return IsClashYaml(content)
                ? ParseYamlToNodes(content, remarkPrefix)
                : ParseV2rayToNodes(content, remarkPrefix);
        }

        public static async Task<string?> FetchSubscriptionContentAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (SupportedNetworkNodeHelper.TryGetNodeType(url, out _)) return url;

            for (var retry = 0; retry < 5; retry++)
            {
                try
                {
                    return await httpClient.GetStringAsync(url);
                }
                catch { }
            }

            return null;
        }

        private static string GenerateYaml(List<SupportedNode> nodes)
        {
            var validNodes = nodes.Where(n => !string.IsNullOrEmpty(n.Server) && n.Port != null).ToList();
            
            // 清理默认值，使其在 YAML 中被省略
            foreach (var node in validNodes)
            {
                // 如果 network 是默认值 tcp，设置为 null 以省略该字段
                if (node.Network == "tcp") node.Network = null;
                
                // 如果 flow 是空字符串，保持为空字符串（原始订阅中有 flow: ''）
            }
            
            var proxyNames = validNodes.Select(n => n.Name).ToList();

            var config = new ClashConfig
            {
                Dns = new DnsConfig
                {
                    Enable = true,
                    Ipv6 = false,
                    DefaultNameserver = ["223.5.5.5", "119.29.29.29", "114.114.114.114"],
                    EnhancedMode = "fake-ip",
                    FakeIpRange = "198.18.0.1/16",
                    UseHosts = true,
                    RespectRules = true,
                    ProxyServerNameserver = ["223.5.5.5", "119.29.29.29", "114.114.114.114"],
                    Nameserver = ["223.5.5.5", "119.29.29.29", "114.114.114.114"],
                    Fallback = ["1.1.1.1", "8.8.8.8"],
                    FallbackFilter = new FallbackFilterConfig
                    {
                        Geoip = true,
                        GeoipCode = "CN",
                        Geosite = ["gfw"],
                        Ipcidr = ["240.0.0.0/4"],
                        Domain = ["+.google.com", "+.facebook.com", "+.youtube.com"]
                    }
                },
                Proxies = validNodes,
                ProxyGroups =
                [
                    new ProxyGroup
                    {
                        Name = "PROXY",
                        Type = "select",
                        Proxies = ["AUTO", "DIRECT", .. proxyNames]
                    },
                    new ProxyGroup
                    {
                        Name = "AUTO",
                        Type = "url-test",
                        Proxies = proxyNames,
                        Url = "https://www.gstatic.com/generate_204",
                        Interval = 300,
                        Tolerance = 100,
                        Lazy = true,
                        ExpectedStatus = 204,
                        Timeout = 5000
                    }
                ],
                Rules =
                [
                    "GEOSITE,category-cryptocurrency,PROXY",
                    "DOMAIN-SUFFIX,localhost,DIRECT",
                    "GEOSITE,private,DIRECT",
                    "GEOSITE,cn,DIRECT",
                    "IP-CIDR,127.0.0.0/8,DIRECT,no-resolve",
                    "IP-CIDR,10.0.0.0/8,DIRECT,no-resolve",
                    "IP-CIDR,172.16.0.0/12,DIRECT,no-resolve",
                    "IP-CIDR,192.168.0.0/16,DIRECT,no-resolve",
                    "IP-CIDR,100.64.0.0/10,DIRECT,no-resolve",
                    "IP-CIDR6,::1/128,DIRECT,no-resolve",
                    "IP-CIDR6,fc00::/7,DIRECT,no-resolve",
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
            IEnumerable<(string url, string remarkPrefix, string? fixedContent)> subscriptions,
            bool isClash)
        {
            var subList = subscriptions.ToList();
            var tasks = subList.Select(async sub =>
            {
                var (url, remarkPrefix, fixedContent) = sub;
                var nodes = new List<SupportedNode>();

                // 如果有固定内容，直接解析
                if (!string.IsNullOrEmpty(fixedContent))
                {
                    nodes.AddRange(ParseContentToNodes(fixedContent, remarkPrefix));
                    return nodes;
                }

                // 如果没有 URL，返回空
                if (string.IsNullOrEmpty(url))
                {
                    return nodes;
                }

                // 单节点处理
                if (SupportedNetworkNodeHelper.TryGetNodeType(url, out _))
                {
                    var node = ParseSingleNode(url, remarkPrefix);
                    if (node != null) nodes.Add(node);
                    return nodes;
                }

                // 从 URL 获取订阅
                var content = await FetchSubscriptionContentAsync(url);
                if (!string.IsNullOrEmpty(content))
                {
                    nodes.AddRange(ParseContentToNodes(content, remarkPrefix));
                }
                return nodes;
            });

            var results = await Task.WhenAll(tasks);
            var allNodes = results.SelectMany(x => x).ToList();

            return isClash ? GenerateYaml(allNodes) : GenerateV2ray(allNodes);
        }
    }
}
