namespace ProjectAegis.Sim.Swarm;

using ProjectAegis.Sim.Core;

/// <summary>Append-only headless swarm order log (SWARM-06). Replay consumes the same rows.</summary>
public sealed class SwarmOrderLog
{
    private readonly List<SwarmOrderLogEntry> _entries = new();
    private ulong _nextSequence = 1;

    public IReadOnlyList<SwarmOrderLogEntry> Entries => _entries;

    public ulong Append(
        ulong simTick,
        double simTime,
        string unitId,
        SwarmIntentKind intent,
        double? targetLatDeg = null,
        double? targetLonDeg = null,
        string? attackTargetUnitId = null)
    {
        var sequenceId = _nextSequence++;
        _entries.Add(new SwarmOrderLogEntry(
            sequenceId,
            simTick,
            simTime,
            unitId,
            intent,
            targetLatDeg,
            targetLonDeg,
            attackTargetUnitId));
        return sequenceId;
    }

    public void Clear()
    {
        _entries.Clear();
        _nextSequence = 1;
    }

    public ulong ComputeFingerprint()
    {
        ulong mix = 0;
        foreach (var e in _entries)
        {
            mix = SimWorldHash.MixLayer(mix, e.SequenceId, SimWorldHash.LayerCore);
            mix = SimWorldHash.MixLayer(mix, e.SimTick, SimWorldHash.LayerCore);
            mix = SimWorldHash.MixLayer(mix, (ulong)(uint)e.Intent, SimWorldHash.LayerCore);
            mix = SimWorldHash.MixLayer(mix, HashString(e.UnitId), SimWorldHash.LayerCore);
            if (e.TargetLatDeg is double lat)
            {
                mix = SimWorldHash.MixLayer(mix, DoubleBits(lat), SimWorldHash.LayerCore);
            }

            if (e.TargetLonDeg is double lon)
            {
                mix = SimWorldHash.MixLayer(mix, DoubleBits(lon), SimWorldHash.LayerCore);
            }

            if (!string.IsNullOrEmpty(e.AttackTargetUnitId))
            {
                mix = SimWorldHash.MixLayer(mix, HashString(e.AttackTargetUnitId), SimWorldHash.LayerCore);
            }
        }

        return mix;
    }

    private static ulong DoubleBits(double value) =>
        unchecked((ulong)BitConverter.DoubleToInt64Bits(value));

    private static ulong HashString(string value)
    {
        ulong h = 14695981039346656037UL;
        foreach (var c in value)
        {
            h ^= c;
            h *= 1099511628211UL;
        }

        return h;
    }
}
