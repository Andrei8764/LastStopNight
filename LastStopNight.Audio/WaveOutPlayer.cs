using System.Runtime.InteropServices;

namespace LastStopNight.Audio;

/// <summary>
/// Trimite ce scoate mixerul la placa de sunet prin winmm (waveOut), API-ul audio cel mai simplu din Windows:
/// cateva buffere mici care se reumplu pe rand, pe un fir separat. Fara dependinte externe.
/// Daca nu exista placa de sunet (sau nu merge), <see cref="TryStart"/> intoarce null si jocul ramane mut.
/// </summary>
public sealed class WaveOutPlayer : IDisposable
{
    /// <summary>Esantioane per buffer (~23 ms); cu 6 buffere, intarzierea maxima e ~140 ms.</summary>
    private const int BufferSamples = 512;
    private const int BufferCount = 6;
    private const uint WaveMapper = uint.MaxValue;
    private const uint CallbackEvent = 0x00050000;
    private const int WhdrDone = 0x1;

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormatEx
    {
        public ushort FormatTag, Channels;
        public uint SamplesPerSec, AvgBytesPerSec;
        public ushort BlockAlign, BitsPerSample, Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveHdr
    {
        public IntPtr Data;
        public uint BufferLength, BytesRecorded;
        public IntPtr User;
        public uint Flags, Loops;
        public IntPtr Next, Reserved;
    }

    [DllImport("winmm.dll")] private static extern int waveOutOpen(out IntPtr handle, uint deviceId, ref WaveFormatEx format, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")] private static extern int waveOutPrepareHeader(IntPtr handle, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutUnprepareHeader(IntPtr handle, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutWrite(IntPtr handle, IntPtr header, int size);
    [DllImport("winmm.dll")] private static extern int waveOutReset(IntPtr handle);
    [DllImport("winmm.dll")] private static extern int waveOutClose(IntPtr handle);

    private static readonly int HeaderSize = Marshal.SizeOf<WaveHdr>();
    private static readonly int FlagsOffset = (int)Marshal.OffsetOf<WaveHdr>(nameof(WaveHdr.Flags));

    private readonly Mixer _mixer;
    private readonly IntPtr _handle;
    private readonly AutoResetEvent _bufferDone;
    private readonly IntPtr[] _headers = new IntPtr[BufferCount];
    private readonly IntPtr[] _data = new IntPtr[BufferCount];
    private readonly short[] _pcm = new short[BufferSamples];
    private readonly Thread _thread;
    private volatile bool _stop;

    private WaveOutPlayer(Mixer mixer, IntPtr handle, AutoResetEvent bufferDone)
    {
        _mixer = mixer;
        _handle = handle;
        _bufferDone = bufferDone;
        _thread = new Thread(Run) { IsBackground = true, Name = "Audio", Priority = ThreadPriority.AboveNormal };
    }

    /// <summary>Deschide placa de sunet implicita; null daca nu se poate (fara exceptii).</summary>
    public static WaveOutPlayer? TryStart(Mixer mixer)
    {
        if (!OperatingSystem.IsWindows()) return null;
        var done = new AutoResetEvent(false);
        try
        {
            var format = new WaveFormatEx
            {
                FormatTag = 1,
                Channels = 1,
                SamplesPerSec = Mixer.SampleRate,
                BitsPerSample = 16,
                BlockAlign = 2,
                AvgBytesPerSec = Mixer.SampleRate * 2,
            };
            // placa de sunet semnaleaza evenimentul de fiecare data cand termina un buffer
            if (waveOutOpen(out var handle, WaveMapper, ref format, done.SafeWaitHandle.DangerousGetHandle(), IntPtr.Zero, CallbackEvent) != 0)
            {
                done.Dispose();
                return null;
            }
            var player = new WaveOutPlayer(mixer, handle, done);
            player.Start();
            return player;
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException or MarshalDirectiveException)
        {
            done.Dispose();
            return null;
        }
    }

    private void Start()
    {
        for (int i = 0; i < BufferCount; i++)
        {
            _data[i] = Marshal.AllocHGlobal(BufferSamples * 2);
            _headers[i] = Marshal.AllocHGlobal(HeaderSize);
            Marshal.StructureToPtr(new WaveHdr { Data = _data[i], BufferLength = BufferSamples * 2 }, _headers[i], false);
            waveOutPrepareHeader(_handle, _headers[i], HeaderSize);
        }
        for (int i = 0; i < BufferCount; i++) Submit(i);
        _thread.Start();
    }

    private void Submit(int i)
    {
        _mixer.Render(_pcm);
        Marshal.Copy(_pcm, 0, _data[i], BufferSamples);
        waveOutWrite(_handle, _headers[i], HeaderSize);
    }

    private void Run()
    {
        while (!_stop)
        {
            _bufferDone.WaitOne(50);
            for (int i = 0; i < BufferCount && !_stop; i++)
            {
                int flags = Marshal.ReadInt32(_headers[i], FlagsOffset);
                if ((flags & WhdrDone) == 0) continue;
                Marshal.WriteInt32(_headers[i], FlagsOffset, flags & ~WhdrDone);
                Submit(i);
            }
        }
    }

    public void Dispose()
    {
        if (_stop) return;
        _stop = true;
        _bufferDone.Set();
        _thread.Join(500);
        waveOutReset(_handle);
        for (int i = 0; i < BufferCount; i++)
        {
            if (_headers[i] == IntPtr.Zero) continue;
            waveOutUnprepareHeader(_handle, _headers[i], HeaderSize);
            Marshal.FreeHGlobal(_headers[i]);
            Marshal.FreeHGlobal(_data[i]);
        }
        waveOutClose(_handle);
        _bufferDone.Dispose();
    }
}
