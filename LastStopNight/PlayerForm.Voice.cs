using LastStopNight.Audio;
using LastStopNight.Core.IO;
using LastStopNight.Core.Model;
using LastStopNight.Game;

namespace LastStopNight;

/// <summary>
/// Naratorul si replicile: vocea citeste textul fiecarui bloc (fisierele din voice/ ale povestii, generate de
/// tools/build_voice.py), iar replicile din text apar in bule deasupra celui care le spune, cam cand le rosteste vocea.
/// </summary>
public sealed partial class PlayerForm
{
    /// <summary>Pauza dintre textul blocului si paragraful insotitorului.</summary>
    private const float NarrationGapSeconds = 0.5f;

    private readonly Dictionary<string, WavClip?> _voiceClips = new();

    private StoryPackage? _voicePackage;

    /// <summary>
    /// Ce parte din textul de jos citeste naratorul si cand: inceputul si lungimea in text, secunda la care
    /// incepe, cat dureaza si cate caractere citeste (textul blocului se citeste impreuna cu hartia).
    /// </summary>
    private readonly List<(int TextStart, int TextLength, float From, float Seconds, int SpokenLength)> _narrationParts = new();

    /// <summary>Vocea unui fisier din poveste (decodata o singura data); null daca povestea n-are voce pentru el.</summary>
    private WavClip? VoiceClip(string path)
    {
        if (_voicePackage != _package)
        {
            _voiceClips.Clear();
            _voicePackage = _package;
        }
        if (_voiceClips.TryGetValue(path, out var clip)) return clip;
        clip = null;
        if (_package?.GetFile(path) is { } data)
        {
            try { clip = WavClip.Load(path, new MemoryStream(data)); }
            catch (InvalidDataException) { } // fisier stricat: blocul ramane fara voce
        }
        return _voiceClips[path] = clip;
    }

    /// <summary>
    /// Naratorul citeste blocul: textul (cu hartia), apoi paragrafele insotitorilor din echipa, in ordinea din text.
    /// Intoarce cat dureaza (0 = fara voce: oprita din Setari sau povestea n-are voce pentru bloc).
    /// </summary>
    private float NarrateBlock(StoryBlock block, float delay)
    {
        _narrationParts.Clear();
        if (_settings.NarrationVolume == 0 || _engine is null)
        {
            _audio.Narrate(null);
            return 0;
        }
        var parts = new List<WavClip>();
        float time = 0;
        void Add(WavClip? clip, int textStart, string text, string spoken)
        {
            if (clip is null) return;
            _narrationParts.Add((textStart, text.Length, time, (float)clip.Seconds, Math.Max(1, spoken.Length)));
            parts.Add(clip);
            time += (float)clip.Seconds + NarrationGapSeconds;
        }
        // aceeasi ordine ca in textul de jos (StoryBlock.TextFor): textul, apoi paragrafele insotitorilor
        Add(VoiceClip(StoryVoice.PathFor(block.Id)), 0, block.Text, StoryVoice.SpokenText(block));
        int at = block.Text.Length + 2;
        foreach (var companion in _engine.Party.Where(block.CompanionText.ContainsKey))
        {
            string extra = block.CompanionText[companion];
            Add(VoiceClip(StoryVoice.PathFor(block.Id, companion)), at, extra, extra);
            at += extra.Length + 2;
        }
        if (parts.Count == 0)
        {
            _audio.Narrate(null);
            return 0;
        }
        var clip = WavClip.Join(block.Id, parts, NarrationGapSeconds);
        _audio.Narrate(clip, delay);
        return (float)clip.Seconds;
    }

    /// <summary>
    /// Replicile din text apar in bule deasupra personajelor. Cu narator, bula apare cand vocea ajunge la replica
    /// (estimat dupa pozitia in text); fara, cand ajunge acolo textul care se scrie jos.
    /// </summary>
    private void ShowDialogue(StoryBlock block, string text, float delay, float narrationSeconds)
    {
        if (_scene is null || _engine is null || block.IsEnding) return;
        var scene = _scene;
        var speakers = new List<(string Id, string Name)>();
        foreach (var actor in new Actors.Actor?[] { scene.Player }.Concat(scene.Followers).Concat(scene.Remotes).Concat(scene.Npcs))
            if (actor?.Definition is { } def && speakers.All(s => s.Id != def.Id)) speakers.Add((def.Id, def.Name));
        var lines = DialogueLines.Find(text, speakers, _engine.Story.PlayerCharacterId);
        if (lines.Count == 0) return;

        var busyUntil = new Dictionary<string, float>();
        foreach (var line in lines)
        {
            var who = scene.FindActor(line.CharacterId) ?? scene.Npcs.FirstOrDefault(n => n.Definition.Id == line.CharacterId);
            if (who is null) continue;
            // cu narator: momentul in care vocea ajunge la replica, in paragraful ei; fara: cand ajunge textul scris
            var part = _narrationParts.FirstOrDefault(p => line.Offset >= p.TextStart && line.Offset < p.TextStart + p.TextLength);
            float at = narrationSeconds > 0 && part.Seconds > 0
                ? delay + part.From + part.Seconds * Math.Min(1, (line.Offset - part.TextStart) / (float)part.SpokenLength)
                : delay + line.Offset / Math.Max(10, _text.CharsPerSecond);
            at = Math.Max(at, busyUntil.GetValueOrDefault(line.CharacterId));
            var bubble = new Effects.SpeechBubbleEffect(line.Text, who == scene.Player ? null : who, at);
            scene.Effects.Add(bubble);
            busyUntil[line.CharacterId] = bubble.TotalDuration + 0.2f;
        }
    }
}
