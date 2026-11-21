using System.Text.Json.Serialization;

namespace LastStopNight.Core.Engine;

/// <summary>
/// O actiune a unui jucator care schimba povestea. Motorul e determinist: aceeasi poveste, aceeasi
/// echipa si acelasi sir de actiuni dau mereu aceeasi stare. Pe asta se bazeaza salvarea
/// („Continua” = reaplicarea actiunilor) si, mai tarziu, multiplayer-ul (se trimit doar actiunile).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChooseAction), "choose")]
[JsonDerivedType(typeof(ExamineAction), "examine")]
[JsonDerivedType(typeof(PartyAction), "party")]
public abstract record GameAction(string BlockId, string Player);

/// <summary>Alegerea deciziei cu indexul dat (in lista completa a blocului).</summary>
public sealed record ChooseAction(string BlockId, int Index, string Player) : GameAction(BlockId, Player);

/// <summary>Examinarea unui obiect (al locatiei sau al blocului).</summary>
public sealed record ExamineAction(string BlockId, string ExamineId, string Player) : GameAction(BlockId, Player);

/// <summary>
/// Echipa se desparte (multiplayer, drumuri separate): de aici inainte, in povestea asta raman doar
/// <paramref name="Members"/>, o submultime a echipei de acum. Textele si deciziile insotitorilor plecati dispar.
/// </summary>
public sealed record PartyAction(string BlockId, string Player, IReadOnlyList<string> Members) : GameAction(BlockId, Player);
