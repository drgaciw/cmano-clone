namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Core;

/// <summary>Tracks destroyed hostile target ids for engage gating and world hash.</summary>
public sealed class KilledTargetRegistry
{
    private readonly HashSet<ulong> _killed = new();
    private readonly List<(ulong Id, string Label)> _newKills = new();

    public bool IsKilled(ulong targetId) => _killed.Contains(targetId);

    public bool MarkKilled(ulong targetId, string targetLabel)
    {
        if (!_killed.Add(targetId))
        {
            return false;
        }

        _newKills.Add((targetId, targetLabel));
        return true;
    }

    public IReadOnlyList<(ulong Id, string Label)> DrainNewKills()
    {
        if (_newKills.Count == 0)
        {
            return Array.Empty<(ulong, string)>();
        }

        var batch = _newKills.ToArray();
        _newKills.Clear();
        return batch;
    }

    public int Count => _killed.Count;

    public ulong MixHash()
    {
        if (_killed.Count == 0)
        {
            return 0;
        }

        var ids = _killed.OrderBy(id => id).ToArray();
        ulong mix = 0;
        foreach (var id in ids)
        {
            mix = SimWorldHash.MixLayer(mix, id, SimWorldHash.LayerCombatOutcome);
        }

        return mix;
    }
}