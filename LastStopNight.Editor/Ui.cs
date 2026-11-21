using System.Drawing.Drawing2D;

namespace LastStopNight.Editor;

/// <summary>Fabrica de controale cu stilul editorului.</summary>
public static class Ui
{
    public static readonly Color Back = Color.FromArgb(30, 28, 32);
    public static readonly Color Panel = Color.FromArgb(40, 37, 42);
    public static readonly Color Fore = Color.FromArgb(226, 220, 212);
    public static readonly Color Accent = Color.FromArgb(200, 50, 60);

    public static Label Label(string text, bool bold = false) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = bold ? Accent : Color.FromArgb(170, 162, 156),
        Font = new Font("Segoe UI", bold ? 11f : 9f, bold ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(3, 7, 3, 3),
    };

    public static TextBox Text(bool multiline = false) => new()
    {
        Dock = DockStyle.Fill,
        Multiline = multiline,
        ScrollBars = multiline ? ScrollBars.Vertical : ScrollBars.None,
        BackColor = Panel,
        ForeColor = Fore,
        BorderStyle = BorderStyle.FixedSingle,
        Font = new Font("Segoe UI", 9.5f),
        AcceptsReturn = multiline,
    };

    public static ComboBox Combo(IEnumerable<string> items, bool editable = false)
    {
        var c = new ComboBox
        {
            Dock = DockStyle.Fill,
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
            BackColor = Panel,
            ForeColor = Fore,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
        };
        c.Items.AddRange(items.Cast<object>().ToArray());
        return c;
    }

    public static void Select(ComboBox c, string? value)
    {
        value ??= "";
        if (!c.Items.Contains(value)) c.Items.Insert(0, value);
        c.SelectedItem = value;
    }

    public static Button Button(string text, EventHandler onClick)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            FlatStyle = FlatStyle.Flat,
            BackColor = Panel,
            ForeColor = Fore,
            Margin = new Padding(3),
            Padding = new Padding(6, 2, 6, 2),
        };
        b.FlatAppearance.BorderColor = Color.FromArgb(90, 40, 46);
        b.Click += onClick;
        return b;
    }

    public static DataGridView Grid()
    {
        var g = new DataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = true,
            AllowUserToDeleteRows = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersWidth = 30,
            BackgroundColor = Back,
            GridColor = Color.FromArgb(60, 56, 62),
            BorderStyle = BorderStyle.None,
            EditMode = DataGridViewEditMode.EditOnEnter,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
        };
        g.DefaultCellStyle.BackColor = Panel;
        g.DefaultCellStyle.ForeColor = Fore;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(90, 34, 40);
        g.DefaultCellStyle.SelectionForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 30, 34);
        g.ColumnHeadersDefaultCellStyle.ForeColor = Fore;
        g.RowHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 30, 34);
        g.DataError += (_, e) => e.ThrowException = false; // valori necunoscute in coloanele combo
        return g;
    }

    public static void AddText(DataGridView g, string name, string header, float weight = 100)
    {
        g.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = header, FillWeight = weight });
    }

    public static void AddCheck(DataGridView g, string name, string header, float weight = 40)
    {
        g.Columns.Add(new DataGridViewCheckBoxColumn { Name = name, HeaderText = header, FillWeight = weight });
    }

    public static DataGridViewComboBoxColumn AddCombo(DataGridView g, string name, string header, IEnumerable<string> items, float weight = 100)
    {
        var col = new DataGridViewComboBoxColumn
        {
            Name = name,
            HeaderText = header,
            FillWeight = weight,
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
        };
        col.Items.AddRange(items.Distinct().Cast<object>().ToArray());
        g.Columns.Add(col);
        return col;
    }

    /// <summary>Adauga in coloana combo valorile existente care nu sunt in lista (evita DataError).</summary>
    public static void EnsureItems(DataGridViewComboBoxColumn col, IEnumerable<string?> values)
    {
        foreach (var v in values.Select(v => v ?? "").Distinct())
            if (!col.Items.Contains(v)) col.Items.Add(v);
    }

    public static IEnumerable<DataGridViewRow> Rows(DataGridView g) =>
        g.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow);

    public static string Cell(DataGridViewRow r, string col) => r.Cells[col].Value?.ToString()?.Trim() ?? "";

    public static bool Check(DataGridViewRow r, string col) => r.Cells[col].Value is true;

    public static int Int(DataGridViewRow r, string col, string what)
    {
        var s = Cell(r, col);
        if (!int.TryParse(s, out int v)) throw new FormatException($"Rândul {r.Index + 1}: „{what}” trebuie să fie un număr întreg (acum: „{s}”).");
        return v;
    }

    public static int? OptInt(string s, string what)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        if (!int.TryParse(s.Trim(), out int v)) throw new FormatException($"„{what}” trebuie să fie un număr întreg sau gol.");
        return v;
    }

    public static int[] IntList(string s) =>
        s.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

    public static void PixelArt(Graphics g)
    {
        g.InterpolationMode = InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode = PixelOffsetMode.Half;
    }
}
