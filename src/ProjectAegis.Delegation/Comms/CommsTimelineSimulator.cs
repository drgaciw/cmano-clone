namespace ProjectAegis.Delegation.Comms;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Scenario;

/// <summary>Emits deterministic comms transitions from scenario policy (doc 19).</summary>
public sealed class CommsTimelineSimulator
{
    private readonly ScenarioCommsTransition[] _transitions;
    private int _nextIndex;
    private CommsState _current = CommsState.Nominal;

    public CommsTimelineSimulator(IReadOnlyList<ScenarioCommsTransition> transitions)
    {
        _transitions = transitions
            .OrderBy(t => t.AtTick)
            .ToArray();
    }

    public static CommsTimelineSimulator? TryCreate(ScenarioPolicyProfile? profile) =>
        profile is { CommsTransitions.Count: > 0 }
            ? new CommsTimelineSimulator(profile.CommsTransitions)
            : null;

    public CommsState CurrentState => _current;

    public IReadOnlyList<CommsStateChangeRecord> Drain(ulong simTick, double simTime)
    {
        var emitted = new List<CommsStateChangeRecord>();
        while (_nextIndex < _transitions.Length && _transitions[_nextIndex].AtTick <= simTick)
        {
            var transition = _transitions[_nextIndex++];
            var next = ParseState(transition.NewState);
            if (next == _current)
            {
                continue;
            }

            emitted.Add(new CommsStateChangeRecord(
                0,
                simTime,
                simTick,
                transition.NodeId,
                _current,
                next,
                transition.Reason));
            _current = next;
        }

        return emitted;
    }

    public static CommsState ParseState(string value) =>
        Enum.TryParse<CommsState>(value, ignoreCase: true, out var parsed)
            ? parsed
            : CommsState.Nominal;
}