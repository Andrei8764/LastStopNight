using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace LastStopNight.Net;

/// <summary>
/// O legatura TCP cu alt jucator. Citirea si scrierea merg pe fire separate; mesajele primite
/// asteapta in <see cref="Inbox"/> pana le proceseaza jocul (pe firul interfetei, in Pump),
/// deci logica de joc nu ruleaza niciodata in paralel.
/// </summary>
public sealed class Connection : IDisposable
{
    private readonly TcpClient _tcp;
    private readonly Channel<string> _outbox = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
    private readonly CancellationTokenSource _cts = new();
    private volatile bool _closed;

    public Connection(TcpClient tcp)
    {
        _tcp = tcp;
        _tcp.NoDelay = true; // mesaje mici si dese: fara intarzierea lui Nagle
        Remote = tcp.Client.RemoteEndPoint?.ToString() ?? "?";
    }

    public string Remote { get; }
    public ConcurrentQueue<NetMessage> Inbox { get; } = new();
    public bool IsOpen => !_closed;
    public string? CloseReason { get; private set; }
    /// <summary>ID-ul jucatorului din lobby (0 = inca nu s-a prezentat).</summary>
    public int PlayerId { get; set; }

    /// <summary>Cand am primit ultima data ceva (orice linie, inclusiv semnalul de viata).</summary>
    public DateTime LastReceived { get; private set; } = DateTime.UtcNow;

    public void Start()
    {
        _ = Task.Run(ReadLoop);
        _ = Task.Run(WriteLoop);
        _ = Task.Run(HeartbeatLoop);
    }

    /// <summary>
    /// Semnal de viata: o linie goala la fiecare 2 secunde, de pe un fir separat (merge si cand jocul
    /// e ocupat). Fara el, un Wi-Fi cazut sau un PC adormit n-ar fi observat ani de zile de TCP.
    /// </summary>
    private async Task HeartbeatLoop()
    {
        try
        {
            while (!_cts.IsCancellationRequested && !_closed)
            {
                await Task.Delay(2000, _cts.Token);
                _outbox.Writer.TryWrite("");
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Send(NetMessage m)
    {
        if (!_closed) _outbox.Writer.TryWrite(NetJson.Serialize(m));
    }

    private async Task ReadLoop()
    {
        try
        {
            using var reader = new StreamReader(_tcp.GetStream(), new UTF8Encoding(false), false, 64 * 1024, leaveOpen: true);
            while (!_cts.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(_cts.Token);
                if (line is null) { Close("Celălalt jucător a închis conexiunea."); return; }
                LastReceived = DateTime.UtcNow;
                if (line.Length == 0) continue; // semnal de viata
                NetMessage? msg;
                try { msg = NetJson.Deserialize(line); }
                catch (System.Text.Json.JsonException) { Close("Mesaj de neînțeles (altă versiune a jocului?)."); return; }
                if (msg is not null) Inbox.Enqueue(msg);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Close("Conexiunea s-a întrerupt.");
        }
    }

    private async Task WriteLoop()
    {
        try
        {
            var stream = _tcp.GetStream();
            await foreach (var line in _outbox.Reader.ReadAllAsync(_cts.Token))
            {
                var bytes = Encoding.UTF8.GetBytes(line + "\n");
                await stream.WriteAsync(bytes, _cts.Token);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException or OperationCanceledException)
        {
            Close("Conexiunea s-a întrerupt.");
        }
    }

    /// <summary>Trimite ce mai e in coada (ex. „Bye”), apoi inchide.</summary>
    public void Close(string reason)
    {
        if (_closed) return;
        CloseReason = reason;
        _closed = true;
        _outbox.Writer.TryComplete();
        _ = Task.Run(async () =>
        {
            await Task.Delay(150); // lasa scrierea ultimelor mesaje
            _cts.Cancel();
            _tcp.Close();
        });
    }

    public void Dispose() => Close("Închis.");
}
