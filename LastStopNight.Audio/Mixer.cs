namespace LastStopNight.Audio;

/// <summary>Canalele mixerului; fiecare are volumul lui (din Setari).</summary>
/// <summary>Canalele mixerului. Narration = vocea care citeste povestea (cat vorbeste, ambianta si muzica se aud mai incet).</summary>
public enum AudioChannel { Sfx, Ambient, Music, Narration }

/// <summary>Un sunet care se aude acum. Volumul se schimba lin (fade), niciodata brusc.</summary>
public sealed class Voice
{
    internal Voice(WavClip clip, AudioChannel channel, float volume, float rate, bool loop)
    {
        Clip = clip;
        Channel = channel;
        Target = volume;
        Rate = rate;
        Loop = loop;
    }

    public WavClip Clip { get; }
    public AudioChannel Channel { get; }
    public bool Loop { get; }
    public float Rate { get; }
    /// <summary>Volumul curent (urmeaza <see cref="Target"/> in ritmul fade-ului).</summary>
    public float Volume { get; internal set; }
    public float Target { get; internal set; }
    public bool IsFinished { get; internal set; }
    internal double Position;
    internal float Step;
    internal bool StopWhenSilent;
}

/// <summary>
/// Mixerul software: aduna vocile active intr-un buffer, cu volum pe canal, fade-uri si limitare la [-1, 1].
/// Nu stie nimic de placa de sunet (se poate testa fara ea); e sigur de folosit din doua fire
/// (jocul porneste sunete, firul audio cere esantioane).
/// </summary>
public sealed class Mixer
{
    public const int SampleRate = 22050;
    /// <summary>Peste atatea voci simultane, cel mai vechi efect scurt e oprit.</summary>
    public const int MaxVoices = 24;

    private readonly object _lock = new();
    private readonly List<Voice> _voices = new();
    private readonly float[] _channelVolume = [1, 1, 1, 1];
    private float[] _scratch = [];
    private float _duck = 1;

    /// <summary>Cat de tare raman ambianta si muzica in timp ce vorbeste naratorul (1 = deloc mai incet).</summary>
    public const float DuckLevel = 0.4f;

    /// <summary>Cat de tare se aud acum ambianta si muzica (scade lin cand vorbeste naratorul).</summary>
    public float Duck => _duck;

    public float MasterVolume { get; set; } = 1;
    public bool Muted { get; set; }

    public int VoiceCount { get { lock (_lock) return _voices.Count; } }

    public float GetChannelVolume(AudioChannel channel) => _channelVolume[(int)channel];
    public void SetChannelVolume(AudioChannel channel, float volume) => _channelVolume[(int)channel] = Math.Clamp(volume, 0, 1);

    /// <param name="fadeIn">Secunde pana la volumul cerut (0 = imediat).</param>
    public Voice Play(WavClip clip, AudioChannel channel, float volume = 1, float rate = 1, bool loop = false, float fadeIn = 0)
    {
        var v = new Voice(clip, channel, volume, Math.Clamp(rate, 0.25f, 4f), loop);
        if (fadeIn > 0) SetStep(v, 0, volume, fadeIn);
        else v.Volume = volume;
        lock (_lock)
        {
            if (_voices.Count >= MaxVoices)
            {
                int oldest = _voices.FindIndex(x => !x.Loop && x.Channel != AudioChannel.Narration);
                _voices[oldest < 0 ? 0 : oldest].IsFinished = true;
                _voices.RemoveAt(oldest < 0 ? 0 : oldest);
            }
            _voices.Add(v);
        }
        return v;
    }

    /// <summary>Duce vocea lin la alt volum in <paramref name="seconds"/> secunde.</summary>
    public void Fade(Voice voice, float volume, float seconds)
    {
        lock (_lock) SetStep(voice, voice.Volume, volume, seconds);
    }

    /// <summary>Stinge vocea lin, apoi o scoate din mixer.</summary>
    public void FadeOut(Voice voice, float seconds)
    {
        lock (_lock)
        {
            SetStep(voice, voice.Volume, 0, seconds);
            voice.StopWhenSilent = true;
            if (seconds <= 0) Remove(voice);
        }
    }

    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var v in _voices) v.IsFinished = true;
            _voices.Clear();
        }
    }

    private static void SetStep(Voice v, float from, float to, float seconds)
    {
        v.Volume = from;
        v.Target = to;
        v.Step = seconds <= 0 ? float.MaxValue : Math.Abs(to - from) / (seconds * SampleRate);
        if (seconds <= 0) v.Volume = to;
    }

    private void Remove(Voice v)
    {
        v.IsFinished = true;
        _voices.Remove(v);
    }

    /// <summary>Umple bufferul cu urmatoarele esantioane (suprascrie ce era in el).</summary>
    public void Render(Span<float> output)
    {
        output.Clear();
        lock (_lock)
        {
            // cat vorbeste naratorul, fundalul coboara lin (in ~0.3 s) si revine la fel
            bool narrating = _voices.Any(v => v.Channel == AudioChannel.Narration && !v.IsFinished && v.Target > 0);
            float step = output.Length / (float)SampleRate / 0.3f;
            _duck = narrating ? Math.Max(DuckLevel, _duck - step) : Math.Min(1, _duck + step);
            for (int k = _voices.Count - 1; k >= 0; k--)
            {
                var v = _voices[k];
                float volume = _channelVolume[(int)v.Channel] * (v.Channel is AudioChannel.Ambient or AudioChannel.Music ? _duck : 1);
                RenderVoice(v, output, volume);
                if (v.IsFinished) _voices.RemoveAt(k);
            }
        }
        float master = Muted ? 0 : MasterVolume;
        for (int i = 0; i < output.Length; i++) output[i] = Math.Clamp(output[i] * master, -1f, 1f);
    }

    /// <summary>La fel, dar direct in PCM pe 16 biti (formatul placii de sunet).</summary>
    public void Render(Span<short> output)
    {
        if (_scratch.Length < output.Length) _scratch = new float[output.Length];
        var f = _scratch.AsSpan(0, output.Length);
        Render(f);
        for (int i = 0; i < output.Length; i++) output[i] = (short)(f[i] * 32767);
    }

    private static void RenderVoice(Voice v, Span<float> output, float channelVolume)
    {
        var s = v.Clip.Samples;
        int n = s.Length;
        if (n == 0) { v.IsFinished = true; return; }
        for (int i = 0; i < output.Length; i++)
        {
            if (v.Position >= n)
            {
                if (!v.Loop) { v.IsFinished = true; return; }
                v.Position %= n;
            }
            int a = (int)v.Position;
            int b = a + 1 < n ? a + 1 : v.Loop ? 0 : a;
            float t = (float)(v.Position - a);
            float sample = s[a] + (s[b] - s[a]) * t;

            if (v.Volume != v.Target)
            {
                v.Volume = v.Volume < v.Target ? Math.Min(v.Target, v.Volume + v.Step) : Math.Max(v.Target, v.Volume - v.Step);
            }
            if (v.StopWhenSilent && v.Volume <= 0) { v.IsFinished = true; return; }

            output[i] += sample * v.Volume * channelVolume;
            v.Position += v.Rate;
        }
    }
}
