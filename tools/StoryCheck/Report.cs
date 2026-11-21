namespace StoryCheck;

/// <summary>Scrie rezultatul fiecarei verificari si le numara pe cele esuate (codul de iesire al programului).</summary>
public sealed class Report
{
    public int Failures { get; private set; }

    public void Section(string title) => Console.WriteLine($"\n== {title}");

    public void Info(string text) => Console.WriteLine("  " + text);

    public void Check(bool ok, string what)
    {
        Console.WriteLine($"  [{(ok ? "OK" : "EȘUAT")}] {what}");
        if (!ok) Failures++;
    }

    /// <summary>Verifica faptul ca o actiune arunca exceptia asteptata.</summary>
    public void Throws<TException>(Action action, string what) where TException : Exception
    {
        try
        {
            action();
            Check(false, what + " (nu a aruncat nicio excepție)");
        }
        catch (TException) { Check(true, what); }
        catch (Exception ex) { Check(false, $"{what} (a aruncat {ex.GetType().Name} în loc de {typeof(TException).Name})"); }
    }
}
