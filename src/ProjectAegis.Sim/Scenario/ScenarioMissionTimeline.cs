namespace ProjectAegis.Sim.Scenario;

/// <summary>Mission editor timeline bound to scenario package (req 11 / C4 MVP).</summary>
public sealed class ScenarioMissionTimeline
{
    public ScenarioMissionTimeline(
        IReadOnlyList<string> fireOrder,
        IReadOnlyList<ScenarioMissionEvent> events)
    {
        FireOrder = fireOrder;
        Events = events;
    }

    public IReadOnlyList<string> FireOrder { get; }

    public IReadOnlyList<ScenarioMissionEvent> Events { get; }
}

public sealed record ScenarioMissionEvent(
    string EventId,
    ulong FireAtTick,
    string Kind,
    string Code);