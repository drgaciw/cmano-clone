namespace ProjectAegis.Delegation.Comms;

using ProjectAegis.Sim.Scenario;

/// <summary>Activates spoofed contact IDs from scenario policy on sim ticks (req 19).</summary>
public sealed class SpoofTrackTimelineSimulator
{
    private readonly ScenarioSpoofTransition[] _transitions;
    private readonly HashSet<string> _active = new(StringComparer.Ordinal);
    private int _nextIndex;

    public SpoofTrackTimelineSimulator(IReadOnlyList<ScenarioSpoofTransition> transitions)
    {
        _transitions = transitions
            .OrderBy(t => t.AtTick)
            .ToArray();
    }

    public static SpoofTrackTimelineSimulator? TryCreate(ScenarioPolicyProfile? profile) =>
        profile is { SpoofTransitions.Count: > 0 }
            ? new SpoofTrackTimelineSimulator(profile.SpoofTransitions)
            : null;

    public void Advance(ulong simTick)
    {
        while (_nextIndex < _transitions.Length && _transitions[_nextIndex].AtTick <= simTick)
        {
            _active.Add(_transitions[_nextIndex].ContactId);
            _nextIndex++;
        }
    }

    public bool IsSpoofed(string? contactId) =>
        !string.IsNullOrWhiteSpace(contactId) && _active.Contains(contactId);
}