using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using LastStopNight.Actors;
using LastStopNight.Core.Model;
using LastStopNight.Effects;
using LastStopNight.Rendering;

namespace LastStopNight.Game;

/// <summary>
/// Reprezentarea 2D a unui bloc narativ: fundal, decor animat, lumini, actori, efecte,
/// prim-plan si punctele de interactiune. Se reconstruieste la fiecare schimbare de bloc.
/// </summary>
public sealed class Scene
{
    private static readonly Lazy<Bitmap> VignetteBmp = new(CreateVignette);
    private static readonly Lazy<Bitmap> GlowBmp = new(CreateGlow);
    private static readonly Lazy<ImageAttributes> SepiaAttrs = new(() => Screen2D.Sepia(0.85f));
    private static readonly Lazy<ImageAttributes> ReflectionAttrs = new(() => Screen2D.AlphaTint(0.33f, 0.75f, 0.85f, 1.1f));
    private static readonly Dictionary<string, ImageAttributes> TintCache = new();
    private readonly Queue<Action> _posted = new();
    private readonly List<(DecorDefinition Def, SpriteSheet Sheet)> _decor = new();
    private readonly Random _rnd = new();
    private readonly ImageAttributes? _actorAttrs;
    private float _closeupT;
    private float _focusT;
    private Interactable? _focus;

    // distante (in pixeli logici) fata de jucator cand apar ceilalti in scena
    private const float CompanionGap = 17, RemoteGap = 20, ExtraActorGap = 12, MultiplayerCompanionOffset = 18;
    /// <summary>Distanta de la care jucatorul nu se mai teleporteaza, ci merge pana la locul lui din bloc.</summary>
    private const float WalkInThreshold = 2;

    /// <param name="previous">Scena de dinainte: daca e aceeasi locatie, personajele isi pastreaza locul.</param>
    /// <param name="includePlayer">Fals pentru finaluri si pentru scena din spatele meniului.</param>
    /// <param name="companions">Insotitori controlati de calculator (single player): te urmeaza.</param>
    /// <param name="playerCharacter">Personajul controlat pe acest calculator (implicit protagonistul).</param>
    /// <param name="remotes">Personajele celorlalti jucatori (multiplayer), cu ultima pozitie cunoscuta.</param>
    public Scene(StoryDefinition story, StoryBlock block, ImageCache images, Scene? previous, bool includePlayer = true,
                 IEnumerable<string>? companions = null, string? playerCharacter = null,
                 IReadOnlyDictionary<string, (float X, bool FacingLeft, string BlockId)>? remotes = null)
    {
        Block = block;
        Location = story.FindLocation(block.Location);
        Background = images.Get(story.ResolveBackground(block));
        Closeup = images.Get(block.Closeup);
        Input = previous?.Input ?? new InputState();
        bool sameLocation = previous?.Location is not null && previous.Location.Id == Location?.Id && previous.Player is not null;

        if (includePlayer && Location is not null) Player = CreatePlayer(story, images, previous, sameLocation, playerCharacter);
        if (Player is not null && !block.Solo) AddFollowers(story, images, previous, sameLocation, companions ?? []);
        if (Player is not null && remotes is not null) AddRemotes(story, images, previous, sameLocation, remotes);
        foreach (var cp in block.Characters)
            if (story.FindCharacter(cp.Id) is { } def && images.GetSheet(def) is { } sheet)
                Npcs.Add(new NpcActor(def, sheet, cp));
        if (Location is not null && !block.IsEnding)
            foreach (var d in Location.Decor.Concat(block.Props))
                if (images.GetSheet(d.Image, d.FrameWidth, d.FrameHeight) is { } ds) _decor.Add((d, ds));
        _actorAttrs = ActorTint(Location?.ActorTint);
        AddStartEffects(sameLocation);
    }

    /// <summary>
    /// Jucatorul local: in aceeasi locatie pleaca de unde era si merge pana la locul lui din bloc;
    /// altfel apare direct acolo. In multiplayer, insotitorul sta langa protagonist, nu peste el.
    /// </summary>
    private PlayerActor? CreatePlayer(StoryDefinition story, ImageCache images, Scene? previous, bool sameLocation, string? playerCharacter)
    {
        if (story.FindCharacter(playerCharacter ?? story.PlayerCharacterId) is not { } def || images.GetSheet(def) is not { } sheet)
            return null;
        float target = Math.Clamp(Block.PlayerX ?? (sameLocation ? previous!.Player!.X : MinX + 20), MinX, MaxX);
        if (playerCharacter is not null && playerCharacter != story.PlayerCharacterId && (Block.PlayerX is not null || !sameLocation))
            target = Math.Clamp(target + (target > MidX ? -MultiplayerCompanionOffset : MultiplayerCompanionOffset), MinX, MaxX);
        float start = sameLocation ? previous!.Player!.X : target;
        var player = new PlayerActor(def, sheet, start)
        {
            FacingLeft = sameLocation ? previous!.Player!.FacingLeft : target > Screen2D.Width / 2,
        };
        if (sameLocation && MathF.Abs(start - target) > WalkInThreshold) player.WalkTo(target, null);
        return player;
    }

    /// <summary>Insotitorii (single player): unde ramasesera, daca e aceeasi locatie, altfel langa jucator.</summary>
    private void AddFollowers(StoryDefinition story, ImageCache images, Scene? previous, bool sameLocation, IEnumerable<string> companions)
    {
        int index = 0;
        foreach (var id in companions)
        {
            if (story.FindCharacter(id) is not { } def || images.GetSheet(def) is not { } sheet) continue;
            var old = sameLocation ? previous!.Followers.FirstOrDefault(f => f.Definition.Id == id) : null;
            float x = old?.X ?? BesidePlayer(CompanionGap, index);
            Followers.Add(new FollowerActor(def, sheet, x) { FacingLeft = old?.FacingLeft ?? Player!.FacingLeft });
            index++;
        }
    }

    /// <summary>Ceilalti jucatori (multiplayer): unde au fost vazuti ultima data in aceeasi locatie, altfel langa tine.</summary>
    private void AddRemotes(StoryDefinition story, ImageCache images, Scene? previous, bool sameLocation,
                            IReadOnlyDictionary<string, (float X, bool FacingLeft, string BlockId)> remotes)
    {
        int index = 0;
        foreach (var (id, pos) in remotes)
        {
            if (story.FindCharacter(id) is not { } def || images.GetSheet(def) is not { } sheet) continue;
            var old = previous?.Remotes.FirstOrDefault(r => r.Definition.Id == id);
            float x = sameLocation && old is not null ? old.X
                : pos.BlockId == Block.Id ? pos.X
                : BesidePlayer(RemoteGap, index);
            Remotes.Add(new RemoteActor(def, sheet, x) { FacingLeft = old?.FacingLeft ?? Player!.FacingLeft });
            index++;
        }
    }

    /// <summary>Un loc langa jucator, spre mijlocul scenei; al doilea, al treilea... putin mai departe.</summary>
    private float BesidePlayer(float gap, int index)
    {
        float side = Player!.X > MidX ? 1 : -1;
        return Math.Clamp(Player.X + side * (gap + ExtraActorGap * index), MinX, MaxX);
    }

    private float MidX => (MinX + MaxX) / 2f;

    /// <summary>Lumina locului pe personaje (albastru rece afara noaptea, cald in bucatarie etc.); filtrele se refolosesc.</summary>
    private static ImageAttributes? ActorTint(string? color)
    {
        if (string.IsNullOrWhiteSpace(color)) return null;
        if (!TintCache.TryGetValue(color, out var attrs))
        {
            var c = Screen2D.ParseColor(color, Color.White);
            TintCache[color] = attrs = Screen2D.AlphaTint(1, c.R / 255f, c.G / 255f, c.B / 255f);
        }
        return attrs;
    }

    /// <summary>Efectele permanente ale locatiei, cele ale blocului, intrarea (fade) si cardul de capitol.</summary>
    private void AddStartEffects(bool sameLocation)
    {
        foreach (var fx in Location?.Ambient ?? []) AddNamedEffect(fx);
        foreach (var fx in Block.Fx) AddNamedEffect(fx);
        if (!sameLocation) Effects.Add(new FadeInEffect());
        if (!string.IsNullOrWhiteSpace(Block.Chapter)) Effects.Add(new ChapterCardEffect(Block.Chapter));
    }

    /// <summary>Halou alb, moale, refolosit pentru lumini si ceata.</summary>
    public static Bitmap Glow => GlowBmp.Value;

    public StoryBlock Block { get; }
    public LocationDefinition? Location { get; }
    public Bitmap? Background { get; }
    public Bitmap? Closeup { get; }
    public bool ShowCloseup { get; set; } = true;
    /// <summary>Ascunde prim-planul doar la desenare (meniul de pauza deschis peste joc), fara sa-i schimbe starea.</summary>
    public bool HideCloseup { get; set; }
    /// <summary>Prim-planul e (inca) pe ecran; intre timp nu aratam indicii de interactiune.</summary>
    public bool CloseupVisible => Closeup is not null && _closeupT > 0.05f;
    public PlayerActor? Player { get; }
    public List<NpcActor> Npcs { get; } = new();
    public List<FollowerActor> Followers { get; } = new();
    public List<RemoteActor> Remotes { get; } = new();

    /// <summary>Personajul (jucator, insotitor sau alt jucator) cu acest ID, daca e in scena.</summary>
    public Actor? FindActor(string characterId) =>
        Player?.Definition.Id == characterId ? Player
        : (Actor?)Followers.FirstOrDefault(f => f.Definition.Id == characterId) ?? Remotes.FirstOrDefault(r => r.Definition.Id == characterId);
    public List<SceneEffect> Effects { get; } = new();
    public List<Interactable> Interactables { get; } = new();
    public HashSet<string> SeenExamines { get; set; } = new();
    public InputState Input { get; }
    public PointF CameraOffset { get; set; }
    public float Time { get; private set; }

    public int FloorY => Location?.FloorY ?? 170;
    public int MinX => Location?.MinX ?? 10;
    public int MaxX => Location?.MaxX ?? 310;
    public bool HasChapterCard => Effects.OfType<ChapterCardEffect>().Any();

    public void Post(Action action) => _posted.Enqueue(action);

    /// <summary>Sunetele cerute in cadrul curent; fereastra le preia (<see cref="TakeSounds"/>) si le reda.</summary>
    private readonly List<SoundCue> _sounds = new();
    private const float FollowerStepVolume = 0.5f, RemoteStepVolume = 0.6f;
    private int _stepSound;

    /// <param name="rate">Viteza de redare (si tonul); 1 = normal.</param>
    public void PlaySound(string name, float volume = 1, float rate = 1) => _sounds.Add(new SoundCue(name, volume, rate));

    public List<SoundCue> TakeSounds()
    {
        var list = _sounds.ToList();
        _sounds.Clear();
        return list;
    }

    /// <summary>Pasii: trei variante, cu ton usor diferit, ca sa nu sune ca un metronom.</summary>
    private void Footstep(Actor actor, float volume)
    {
        if (!actor.TakeStep()) return;
        _stepSound = (_stepSound + 1 + _rnd.Next(2)) % 3;
        PlaySound($"step_{_stepSound + 1}", volume * (0.8f + (float)_rnd.NextDouble() * 0.2f), 0.92f + (float)_rnd.NextDouble() * 0.16f);
    }

    public List<Action> TakePosted()
    {
        var list = _posted.ToList();
        _posted.Clear();
        return list;
    }

    public void AddNamedEffect(string name)
    {
        switch (name.Trim().ToLowerInvariant())
        {
            case "rain": Effects.Add(new RainEffect()); break;
            case "dust": Effects.Add(new DustEffect()); break;
            case "fog": Effects.Add(new FogEffect()); break;
            case "flicker": Effects.Add(new FlickerEffect()); break;
            case "shake": Effects.Add(new ShakeEffect(1.2f, 3)); break;
            case "ring":
                if (Location?.Points.TryGetValue("phone", out var p) == true)
                    Effects.Add(new PhoneRingEffect(new PointF(p.X, p.Y)));
                break;
        }
    }

    /// <summary>Bula de dialog deasupra unui personaj (implicit jucatorul), optional dupa o pauza.</summary>
    public SpeechBubbleEffect Say(string text, Actor? speaker = null, float delay = 0)
    {
        foreach (var b in Effects.OfType<SpeechBubbleEffect>().Where(b => b.Speaker == speaker)) b.Dismiss();
        var bubble = new SpeechBubbleEffect(text, speaker, delay);
        Effects.Add(bubble);
        return bubble;
    }

    /// <summary>Cel mai apropiat punct de interactiune; deciziile au prioritate la distanta egala.</summary>
    /// <summary>Punctul din scena unde sta obiectul (marcaj si indiciu).</summary>
    public PointF Anchor(Interactable h) => new(h.X, h.Y ?? FloorY - 34);

    /// <summary>Obiectul de langa jucator si cat de „aprins” e indiciul lui (0..1).</summary>
    public (Interactable? Item, float Fade) Focus => (_focus, _focusT);

    public Interactable? NearestInteractable(float range = 12)
    {
        if (Player is null) return null;
        return Interactables.Where(h => MathF.Abs(h.X - Player.X) <= range)
                            .OrderBy(h => MathF.Abs(h.X - Player.X) + (h.Decision is null ? 0.5f : 0))
                            .FirstOrDefault();
    }

    public void Update(float dt)
    {
        Time += dt;
        Player?.Update(dt, this);
        foreach (var n in Npcs) n.Update(dt, this);
        foreach (var f in Followers) f.Update(dt, this);
        foreach (var r in Remotes) r.Update(dt, this);
        if (Player is not null) Footstep(Player, 1);
        foreach (var f in Followers) Footstep(f, FollowerStepVolume);
        foreach (var r in Remotes) Footstep(r, RemoteStepVolume);
        for (int i = 0; i < Effects.Count; i++) Effects[i].Update(dt, this);
        Effects.RemoveAll(e => e.IsFinished);
        // daca pleci de langa prim-plan, se inchide singur
        if (Player is { IsWalking: true } && _closeupT >= 1) ShowCloseup = false;
        var near = CloseupVisible ? null : NearestInteractable();
        if (near != _focus) { _focus = near; _focusT = 0; }
        _focusT = Math.Min(1, _focusT + dt * 5);
        _closeupT = Math.Clamp(_closeupT + (ShowCloseup && Closeup is not null ? dt : -dt) * 3.5f, 0, 1);
    }

    /// <summary>Deseneaza lumea; transformarea curenta a lui g mapeaza coordonatele 320x180 pe ecran.</summary>
    public void RenderWorld(Graphics g)
    {
        g.FillRectangle(Brushes.Black, 0, 0, Screen2D.Width, Screen2D.Height);
        var state = g.Save();
        g.TranslateTransform(CameraOffset.X, CameraOffset.Y);

        if (Background is not null)
        {
            var dest = new Rectangle(0, 0, Screen2D.Width, Screen2D.Height);
            if (Block.Mood == SceneMood.Sepia)
                g.DrawImage(Background, dest, 0, 0, Background.Width, Background.Height, GraphicsUnit.Pixel, SepiaAttrs.Value);
            else g.DrawImage(Background, dest);
        }

        DrawDecor(g, front: false);
        DrawLights(g);

        if (Location is { Windows.Count: > 0 })
        {
            foreach (var w in Location.Windows)
            {
                var rect = new Rectangle(w.X, w.Y, w.W, w.H);
                foreach (var n in Npcs) n.DrawReflection(g, rect, ReflectionAttrs.Value);
                foreach (var f in Followers) f.DrawReflection(g, rect, ReflectionAttrs.Value);
                foreach (var ra in Remotes) ra.DrawReflection(g, rect, ReflectionAttrs.Value);
                Player?.DrawReflection(g, rect, ReflectionAttrs.Value);
            }
        }

        DrawMarkers(g);
        foreach (var n in Npcs.OrderBy(n => n.X)) n.Draw(g, FloorY, _actorAttrs);
        foreach (var f in Followers) f.Draw(g, FloorY, _actorAttrs);
        foreach (var ra in Remotes) ra.Draw(g, FloorY, _actorAttrs);
        Player?.Draw(g, FloorY, _actorAttrs);
        DrawDecor(g, front: true);

        DrawMood(g);
        foreach (var e in Effects) e.DrawWorld(g, this);
        DrawCloseup(g);
        g.Restore(state);

        var im = g.InterpolationMode;
        g.InterpolationMode = InterpolationMode.Bilinear;
        g.DrawImage(VignetteBmp.Value, 0, 0, Screen2D.Width, Screen2D.Height);
        g.InterpolationMode = im;
    }

    private void DrawDecor(Graphics g, bool front)
    {
        foreach (var (d, sheet) in _decor)
        {
            if (d.Front != front) continue;
            int frame = d.Random
                ? HashFrame((int)(Time * d.Fps), d.X, sheet.FrameCount)
                : (int)(Time * d.Fps) % sheet.FrameCount;
            sheet.Draw(g, frame, false, d.X, d.Y);
        }
    }

    private static int HashFrame(int tick, int seed, int count)
    {
        unchecked
        {
            uint h = (uint)(tick * 73856093) ^ (uint)(seed * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            return (int)(h % (uint)count);
        }
    }

    private void DrawLights(Graphics g)
    {
        if (Location is null || Block.Mood == SceneMood.Dark) return;
        var glow = GlowBmp.Value;
        foreach (var l in Location.Lights.Concat(Block.Lights))
        {
            float flick = l.Flicker <= 0 ? 1 : 1 - l.Flicker * (float)(_rnd.NextDouble() < 0.06 ? 0.9 : _rnd.NextDouble() * 0.25);
            float pulse = 0.9f + 0.1f * MathF.Sin(Time * 2 + l.X);
            var c = Screen2D.ParseColor(l.Color, Color.Orange);
            using var attrs = Screen2D.AlphaTint(l.Alpha / 255f * flick * pulse, c.R / 255f, c.G / 255f, c.B / 255f);
            var r = l.Radius;
            var pts = new[] { new PointF(l.X - r, l.Y - r), new PointF(l.X + r, l.Y - r), new PointF(l.X - r, l.Y + r) };
            g.DrawImage(glow, pts, new RectangleF(0, 0, glow.Width, glow.Height), GraphicsUnit.Pixel, attrs);
        }
    }

    /// <summary>
    /// Marcaje discrete, puse chiar pe obiecte: o licarire calda pentru decizii, o sclipire rece
    /// pentru lucrurile de examinat. Cel mai apropiat obiect primeste colturi luminoase.
    /// </summary>
    private void DrawMarkers(Graphics g)
    {
        if (CloseupVisible) return;
        var glow = GlowBmp.Value;
        var src = new RectangleF(0, 0, glow.Width, glow.Height);
        foreach (var h in Interactables)
        {
            if (h.IsHidden) continue;
            var p = Anchor(h);
            bool focus = h == _focus;
            bool seen = h.Examine is not null && SeenExamines.Contains(h.Examine.Id);
            // fiecare obiect are faza lui, ca sa nu clipeasca toate deodata
            float phase = Time * 2.2f + h.X * 0.13f;
            float pulse = (MathF.Sin(phase) + 1) / 2;
            Color c = h.Decision is not null
                ? (h.IsAvailable ? Color.FromArgb(255, 205, 120) : Color.FromArgb(150, 150, 165))
                : Color.FromArgb(150, 205, 255);
            float strength = focus ? 0.55f + 0.2f * _focusT : seen ? 0.10f : 0.16f + 0.16f * pulse;
            if (!h.IsAvailable) strength *= 0.6f;

            float r = focus ? 11 : 7;
            using (var attrs = Screen2D.AlphaTint(strength, c.R / 255f, c.G / 255f, c.B / 255f))
                g.DrawImage(glow, [new PointF(p.X - r, p.Y - r), new PointF(p.X + r, p.Y - r), new PointF(p.X - r, p.Y + r)], src, GraphicsUnit.Pixel, attrs);

            // sclipirea: o cruciulita de 1 px care apare si dispare (nu sta fixa ca un cursor)
            float tw = h.Decision is not null ? pulse : MathF.Max(0, MathF.Sin(phase * 0.7f) * 1.6f - 0.6f);
            int a = focus ? 255 : seen ? 0 : (int)(220 * Math.Min(1, tw));
            if (a > 8)
            {
                var b = Gfx.Brush(a, Math.Min(255, c.R + 40), Math.Min(255, c.G + 40), Math.Min(255, c.B + 40));
                float len = focus ? 2 : 1 + Math.Min(1, tw);
                g.FillRectangle(b, p.X - 0.5f, p.Y - 0.5f - len, 1, len * 2 + 1);
                g.FillRectangle(b, p.X - 0.5f - len, p.Y - 0.5f, len * 2 + 1, 1);
            }

            if (focus)
            {
                // colturi mici in jurul obiectului: „asta poti folosi”
                float k = 7 + (1 - _focusT) * 4, l = 2.5f;
                var pen = Gfx.Pen(Color.FromArgb((int)(200 * _focusT), c), 1);
                foreach (var (sx, sy) in new[] { (-1, -1), (1, -1), (-1, 1), (1, 1) })
                {
                    float cx = p.X + sx * k, cy = p.Y + sy * k;
                    g.DrawLine(pen, cx, cy, cx - sx * l, cy);
                    g.DrawLine(pen, cx, cy, cx, cy - sy * l);
                }
            }
        }
    }

    private void DrawMood(Graphics g)
    {
        var full = new RectangleF(-10, -10, Screen2D.Width + 20, Screen2D.Height + 20);
        switch (Block.Mood)
        {
            case SceneMood.Dim:
                g.FillRectangle(Gfx.Brush(85, 0, 0, 8), full);
                break;
            case SceneMood.Dark:
                var c = Player is null ? new PointF(160, 110) : new PointF(Player.X, FloorY - 26);
                Darkness.Draw(g, c, 48 + MathF.Sin(Time * 3) * 2, 236);
                break;
            case SceneMood.Red:
                g.FillRectangle(Gfx.Brush(70 + (int)(35 * (MathF.Sin(Time * 5) + 1) / 2), 150, 0, 10), full);
                g.FillRectangle(Gfx.Brush(60, 0, 0, 0), full);
                break;
            case SceneMood.Dawn:
                g.FillRectangle(Gfx.Brush(40, 255, 150, 110), full);
                break;
            case SceneMood.Sepia:
                g.FillRectangle(Gfx.Brush(30, 60, 40, 20), full);
                break;
        }
    }

    private void DrawCloseup(Graphics g)
    {
        if (Closeup is null || _closeupT <= 0 || HideCloseup) return;
        float t = 1 - MathF.Pow(1 - _closeupT, 3);
        g.FillRectangle(Gfx.Brush((int)(120 * t), 0, 0, 0), -10, -10, 340, 200);
        const float k = 0.75f; // nu mai acopera toata scena: se vede si camera din spate
        float w = Closeup.Width * k, h = Closeup.Height * k;
        float x = (Screen2D.Width - w) / 2f;
        float y = 12 + (1 - t) * 30;
        var state = g.Save();
        g.TranslateTransform(x + w / 2f, y + h / 2f);
        g.RotateTransform((1 - t) * -5 - 1.2f);
        g.TranslateTransform(-w / 2f, -h / 2f);
        g.FillRectangle(Gfx.Brush((int)(130 * t), 0, 0, 0), 3, 4, w, h);
        var srcR = new RectangleF(0, 0, Closeup.Width, Closeup.Height);
        if (t >= 0.999f) g.DrawImage(Closeup, new RectangleF(0, 0, w, h), srcR, GraphicsUnit.Pixel);
        else
        {
            using var attrs = Screen2D.AlphaTint(t);
            g.DrawImage(Closeup, [new PointF(0, 0), new PointF(w, 0), new PointF(0, h)], srcR, GraphicsUnit.Pixel, attrs);
        }
        g.Restore(state);
    }

    private static Bitmap CreateVignette() => Screen2D.Generate(Screen2D.Width / 2, Screen2D.Height / 2, (x, y) =>
    {
        float dx = (x - 80f) / 80f, dy = (y - 45f) / 45f;
        float d = MathF.Sqrt(dx * dx + dy * dy) / 1.4142f;
        int a = (int)(Math.Clamp((d - 0.45f) / 0.55f, 0, 1) * 170);
        return a << 24;
    });

    private static Bitmap CreateGlow() => Screen2D.Generate(64, 64, (x, y) =>
    {
        float d = MathF.Sqrt((x - 31.5f) * (x - 31.5f) + (y - 31.5f) * (y - 31.5f)) / 32f;
        int a = (int)(255 * Math.Clamp(1 - d, 0, 1) * Math.Clamp(1 - d, 0, 1));
        return (a << 24) | 0xFFFFFF;
    });
}
