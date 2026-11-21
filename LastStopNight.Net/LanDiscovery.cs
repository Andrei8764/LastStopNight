using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace LastStopNight.Net;

/// <summary>Un joc gazduit in reteaua locala, anuntat prin UDP.</summary>
public sealed record LanGame(string Host, string Story, int Players, int Max, IPEndPoint EndPoint, DateTime Seen);

/// <summary>
/// Descoperirea in reteaua locala: gazda anunta jocul pe UDP o data pe secunda, iar cine cauta asculta.
/// Asa, pe acelasi Wi-Fi nu e nevoie de niciun cod.
/// </summary>
public sealed class LanDiscovery : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, LanGame> _games = new();
    private UdpClient? _udp;

    public IReadOnlyList<LanGame> Games =>
        _games.Values.Where(g => DateTime.UtcNow - g.Seen < TimeSpan.FromSeconds(4)).OrderBy(g => g.Host).ToList();

    /// <summary>Gazda: anunta periodic (informatiile se citesc din nou la fiecare anunt).</summary>
    /// <param name="loopbackOnly">Doar pe acest PC (teste), fara broadcast in retea.</param>
    public void StartAnnouncing(Func<(string Host, string Story, int Players, int Max, int Port)> info, bool loopbackOnly = false)
    {
        _udp = new UdpClient { EnableBroadcast = true };
        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var i = info();
                    var json = JsonSerializer.SerializeToUtf8Bytes(new { game = "LastStop", v = NetJson.ProtocolVersion, i.Host, i.Story, i.Players, i.Max, i.Port });
                    if (!loopbackOnly)
                    {
                        // pe fiecare placa de retea separat (Wi-Fi, cablu, Tailscale...): un singur broadcast
                        // pleaca doar pe placa preferata de Windows, care poate fi chiar cea virtuala
                        foreach (var (local, bcast) in NetInfo.BroadcastTargets())
                        {
                            try
                            {
                                using var u = new UdpClient(new IPEndPoint(local, 0)) { EnableBroadcast = true };
                                await u.SendAsync(json, new IPEndPoint(bcast, NetJson.DiscoveryPort), _cts.Token);
                            }
                            catch (SocketException) { } // placa fara broadcast: trecem la urmatoarea
                        }
                        await _udp.SendAsync(json, new IPEndPoint(IPAddress.Broadcast, NetJson.DiscoveryPort), _cts.Token);
                    }
                    await _udp.SendAsync(json, new IPEndPoint(IPAddress.Loopback, NetJson.DiscoveryPort), _cts.Token);
                }
                catch (Exception ex) when (ex is SocketException or ObjectDisposedException) { }
                catch (OperationCanceledException) { return; }
                try { await Task.Delay(1000, _cts.Token); } catch (OperationCanceledException) { return; }
            }
        });
    }

    /// <summary>Cine cauta: asculta anunturile (pot asculta mai multe ferestre pe acelasi PC).</summary>
    public void StartListening()
    {
        try
        {
            _udp = new UdpClient { ExclusiveAddressUse = false, EnableBroadcast = true };
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(new IPEndPoint(IPAddress.Any, NetJson.DiscoveryPort));
        }
        catch (SocketException) { return; } // portul e luat: lista de jocuri ramane goala, codul merge in continuare
        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var r = await _udp.ReceiveAsync(_cts.Token);
                    using var doc = JsonDocument.Parse(r.Buffer);
                    var root = doc.RootElement;
                    if (root.GetProperty("game").GetString() != "LastStop" || root.GetProperty("v").GetInt32() != NetJson.ProtocolVersion) continue;
                    var ep = new IPEndPoint(r.RemoteEndPoint.Address, root.GetProperty("Port").GetInt32());
                    // acelasi joc anuntat si pe loopback, si pe broadcast: pastram adresa din retea daca exista
                    var key = root.GetProperty("Host").GetString() + "|" + ep.Port;
                    // acelasi joc primit pe mai multe cai: pastram cea mai buna adresa (retea locala > Tailscale > localhost)
                    if (_games.TryGetValue(key, out var old) && DateTime.UtcNow - old.Seen < TimeSpan.FromSeconds(3)
                        && NetInfo.Rank(old.EndPoint.Address) > NetInfo.Rank(ep.Address))
                        ep = old.EndPoint;
                    _games[key] = new LanGame(root.GetProperty("Host").GetString() ?? "?", root.GetProperty("Story").GetString() ?? "",
                        root.GetProperty("Players").GetInt32(), root.GetProperty("Max").GetInt32(), ep, DateTime.UtcNow);
                }
                catch (OperationCanceledException) { return; }
                catch (Exception ex) when (ex is SocketException or JsonException or KeyNotFoundException or InvalidOperationException or ObjectDisposedException) { }
            }
        });
    }

    public void Dispose()
    {
        _cts.Cancel();
        _udp?.Dispose();
    }
}
