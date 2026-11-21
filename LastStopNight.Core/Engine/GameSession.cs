using LastStopNight.Core.Model;

namespace LastStopNight.Core.Engine;

/// <summary>Rezultatul unei examinari: cine s-a uitat si ce a vazut.</summary>
public sealed record ExamineResult(ExamineDefinition Examine, string Player, string Text);

/// <summary>
/// Calea prin care interfata cere actiuni. Interfata nu modifica direct motorul: cere o actiune,
/// iar sesiunea decide cand si daca se aplica. Local se aplica imediat; in retea (mai tarziu),
/// gazda le ordoneaza si le trimite tuturor.
/// </summary>
public interface IGameSession
{
    GameEngine Engine { get; }

    /// <summary>Personajul controlat de acest calculator.</summary>
    string LocalPlayer { get; }

    /// <summary>Se declanseaza dupa ce o examinare a fost aplicata (textul de afisat).</summary>
    event EventHandler<ExamineResult>? Examined;

    void Start(IEnumerable<string> companions);
    void RequestChoose(DecisionOption option);
    void RequestExamine(ExamineDefinition examine);
}

/// <summary>Sesiunea de single player: actiunile se aplica pe loc.</summary>
public sealed class LocalSession(GameEngine engine) : IGameSession
{
    public GameEngine Engine { get; } = engine;
    public string LocalPlayer => Engine.Story.PlayerCharacterId;

    public event EventHandler<ExamineResult>? Examined;

    public void Start(IEnumerable<string> companions) => Engine.Start(companions);

    public void RequestChoose(DecisionOption option)
    {
        if (Engine.CurrentBlock is null || !option.IsAvailable) return;
        Engine.Apply(new ChooseAction(Engine.CurrentBlock.Id, option.Index, LocalPlayer));
    }

    public void RequestExamine(ExamineDefinition examine)
    {
        if (Engine.CurrentBlock is null) return;
        var text = Engine.Apply(new ExamineAction(Engine.CurrentBlock.Id, examine.Id, LocalPlayer));
        Examined?.Invoke(this, new ExamineResult(examine, LocalPlayer, text ?? examine.Text));
    }
}
