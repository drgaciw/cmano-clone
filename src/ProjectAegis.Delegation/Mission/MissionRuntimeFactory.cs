namespace ProjectAegis.Delegation.Mission;

using ProjectAegis.Sim.Scenario;

/// <summary>Maps scenario mission JSON to headless <see cref="MissionRuntime"/>.</summary>
public static class MissionRuntimeFactory
{
    public static MissionRuntime? TryCreate(ScenarioMissionTimeline? timeline)
    {
        if (timeline == null || timeline.Events.Count == 0)
        {
            return null;
        }

        var events = timeline.Events
            .Select(e => new MissionEventDefinition(
                e.EventId,
                e.FireAtTick,
                ParseKind(e.Kind),
                e.Code))
            .ToArray();
        return new MissionRuntime(events, timeline.FireOrder);
    }

    private static MissionEventKind ParseKind(string kind) =>
        Enum.TryParse<MissionEventKind>(kind, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidDataException($"Unknown mission event kind: {kind}");
}