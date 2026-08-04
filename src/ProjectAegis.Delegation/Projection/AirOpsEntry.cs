namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One air-asset readiness row for CMD-24 Phase A Air Operations panel (presentation only).
/// </summary>
public sealed record AirOpsEntry(
    string UnitId,
    string PlatformTypeLabel,
    string HostLabel,
    bool ReadyForLaunch,
    string StatusLine,
    string? RefusalCode);
