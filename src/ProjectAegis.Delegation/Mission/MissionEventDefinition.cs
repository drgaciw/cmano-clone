namespace ProjectAegis.Delegation.Mission;

public enum MissionEventKind
{
    MissionTransition = 0,
    EventFired = 1,
}

/// <summary>Scenario-authored mission timeline event (locked fire_order).</summary>
public sealed record MissionEventDefinition(
    string EventId,
    ulong FireAtTick,
    MissionEventKind Kind,
    string Code);