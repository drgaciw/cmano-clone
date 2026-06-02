namespace ProjectAegis.Delegation.Projection;

/// <summary>Mission timeline row for doc-20 missions tab.</summary>
public sealed record MissionListEntry(
    string EventId,
    ulong FireAtTick,
    string Kind,
    string Code);