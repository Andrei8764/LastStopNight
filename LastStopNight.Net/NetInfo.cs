using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LastStopNight.Net;

public static class NetInfo
{
    /// <summary>
    /// Adresele retelelor virtuale (Tailscale, Radmin VPN, ZeroTier, Hamachi): cand prin internet nu merge
    /// direct, jucatorii intra in aceeasi retea virtuala si folosesc codul de aici.
    /// </summary>
    /// <summary>Pentru fiecare placa activa: adresa ei si adresa de broadcast a subretelei.</summary>
    public static List<(IPAddress Local, IPAddress Broadcast)> BroadcastTargets()
    {
        var list = new List<(IPAddress, IPAddress)>();
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback))
            foreach (var a in n.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
            {
                if (a.IPv4Mask is null || a.IPv4Mask.Equals(IPAddress.Any)) continue;
                var ip = a.Address.GetAddressBytes();
                var mask = a.IPv4Mask.GetAddressBytes();
                var b = new byte[4];
                for (int i = 0; i < 4; i++) b[i] = (byte)(ip[i] | ~mask[i]);
                list.Add((a.Address, new IPAddress(b)));
            }
        return list;
    }

    /// <summary>
    /// Cat de buna e o adresa pentru conectare: reteaua de acasa 192.168.x (3) &gt; alte retele private,
    /// inclusiv placile virtuale WSL / Hyper-V (2) &gt; Tailscale / Radmin / Hamachi (1) &gt; localhost (0).
    /// </summary>
    public static int Rank(IPAddress ip)
    {
        if (IPAddress.IsLoopback(ip)) return 0;
        var b = ip.GetAddressBytes();
        if ((b[0] == 100 && b[1] is >= 64 and <= 127) || b[0] is 25 or 26) return 1;
        return b[0] == 192 && b[1] == 168 ? 3 : 2;
    }

    public static List<(string Label, IPAddress Address)> VirtualNetworks()
    {
        var list = new List<(string, IPAddress)>();
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up))
            foreach (var a in n.GetIPProperties().UnicastAddresses.Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork))
            {
                var b = a.Address.GetAddressBytes();
                string d = n.Description + " " + n.Name;
                string? label = d.Contains("Tailscale", StringComparison.OrdinalIgnoreCase) || (b[0] == 100 && b[1] is >= 64 and <= 127) ? "Tailscale"
                    : d.Contains("Radmin", StringComparison.OrdinalIgnoreCase) || b[0] == 26 ? "Radmin VPN"
                    : d.Contains("ZeroTier", StringComparison.OrdinalIgnoreCase) ? "ZeroTier"
                    : d.Contains("Hamachi", StringComparison.OrdinalIgnoreCase) || b[0] == 25 ? "Hamachi"
                    : null;
                if (label is not null) list.Add((label, a.Address));
            }
        return list;
    }

    /// <summary>IP-ul din reteaua locala (placa activa cu gateway, nu loopback / virtuala daca se poate).</summary>
    public static IPAddress LocalIPv4()
    {
        var candidates = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .Select(n => (n, p: n.GetIPProperties()))
            .SelectMany(x => x.p.UnicastAddresses
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => (a.Address, HasGateway: x.p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork
                                                                                  && !g.Address.Equals(IPAddress.Any)),
                              Virtual: x.n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase)
                                       || x.n.Description.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase)
                                       || x.n.Description.Contains("VPN", StringComparison.OrdinalIgnoreCase)
                                       || x.n.Description.Contains("Tailscale", StringComparison.OrdinalIgnoreCase)
                                       || x.n.Description.Contains("ZeroTier", StringComparison.OrdinalIgnoreCase)
                                       || x.n.Description.Contains("Hamachi", StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(c => c.HasGateway).ThenBy(c => c.Virtual)
            .ToList();
        return candidates.FirstOrDefault().Address ?? IPAddress.Loopback;
    }
}
