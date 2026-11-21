using LastStopNight.Core.Expressions;
using LastStopNight.Core.Model;

namespace LastStopNight.Editor.Pages;

/// <summary>Redirectionari automate (ex. player.life &lt;= 0 → ending.captured).</summary>
public sealed class RedirectsPage : EditorPage
{
    private readonly DataGridView _grid = Ui.Grid();

    public RedirectsPage(EditorContext ctx) : base(ctx)
    {
        Ui.AddText(_grid, "cond", "Condiție", 160);
        var target = Ui.AddCombo(_grid, "target", "Bloc destinație", ctx.BlockIds, 110);
        Ui.AddText(_grid, "desc", "Descriere", 160);
        Ui.EnsureItems(target, ctx.Story.Redirects.Select(r => r.Target));
        foreach (var r in ctx.Story.Redirects)
            _grid.Rows.Add(r.Condition?.ToExpression() ?? "", r.Target, r.Description);
        Controls.Add(_grid);
        Controls.Add(Title("REDIRECȚIONĂRI AUTOMATE",
            "Verificate după fiecare decizie. Condiții: cheie op număr, combinate cu AND / OR și paranteze."));
    }

    public override string? Commit()
    {
        var list = new List<RedirectDefinition>();
        foreach (var r in Ui.Rows(_grid))
        {
            if (!ConditionParser.TryParse(Ui.Cell(r, "cond"), out var c, out var err))
                return $"Rândul {r.Index + 1}: {err}";
            list.Add(new RedirectDefinition { Condition = c, Target = Ui.Cell(r, "target"), Description = Ui.Cell(r, "desc") });
        }
        Ctx.Story.Redirects = list;
        return null;
    }
}
