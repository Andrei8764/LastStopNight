using System.Text.Json;
using System.Text.Json.Serialization;
using LastStopNight.Core.Engine;

namespace LastStopNight.Net;

/// <summary>
/// Mesajele dintre jucatori. Fiecare mesaj e o linie JSON, cu discriminatorul „t”.
/// Gazda e arbitrul: ea ordoneaza actiunile (decizii, examinari) si le trimite tuturor, iar
/// fiecare calculator le aplica pe motorul lui. Prin retea nu circula niciodata starea, doar actiunile.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "t")]
[JsonDerivedType(typeof(HelloMsg), "hello")]
[JsonDerivedType(typeof(WelcomeMsg), "welcome")]
[JsonDerivedType(typeof(RefuseMsg), "refuse")]
[JsonDerivedType(typeof(StoryDataMsg), "story")]
[JsonDerivedType(typeof(LobbyMsg), "lobby")]
[JsonDerivedType(typeof(PickMsg), "pick")]
[JsonDerivedType(typeof(ReadyMsg), "ready")]
[JsonDerivedType(typeof(StartMsg), "start")]
[JsonDerivedType(typeof(MoveMsg), "move")]
[JsonDerivedType(typeof(VoteMsg), "vote")]
[JsonDerivedType(typeof(VotesMsg), "votes")]
[JsonDerivedType(typeof(ExamineReqMsg), "examine")]
[JsonDerivedType(typeof(ActionMsg), "action")]
[JsonDerivedType(typeof(ForkMsg), "fork")]
[JsonDerivedType(typeof(HashMsg), "hash")]
[JsonDerivedType(typeof(ResyncMsg), "resync")]
[JsonDerivedType(typeof(ReturnToLobbyMsg), "tolobby")]
[JsonDerivedType(typeof(ByeMsg), "bye")]
public abstract record NetMessage;

/// <summary>Clientul se prezinta: versiunea protocolului, numele si ce povesti are deja (amprente).</summary>
public sealed record HelloMsg(int Protocol, string Name, List<string> Stories) : NetMessage;

/// <summary>Gazda accepta: ID-ul jucatorului, povestea si daca i-o trimite (n-o are).</summary>
public sealed record WelcomeMsg(int YourId, string StoryTitle, string Fingerprint, bool SendingStory) : NetMessage;

public sealed record RefuseMsg(string Reason) : NetMessage;

/// <summary>Povestea (zip in base64), pentru cine n-o are.</summary>
public sealed record StoryDataMsg(string Zip) : NetMessage;

public sealed record LobbyMsg(LobbyState Lobby) : NetMessage;

/// <summary>Alegerea personajului in lobby (null = renunta la el).</summary>
public sealed record PickMsg(string? CharacterId) : NetMessage;

public sealed record ReadyMsg(bool Ready) : NetMessage;

/// <summary>Tura incepe: insotitorii din echipa si ce personaj are fiecare jucator.</summary>
public sealed record StartMsg(List<string> Companions, Dictionary<int, string> Assignments) : NetMessage;

/// <summary>Pozitia personajului unui jucator (doar vizual, nu afecteaza povestea).</summary>
public sealed record MoveMsg(int PlayerId, string BlockId, float X, bool FacingLeft, bool Walking) : NetMessage;

/// <summary>Votul unui jucator pentru o decizie a blocului curent.</summary>
public sealed record VoteMsg(int PlayerId, string BlockId, int Index) : NetMessage;

/// <summary>Voturile de pana acum (jucator -&gt; decizie) si, pe fiecare drum, cate secunde mai sunt pana se decide.</summary>
public sealed record VotesMsg(Dictionary<int, int> Votes, Dictionary<int, float> SecondsLeft) : NetMessage;

public sealed record ExamineReqMsg(string BlockId, string ExamineId) : NetMessage;

/// <summary>O actiune confirmata de gazda, cu numarul ei de ordine si drumul (timeline) pe care se aplica.</summary>
public sealed record ActionMsg(int Seq, int Timeline, GameAction Action) : NetMessage;

/// <summary>
/// Drumurile se despart: jucatorii dati trec pe un drum nou (<paramref name="NewTimeline"/>), care porneste
/// ca o copie exacta a drumului <paramref name="From"/>. Are numar de ordine, ca actiunile.
/// </summary>
public sealed record ForkMsg(int Seq, int From, int NewTimeline, List<int> Players) : NetMessage;

/// <summary>Amprenta starii unui drum dupa un numar de actiuni (verificarea sincronizarii).</summary>
public sealed record HashMsg(int Timeline, int LogCount, string Hash) : NetMessage;

/// <summary>Un drum complet: cine e pe el si toate actiunile lui, de la inceputul turei.</summary>
public sealed record TimelineData(int Id, List<int> Players, List<GameAction> Actions);

/// <summary>Toate drumurile, de la inceput: clientul desincronizat le reface. Seq = ultimul numar de ordine.</summary>
public sealed record ResyncMsg(List<string> Companions, List<TimelineData> Timelines, int Seq) : NetMessage;

public sealed record ReturnToLobbyMsg : NetMessage;

public sealed record ByeMsg(string Reason) : NetMessage;

public sealed class LobbyPlayer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? CharacterId { get; set; }
    public bool Ready { get; set; }
    public bool IsHost { get; set; }
}

public sealed class LobbyState
{
    public string StoryTitle { get; set; } = "";
    public string Fingerprint { get; set; } = "";
    public bool InGame { get; set; }
    /// <summary>Daca jucatorii voteaza diferit, fiecare merge pe drumul lui (altfel castiga majoritatea).</summary>
    public bool AllowSplit { get; set; } = true;
    public List<LobbyPlayer> Players { get; set; } = new();
}

public static class NetJson
{
    /// <summary>Versiunea protocolului: doua exe-uri diferite primesc un mesaj clar, nu o desincronizare.</summary>
    public const int ProtocolVersion = 2;
    public const int DefaultPort = 27013;
    public const int DiscoveryPort = 27014;

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        AllowOutOfOrderMetadataProperties = true,
    };

    public static string Serialize(NetMessage m) => JsonSerializer.Serialize(m, Options);
    public static NetMessage? Deserialize(string line) => JsonSerializer.Deserialize<NetMessage>(line, Options);
}
