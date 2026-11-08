namespace ProjectAegis.Delegation.C2Network;

/// <summary>
/// A track contributor preserved as last-known when a link cannot carry live updates.
/// <see cref="IsLiveCapability"/> is false when the path is partitioned or globally denied.
/// </summary>
public sealed record C2NetworkContributor(
    string UnitId,
    string ContactId,
    string TargetId,
    string LifecycleState,
    ulong LastKnownSimTick,
    bool IsLiveCapability);
