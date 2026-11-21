using LastStopNight.Core.Expressions;
using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>
/// Editarea unui bloc narativ: date, text, prezentare 2D (locatie, personaje, efecte) si
/// tabelul de decizii (destinatie, conditie, efecte, hotspot). Include previzualizarea scenei.
/// </summary>
public sealed class BlockPage : EditorPage
{
    private readonly StoryBlock _block;
    private readonly TextBox _id = Ui.Text(), _title = Ui.Text(), _time = Ui.Text(), _chapter = Ui.Text();
    private readonly TextBox _fx = Ui.Text(), _playerX = Ui.Text(), _chars = Ui.Text(), _endingTitle = Ui.Text();
    private readonly TextBox _text = Ui.Text(true);
    private readonly ComboBox _location, _background, _closeup, _mood, _endingType;
    private readonly CheckBox _isEnding = new() { Text = "Bloc final", ForeColor = Ui.Fore, AutoSize = true, Margin = new Padding(3, 6, 3, 3) };
    private readonly DataGridView _grid = Ui.Grid();
    private readonly DataGridView _examine = Ui.Grid();
    private readonly DataGridView _companion = Ui.Grid();
    private readonly CheckBox _solo = new() { Text = "Protagonistul e singur în scenă (însoțitorii nu apar)", ForeColor = Ui.Fore, AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(4, 4, 0, 4) };
    private readonly Panel _preview = new() { Width = 480, Height = 270, BackColor = Color.Black, Margin = new Padding(10, 3, 3, 3) };

    public BlockPage(EditorContext ctx, StoryBlock block, int focusDecision = -1) : base(ctx)
    {
        _block = block;
        var images = ctx.ImagePaths;
        _location = Ui.Combo(["", .. ctx.Story.Locations.Select(l => l.Id)]);
        _background = Ui.Combo(["", .. images]);
        _closeup = Ui.Combo(["", .. images]);
        _mood = Ui.Combo(Enum.GetNames<SceneMood>());
        _endingType = Ui.Combo(Enum.GetNames<EndingType>());

        _id.Text = block.Id;
        _title.Text = block.Title;
        _time.Text = block.Time;
        _chapter.Text = block.Chapter ?? "";
        Ui.Select(_location, block.Location);
        Ui.Select(_background, block.Background);
        Ui.Select(_closeup, block.Closeup);
        Ui.Select(_mood, block.Mood.ToString());
        _fx.Text = string.Join(", ", block.Fx);
        _playerX.Text = block.PlayerX?.ToString() ?? "";
        _chars.Text = FormatCharacters(block.Characters);
        _isEnding.Checked = block.IsEnding;
        Ui.Select(_endingType, block.EndingType.ToString());
        _endingTitle.Text = block.EndingTitle ?? "";
        _text.Text = block.Text.Replace("\n", Environment.NewLine);

        // decizii
        Ui.AddText(_grid, "text", "Text decizie", 170);
        var target = Ui.AddCombo(_grid, "target", "Destinație", ctx.BlockIds, 110);
        Ui.AddText(_grid, "cond", "Condiție (ex. story.evidence >= 2 AND player.life > 0)", 150);
        Ui.AddText(_grid, "effects", "Efecte (ex. player.life -20; story.night = 2)", 150);
        Ui.AddText(_grid, "hotspot", "Hotspot X", 45);
        Ui.AddText(_grid, "hy", "Y", 30);
        Ui.AddText(_grid, "hint", "Mesaj când e blocată", 100);
        Ui.AddText(_grid, "req", "Doar cu", 55);
        Ui.AddCheck(_grid, "show", "Arată blocată", 40);
        Ui.EnsureItems(target, block.Decisions.Select(d => d.Target));
        foreach (var d in block.Decisions)
            _grid.Rows.Add(d.Text, d.Target, d.Condition?.ToExpression() ?? "", EffectParser.Format(d.Effects),
                d.HotspotX?.ToString() ?? "", d.HotspotY?.ToString() ?? "", d.LockedHint ?? "", d.RequiresCharacter ?? "", d.ShowWhenLocked);
        _grid.DefaultValuesNeeded += (_, e) => e.Row.Cells["show"].Value = true;

        // obiecte de examinat (tasta E): text, efecte o singura data, ascunse = easter egg
        Ui.AddText(_examine, "id", "ID", 70);
        Ui.AddText(_examine, "x", "X", 35);
        Ui.AddText(_examine, "y", "Y", 30);
        Ui.AddText(_examine, "label", "Nume", 90);
        Ui.AddText(_examine, "text", "Ce vede personajul", 260);
        Ui.AddText(_examine, "effects", "Efecte", 110);
        Ui.AddText(_examine, "cond", "Condiție", 90);
        Ui.AddText(_examine, "by", "Ce zic însoțitorii (id: text || id: text)", 160);
        Ui.AddCheck(_examine, "once", "O dată", 35);
        Ui.AddCheck(_examine, "hidden", "Ascuns", 35);
        foreach (var ex in block.Examine)
            _examine.Rows.Add(ex.Id, ex.X, ex.Y?.ToString() ?? "", ex.Label, ex.Text, EffectParser.Format(ex.Effects), ex.Condition?.ToExpression() ?? "",
                FormatPairs(ex.TextBy), ex.Once, ex.Hidden);
        _examine.DefaultValuesNeeded += (_, e) => e.Row.Cells["once"].Value = true;
        _examine.CellValueChanged += (_, _) => _preview.Invalidate();
        if (focusDecision >= 0 && focusDecision < _grid.Rows.Count)
            Load += (_, _) => { _grid.ClearSelection(); _grid.Rows[focusDecision].Selected = true; };

        // layout
        var form = Form(4);
        form.Dock = DockStyle.Fill;
        Row(form, ("ID", _id), ("Titlu", _title));
        Row(form, ("Ora", _time), ("Capitol", _chapter));
        Row(form, ("Locație", _location), ("Atmosferă", _mood));
        Row(form, ("Fundal propriu", _background), ("Prim-plan", _closeup));
        Row(form, ("Efecte (fx)", _fx), ("Alex la X", _playerX));
        Row(form, ("Personaje", _chars), ("", _isEnding));
        Row(form, ("Tip final", _endingType), ("Titlu final", _endingTitle));

        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 280, ColumnCount = 2 };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.Controls.Add(form, 0, 0);
        top.Controls.Add(_preview, 1, 0);

        var textPanel = new Panel { Dock = DockStyle.Top, Height = 150, Padding = new Padding(0, 4, 0, 6) };
        textPanel.Controls.Add(_text);
        textPanel.Controls.Add(new Label { Text = "Textul scenei", Dock = DockStyle.Top, Height = 20, ForeColor = Color.FromArgb(170, 162, 156) });

        var gridLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(170, 162, 156),
            Text = "Decizii. Hotspot X = punctul din scenă unde personajul declanșează decizia cu tasta E (gol = doar buton).",
        };
        var tabs = new TabControl { Dock = DockStyle.Fill };
        var tabDecisions = new TabPage("Decizii") { BackColor = Ui.Back };
        tabDecisions.Controls.Add(_grid);
        tabDecisions.Controls.Add(gridLabel);
        var tabExamine = new TabPage($"Obiecte de examinat ({block.Examine.Count})") { BackColor = Ui.Back };
        tabExamine.Controls.Add(_examine);
        tabExamine.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(170, 162, 156),
            Text = "Lucruri la care personajul se uită cu E. Bifa Ascuns = fără marcaj în scenă (easter egg).",
        });
        Ui.AddText(_companion, "id", "Însoțitor (ID)", 60);
        Ui.AddText(_companion, "text", "Paragraf adăugat la text când e în tură", 300);
        foreach (var kv in block.CompanionText) _companion.Rows.Add(kv.Key, kv.Value.Replace("\n", Environment.NewLine));
        _solo.Checked = block.Solo;
        var tabCompanion = new TabPage($"Însoțitori ({block.CompanionText.Count})") { BackColor = Ui.Back };
        tabCompanion.Controls.Add(_companion);
        tabCompanion.Controls.Add(_solo);
        tabs.TabPages.Add(tabDecisions);
        tabs.TabPages.Add(tabExamine);
        tabs.TabPages.Add(tabCompanion);
        Controls.Add(tabs);
        Controls.Add(textPanel);
        Controls.Add(top);
        Controls.Add(Title("BLOC NARATIV", "Personaje: id@x, cu „<” pentru privire spre stânga (ex. faceless@112, red_woman@246<). Efecte: flicker, shake, ring, rain."));

        _preview.Paint += PaintPreview;
        foreach (Control c in new Control[] { _location, _background, _closeup, _mood, _playerX, _chars, _isEnding })
            c.TextChanged += (_, _) => _preview.Invalidate();
        _isEnding.CheckedChanged += (_, _) => _preview.Invalidate();
        _grid.CellValueChanged += (_, _) => _preview.Invalidate();
    }

    /// <summary>„ioana: text || vlad: text” ↔ dictionar (personaj → text).</summary>
    private static string FormatPairs(Dictionary<string, string> d) => string.Join(" || ", d.Select(kv => $"{kv.Key}: {kv.Value}"));

    private static Dictionary<string, string> ParsePairs(string s) =>
        s.Split("||", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
         .Select(p => p.Split(':', 2))
         .Where(kv => kv.Length == 2 && kv[0].Trim().Length > 0)
         .GroupBy(kv => kv[0].Trim()).ToDictionary(g => g.Key, g => g.First()[1].Trim());

    private static string FormatCharacters(IEnumerable<CharacterPlacement> list) =>
        string.Join(", ", list.Select(c => $"{c.Id}@{c.X}{(c.FacingLeft ? "<" : "")}"));

    private static List<CharacterPlacement> ParseCharacters(string s) =>
        s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(part =>
        {
            var kv = part.Split('@', 2);
            bool left = kv.Length == 2 && kv[1].EndsWith('<');
            if (kv.Length != 2 || !int.TryParse(kv[1].TrimEnd('<'), out int x))
                throw new FormatException($"Personaj nevalid „{part}”. Formă: id@x sau id@x<");
            return new CharacterPlacement { Id = kv[0].Trim(), X = x, FacingLeft = left };
        }).ToList();

    /// <summary>Previzualizare 2D a blocului: fundal, personaje, Alex, hotspot-uri si prim-plan.</summary>
    private void PaintPreview(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        var s = Ctx.Story;
        float k = _preview.Width / 320f;
        Ui.PixelArt(g);
        var loc = s.FindLocation(_location.Text);
        string? bgPath = string.IsNullOrWhiteSpace(_background.Text) ? loc?.Image : _background.Text;
        if (Ctx.GetImage(bgPath) is { } bg) g.DrawImage(bg, 0, 0, 320 * k, 180 * k);
        else g.DrawString("(fără fundal)", Font, Brushes.Gray, 10, 10);
        int floor = loc?.FloorY ?? 170;

        void DrawChar(string id, int x, bool left, int alpha)
        {
            if (s.FindCharacter(id) is not { } def || Ctx.GetImage(def.Sprite) is not { } sheet) return;
            var dest = new RectangleF((x - def.FrameWidth / 2f) * k, (floor - def.FrameHeight + 2) * k, def.FrameWidth * k, def.FrameHeight * k);
            if (left) { dest.X += dest.Width; dest.Width = -dest.Width; }
            using var attrs = new System.Drawing.Imaging.ImageAttributes();
            attrs.SetColorMatrix(new System.Drawing.Imaging.ColorMatrix { Matrix33 = alpha / 255f });
            var pts = new[] { new PointF(dest.Left, dest.Top), new PointF(dest.Right, dest.Top), new PointF(dest.Left, dest.Bottom) };
            g.DrawImage(sheet, pts, new RectangleF(0, 0, def.FrameWidth, def.FrameHeight), GraphicsUnit.Pixel, attrs);
        }

        if (!_isEnding.Checked)
            foreach (var p in _block.Props)
                if (Ctx.GetImage(p.Image) is { } pim)
                {
                    int fw = p.FrameWidth > 0 ? p.FrameWidth : pim.Width, fh = p.FrameHeight > 0 ? p.FrameHeight : pim.Height;
                    g.DrawImage(pim, new RectangleF(p.X * k, p.Y * k, fw * k, fh * k), new RectangleF(0, 0, fw, fh), GraphicsUnit.Pixel);
                }
        try { foreach (var c in ParseCharacters(_chars.Text)) DrawChar(c.Id, c.X, c.FacingLeft, 255); } catch (FormatException) { }
        if (!_isEnding.Checked && loc is not null && int.TryParse(_playerX.Text, out int px))
            DrawChar(s.PlayerCharacterId, px, px > 160, 200);

        using var look = new SolidBrush(Color.FromArgb(230, 150, 200, 255));
        foreach (var r in Ui.Rows(_examine))
            if (int.TryParse(Ui.Cell(r, "x"), out int ex))
            {
                int ey = int.TryParse(Ui.Cell(r, "y"), out int yy) ? yy : floor - 34;
                e.Graphics.FillEllipse(look, ex * k - 3, ey * k - 3, 6, 6);
            }
        using var hot = new SolidBrush(Color.FromArgb(230, 255, 214, 130));
        using var font = new Font("Segoe UI", 7.5f);
        foreach (var r in Ui.Rows(_grid))
            if (int.TryParse(Ui.Cell(r, "hotspot"), out int hx))
            {
                float x = hx * k, y = (int.TryParse(Ui.Cell(r, "hy"), out int hy) ? hy : floor - 34) * k;
                g.FillPolygon(hot, [new PointF(x, y - 5), new PointF(x + 5, y), new PointF(x, y + 5), new PointF(x - 5, y)]);
                g.DrawString((r.Index + 1).ToString(), font, hot, x + 6, y - 7);
            }

        if (Ctx.GetImage(_closeup.Text) is { } cu)
        {
            using var dim = new SolidBrush(Color.FromArgb(110, 0, 0, 0));
            g.FillRectangle(dim, 0, 0, _preview.Width, _preview.Height);
            g.DrawImage(cu, (320 - cu.Width) / 2f * k, 18 * k, cu.Width * k, cu.Height * k);
        }
        using var moodFont = new Font("Segoe UI", 8f, FontStyle.Bold);
        g.DrawString(_mood.Text + (string.IsNullOrWhiteSpace(_fx.Text) ? "" : " · " + _fx.Text), moodFont, Brushes.White, 6, _preview.Height - 18);
    }

    public override string? Commit()
    {
        try
        {
            string id = _id.Text.Trim();
            if (id.Length == 0) return "Blocul trebuie să aibă un ID.";
            if (id != _block.Id && Ctx.Story.Blocks.Any(b => b != _block && b.Id == id)) return $"Există deja un bloc cu ID-ul „{id}”.";

            var decisions = new List<DecisionDefinition>();
            foreach (var r in Ui.Rows(_grid))
            {
                if (!ConditionParser.TryParse(Ui.Cell(r, "cond"), out var cond, out var err))
                    return $"Decizia {r.Index + 1}: {err}";
                List<EffectDefinition> effects;
                try { effects = EffectParser.Parse(Ui.Cell(r, "effects")); }
                catch (ExpressionException ex) { return $"Decizia {r.Index + 1}: {ex.Message}"; }
                decisions.Add(new DecisionDefinition
                {
                    Text = Ui.Cell(r, "text"),
                    Target = Ui.Cell(r, "target"),
                    Condition = cond,
                    Effects = effects,
                    HotspotX = Ui.OptInt(Ui.Cell(r, "hotspot"), $"Hotspot X (decizia {r.Index + 1})"),
                    HotspotY = Ui.OptInt(Ui.Cell(r, "hy"), $"Hotspot Y (decizia {r.Index + 1})"),
                    LockedHint = PropertiesPage.NullIfEmpty(Ui.Cell(r, "hint")),
                    RequiresCharacter = PropertiesPage.NullIfEmpty(Ui.Cell(r, "req")),
                    ShowWhenLocked = r.Cells["show"].Value is not false,
                });
            }

            var examines = new List<ExamineDefinition>();
            foreach (var r in Ui.Rows(_examine))
            {
                if (!ConditionParser.TryParse(Ui.Cell(r, "cond"), out var econd, out var eerr))
                    return $"Obiectul {r.Index + 1}: {eerr}";
                List<EffectDefinition> eff;
                try { eff = EffectParser.Parse(Ui.Cell(r, "effects")); }
                catch (ExpressionException ex) { return $"Obiectul {r.Index + 1}: {ex.Message}"; }
                examines.Add(new ExamineDefinition
                {
                    Id = Ui.Cell(r, "id"),
                    X = Ui.Int(r, "x", "X"),
                    Y = Ui.OptInt(Ui.Cell(r, "y"), "Y"),
                    Label = Ui.Cell(r, "label"),
                    Text = Ui.Cell(r, "text"),
                    Effects = eff,
                    Condition = econd,
                    Once = r.Cells["once"].Value is not false,
                    Hidden = Ui.Check(r, "hidden"),
                    TextBy = ParsePairs(Ui.Cell(r, "by")),
                });
            }

            var chars = ParseCharacters(_chars.Text);
            int? playerX = Ui.OptInt(_playerX.Text, "Alex la X");

            StructureChanged = id != _block.Id || _title.Text.Trim() != _block.Title || _isEnding.Checked != _block.IsEnding
                               || decisions.Count != _block.Decisions.Count
                               || decisions.Select(d => d.Target + d.Text).SequenceEqual(_block.Decisions.Select(d => d.Target + d.Text)) == false;
            if (id != _block.Id) Ctx.RenameBlock(_block.Id, id);

            _block.Id = id;
            _block.Title = _title.Text.Trim();
            _block.Time = _time.Text.Trim();
            _block.Chapter = PropertiesPage.NullIfEmpty(_chapter.Text.Trim());
            _block.Location = PropertiesPage.NullIfEmpty(_location.Text);
            _block.Background = PropertiesPage.NullIfEmpty(_background.Text);
            _block.Closeup = PropertiesPage.NullIfEmpty(_closeup.Text);
            _block.Mood = Enum.TryParse<SceneMood>(_mood.Text, out var m) ? m : SceneMood.Normal;
            _block.Fx = _fx.Text.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries).ToList();
            _block.PlayerX = playerX;
            _block.Characters = chars;
            _block.IsEnding = _isEnding.Checked;
            _block.EndingType = Enum.TryParse<EndingType>(_endingType.Text, out var et) ? et : EndingType.None;
            _block.EndingTitle = PropertiesPage.NullIfEmpty(_endingTitle.Text.Trim());
            _block.Text = _text.Text.Replace("\r\n", "\n").Trim();
            _block.Decisions = decisions;
            _block.Examine = examines;
            _block.Solo = _solo.Checked;
            _block.CompanionText = Ui.Rows(_companion).Where(r => Ui.Cell(r, "id").Length > 0)
                .GroupBy(r => Ui.Cell(r, "id"))
                .ToDictionary(g => g.Key, g => Ui.Cell(g.First(), "text").Replace("\r\n", "\n"));
            return null;
        }
        catch (FormatException ex) { return ex.Message; }
    }
}
