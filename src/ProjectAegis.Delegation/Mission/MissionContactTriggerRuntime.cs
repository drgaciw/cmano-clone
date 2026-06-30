namespace ProjectAegis.Delegation.Mission;

using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

/// <summary>Fires mission transitions and ROE updates on first recon contact (Unknown → Detected).</summary>
public sealed class MissionContactTriggerRuntime
{
    private readonly ScenarioMissionContactTrigger[] _triggers;
    private readonly HashSet<string> _fired = new(StringComparer.Ordinal);

    public MissionContactTriggerRuntime(IReadOnlyList<ScenarioMissionContactTrigger> triggers)
    {
        _triggers = triggers
            .OrderBy(t => t.TriggerId, StringComparer.Ordinal)
            .ToArray();
    }

    public IReadOnlyList<MissionContactTriggerEmission> Evaluate(
        ContactTransition transition,
        double simTime,
        ulong simTick)
    {
        if (transition.PreviousState != ContactLifecycleState.Unknown ||
            transition.NewState != ContactLifecycleState.Detected)
        {
            return Array.Empty<MissionContactTriggerEmission>();
        }

        var emissions = new List<MissionContactTriggerEmission>();
        foreach (var trigger in _triggers)
        {
            if (_fired.Contains(trigger.TriggerId))
            {
                continue;
            }

            if (!string.Equals(trigger.ObserverId, transition.ObserverId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!MissionContactTargetClassifier.Matches(trigger.TargetClass, transition.TargetId))
            {
                continue;
            }

            _fired.Add(trigger.TriggerId);
            emissions.Add(new MissionContactTriggerEmission(trigger, simTime, simTick));
        }

        return emissions;
    }
}

public sealed record MissionContactTriggerEmission(
    ScenarioMissionContactTrigger Trigger,
    double SimTime,
    ulong SimTick);
