namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// One embarked-craft row for CMD-25 Boat Operations panel (presentation only).
/// </summary>
public sealed record BoatOpsEntry(
    string CraftId,
    string HostLabel,
    string PhaseLabel,
    string StatusLine,
    string? RefusalCode,
    int SeaStateDouglas,
    int MaxLaunchSeaState,
    int MaxRecoverySeaState,
    bool SeaStateLaunchOk,
    bool SeaStateRecoveryOk,
    int PersonnelEmbarked,
    int PersonnelCapacity,
    double CargoTons,
    double CargoCapacityTons,
    int TimeToReadyTicks,
    bool CanLaunch,
    bool CanRecover,
    bool CanAbort,
    string? LaunchDisabledReason,
    string? RecoverDisabledReason);
