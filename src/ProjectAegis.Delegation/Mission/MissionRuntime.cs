namespace ProjectAegis.Delegation.Mission;

using ProjectAegis.Delegation.Decision;

/// <summary>Deterministic mission timeline: fires events in locked fire_order at tick boundaries.</summary>
public sealed class MissionRuntime
{
    private readonly MissionEventDefinition[] _events;
    private int _nextIndex;

    public MissionRuntime(IReadOnlyList<MissionEventDefinition> events, IReadOnlyList<string> fireOrder)
    {
        var orderIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < fireOrder.Count; i++)
        {
            orderIndex[fireOrder[i]] = i;
        }

        _events = events
            .OrderBy(e => e.FireAtTick)
            .ThenBy(e => orderIndex.TryGetValue(e.EventId, out var idx) ? idx : int.MaxValue)
            .ThenBy(e => e.EventId, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<MissionTickEmission> Tick(ulong simTick, double simTime, ulong sequenceStart)
    {
        var emissions = new List<MissionTickEmission>();
        while (_nextIndex < _events.Length && _events[_nextIndex].FireAtTick <= simTick)
        {
            var evt = _events[_nextIndex++];
            emissions.Add(new MissionTickEmission(evt, simTime, simTick, sequenceStart + (ulong)emissions.Count));
        }

        return emissions;
    }
}

public sealed record MissionTickEmission(
    MissionEventDefinition Event,
    double SimTime,
    ulong SimTick,
    ulong SequenceId);