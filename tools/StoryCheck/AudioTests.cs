using System.Text;
using System.Text.RegularExpressions;
using LastStopNight.Audio;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;

namespace StoryCheck;

/// <summary>
/// Sunetul, fara placa de sunet: fisierele WAV, mixerul (suma, volume, bucle, fade, limitare), motorul audio
/// si legatura cu povestile (fiecare sunet cerut de o locatie sau de cod are fisierul lui).
/// </summary>
public static partial class AudioTests
{
    private static readonly string SoundsDir = Path.Combine("LastStopNight", "Assets", "sounds");
    private static readonly string PlayerSourceDir = "LastStopNight";

    public static void Run(Report report, IReadOnlyList<string> storyPaths)
    {
        report.Section("Sunet");
        var bank = SoundBank.FromFolder(SoundsDir);
        SoundFiles(report, bank);
        WavParsing(report);
        MixerBasics(report);
        MixerFades(report);
        Engine(report, bank);
        NamesHaveFiles(report, bank, storyPaths);
    }

    private static WavClip Constant(float value, int samples) => new("test", Enumerable.Repeat(value, samples).ToArray());

    private static float[] Render(Mixer m, int samples)
    {
        var buf = new float[samples];
        m.Render(buf);
        return buf;
    }

    private static bool Near(float a, float b, float eps = 1e-4f) => MathF.Abs(a - b) <= eps;

    private static void SoundFiles(Report report, SoundBank bank)
    {
        int files = Directory.Exists(SoundsDir) ? Directory.GetFiles(SoundsDir, "*.wav").Length : 0;
        report.Check(files > 0 && bank.Count == files, $"toate cele {files} fișiere WAV se încarcă");
        var silent = bank.Clips.Where(c => c.Samples.Length == 0 || c.Samples.Max(MathF.Abs) < 0.05f).Select(c => c.Name).ToList();
        report.Check(silent.Count == 0, "niciun sunet nu e gol sau mut" + (silent.Count > 0 ? $" ({string.Join(", ", silent)})" : ""));
        // o bucla e fara cusatura daca saltul de la ultimul esantion la primul nu e mai mare decat cel mai mare salt din interior
        var seams = bank.Clips.Where(c => c.Name.StartsWith("amb_") || c.Name.StartsWith("music_")).Where(c =>
        {
            var s = c.Samples;
            float inner = 0;
            for (int i = 1; i < s.Length; i++) inner = Math.Max(inner, MathF.Abs(s[i] - s[i - 1]));
            return MathF.Abs(s[0] - s[^1]) > inner;
        }).Select(c => c.Name).ToList();
        report.Check(seams.Count == 0, "buclele de ambianță și muzica se leagă fără pocnituri" + (seams.Count > 0 ? $" ({string.Join(", ", seams)})" : ""));
    }

    /// <summary>Un WAV stereo la 44100 Hz devine mono la frecventa mixerului.</summary>
    private static void WavParsing(Report report)
    {
        const int frames = 4410;
        using var ms = new MemoryStream();
        using (var w = new BinaryWriter(ms, Encoding.ASCII, leaveOpen: true))
        {
            w.Write("RIFF"u8); w.Write(36 + frames * 4); w.Write("WAVE"u8);
            w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)2); w.Write(44100); w.Write(44100 * 4); w.Write((short)4); w.Write((short)16);
            w.Write("data"u8); w.Write(frames * 4);
            for (int i = 0; i < frames; i++) { w.Write((short)16384); w.Write((short)-8192); }
        }
        ms.Position = 0;
        var clip = WavClip.Load("stereo", ms);
        report.Check(clip.Samples.Length == frames / 2 && Near(clip.Samples[100], (16384 - 8192) / 2f / 32768f, 1e-3f),
            "WAV stereo 44,1 kHz: amestecat în mono și convertit la 22,05 kHz");
        report.Throws<InvalidDataException>(() => WavClip.Load("rau", new MemoryStream("nu e wav"u8.ToArray())), "un fișier care nu e WAV e respins");
    }

    private static void MixerBasics(Report report)
    {
        var m = new Mixer();
        m.Play(Constant(0.25f, 100), AudioChannel.Sfx);
        m.Play(Constant(0.25f, 100), AudioChannel.Sfx);
        report.Check(Near(Render(m, 10)[5], 0.5f), "două voci se adună");

        m.SetChannelVolume(AudioChannel.Sfx, 0.5f);
        report.Check(Near(Render(m, 10)[5], 0.25f), "volumul canalului se aplică");
        m.SetChannelVolume(AudioChannel.Sfx, 1);

        Render(m, 200);
        report.Check(m.VoiceCount == 0, "un sunet scurt se termină și iese din mixer");

        for (int i = 0; i < 5; i++) m.Play(Constant(0.5f, 50), AudioChannel.Sfx);
        report.Check(Near(Render(m, 10)[0], 1f), "suma e limitată la [-1, 1] (fără depășire în PCM)");
        m.StopAll();

        var ramp = new WavClip("ramp", Enumerable.Range(0, 10).Select(i => i / 10f).ToArray());
        m.Play(ramp, AudioChannel.Ambient, loop: true);
        var looped = Render(m, 25);
        report.Check(Near(looped[3], 0.3f) && Near(looped[13], 0.3f) && Near(looped[23], 0.3f) && m.VoiceCount == 1,
            "o buclă reia de la început și nu se termină");
        m.StopAll();

        m.Play(Constant(0.4f, 100), AudioChannel.Sfx, rate: 2);
        Render(m, 49);
        report.Check(m.VoiceCount == 1 && Render(m, 2)[1] == 0 && m.VoiceCount == 0, "viteza 2 redă sunetul în jumătate din timp");

        m.Play(Constant(0.4f, 100), AudioChannel.Sfx);
        m.Muted = true;
        report.Check(Render(m, 10).All(v => v == 0), "fără sunet când e oprit (M)");
        m.Muted = false;
        m.StopAll();

        for (int i = 0; i < Mixer.MaxVoices + 5; i++) m.Play(Constant(0.01f, 1000), AudioChannel.Sfx);
        report.Check(m.VoiceCount == Mixer.MaxVoices, $"cel mult {Mixer.MaxVoices} voci simultan (cele vechi lasă locul)");

        var pcm = new short[4];
        m.StopAll();
        m.Play(Constant(0.5f, 10), AudioChannel.Sfx);
        m.Render(pcm);
        report.Check(pcm[0] == (short)(0.5f * 32767), "ieșirea PCM pe 16 biți");
    }

    private static void MixerFades(Report report)
    {
        var m = new Mixer();
        int second = Mixer.SampleRate;
        var v = m.Play(Constant(1f, second * 3), AudioChannel.Ambient, volume: 0.8f, fadeIn: 1);
        var first = Render(m, second);
        bool rising = true;
        for (int i = 1; i < first.Length; i++) if (first[i] < first[i - 1]) rising = false;
        report.Check(rising && first[0] < 0.01f && Near(first[^1], 0.8f, 0.01f), "fade-in: de la liniște la volumul cerut, lin");
        m.FadeOut(v, 0.5f);
        Render(m, second / 2 + 10);
        report.Check(v.IsFinished && m.VoiceCount == 0, "fade-out: se stinge și iese din mixer");
    }

    private static void Engine(Report report, SoundBank bank)
    {
        using var audio = new AudioEngine(bank, output: false);
        audio.SetAmbient([("amb_rain", 1f), ("amb_wind", 0.5f), ("nu_exista", 1f)]);
        var rain = audio.AmbientNames.ToHashSet();
        audio.SetAmbient([("amb_rain", 0.7f)]);
        report.Check(rain.SetEquals(["amb_rain", "amb_wind"]) && audio.AmbientNames.SequenceEqual(["amb_rain"]),
            "ambianța: sunetele noi pornesc, cele vechi se sting, numele necunoscute sunt ignorate");
        int voices = audio.Mixer.VoiceCount;
        audio.SetMusic("music_menu");
        audio.SetMusic("music_menu");
        report.Check(audio.Mixer.VoiceCount == voices + 1 && audio.MusicName == "music_menu", "aceeași muzică cerută din nou nu repornește");
        audio.StopLoops(0);
        report.Check(audio.AmbientNames.Count == 0 && audio.MusicName is null, "la final se opresc ambianța și muzica");
        audio.Play("nu_exista");
        report.Check(!AudioEngine.Silent().HasOutput, "fără sunete, motorul e mut, fără erori");
    }

    /// <summary>Orice nume de sunet folosit de povesti sau de cod are fisierul lui.</summary>
    private static void NamesHaveFiles(Report report, SoundBank bank, IReadOnlyList<string> storyPaths)
    {
        var missing = new SortedSet<string>();
        void Need(string name) { if (!bank.Contains(name)) missing.Add(name); }

        foreach (var s in LocationDefinition.KnownSounds) Need(Soundscape.FileName(s));
        foreach (var t in Enum.GetValues<EndingType>()) Need(Soundscape.EndingSting(t));
        Need(Soundscape.MenuMusic);
        foreach (var path in storyPaths)
        {
            var story = StoryArchive.Load(path).Story;
            foreach (var b in story.Blocks)
                foreach (var (name, _) in Soundscape.Loops(b, story.FindLocation(b.Location))) Need(name);
        }
        for (int i = 1; i <= 3; i++) Need($"step_{i}");

        // numele scrise direct in codul jocului, pe liniile care redau sunete
        int used = 0;
        if (Directory.Exists(PlayerSourceDir))
            foreach (var file in Directory.EnumerateFiles(PlayerSourceDir, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains("obj")))
                foreach (var line in File.ReadLines(file).Where(l => SoundCall().IsMatch(l)))
                    foreach (Match m in NameLiteral().Matches(line)) { Need(m.Groups[1].Value); used++; }
        report.Check(missing.Count == 0 && used > 10,
            $"fiecare sunet cerut (locații, finaluri, meniuri, {used} apeluri din cod) are fișierul lui" +
            (missing.Count > 0 ? $" (lipsesc: {string.Join(", ", missing)})" : ""));
    }

    [GeneratedRegex(@"\b(PlaySfx|PlaySound|PlayMenuSound|Sound\?\.Invoke)\(")]
    private static partial Regex SoundCall();

    [GeneratedRegex("(?<![$@])\"([a-z][a-z0-9]*_[a-z0-9_]+|[a-z]+)\"")]
    private static partial Regex NameLiteral();
}
