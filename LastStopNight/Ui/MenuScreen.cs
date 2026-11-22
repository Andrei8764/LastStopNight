namespace LastStopNight.Ui;

/// <summary>O optiune de meniu: eticheta, valoare optionala (setari) si actiunile pe Enter / stanga-dreapta.</summary>
public sealed class MenuItem(string label, Action? activate = null)
{
    public string Label { get; set; } = label;
    public Action? Activate { get; set; } = activate;
    /// <summary>Valoarea afisata in dreapta (ex. „Normala”); null = optiune simpla.</summary>
    public Func<string>? Value { get; init; }
    /// <summary>Stanga (-1) / dreapta (+1) schimba valoarea.</summary>
    public Action<int>? Adjust { get; init; }
    public bool Enabled { get; init; } = true;
    /// <summary>Text mic sub meniu cand optiunea e selectata.</summary>
    public string? Hint { get; init; }
    /// <summary>Optiunea primeste litere de la tastatura (ex. numele jucatorului).</summary>
    public Action<char>? TypeChar { get; init; }
}

/// <summary>
/// Un ecran de meniu desenat direct peste scena (nu controale Windows): navigare cu sageti, Enter,
/// Esc si mouse. Ecranele speciale (povesti, personaje, finaluri) isi deseneaza singure continutul
/// prin <see cref="Draw"/>, iar lista de optiuni ramane pentru navigare.
/// </summary>
public class MenuScreen(string title)
{
    private readonly List<RectangleF> _hits = new();

    public string Title { get; set; } = title;
    public string? Subtitle { get; set; }
    public List<MenuItem> Items { get; } = new();
    public int Selected { get; set; }
    public Action? Back { get; set; }
    /// <summary>Desenare proprie (in loc de lista verticala); primeste dreptunghiurile de click de completat.</summary>
    public Action<Graphics, RectangleF, float, List<RectangleF>>? Draw { get; set; }
    /// <summary>Navigarea e pe orizontala (cartonase).</summary>
    public bool Horizontal { get; set; }
    public float Time { get; set; }
    /// <summary>Apelat cand se schimba optiunea selectata (ex. fundalul se schimba dupa poveste).</summary>
    public Action<int>? SelectionChanged { get; set; }
    /// <summary>Reda sunetele meniului: „ui_move”, „ui_confirm”, „ui_back”.</summary>
    public Action<string>? Sound { get; set; }

    public MenuItem? Current => Selected >= 0 && Selected < Items.Count ? Items[Selected] : null;

    public void Move(int dir)
    {
        if (Items.Count == 0) return;
        int before = Selected;
        for (int i = 0; i < Items.Count; i++)
        {
            Selected = (Selected + dir + Items.Count) % Items.Count;
            if (Items[Selected].Enabled) break;
        }
        if (Selected != before)
        {
            Sound?.Invoke("ui_move");
            SelectionChanged?.Invoke(Selected);
        }
    }

    public void EnsureValid()
    {
        if (Items.Count == 0) { Selected = 0; return; }
        Selected = Math.Clamp(Selected, 0, Items.Count - 1);
        if (!Items[Selected].Enabled) Move(1);
    }

    /// <summary>Tastele de navigare. Intoarce true daca tasta a fost folosita.</summary>
    public bool HandleKey(Keys key)
    {
        var cur = Current;
        switch (key)
        {
            case Keys.Up when !Horizontal:
            case Keys.Left when Horizontal:
                Move(-1); return true;
            case Keys.Down when !Horizontal:
            case Keys.Right when Horizontal:
                Move(1); return true;
            case Keys.Left when cur?.Adjust is not null:
                Sound?.Invoke("ui_move");
                cur.Adjust(-1); return true;
            case Keys.Right when cur?.Adjust is not null:
                Sound?.Invoke("ui_move");
                cur.Adjust(1); return true;
            case Keys.Enter or Keys.Space when cur is { Enabled: true }:
                Use(cur);
                return true;
            case Keys.Escape when Back is not null:
                Sound?.Invoke("ui_back");
                Back(); return true;
            case Keys.Back when cur?.TypeChar is not null:
                cur.TypeChar('\b'); return true;
        }
        return false;
    }

    public void HandleChar(char c) => Current?.TypeChar?.Invoke(c);

    /// <summary>Mouse-ul deasupra unei optiuni o selecteaza.</summary>
    public void Hover(PointF p)
    {
        for (int i = 0; i < _hits.Count && i < Items.Count; i++)
            if (_hits[i].Contains(p) && Items[i].Enabled)
            {
                if (Selected != i) { Selected = i; Sound?.Invoke("ui_move"); SelectionChanged?.Invoke(i); }
                return;
            }
    }

    /// <summary>
    /// Unde e desenata valoarea fiecarei optiuni reglabile („&lt;  80%  &gt;”): click in jumatatea stanga scade,
    /// in cea dreapta creste (altfel click-ul pe „&lt;” ar fi crescut valoarea).
    /// </summary>
    private readonly Dictionary<int, RectangleF> _valueHits = new();

    /// <summary>Desenarea anunta zona valorii optiunii <paramref name="index"/> (vezi <see cref="Click"/>).</summary>
    public void AddValueHit(int index, RectangleF area) => _valueHits[index] = area;

    public bool Click(PointF p)
    {
        for (int i = 0; i < _hits.Count && i < Items.Count; i++)
        {
            if (!_hits[i].Contains(p) || !Items[i].Enabled) continue;
            Selected = i;
            if (Items[i].Adjust is { } adjust && Items[i].Activate is null && _valueHits.TryGetValue(i, out var value))
            {
                Sound?.Invoke("ui_move");
                // pe valoare: stanga = mai putin, dreapta = mai mult; in restul randului, la fel, fata de mijlocul valorii
                adjust(p.X < value.X + value.Width / 2 ? -1 : 1);
                return true;
            }
            Use(Items[i]);
            return true;
        }
        return false;
    }

    /// <summary>Enter sau click pe o optiune: o activeaza (sau, pentru o valoare, trece la urmatoarea).</summary>
    private void Use(MenuItem item)
    {
        if (item.Activate is null && item.Adjust is null) return;
        Sound?.Invoke(item.Activate is not null ? "ui_confirm" : "ui_move");
        if (item.Activate is not null) item.Activate();
        else item.Adjust!(1);
    }

    public void Render(Graphics g, RectangleF view, float s)
    {
        _hits.Clear();
        _valueHits.Clear();
        if (Draw is not null) Draw(g, view, s, _hits);
        else MenuStyle.DrawList(g, view, s, this, _hits);
    }
}
