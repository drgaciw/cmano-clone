namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// SWARM-14 Phase B: presentation snapshot for swarm unit panel.
/// Independent of Sim controller SoT — callers supply fields after B1/B6 wire-up.
/// </summary>
public sealed record SwarmPanelSnapshot(
    string UnitId,
    int DroneCount,
    int MaxDrones,
    string? Mode = null,
    string? HostId = null,
    string? LinkState = null,
    string? CecMeshState = null,
    bool IsDestroyed = false,
    string? ModeUnknownReason = null,
    string? HostUnknownReason = null,
    string? LinkUnknownReason = null,
    string? CecUnknownReason = null);
