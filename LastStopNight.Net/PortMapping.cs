using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Linq;

namespace LastStopNight.Net;

/// <summary>
/// UPnP: cere routerului sa deschida portul gazdei spre internet (daca routerul permite).
/// Fara biblioteci externe: cautare SSDP, descrierea routerului (XML) si o cerere SOAP AddPortMapping.
/// </summary>
public sealed class PortMapping : IDisposable
{
    private string? _controlUrl, _serviceType;
    private int _port;

    public IPAddress? ExternalIp { get; private set; }
    /// <summary>IP-ul extern e public (nu CGNAT): din internet se poate intra direct.</summary>
    public bool IsPublic => ExternalIp is not null && !IsPrivate(ExternalIp);
    public bool Mapped { get; private set; }
    public string Message { get; private set; } = "";

    public static Task<PortMapping> TryOpenAsync(int port, TimeSpan timeout) => RunAsync(port, timeout, map: true);

    /// <summary>Doar citeste: exista router cu UPnP si ce IP public are? Nu deschide niciun port.</summary>
    public static Task<PortMapping> ProbeAsync(TimeSpan timeout) => RunAsync(0, timeout, map: false);

    /// <summary>Routerul raspunde la UPnP si permite deschiderea porturilor.</summary>
    public bool Supported { get; private set; }

    private static async Task<PortMapping> RunAsync(int port, TimeSpan timeout, bool map)
    {
        var pm = new PortMapping { _port = port };
        try { await pm.OpenAsync(timeout, map); }
        catch (OperationCanceledException)
        {
            pm.Message = pm.Supported ? "Routerul a răspuns prea încet la UPnP." : "Nu am găsit un router cu UPnP (e oprit sau blocat).";
        }
        catch (Exception ex) when (ex is SocketException or HttpRequestException or System.Xml.XmlException
                                   or InvalidOperationException or IOException or UriFormatException or NullReferenceException)
        {
            pm.Message = "Routerul nu a răspuns corect la UPnP.";
        }
        return pm;
    }

    private async Task OpenAsync(TimeSpan timeout, bool map)
    {
        using var cts = new CancellationTokenSource(timeout);
        var location = await DiscoverAsync(cts.Token);
        if (location is null) { Message = "Nu am găsit un router cu UPnP."; return; }

        using var http = new HttpClient { Timeout = timeout };
        var xml = XDocument.Parse(await http.GetStringAsync(location, cts.Token));
        XNamespace ns = xml.Root!.Name.Namespace;
        var service = xml.Descendants(ns + "service").FirstOrDefault(s =>
            (s.Element(ns + "serviceType")?.Value ?? "").Contains("WANIPConnection") ||
            (s.Element(ns + "serviceType")?.Value ?? "").Contains("WANPPPConnection"));
        if (service is null) { Message = "Routerul nu permite deschiderea porturilor."; return; }
        _serviceType = service.Element(ns + "serviceType")!.Value;
        _controlUrl = new Uri(location, service.Element(ns + "controlURL")!.Value).ToString();

        Supported = true;
        var local = NetInfo.LocalIPv4();
        if (map)
            await SoapAsync(http, "AddPortMapping",
            $"<NewRemoteHost></NewRemoteHost><NewExternalPort>{_port}</NewExternalPort><NewProtocol>TCP</NewProtocol>" +
            $"<NewInternalPort>{_port}</NewInternalPort><NewInternalClient>{local}</NewInternalClient><NewEnabled>1</NewEnabled>" +
            $"<NewPortMappingDescription>Last Stop</NewPortMappingDescription><NewLeaseDuration>0</NewLeaseDuration>", cts.Token);
        Mapped = map;
        var ipResp = await SoapAsync(http, "GetExternalIPAddress", "", cts.Token);
        var ipText = XDocument.Parse(ipResp).Descendants().FirstOrDefault(e => e.Name.LocalName == "NewExternalIPAddress")?.Value;
        if (IPAddress.TryParse(ipText, out var ext)) ExternalIp = ext;
        if (!map) { Message = ExternalIp is null ? "Router cu UPnP, fără IP public raportat." : IsPublic ? "Router cu UPnP și IP public." : "Router cu UPnP, dar IP privat (CGNAT)."; return; }
        Message = ExternalIp is null ? "Portul e deschis, dar routerul nu spune IP-ul public."
            : IsPrivate(ExternalIp) ? "Routerul are tot un IP privat (CGNAT): din internet probabil nu se poate intra direct."
            : $"Portul {_port} e deschis pe router.";
    }

    private static bool IsPrivate(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return b[0] == 10 || (b[0] == 172 && b[1] is >= 16 and <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 100 && b[1] is >= 64 and <= 127);
    }

    private static async Task<Uri?> DiscoverAsync(CancellationToken ct)
    {
        // pe placa retelei locale: altfel, cu Tailscale / VPN, cautarea poate pleca pe reteaua virtuala
        using var udp = new UdpClient(new IPEndPoint(NetInfo.LocalIPv4(), 0));
        var req = Encoding.ASCII.GetBytes(
            "M-SEARCH * HTTP/1.1\r\nHOST: 239.255.255.250:1900\r\nST: urn:schemas-upnp-org:device:InternetGatewayDevice:1\r\n" +
            "MAN: \"ssdp:discover\"\r\nMX: 2\r\n\r\n");
        var target = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);
        await udp.SendAsync(req, target, ct);
        await udp.SendAsync(req, target, ct);
        while (!ct.IsCancellationRequested)
        {
            var r = await udp.ReceiveAsync(ct);
            var text = Encoding.ASCII.GetString(r.Buffer);
            var loc = text.Split("\r\n").FirstOrDefault(l => l.StartsWith("LOCATION:", StringComparison.OrdinalIgnoreCase));
            if (loc is not null && Uri.TryCreate(loc[9..].Trim(), UriKind.Absolute, out var uri)) return uri;
        }
        return null;
    }

    private async Task<string> SoapAsync(HttpClient http, string action, string args, CancellationToken ct)
    {
        var body = "<?xml version=\"1.0\"?><s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" " +
                   "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\"><s:Body>" +
                   $"<u:{action} xmlns:u=\"{_serviceType}\">{args}</u:{action}></s:Body></s:Envelope>";
        using var content = new StringContent(body, Encoding.UTF8, "text/xml");
        content.Headers.Add("SOAPACTION", $"\"{_serviceType}#{action}\"");
        using var resp = await http.PostAsync(_controlUrl, content, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) throw new InvalidOperationException($"UPnP {action}: {(int)resp.StatusCode}");
        return text;
    }

    /// <summary>Inchide portul la iesire (pe fundal, fara sa blocheze inchiderea jocului).</summary>
    public void Dispose()
    {
        if (!Mapped || _controlUrl is null) return;
        Mapped = false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            SoapAsync(http, "DeletePortMapping",
                $"<NewRemoteHost></NewRemoteHost><NewExternalPort>{_port}</NewExternalPort><NewProtocol>TCP</NewProtocol>",
                CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
        }
        catch (Exception ex) when (ex is AggregateException or HttpRequestException or InvalidOperationException) { }
    }
}
