using System.Text;
using System.Text.Json.Nodes;
using System.Web;
using YamlDotNet.Serialization;

namespace V2SubsCombinator.Models
{
    public static class SupportedNetworkNodeHelper
    {
        public const string Vmess = "vmess";
        public const string Vless = "vless";
        public const string Trojan = "trojan";
        public const string Shadowsocks = "ss";
        public const string ShadowsocksR = "ssr";

        public static bool TryGetNodeType(string url, out string nodeType)
        {
            url = url.Trim();
            if (url.StartsWith("vmess://")) { nodeType = Vmess; return true; }
            if (url.StartsWith("vless://")) { nodeType = Vless; return true; }
            if (url.StartsWith("trojan://")) { nodeType = Trojan; return true; }
            if (url.StartsWith("ss://")) { nodeType = Shadowsocks; return true; }
            if (url.StartsWith("ssr://")) { nodeType = ShadowsocksR; return true; }
            nodeType = string.Empty;
            return false;
        }

        public static string DecodeBase64(string content)
        {
            try
            {
                var base64 = content.Trim().Replace('-', '+').Replace('_', '/');
                var padding = base64.Length % 4;
                if (padding > 0) base64 += new string('=', 4 - padding);
                return Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            }
            catch { return content; }
        }

        public static string EncodeBase64(string content)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        }
    }

    public class SupportedNode
    {
        public SupportedNode() { }

        public SupportedNode(string url, string remarkPrefix = "")
        {
            _ = ParseFromUrl(url, remarkPrefix);
        }

        private bool ParseFromUrl(string url, string remarkPrefix = "")
        {
            url = url.Trim();
            if (!SupportedNetworkNodeHelper.TryGetNodeType(url, out var nodeType))
                return false;

            Type = nodeType;
            return nodeType switch
            {
                SupportedNetworkNodeHelper.Vmess => ParseVmess(url, remarkPrefix),
                SupportedNetworkNodeHelper.Vless => ParseVless(url, remarkPrefix),
                SupportedNetworkNodeHelper.Trojan => ParseTrojan(url, remarkPrefix),
                SupportedNetworkNodeHelper.Shadowsocks => ParseShadowsocks(url, remarkPrefix),
                SupportedNetworkNodeHelper.ShadowsocksR => ParseShadowsocksR(url, remarkPrefix),
                _ => false
            };
        }

        public string? ToUrl()
        {
            if (string.IsNullOrEmpty(Server) || Port == null) return null;
            return Type switch
            {
                SupportedNetworkNodeHelper.Vmess => ToVmessUrl(),
                SupportedNetworkNodeHelper.Vless => ToVlessUrl(),
                SupportedNetworkNodeHelper.Trojan => ToTrojanUrl(),
                SupportedNetworkNodeHelper.Shadowsocks => ToShadowsocksUrl(),
                SupportedNetworkNodeHelper.ShadowsocksR => ToShadowsocksRUrl(),
                _ => null
            };
        }

        #region ParseFromUrl Methods
        private bool ParseVmess(string url, string remarkPrefix)
        {
            try
            {
                var decoded = SupportedNetworkNodeHelper.DecodeBase64(url[8..]);
                var json = JsonNode.Parse(decoded);
                if (json == null) return false;

                Name = remarkPrefix + (json["ps"]?.ToString() ?? "vmess");
                Server = json["add"]?.ToString();
                Port = int.TryParse(json["port"]?.ToString(), out var p) ? p : null;
                Uuid = json["id"]?.ToString();
                AlterId = int.TryParse(json["aid"]?.ToString(), out var aid) ? aid : 0;
                Network = json["net"]?.ToString() ?? "tcp";
                Cipher = json["scy"]?.ToString() ?? "auto";

                var tls = json["tls"]?.ToString();
                Tls = !string.IsNullOrEmpty(tls) && tls != "none";
                Sni = json["sni"]?.ToString();
                if (!string.IsNullOrEmpty(json["host"]?.ToString()))
                    Servername = json["host"]?.ToString();
                Udp = true;
                SkipCertVerify = false;

                if (Network == "ws")
                {
                    WsOpts = new WsOpts
                    {
                        Path = json["path"]?.ToString(),
                        Headers = !string.IsNullOrEmpty(Servername) ? new Dictionary<string, string> { ["Host"] = Servername } : null
                    };
                }
                else if (Network == "grpc")
                {
                    GrpcOpts = new GrpcOpts { GrpcServiceName = json["path"]?.ToString() };
                }
                return true;
            }
            catch { return false; }
        }

        private bool ParseVless(string url, string remarkPrefix)
        {
            try
            {
                var uri = new Uri(url);
                Uuid = uri.UserInfo;
                Server = uri.Host;
                Port = uri.Port;
                Name = remarkPrefix + (string.IsNullOrEmpty(uri.Fragment) ? "vless" : Uri.UnescapeDataString(uri.Fragment[1..]));

                var query = HttpUtility.ParseQueryString(uri.Query);
                Network = query["type"] ?? "tcp";
                Flow = query["flow"];
                Sni = query["sni"];
                Servername = query["host"];
                ClientFingerprint = query["fp"];

                var security = query["security"];
                Tls = security == "tls" || security == "reality";

                if (security == "reality")
                {
                    RealityOpts = new RealityOpts { PublicKey = query["pbk"], ShortId = query["sid"] };
                }

                var alpn = query["alpn"];
                if (!string.IsNullOrEmpty(alpn)) Alpn = [.. alpn.Split(',')];

                if (Network == "ws")
                {
                    WsOpts = new WsOpts
                    {
                        Path = query["path"],
                        Headers = !string.IsNullOrEmpty(Servername) ? new Dictionary<string, string> { ["Host"] = Servername } : null
                    };
                }
                else if (Network == "grpc")
                {
                    GrpcOpts = new GrpcOpts { GrpcServiceName = query["serviceName"] };
                }
                return true;
            }
            catch { return false; }
        }

        private bool ParseTrojan(string url, string remarkPrefix)
        {
            try
            {
                var uri = new Uri(url);
                Password = Uri.UnescapeDataString(uri.UserInfo);
                Server = uri.Host;
                Port = uri.Port;
                Name = remarkPrefix + (string.IsNullOrEmpty(uri.Fragment) ? "trojan" : Uri.UnescapeDataString(uri.Fragment[1..]));

                var query = HttpUtility.ParseQueryString(uri.Query);
                Network = query["type"] ?? "tcp";
                Sni = query["sni"];
                Udp = true;
                if (!string.IsNullOrEmpty(query["host"]))
                {
                    Servername = query["host"];
                }
                else
                {
                    Servername = query["peer"];
                }
                ClientFingerprint = query["fp"];

                var security = query["security"];
                Tls = string.IsNullOrEmpty(security) || security == "tls" || security == "reality";

                SkipCertVerify = query["allowInsecure"] == "1" || query["insecure"] == "1";

                if (security == "reality")
                {
                    RealityOpts = new RealityOpts { PublicKey = query["pbk"], ShortId = query["sid"] };
                }

                var alpn = query["alpn"];
                if (!string.IsNullOrEmpty(alpn)) Alpn = [.. alpn.Split(',')];

                if (Network == "ws")
                {
                    WsOpts = new WsOpts
                    {
                        Path = query["path"],
                        Headers = !string.IsNullOrEmpty(Servername) ? new Dictionary<string, string> { ["Host"] = Servername } : null
                    };
                }
                else if (Network == "grpc")
                {
                    GrpcOpts = new GrpcOpts { GrpcServiceName = query["serviceName"] };
                }
                return true;
            }
            catch { return false; }
        }

        private bool ParseShadowsocks(string url, string remarkPrefix)
        {
            try
            {
                var content = url[5..];
                var fragmentIndex = content.IndexOf('#');
                string main, remark = "ss";

                if (fragmentIndex != -1)
                {
                    main = content[..fragmentIndex];
                    remark = Uri.UnescapeDataString(content[(fragmentIndex + 1)..]);
                }
                else
                {
                    main = content;
                }

                Name = remarkPrefix + remark;

                var decoded = SupportedNetworkNodeHelper.DecodeBase64(main);
                var atIndex = decoded.LastIndexOf('@');

                if (atIndex != -1)
                {
                    var userInfo = decoded[..atIndex];
                    var hostPort = decoded[(atIndex + 1)..];

                    var colonIndex = userInfo.IndexOf(':');
                    if (colonIndex == -1) return false;
                    Cipher = userInfo[..colonIndex];
                    Password = userInfo[(colonIndex + 1)..];

                    var lastColon = hostPort.LastIndexOf(':');
                    if (lastColon == -1) return false;
                    Server = hostPort[..lastColon];
                    Port = int.TryParse(hostPort[(lastColon + 1)..], out var p) ? p : null;
                }
                else
                {
                    var uri = new Uri(url);
                    Server = uri.Host;
                    Port = uri.Port;

                    var userDecoded = uri.UserInfo.Contains(':')
                        ? Uri.UnescapeDataString(uri.UserInfo)
                        : SupportedNetworkNodeHelper.DecodeBase64(uri.UserInfo);

                    var colonIndex = userDecoded.IndexOf(':');
                    if (colonIndex == -1) return false;
                    Cipher = userDecoded[..colonIndex];
                    Password = userDecoded[(colonIndex + 1)..];
                }

                return true;
            }
            catch { return false; }
        }

        private bool ParseShadowsocksR(string url, string remarkPrefix)
        {
            try
            {
                var decoded = SupportedNetworkNodeHelper.DecodeBase64(url[6..]);
                var parts = decoded.Split('/');
                var mainPart = parts[0];
                var paramPart = parts.Length > 1 ? parts[1] : "";

                var mainParts = mainPart.Split(':');
                if (mainParts.Length < 6) return false;

                Server = mainParts[0];
                Port = int.TryParse(mainParts[1], out var p) ? p : null;
                Protocol = mainParts[2];
                Cipher = mainParts[3];
                Obfs = mainParts[4];
                Password = SupportedNetworkNodeHelper.DecodeBase64(mainParts[5]);

                var query = HttpUtility.ParseQueryString(paramPart.TrimStart('?'));
                var remarks = query["remarks"];
                Name = remarkPrefix + (!string.IsNullOrEmpty(remarks) ? SupportedNetworkNodeHelper.DecodeBase64(remarks) : "ssr");

                var obfsParam = query["obfsparam"];
                if (!string.IsNullOrEmpty(obfsParam)) ObfsParam = SupportedNetworkNodeHelper.DecodeBase64(obfsParam);

                var protoParam = query["protoparam"];
                if (!string.IsNullOrEmpty(protoParam)) ProtocolParam = SupportedNetworkNodeHelper.DecodeBase64(protoParam);

                return true;
            }
            catch { return false; }
        }
        #endregion

        #region ToUrl Methods
        private string ToVmessUrl()
        {
            var json = new JsonObject
            {
                ["v"] = "2",
                ["ps"] = Name,
                ["add"] = Server,
                ["port"] = Port,
                ["id"] = Uuid,
                ["aid"] = AlterId ?? 0,
                ["scy"] = Cipher ?? "auto",
                ["net"] = Network ?? "tcp",
                ["tls"] = Tls == true ? "tls" : ""
            };
            if (!string.IsNullOrEmpty(Sni)) json["sni"] = Sni;
            if (WsOpts != null)
            {
                json["path"] = WsOpts.Path;
                if (WsOpts.Headers?.TryGetValue("Host", out var host) == true) json["host"] = host;
            }
            if (GrpcOpts != null) json["path"] = GrpcOpts.GrpcServiceName;
            return "vmess://" + SupportedNetworkNodeHelper.EncodeBase64(json.ToJsonString());
        }

        private string ToVlessUrl()
        {
            var query = new List<string> { $"type={Network ?? "tcp"}" };
            if (!string.IsNullOrEmpty(Flow)) query.Add($"flow={Flow}");
            if (!string.IsNullOrEmpty(Sni)) query.Add($"sni={Sni}");
            if (!string.IsNullOrEmpty(ClientFingerprint)) query.Add($"fp={ClientFingerprint}");
            if (Alpn?.Count > 0) query.Add($"alpn={string.Join(",", Alpn)}");

            if (RealityOpts != null)
            {
                query.Add("security=reality");
                if (!string.IsNullOrEmpty(RealityOpts.PublicKey)) query.Add($"pbk={RealityOpts.PublicKey}");
                if (!string.IsNullOrEmpty(RealityOpts.ShortId)) query.Add($"sid={RealityOpts.ShortId}");
            }
            else if (Tls == true)
            {
                query.Add("security=tls");
            }

            if (WsOpts != null)
            {
                if (!string.IsNullOrEmpty(WsOpts.Path)) query.Add($"path={Uri.EscapeDataString(WsOpts.Path)}");
                if (WsOpts.Headers?.TryGetValue("Host", out var host) == true) query.Add($"host={host}");
            }
            if (GrpcOpts != null && !string.IsNullOrEmpty(GrpcOpts.GrpcServiceName))
            {
                query.Add($"serviceName={GrpcOpts.GrpcServiceName}");
            }

            return $"vless://{Uuid}@{Server}:{Port}?{string.Join("&", query)}#{Uri.EscapeDataString(Name)}";
        }

        private string ToTrojanUrl()
        {
            var query = new List<string> { $"type={Network ?? "tcp"}" };
            if (!string.IsNullOrEmpty(Sni)) query.Add($"sni={Sni}");
            if (!string.IsNullOrEmpty(ClientFingerprint)) query.Add($"fp={ClientFingerprint}");
            if (Alpn?.Count > 0) query.Add($"alpn={string.Join(",", Alpn)}");

            if (RealityOpts != null)
            {
                query.Add("security=reality");
                if (!string.IsNullOrEmpty(RealityOpts.PublicKey)) query.Add($"pbk={RealityOpts.PublicKey}");
                if (!string.IsNullOrEmpty(RealityOpts.ShortId)) query.Add($"sid={RealityOpts.ShortId}");
            }
            else if (Tls == true)
            {
                query.Add("security=tls");
            }

            if (WsOpts != null)
            {
                if (!string.IsNullOrEmpty(WsOpts.Path)) query.Add($"path={Uri.EscapeDataString(WsOpts.Path)}");
                if (WsOpts.Headers?.TryGetValue("Host", out var host) == true) query.Add($"host={host}");
            }
            if (GrpcOpts != null && !string.IsNullOrEmpty(GrpcOpts.GrpcServiceName))
            {
                query.Add($"serviceName={GrpcOpts.GrpcServiceName}");
            }

            return $"trojan://{Uri.EscapeDataString(Password!)}@{Server}:{Port}?{string.Join("&", query)}#{Uri.EscapeDataString(Name)}";
        }

        private string ToShadowsocksUrl()
        {
            var userInfo = SupportedNetworkNodeHelper.EncodeBase64($"{Cipher}:{Password}");
            return $"ss://{userInfo}@{Server}:{Port}#{Uri.EscapeDataString(Name)}";
        }

        private string ToShadowsocksRUrl()
        {
            var password = SupportedNetworkNodeHelper.EncodeBase64(Password ?? "");
            var remarks = SupportedNetworkNodeHelper.EncodeBase64(Name);
            var main = $"{Server}:{Port}:{Protocol}:{Cipher}:{Obfs}:{password}/?remarks={remarks}";
            return "ssr://" + SupportedNetworkNodeHelper.EncodeBase64(main);
        }
        #endregion

        #region Properties
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Server { get; set; }
        public int? Port { get; set; }
        public string? Password { get; set; }
        public string? Protocol { get; set; }
        public string? Uuid { get; set; }
        [YamlMember(Alias = "alterId", ApplyNamingConventions = false)]
        public int? AlterId { get; set; }
        public string? Cipher { get; set; }
        public string? Network { get; set; }
        public string? Flow { get; set; }
        public bool? Tls { get; set; }
        public string? Servername { get; set; }
        public string? Sni { get; set; }
        public List<string>? Alpn { get; set; }
        public string? ClientFingerprint { get; set; }
        public bool? SkipCertVerify { get; set; }
        public bool? Udp { get; set; }
        public string? Obfs { get; set; }
        public string? ProtocolParam { get; set; }
        public string? ObfsParam { get; set; }
        public RealityOpts? RealityOpts { get; set; }
        public WsOpts? WsOpts { get; set; }
        public GrpcOpts? GrpcOpts { get; set; }
        #endregion
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
