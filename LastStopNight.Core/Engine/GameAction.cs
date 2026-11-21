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
[JsonDerivedType(typeof(ShareAction), "share")]
public abstract record GameAction(string BlockId, string Player);

/// <summary>Alegerea deciziei cu indexul dat (in lista completa a blocului).</summary>
public sealed record ChooseAction(string BlockId, int Index, string Player) : GameAction(BlockId, Player);

/// <summary>Examinarea unui obiect (al locatiei sau al blocului).</summary>
public sealed record ExamineAction(string BlockId, string ExamineId, string Player) : GameAction(BlockId, Player);

/// <summary>
/// Echipa se schimba (multiplayer, drumuri separate): de aici inainte, in povestea asta sunt doar <paramref name="Members"/>.
/// Cand cineva pleaca pe alt drum, personajul lui dispare (cu textele si deciziile lui); cand vine dupa altcineva, apare.
/// </summary>
public sealed record PartyAction(string BlockId, string Player, IReadOnlyList<string> Members) : GameAction(BlockId, Player);

/// <summary>
/// Drumuri separate: o proprietate pusa la comun (<see cref="Model.StatePropertyDefinition.Shared"/>, ex. indiciile)
/// primeste valoarea <paramref name="Value"/>, pentru ca alt drum a gasit ceva. <paramref name="Player"/> e cine a gasit.
/// </summary>
public sealed record ShareAction(string BlockId, string Player, string Key, int Value) : GameAction(BlockId, Player);
