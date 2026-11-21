using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>Metadatele povestii: titlu, autor, descriere, bloc de start, personajul jucatorului.</summary>
public sealed class StoryPage : EditorPage
{
    private readonly TextBox _title = Ui.Text(), _author = Ui.Text(), _desc = Ui.Text(true);
    private readonly ComboBox _start, _player;
    private readonly TextBox _order = Ui.Text(), _maxPlayers = Ui.Text();
    private readonly DataGridView _playable = Ui.Grid();

    public StoryPage(EditorContext ctx) : base(ctx)
    {
        var s = ctx.Story;
        _maxPlayers.Text = s.MaxPlayers.ToString();
        Ui.AddCombo(_playable, "id", "Personaj jucabil", s.Characters.Select(c => c.Id), 90);
        Ui.AddText(_playable, "role", "Rol (pe cartonaș)", 120);
        Ui.AddText(_playable, "blurb", "Descriere scurtă", 260);
        foreach (var p in s.Playable) _playable.Rows.Add(p.CharacterId, p.Role, p.Blurb);
        _playable.Dock = DockStyle.Fill;
        _start = Ui.Combo(ctx.BlockIds);
        _player = Ui.Combo(s.Characters.Select(c => c.Id));
        _title.Text = s.Title;
        _author.Text = s.Author;
        _order.Text = s.Order.ToString();
        _desc.Text = s.Description.Replace("\n", Environment.NewLine);
        _desc.Height = 110;
        Ui.Select(_start, s.StartBlockId);
        Ui.Select(_player, s.PlayerCharacterId);

        var form = Form(2);
        Row(form, ("Titlu", _title), ("Autor", _author), ("Descriere", _desc), ("Bloc de start", _start),
            ("Personajul jucătorului", _player), ("Ordine în meniu", _order), ("Jucători pe tură", _maxPlayers));
        var stats = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 90,
            ForeColor = Color.FromArgb(160, 150, 145),
            Padding = new Padding(4, 14, 0, 0),
            Text = $"Blocuri: {s.Blocks.Count}   ·   Decizii: {s.Blocks.Sum(b => b.Decisions.Count)}   ·   " +
                   $"Finaluri: {s.Blocks.Count(b => b.IsEnding)}   ·   Proprietăți: {s.Properties.Count}   ·   " +
                   $"Locații: {s.Locations.Count}   ·   Personaje: {s.Characters.Count}   ·   Imagini: {ctx.ImagePaths.Length}",
        };
        var playableLabel = new Label
        {
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.FromArgb(170, 162, 156),
            Text = "Personaje care pot fi alese din meniu: protagonistul spune povestea, ceilalți sunt însoțitori.",
        };
        Controls.Add(_playable);
        Controls.Add(playableLabel);
        Controls.Add(stats);
        Controls.Add(form);
        Controls.Add(Title("POVESTE", "Datele generale ale poveștii."));
    }

    public override string? Commit()
    {
        var s = Ctx.Story;
        s.Title = _title.Text.Trim();
        s.Author = _author.Text.Trim();
        s.Description = _desc.Text.Replace("\r\n", "\n").Trim();
        s.StartBlockId = _start.SelectedItem?.ToString() ?? "";
        s.PlayerCharacterId = _player.SelectedItem?.ToString() ?? "";
        if (!int.TryParse(_order.Text.Trim(), out int order)) return "Ordinea trebuie să fie un număr întreg.";
        s.Order = order;
        if (!int.TryParse(_maxPlayers.Text.Trim(), out int max) || max < 1) return "Jucători pe tură trebuie să fie un număr de la 1 în sus.";
        s.MaxPlayers = max;
        s.Playable = Ui.Rows(_playable).Where(r => Ui.Cell(r, "id").Length > 0)
            .Select(r => new PlayableDefinition { CharacterId = Ui.Cell(r, "id"), Role = Ui.Cell(r, "role"), Blurb = Ui.Cell(r, "blurb") })
            .ToList();
        return null;
    }
}
