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
    /// Citeste un WAV PCM pe 16 biti (mono sau stereo, orice frecventa) sau IMA ADPCM mono (4 biti pe esantion,
    /// formatul vocilor naratorului: de 4 ori mai mic). Stereo se amesteca in mono, iar frecventa se converteste
    /// liniar la <see cref="Mixer.SampleRate"/>.
    /// </summary>
    /// <exception cref="InvalidDataException">Fisierul nu e un WAV PCM pe 16 biti sau IMA ADPCM mono.</exception>
    public static WavClip Load(string name, Stream stream)
    {
        using var r = new BinaryReader(stream);
        if (new string(r.ReadChars(4)) != "RIFF") throw new InvalidDataException($"{name}: nu e un fișier WAV");
        r.ReadInt32();
        if (new string(r.ReadChars(4)) != "WAVE") throw new InvalidDataException($"{name}: nu e un fișier WAV");

        int channels = 0, rate = 0, bits = 0, format = 0, blockAlign = 0;
        short[]? pcm = null;
        while (r.BaseStream.Position + 8 <= r.BaseStream.Length)
        {
            string id = new(r.ReadChars(4));
            int size = r.ReadInt32();
            long next = r.BaseStream.Position + size + (size & 1);
            if (id == "fmt ")
            {
                format = r.ReadInt16();
                if (format is not (PcmFormat or ImaAdpcmFormat)) throw new InvalidDataException($"{name}: doar PCM sau IMA ADPCM");
                channels = r.ReadInt16();
                rate = r.ReadInt32();
                r.ReadInt32();
                blockAlign = r.ReadInt16();
                bits = r.ReadInt16();
            }
            else if (id == "data")
            {
                var bytes = r.ReadBytes(size);
                if (format == ImaAdpcmFormat)
                {
                    if (channels != 1 || blockAlign <= 4) throw new InvalidDataException($"{name}: IMA ADPCM doar mono");
                    pcm = DecodeImaAdpcm(bytes, blockAlign);
                    bits = 16;
                }
                else
                {
                    pcm = new short[bytes.Length / 2];
                    Buffer.BlockCopy(bytes, 0, pcm, 0, pcm.Length * 2);
                }
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

    private const int PcmFormat = 1, ImaAdpcmFormat = 0x11;

    private static readonly int[] ImaIndex = [-1, -1, -1, -1, 2, 4, 6, 8, -1, -1, -1, -1, 2, 4, 6, 8];

    private static readonly int[] ImaStep =
    [
        7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 21, 23, 25, 28, 31, 34, 37, 41, 45, 50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
        130, 143, 157, 173, 190, 209, 230, 253, 279, 307, 337, 371, 408, 449, 494, 544, 598, 658, 724, 796, 876, 963, 1060,
        1166, 1282, 1411, 1552, 1707, 1878, 2066, 2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358, 5894, 6484,
        7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899, 15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767,
    ];

    /// <summary>
    /// IMA ADPCM mono (formatul 0x11 din WAV): blocuri de <paramref name="blockAlign"/> octeti, fiecare cu un
    /// esantion de pornire si indexul pasului, apoi cate doua esantioane pe octet (intai nibble-ul de jos).
    /// </summary>
    private static short[] DecodeImaAdpcm(byte[] data, int blockAlign)
    {
        int perBlock = 1 + (blockAlign - 4) * 2;
        var output = new List<short>(data.Length / blockAlign * perBlock + perBlock);
        for (int b = 0; b + 4 <= data.Length; b += blockAlign)
        {
            int predictor = (short)(data[b] | (data[b + 1] << 8));
            int index = Math.Clamp((int)data[b + 2], 0, ImaStep.Length - 1);
            output.Add((short)predictor);
            int end = Math.Min(b + blockAlign, data.Length);
            for (int i = b + 4; i < end; i++)
                for (int half = 0; half < 2; half++)
                {
                    int nibble = half == 0 ? data[i] & 0x0F : data[i] >> 4;
                    int step = ImaStep[index];
                    int diff = step >> 3;
                    if ((nibble & 4) != 0) diff += step;
                    if ((nibble & 2) != 0) diff += step >> 1;
                    if ((nibble & 1) != 0) diff += step >> 2;
                    predictor = Math.Clamp((nibble & 8) != 0 ? predictor - diff : predictor + diff, short.MinValue, short.MaxValue);
                    index = Math.Clamp(index + ImaIndex[nibble], 0, ImaStep.Length - 1);
                    output.Add((short)predictor);
                }
        }
        return output.ToArray();
    }

    /// <summary>Lipeste mai multe sunete, cu <paramref name="gapSeconds"/> de liniste intre ele (ex. textul + paragraful insotitorului).</summary>
    public static WavClip Join(string name, IReadOnlyList<WavClip> clips, float gapSeconds)
    {
        if (clips.Count == 1) return clips[0];
        int gap = (int)(gapSeconds * Mixer.SampleRate);
        var all = new float[clips.Sum(c => c.Samples.Length) + gap * Math.Max(0, clips.Count - 1)];
        int at = 0;
        foreach (var c in clips)
        {
            c.Samples.CopyTo(all, at);
            at += c.Samples.Length + gap;
        }
        return new WavClip(name, all);
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
