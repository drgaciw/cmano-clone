namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One agent/controller row for the CMD-37 agent roster panel (presentation only).
/// </summary>
public sealed record AgentRosterEntry(
    string AgentId,
    string UnitId,
    string AutonomyLabel,
    string StatusLabel,
    string AttentionLabel,
    string ModeLabel);
