namespace LastStopNight.Editor.Pages;

/// <summary>
/// Pagina de editare afisata in dreapta arborelui. Fiecare pagina isi incarca datele din model
/// la construire si le scrie inapoi in <see cref="Commit"/> (validand formatul).
/// </summary>
public abstract class EditorPage : UserControl
{
    protected EditorPage(EditorContext ctx)
    {
        Ctx = ctx;
        Dock = DockStyle.Fill;
        BackColor = Ui.Back;
        ForeColor = Ui.Fore;
        Padding = new Padding(10);
    }

    protected EditorContext Ctx { get; }

    /// <summary>Scrie modificarile in model. Returneaza un mesaj de eroare sau null daca totul e valid.</summary>
    public abstract string? Commit();

    /// <summary>True daca ultima salvare a modificat structura (ID-uri, liste) si arborele trebuie refacut.</summary>
    public bool StructureChanged { get; protected set; }

    protected static TableLayoutPanel Form(int columns)
    {
        var t = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = columns };
        for (int i = 0; i < columns; i++)
            t.ColumnStyles.Add(i % 2 == 0 ? new ColumnStyle(SizeType.AutoSize) : new ColumnStyle(SizeType.Percent, 100f / (columns / 2)));
        return t;
    }

    protected static void Row(TableLayoutPanel t, params (string Label, Control Control)[] cells)
    {
        foreach (var (label, control) in cells)
        {
            t.Controls.Add(Ui.Label(label));
            t.Controls.Add(control);
        }
    }

    protected static Label Title(string text, string help)
    {
        return new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = 46,
            ForeColor = Ui.Fore,
            Text = text + Environment.NewLine + help,
            Font = new Font("Segoe UI", 9f),
            Padding = new Padding(0, 0, 0, 6),
        };
    }
}
