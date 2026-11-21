namespace LastStopNight.Audio;

/// <summary>
/// Ce foloseste jocul: efecte scurte (Play), ambianta in bucla (SetAmbient, cu trecere lina intre locatii)
/// si muzica (SetMusic). Numele necunoscute sunt ignorate, iar fara placa de sunet totul devine mut, fara erori.
/// </summary>
public sealed class AudioEngine : IDisposable
{
    /// <summary>Cat dureaza trecerea de la o ambianta (sau muzica) la alta.</summary>
    public const float CrossfadeSeconds = 1.5f;

    private readonly SoundBank _bank;
    private readonly Mixer _mixer = new();
    private readonly WaveOutPlayer? _player;
    private readonly Dictionary<string, Voice> _ambient = new(StringComparer.OrdinalIgnoreCase);
    private (string Name, Voice Voice)? _music;
    private Voice? _narration;

    /// <param name="output">Fals = nu deschide placa de sunet (teste, capturi automate); mixerul merge oricum.</param>
    public AudioEngine(SoundBank bank, bool output = true)
    {
        _bank = bank;
        if (output && bank.Count > 0) _player = WaveOutPlayer.TryStart(_mixer);
    }

    /// <summary>Un motor fara sunete si fara placa de sunet.</summary>
    public static AudioEngine Silent() => new(new SoundBank(), output: false);

    public Mixer Mixer => _mixer;
    public SoundBank Bank => _bank;
    /// <summary>Adevarat daca sunetul chiar ajunge la difuzoare.</summary>
    public bool HasOutput => _player is not null;

    public bool Muted { get => _mixer.Muted; set => _mixer.Muted = value; }

    /// <summary>Volumele canalelor, intre 0 si 1.</summary>
    public void SetVolumes(float sfx, float ambient, float music, float narration = 1)
    {
        _mixer.SetChannelVolume(AudioChannel.Sfx, sfx);
        _mixer.SetChannelVolume(AudioChannel.Ambient, ambient);
        _mixer.SetChannelVolume(AudioChannel.Music, music);
        _mixer.SetChannelVolume(AudioChannel.Narration, narration);
    }

    /// <summary>
    /// Naratorul citeste un text (null = tace). Ce citea inainte se stinge repede, ca sa nu vorbeasca peste.
    /// <paramref name="delay"/> = secunde de liniste la inceput (ex. cat se vede cartonasul de capitol).
    /// </summary>
    public void Narrate(WavClip? clip, float delay = 0)
    {
        if (_narration is { } old) _mixer.FadeOut(old, 0.25f);
        _narration = null;
        if (clip is null || clip.Samples.Length == 0) return;
        if (delay > 0)
        {
            var padded = new float[(int)(delay * Mixer.SampleRate) + clip.Samples.Length];
            clip.Samples.CopyTo(padded, padded.Length - clip.Samples.Length);
            clip = new WavClip(clip.Name, padded);
        }
        _narration = _mixer.Play(clip, AudioChannel.Narration);
    }

    /// <summary>Naratorul inca vorbeste.</summary>
    public bool IsNarrating => _narration is { IsFinished: false };

    /// <summary>Un efect scurt. <paramref name="rate"/> schimba tonul (1 = normal), util ca pasii sa nu sune identic.</summary>
    public void Play(string name, float volume = 1, float rate = 1)
    {
        if (_bank.Get(name) is { } clip && volume > 0) _mixer.Play(clip, AudioChannel.Sfx, volume, rate);
    }

    /// <summary>
    /// Ambianta ceruta (nume de fisiere, cu volumul fiecaruia). Ce era deja pornit continua fara intrerupere,
    /// ce nu mai e cerut se stinge lin, ce e nou porneste lin.
    /// </summary>
    public void SetAmbient(IEnumerable<(string Name, float Volume)> loops)
    {
        var wanted = loops.Where(l => _bank.Contains(l.Name))
                          .GroupBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
                          .ToDictionary(g => g.Key, g => g.Max(l => l.Volume), StringComparer.OrdinalIgnoreCase);
        foreach (var name in _ambient.Keys.Where(n => !wanted.ContainsKey(n)).ToList())
        {
            _mixer.FadeOut(_ambient[name], CrossfadeSeconds);
            _ambient.Remove(name);
        }
        foreach (var (name, volume) in wanted)
        {
            if (_ambient.TryGetValue(name, out var v)) _mixer.Fade(v, volume, CrossfadeSeconds);
            else _ambient[name] = _mixer.Play(_bank.Get(name)!, AudioChannel.Ambient, volume, loop: true, fadeIn: CrossfadeSeconds);
        }
    }

    /// <summary>Numele buclelor de ambianta care se aud acum (pentru teste si depanare).</summary>
    public IReadOnlyCollection<string> AmbientNames => _ambient.Keys;

    /// <summary>Muzica de fundal (null = liniste). Aceeasi melodie ceruta din nou nu reporneste.</summary>
    public void SetMusic(string? name, float volume = 1)
    {
        if (_music is { } m && string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            _mixer.Fade(m.Voice, volume, CrossfadeSeconds);
            return;
        }
        if (_music is { } old) _mixer.FadeOut(old.Voice, CrossfadeSeconds);
        _music = name is not null && _bank.Get(name) is { } clip
            ? (name, _mixer.Play(clip, AudioChannel.Music, volume, loop: true, fadeIn: CrossfadeSeconds))
            : null;
    }

    public string? MusicName => _music?.Name;

    /// <summary>Opreste tot ce e in bucla (ex. la final, inainte de tema finalului).</summary>
    public void StopLoops(float seconds = CrossfadeSeconds)
    {
        SetAmbient([]);
        if (_music is { } m) _mixer.FadeOut(m.Voice, seconds);
        _music = null;
    }

    public void Dispose()
    {
        _player?.Dispose();
        _mixer.StopAll();
    }
}
