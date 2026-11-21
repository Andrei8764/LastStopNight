namespace LastStopNight.Core.Engine;

/// <summary>Un punct de pe harta turei: un bloc atins pe unul sau mai multe drumuri.</summary>
public sealed class RouteNode
{
    internal RouteNode(string blockId, int depth, RouteNode? parent)
    {
        BlockId = blockId;
        Depth = depth;
        Parent = parent;
    }

    public string BlockId { get; }
    /// <summary>Al catelea bloc e pe drum (0 = startul): randul pe harta.</summary>
    public int Depth { get; }
    /// <summary>Coloana pe harta: drumurile care se despart primesc coloane noi.</summary>
    public int Lane { get; internal set; }
    public RouteNode? Parent { get; }
    public List<RouteNode> Children { get; } = new();
    /// <summary>Indicii drumurilor care trec prin acest punct.</summary>
    public List<int> Routes { get; } = new();
    /// <summary>Indicii drumurilor care sunt acum aici (ultimul bloc al drumului).</summary>
    public List<int> Current { get; } = new();
}

/// <summary>
/// Harta turei: drumurile (sirurile de blocuri prin care au trecut) puse intr-un arbore. Partea comuna
/// apare o singura data; unde drumurile se despart, arborele se ramifica. Doua drumuri care ajung separat
/// in acelasi bloc raman ramuri diferite (fiecare are starea lui).
/// </summary>
public static class RouteTree
{
    /// <returns>Toate punctele, in ordinea parcurgerii (parintele inaintea copiilor), cu randul si coloana stabilite.</returns>
    public static List<RouteNode> Build(IReadOnlyList<IReadOnlyList<string>> paths)
    {
        var roots = new List<RouteNode>();
        for (int i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            if (path.Count == 0) continue;
            RouteNode? node = roots.FirstOrDefault(r => r.BlockId == path[0]);
            if (node is null) roots.Add(node = new RouteNode(path[0], 0, null));
            node.Routes.Add(i);
            for (int k = 1; k < path.Count; k++)
            {
                var next = node.Children.FirstOrDefault(c => c.BlockId == path[k]);
                if (next is null) node.Children.Add(next = new RouteNode(path[k], k, node));
                next.Routes.Add(i);
                node = next;
            }
            node.Current.Add(i);
        }

        var all = new List<RouteNode>();
        int lanes = 0;
        foreach (var root in roots) Place(root, lanes++, all, ref lanes);
        return all;
    }

    /// <summary>Primul copil continua pe aceeasi coloana (linia principala ramane dreapta), ceilalti primesc coloane noi.</summary>
    private static void Place(RouteNode node, int lane, List<RouteNode> all, ref int lanes)
    {
        node.Lane = lane;
        all.Add(node);
        var ordered = node.Children.OrderBy(c => c.Routes.Min()).ToList();
        for (int i = 0; i < ordered.Count; i++) Place(ordered[i], i == 0 ? lane : lanes++, all, ref lanes);
    }
}
