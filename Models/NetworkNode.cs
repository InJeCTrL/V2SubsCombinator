using System.Text;
using System.Text.Json.Nodes;
using System.Web;
using YamlDotNet.Serialization;

namespace V2SubsCombinator.Models
{
    public static class SupportedNetworkNodeParser
    {
        public const string Vmess = "vmess";
        public const string Vless = "vless";
        public const string Trojan = "trojan";
        public const string Shadowsocks = "ss";
        public const string ShadowsocksR = "ssr";

        public static bool TryGetNodeType(string url, out string nodeType)
        {
            url = url.Trim();
            if (url.StartsWith("vmess://"))
            {
                nodeType = Vmess;
                return true;
            }
            else if (url.StartsWith("vless://"))
            {
                nodeType = Vless;
                return true;
            }
            else if (url.StartsWith("trojan://"))
            {
                nodeType = Trojan;
                return true;
            }
            else if (url.StartsWith("ss://"))
            {
                nodeType = Shadowsocks;
                return true;
            }
            else if (url.StartsWith("ssr://"))
            {
                nodeType = ShadowsocksR;
                return true;
            }

            nodeType = string.Empty;
            return false;
        }

        public static bool TryParse<T>(string url, out T? node, string remarkPrefix = "") where T : ISupportedNode, new()
        {
            node = new T();
            if (node.Parse(url.Trim()))
            {
                node.Name = remarkPrefix + node.Name;
                return true;
            }
            node = default;
            return false;
        }

        public static ISupportedNode? Parse(string url, string remarkPrefix = "")
        {
            url = url.Trim();

            if (!TryGetNodeType(url, out var nodeType))
                return null;

            ISupportedNode? node = nodeType switch
            {
                Vmess => TryParse<Vmess>(url, out var vmess, remarkPrefix) ? vmess : null,
                Vless => TryParse<Vless>(url, out var vless, remarkPrefix) ? vless : null,
                Trojan => TryParse<Trojan>(url, out var trojan, remarkPrefix) ? trojan : null,
                Shadowsocks => TryParse<Shadowsocks>(url, out var ss, remarkPrefix) ? ss : null,
                ShadowsocksR => TryParse<ShadowsocksR>(url, out var ssr, remarkPrefix) ? ssr : null,
                _ => null
            };

            return node;
        }

        public static string DecodeBase64(string content)
        {
            try
            {
                var bytes = Convert.FromBase64String(content.Trim());
                return Encoding.UTF8.GetString(bytes);
            }
            catch { return content; }
        }

        public static string EncodeBase64(string content)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        }
    }

    public interface ISupportedNode
    {
        string Name { get; set; }
        string Type { get; }
        bool Parse(string url);
        string ToUrl();
    }

    public class Vmess : ISupportedNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "vmess";
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Uuid { get; set; } = string.Empty;
        [YamlMember(Alias = "alterId", ApplyNamingConventions = false)]
        public int AlterId { get; set; } = 0;
        public string Cipher { get; set; } = "auto";
        public string Network { get; set; } = "tcp";
        public bool? Tls { get; set; }
        public string? Servername { get; set; }
        public List<string>? Alpn { get; set; }
        public bool? SkipCertVerify { get; set; }
        public bool Udp { get; set; } = true;
        public WsOpts? WsOpts { get; set; }
        public GrpcOpts? GrpcOpts { get; set; }

        public bool Parse(string url)
        {
            try
            {
                var base64Part = url[8..];
                var decoded = SupportedNetworkNodeParser.DecodeBase64(base64Part);
                var json = JsonNode.Parse(decoded);
                if (json == null) return false;

                Name = json["ps"]?.GetValue<string>() ?? "vmess";
                Server = json["add"]?.GetValue<string>() ?? "";
                Port = int.TryParse(json["port"]?.ToString(), out var port) ? port : 443;
                Uuid = json["id"]?.GetValue<string>() ?? "";
                AlterId = int.TryParse(json["aid"]?.ToString(), out var aid) ? aid : 0;
                Cipher = json["scy"]?.GetValue<string>() ?? "auto";
                Network = json["net"]?.GetValue<string>() ?? "tcp";

                var tls = json["tls"]?.GetValue<string>();
                if (tls == "tls")
                {
                    Tls = true;
                    Servername = json["sni"]?.GetValue<string>();
                    var alpn = json["alpn"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(alpn))
                        Alpn = [.. alpn.Split(',')];
                }

                if (Network == "ws")
                {
                    var path = json["path"]?.GetValue<string>();
                    var host = json["host"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(path) || !string.IsNullOrEmpty(host))
                    {
                        WsOpts = new WsOpts { Path = path };
                        if (!string.IsNullOrEmpty(host))
                            WsOpts.Headers = new Dictionary<string, string> { ["Host"] = host };
                    }
                }
                else if (Network == "grpc")
                {
                    var serviceName = json["path"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(serviceName))
                        GrpcOpts = new GrpcOpts { GrpcServiceName = serviceName };
                }

                return true;
            }
            catch { return false; }
        }

        public string ToUrl()
        {
            var json = new JsonObject
            {
                ["v"] = "2",
                ["ps"] = Name,
                ["add"] = Server,
                ["port"] = Port.ToString(),
                ["id"] = Uuid,
                ["aid"] = AlterId.ToString(),
                ["scy"] = Cipher,
                ["net"] = Network
            };

            if (Tls == true)
            {
                json["tls"] = "tls";
                if (!string.IsNullOrEmpty(Servername))
                    json["sni"] = Servername;
            }

            if (WsOpts != null)
            {
                if (!string.IsNullOrEmpty(WsOpts.Path))
                    json["path"] = WsOpts.Path;
                if (WsOpts.Headers?.TryGetValue("Host", out var host) == true)
                    json["host"] = host;
            }

            return "vmess://" + SupportedNetworkNodeParser.EncodeBase64(json.ToJsonString());
        }
    }

    public class Vless : ISupportedNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "vless";
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string Network { get; set; } = "tcp";
        public string? Flow { get; set; }
        public bool? Tls { get; set; }
        public string? Servername { get; set; }
        public List<string>? Alpn { get; set; }
        public string? ClientFingerprint { get; set; }
        public bool? SkipCertVerify { get; set; }
        public bool Udp { get; set; } = true;
        public RealityOpts? RealityOpts { get; set; }
        public WsOpts? WsOpts { get; set; }
        public GrpcOpts? GrpcOpts { get; set; }

        public bool Parse(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);

                Name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
                if (string.IsNullOrEmpty(Name)) Name = "vless";
                Server = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 443;
                Uuid = uri.UserInfo;
                Network = query["type"] ?? "tcp";
                Flow = query["flow"];

                var security = query["security"];
                if (security == "tls" || security == "reality")
                {
                    Tls = true;
                    Servername = query["sni"];
                    ClientFingerprint = query["fp"];
                    var alpn = query["alpn"];
                    if (!string.IsNullOrEmpty(alpn))
                        Alpn = [.. Uri.UnescapeDataString(alpn).Split(',')];

                    if (security == "reality")
                    {
                        var pbk = query["pbk"];
                        var sid = query["sid"];
                        if (!string.IsNullOrEmpty(pbk) || !string.IsNullOrEmpty(sid))
                            RealityOpts = new RealityOpts { PublicKey = pbk, ShortId = sid };
                    }
                }

                if (Network == "ws")
                {
                    var path = query["path"];
                    var host = query["host"];
                    if (!string.IsNullOrEmpty(path) || !string.IsNullOrEmpty(host))
                    {
                        WsOpts = new WsOpts { Path = path != null ? Uri.UnescapeDataString(path) : null };
                        if (!string.IsNullOrEmpty(host))
                            WsOpts.Headers = new Dictionary<string, string> { ["Host"] = host };
                    }
                }
                else if (Network == "grpc")
                {
                    var serviceName = query["serviceName"];
                    if (!string.IsNullOrEmpty(serviceName))
                        GrpcOpts = new GrpcOpts { GrpcServiceName = serviceName };
                }

                return true;
            }
            catch { return false; }
        }

        public string ToUrl()
        {
            var queryParams = new List<string> { $"type={Network}" };

            if (!string.IsNullOrEmpty(Flow))
                queryParams.Add($"flow={Flow}");

            if (Tls == true)
            {
                queryParams.Add(RealityOpts != null ? "security=reality" : "security=tls");
                if (!string.IsNullOrEmpty(Servername))
                    queryParams.Add($"sni={Servername}");
                if (!string.IsNullOrEmpty(ClientFingerprint))
                    queryParams.Add($"fp={ClientFingerprint}");
                if (RealityOpts != null)
                {
                    if (!string.IsNullOrEmpty(RealityOpts.PublicKey))
                        queryParams.Add($"pbk={RealityOpts.PublicKey}");
                    if (!string.IsNullOrEmpty(RealityOpts.ShortId))
                        queryParams.Add($"sid={RealityOpts.ShortId}");
                }
            }

            if (WsOpts != null)
            {
                if (!string.IsNullOrEmpty(WsOpts.Path))
                    queryParams.Add($"path={Uri.EscapeDataString(WsOpts.Path)}");
                if (WsOpts.Headers?.TryGetValue("Host", out var host) == true)
                    queryParams.Add($"host={host}");
            }

            if (GrpcOpts?.GrpcServiceName != null)
                queryParams.Add($"serviceName={GrpcOpts.GrpcServiceName}");

            var query = string.Join("&", queryParams);
            return $"vless://{Uuid}@{Server}:{Port}?{query}#{Uri.EscapeDataString(Name)}";
        }
    }

    public class Trojan : ISupportedNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "trojan";
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Network { get; set; } = "tcp";
        public string? Servername { get; set; }
        public string? Sni { get; set; }
        public bool Udp { get; set; } = true;
        public List<string>? Alpn { get; set; }
        public string? ClientFingerprint { get; set; }
        public bool? SkipCertVerify { get; set; }
        public WsOpts? WsOpts { get; set; }
        public GrpcOpts? GrpcOpts { get; set; }

        public bool Parse(string url)
        {
            try
            {
                var uri = new Uri(url);
                var query = HttpUtility.ParseQueryString(uri.Query);

                Name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
                if (string.IsNullOrEmpty(Name)) Name = "trojan";
                Server = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 443;
                Password = uri.UserInfo;
                Sni = query["sni"];
                Servername = query["peer"] ?? query["sni"];
                ClientFingerprint = query["fp"];

                var allowInsecure = query["allowInsecure"];
                if (allowInsecure == "1" || allowInsecure?.ToLower() == "true")
                    SkipCertVerify = true;

                var alpn = query["alpn"];
                if (!string.IsNullOrEmpty(alpn))
                    Alpn = [.. Uri.UnescapeDataString(alpn).Split(',')];

                var network = query["type"];
                if (!string.IsNullOrEmpty(network) && network != "tcp")
                {
                    Network = network;
                    if (network == "ws")
                    {
                        var path = query["path"];
                        var host = query["host"];
                        if (!string.IsNullOrEmpty(path) || !string.IsNullOrEmpty(host))
                        {
                            WsOpts = new WsOpts { Path = path != null ? Uri.UnescapeDataString(path) : null };
                            if (!string.IsNullOrEmpty(host))
                                WsOpts.Headers = new Dictionary<string, string> { ["Host"] = host };
                        }
                    }
                    else if (network == "grpc")
                    {
                        var serviceName = query["serviceName"];
                        if (!string.IsNullOrEmpty(serviceName))
                            GrpcOpts = new GrpcOpts { GrpcServiceName = serviceName };
                    }
                }

                return true;
            }
            catch { return false; }
        }

        public string ToUrl()
        {
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(Sni))
                queryParams.Add($"sni={Sni}");

            if (Network != "tcp")
            {
                queryParams.Add($"type={Network}");
                if (WsOpts?.Path != null)
                    queryParams.Add($"path={Uri.EscapeDataString(WsOpts.Path)}");
                if (GrpcOpts?.GrpcServiceName != null)
                    queryParams.Add($"serviceName={GrpcOpts.GrpcServiceName}");
            }

            var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            return $"trojan://{Password}@{Server}:{Port}{query}#{Uri.EscapeDataString(Name)}";
        }
    }

    public class Shadowsocks : ISupportedNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "ss";
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Cipher { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool Udp { get; set; } = true;

        public bool Parse(string url)
        {
            try
            {
                var uri = new Uri(url);
                Name = Uri.UnescapeDataString(uri.Fragment.TrimStart('#'));
                if (string.IsNullOrEmpty(Name)) Name = "ss";
                Server = uri.Host;
                Port = uri.Port > 0 ? uri.Port : 443;

                if (uri.UserInfo.Contains(':'))
                {
                    var parts = uri.UserInfo.Split(':');
                    Cipher = parts[0];
                    Password = parts[1];
                }
                else
                {
                    var decoded = SupportedNetworkNodeParser.DecodeBase64(uri.UserInfo);
                    var colonIndex = decoded.IndexOf(':');
                    if (colonIndex == -1) return false;
                    Cipher = decoded[..colonIndex];
                    Password = decoded[(colonIndex + 1)..];
                }

                return true;
            }
            catch { return false; }
        }

        public string ToUrl()
        {
            var userInfo = SupportedNetworkNodeParser.EncodeBase64($"{Cipher}:{Password}");
            return $"ss://{userInfo}@{Server}:{Port}#{Uri.EscapeDataString(Name)}";
        }
    }

    public class ShadowsocksR : ISupportedNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "ssr";
        public string Server { get; set; } = string.Empty;
        public int Port { get; set; }
        public string Cipher { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Protocol { get; set; } = "origin";
        public string Obfs { get; set; } = "plain";
        public string? ProtocolParam { get; set; }
        public string? ObfsParam { get; set; }
        public bool Udp { get; set; } = true;

        public bool Parse(string url)
        {
            try
            {
                var base64Part = url[6..];
                var decoded = SupportedNetworkNodeParser.DecodeBase64(base64Part);

                var mainPart = decoded.Split('/')[0];
                var paramPart = decoded.Contains('/') ? decoded[(decoded.IndexOf('/') + 1)..] : "";

                var parts = mainPart.Split(':');
                if (parts.Length < 6) return false;

                Server = parts[0];
                Port = int.Parse(parts[1]);
                Protocol = parts[2];
                Cipher = parts[3];
                Obfs = parts[4];
                Password = SupportedNetworkNodeParser.DecodeBase64(parts[5]);

                var query = HttpUtility.ParseQueryString(paramPart.TrimStart('?'));
                var remarks = query["remarks"];
                Name = !string.IsNullOrEmpty(remarks) ? SupportedNetworkNodeParser.DecodeBase64(remarks) : "ssr";

                var obfsParam = query["obfsparam"];
                if (!string.IsNullOrEmpty(obfsParam))
                    ObfsParam = SupportedNetworkNodeParser.DecodeBase64(obfsParam);

                var protoParam = query["protoparam"];
                if (!string.IsNullOrEmpty(protoParam))
                    ProtocolParam = SupportedNetworkNodeParser.DecodeBase64(protoParam);

                return true;
            }
            catch { return false; }
        }

        public string ToUrl()
        {
            var password = SupportedNetworkNodeParser.EncodeBase64(Password);
            var remarks = SupportedNetworkNodeParser.EncodeBase64(Name);
            var main = $"{Server}:{Port}:{Protocol}:{Cipher}:{Obfs}:{password}/?remarks={remarks}";
            return "ssr://" + SupportedNetworkNodeParser.EncodeBase64(main);
        }
    }

    public class WsOpts
    {
        public string? Path { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

    public class GrpcOpts
    {
        public string? GrpcServiceName { get; set; }
    }

    public class RealityOpts
    {
        public string? PublicKey { get; set; }
        public string? ShortId { get; set; }
    }
}
