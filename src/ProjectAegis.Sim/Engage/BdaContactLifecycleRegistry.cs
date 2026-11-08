namespace ProjectAegis.Sim.Engage;

/// <summary>Tracks hostile platform ids pending BDA Lost promotion for the current tick.</summary>
public sealed class BdaContactLifecycleRegistry
{
    private readonly HashSet<string> _promoted = new(StringComparer.Ordinal);
    private readonly List<string> _pending = new();

    public bool MarkLost(string targetId)
    {
        if (!_promoted.Add(targetId))
        {
            return false;
        }

        _pending.Add(targetId);
        return true;
    }

    public IReadOnlyList<string> DrainNewLostTargets()
    {
        if (_pending.Count == 0)
        {
            return Array.Empty<string>();
        }

        var batch = _pending
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();
        _pending.Clear();
        return batch;
    }

    public int PromotedCount => _promoted.Count;
}