namespace ProjectAegis.Sim.Scenario;

/// <summary>Mission editor timeline bound to scenario package (req 11 / C4 MVP).</summary>
public sealed class ScenarioMissionTimeline
{
    public ScenarioMissionTimeline(
        IReadOnlyList<string> fireOrder,
        IReadOnlyList<ScenarioMissionEvent> events,
        IReadOnlyList<ScenarioMissionContactTrigger>? contactTriggers = null)
    {
        FireOrder = fireOrder;
        Events = events;
        ContactTriggers = contactTriggers ?? Array.Empty<ScenarioMissionContactTrigger>();
    }

    public IReadOnlyList<string> FireOrder { get; }

    public IReadOnlyList<ScenarioMissionEvent> Events { get; }

    public IReadOnlyList<ScenarioMissionContactTrigger> ContactTriggers { get; }
}

public sealed record ScenarioMissionEvent(
    string EventId,
    ulong FireAtTick,
    string Kind,
    string Code);