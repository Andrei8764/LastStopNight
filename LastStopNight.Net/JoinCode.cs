using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LastStopNight.Net;

/// <summary>
/// Codul de intrare: IP-ul (IPv4) si portul gazdei, in 10 caractere usor de dictat sau de lipit pe Discord
/// (alfabet Crockford, fara I, L, O, U, ca sa nu se confunde cifrele cu literele). Ex. „4K7QM-2XD9A”.
/// </summary>
public static class JoinCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    public static string Encode(IPAddress ip, int port)
    {
        var b = ip.MapToIPv4().GetAddressBytes();
        ulong v = ((ulong)b[0] << 40) | ((ulong)b[1] << 32) | ((ulong)b[2] << 24) | ((ulong)b[3] << 16) | (uint)(port & 0xFFFF);
        var sb = new StringBuilder();
        for (int i = 0; i < 10; i++) { sb.Insert(0, Alphabet[(int)(v & 31)]); v >>= 5; }
        return sb.ToString(0, 5) + "-" + sb.ToString(5, 5);
    }

    /// <summary>Accepta un cod, sau direct „ip:port” / „ip” (portul implicit).</summary>
    public static bool TryParse(string text, out IPEndPoint endpoint)
    {
        endpoint = null!;
        text = text.Trim();
        if (IPEndPoint.TryParse(text, out var ep) && ep.Address.AddressFamily == AddressFamily.InterNetwork)
        {
            endpoint = new IPEndPoint(ep.Address, ep.Port == 0 ? NetJson.DefaultPort : ep.Port);
            return true;
        }
        var clean = text.ToUpperInvariant().Replace("-", "").Replace(" ", "")
            .Replace('O', '0').Replace('I', '1').Replace('L', '1');
        if (clean.Length != 10) return false;
        ulong v = 0;
        foreach (char c in clean)
        {
            int d = Alphabet.IndexOf(c);
            if (d < 0) return false;
            v = (v << 5) | (uint)d;
        }
        var bytes = new[] { (byte)(v >> 40), (byte)(v >> 32), (byte)(v >> 24), (byte)(v >> 16) };
        int port = (int)(v & 0xFFFF);
        if (port == 0) return false;
        endpoint = new IPEndPoint(new IPAddress(bytes), port);
        return true;
    }
}
