namespace LastStopNight.Audio;

/// <summary>Un sunet incarcat in memorie: esantioane float (-1..1), mono, la frecventa mixerului.</summary>
public sealed class WavClip
{
    public WavClip(string name, float[] samples)
    {
        Name = name;
        Samples = samples;
    }

    public string Name { get; }
    public float[] Samples { get; }
    public double Seconds => Samples.Length / (double)Mixer.SampleRate;

    /// <summary>
    /// Citeste un WAV PCM pe 16 biti (mono sau stereo, orice frecventa). Stereo se amesteca in mono,
    /// iar frecventa se converteste liniar la <see cref="Mixer.SampleRate"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">Fisierul nu e un WAV PCM pe 16 biti.</exception>
    public static WavClip Load(string name, Stream stream)
    {
        using var r = new BinaryReader(stream);
        if (new string(r.ReadChars(4)) != "RIFF") throw new InvalidDataException($"{name}: nu e un fișier WAV");
        r.ReadInt32();
        if (new string(r.ReadChars(4)) != "WAVE") throw new InvalidDataException($"{name}: nu e un fișier WAV");

        int channels = 0, rate = 0, bits = 0;
        short[]? pcm = null;
        while (r.BaseStream.Position + 8 <= r.BaseStream.Length)
        {
            string id = new(r.ReadChars(4));
            int size = r.ReadInt32();
            long next = r.BaseStream.Position + size + (size & 1);
            if (id == "fmt ")
            {
                if (r.ReadInt16() != 1) throw new InvalidDataException($"{name}: doar PCM necomprimat");
                channels = r.ReadInt16();
                rate = r.ReadInt32();
                r.ReadInt32();
                r.ReadInt16();
                bits = r.ReadInt16();
            }
            else if (id == "data")
            {
                var bytes = r.ReadBytes(size);
                pcm = new short[bytes.Length / 2];
                Buffer.BlockCopy(bytes, 0, pcm, 0, pcm.Length * 2);
            }
            if (next > r.BaseStream.Length) break;
            r.BaseStream.Position = next;
        }
        if (bits != 16 || channels is < 1 or > 2 || rate <= 0 || pcm is null)
            throw new InvalidDataException($"{name}: doar WAV PCM pe 16 biti, mono sau stereo");

        var mono = new float[pcm.Length / channels];
        for (int i = 0; i < mono.Length; i++)
        {
            float sum = 0;
            for (int c = 0; c < channels; c++) sum += pcm[i * channels + c];
            mono[i] = sum / channels / 32768f;
        }
        return new WavClip(name, Resample(mono, rate, Mixer.SampleRate));
    }

    private static float[] Resample(float[] src, int from, int to)
    {
        if (from == to || src.Length == 0) return src;
        var dst = new float[(int)((long)src.Length * to / from)];
        double step = (double)from / to;
        for (int i = 0; i < dst.Length; i++)
        {
            double p = i * step;
            int k = (int)p;
            float t = (float)(p - k);
            float a = src[Math.Min(k, src.Length - 1)], b = src[Math.Min(k + 1, src.Length - 1)];
            dst[i] = a + (b - a) * t;
        }
        return dst;
    }
}
