using System.Reflection;

namespace LastStopNight.Audio;

/// <summary>Sunetele jocului, dupa nume (numele fisierului fara .wav).</summary>
public sealed class SoundBank
{
    private readonly Dictionary<string, WavClip> _clips = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<WavClip> Clips => _clips.Values;
    public int Count => _clips.Count;

    public WavClip? Get(string name) => _clips.GetValueOrDefault(name);
    public bool Contains(string name) => _clips.ContainsKey(name);

    public void Add(WavClip clip) => _clips[clip.Name] = clip;

    /// <summary>Toate fisierele *.wav dintr-un dosar (cele stricate sunt sarite).</summary>
    public static SoundBank FromFolder(string folder)
    {
        var bank = new SoundBank();
        if (!Directory.Exists(folder)) return bank;
        foreach (var path in Directory.GetFiles(folder, "*.wav").Order())
            bank.TryAdd(Path.GetFileNameWithoutExtension(path), () => File.OpenRead(path));
        return bank;
    }

    /// <summary>Sunetele incorporate in exe (resurse cu numele „sounds/x.wav”).</summary>
    public static SoundBank FromResources(Assembly assembly, string prefix = "sounds/")
    {
        var bank = new SoundBank();
        foreach (var res in assembly.GetManifestResourceNames().Where(n => n.StartsWith(prefix) && n.EndsWith(".wav")).Order())
            bank.TryAdd(res[prefix.Length..^4], () => assembly.GetManifestResourceStream(res)!);
        return bank;
    }

    private void TryAdd(string name, Func<Stream> open)
    {
        try
        {
            using var s = open();
            Add(WavClip.Load(name, s));
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException) { }
    }
}
