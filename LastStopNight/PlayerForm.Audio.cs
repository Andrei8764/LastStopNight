using LastStopNight.Audio;
using LastStopNight.Core.Model;
using LastStopNight.Game;
using MenuItem = LastStopNight.Ui.MenuItem;

namespace LastStopNight;

/// <summary>
/// Sunetul: efectele cerute de scena, ambianta locatiei, muzica meniului, temele finalurilor si tasta M.
/// Sunetele stau langa exe (Assets/sounds) sau, in exe-ul publicat, incorporate in el.
/// LASTSTOP_MUTE=1 (si modurile de test) pastreaza totul, dar fara iesire la placa de sunet.
/// </summary>
public sealed partial class PlayerForm
{
    private const float MenuSoundVolume = 0.7f;
    private readonly AudioEngine _audio = CreateAudio();

    private static AudioEngine CreateAudio()
    {
        bool output = Environment.GetEnvironmentVariable("LASTSTOP_MUTE") != "1";
        var bank = SoundBank.FromFolder(Path.Combine(AppContext.BaseDirectory, "Assets", "sounds"));
        if (bank.Count == 0) bank = SoundBank.FromResources(typeof(PlayerForm).Assembly);
        return new AudioEngine(bank, output);
    }

    private void ApplyAudioSettings()
    {
        float max = GameSettings.MaxVolume;
        _audio.SetVolumes(_settings.SfxVolume / max, _settings.AmbientVolume / max, _settings.MusicVolume / max);
        _audio.Muted = false; // tasta M nu mai exista: cine a oprit sunetul cu ea il primeste inapoi (volumele sunt in Setari)
    }

    private void PlaySfx(string name, float volume = 1, float rate = 1) => _audio.Play(name, volume, rate);

    private void PlayMenuSound(string name) => _audio.Play(name, MenuSoundVolume);

    /// <summary>Sunetele cerute de scena in cadrul asta (pasi, telefon, neon...).</summary>
    private void PlaySceneSounds()
    {
        if (_scene is null) return;
        foreach (var cue in _scene.TakeSounds()) _audio.Play(cue.Name, cue.Volume, cue.Rate);
    }

    /// <summary>In meniuri: muzica, fara ambianta.</summary>
    private void UseMenuSoundscape()
    {
        _audio.SetAmbient([]);
        _audio.SetMusic(Soundscape.MenuMusic);
    }

    /// <summary>La intrarea intr-un bloc: ambianta locatiei, sau tema finalului.</summary>
    private void UseBlockSoundscape(StoryBlock block)
    {
        _audio.SetMusic(null);
        if (block.IsEnding)
        {
            _audio.StopLoops();
            PlaySfx(Soundscape.EndingSting(block.EndingType));
            return;
        }
        _audio.SetAmbient(Soundscape.Loops(block, _engine?.Story.FindLocation(block.Location)));
    }

    /// <summary>O optiune de volum pentru Setari (stanga / dreapta, 0..10).</summary>
    private MenuItem VolumeItem(string label, Func<int> get, Action<int> set, string hint) => new(label)
    {
        Value = () => get() == 0 ? "Oprit" : $"{get() * 100 / GameSettings.MaxVolume}%",
        Adjust = d =>
        {
            set(Math.Clamp(get() + d, 0, GameSettings.MaxVolume));
            ApplyAudioSettings();
            _settings.Save();
        },
        Hint = hint,
    };
}
