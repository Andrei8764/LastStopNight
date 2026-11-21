using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>Proprietatile numerice ale starii si afisarea lor in HUD.</summary>
public sealed class PropertiesPage : EditorPage
{
    private readonly DataGridView _grid = Ui.Grid();

    public PropertiesPage(EditorContext ctx) : base(ctx)
    {
        Ui.AddText(_grid, "key", "Cheie", 130);
        Ui.AddText(_grid, "min", "Min", 40);
        Ui.AddText(_grid, "max", "Max", 40);
        Ui.AddText(_grid, "init", "Inițial", 45);
        Ui.AddText(_grid, "label", "Etichetă HUD", 110);
        Ui.AddCombo(_grid, "display", "Afișare", Enum.GetNames<HudDisplay>(), 60);
        var icon = Ui.AddCombo(_grid, "icon", "Iconiță", ["", .. ctx.ImagesIn("images/ui/")], 110);
        Ui.AddText(_grid, "color", "Culoare", 60);
        Ui.AddText(_grid, "format", "Format", 50);
        Ui.AddCheck(_grid, "shared", "La comun", 45);
        Ui.EnsureItems(icon, ctx.Story.Properties.Select(p => p.Icon));
        foreach (var p in ctx.Story.Properties)
            _grid.Rows.Add(p.Key, p.Min, p.Max, p.Initial, p.HudLabel, p.Display.ToString(), p.Icon ?? "", p.Color ?? "", p.Format ?? "", p.Shared);
        Controls.Add(_grid);
        Controls.Add(Title("PROPRIETĂȚI DE STARE",
            "Valori numerice limitate la [min, max]. Afișare: Bar / Value / Flag / Hidden. Format: {0} = valoarea (ex. ${0}). " +
            "La comun: în multiplayer, pe drumuri separate, ce găsește un drum (ex. indiciile) se adună și la ceilalți."));
    }

    public override string? Commit()
    {
        try
        {
            var list = Ui.Rows(_grid).Select(r => new StatePropertyDefinition
            {
                Key = Ui.Cell(r, "key"),
                Min = Ui.Int(r, "min", "Min"),
                Max = Ui.Int(r, "max", "Max"),
                Initial = Ui.Int(r, "init", "Inițial"),
                HudLabel = Ui.Cell(r, "label"),
                Display = Enum.TryParse<HudDisplay>(Ui.Cell(r, "display"), out var d) ? d : HudDisplay.Value,
                Icon = NullIfEmpty(Ui.Cell(r, "icon")),
                Color = NullIfEmpty(Ui.Cell(r, "color")),
                Format = NullIfEmpty(Ui.Cell(r, "format")),
                Shared = Ui.Check(r, "shared"),
            }).ToList();
            if (list.Any(p => p.Key.Length == 0)) return "Fiecare proprietate trebuie să aibă o cheie.";
            Ctx.Story.Properties = list;
            return null;
        }
        catch (FormatException ex) { return ex.Message; }
    }

    internal static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
