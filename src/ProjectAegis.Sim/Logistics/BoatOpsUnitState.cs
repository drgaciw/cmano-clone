namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Immutable per-craft boat-ops state for the LOG-09…11 FSM (CMD-25).
/// Sea-state limits and embarked load are first-class (recovery asymmetry + LOG-11).
/// </summary>
public sealed record BoatOpsUnitState(
    string CraftId,
    string? HostId,
    BoatOpsPhase Phase,
    int MaxLaunchSeaState,
    int MaxRecoverySeaState,
    int PersonnelEmbarked,
    int PersonnelCapacity,
    double CargoTons,
    double CargoCapacityTons,
    int TimeToReadyTicks,
    bool CanAbortLaunch)
{
    /// <summary>Factory for a stowed craft ready for launch (default sea-state 4/3).</summary>
    public static BoatOpsUnitState Stowed(
        string craftId,
        string? hostId = null,
        int maxLaunchSeaState = 4,
        int maxRecoverySeaState = 3,
        int personnelEmbarked = 0,
        int personnelCapacity = 12,
        double cargoTons = 0,
        double cargoCapacityTons = 2.0) =>
        new(
            CraftId: craftId,
            HostId: hostId,
            Phase: BoatOpsPhase.Stowed,
            MaxLaunchSeaState: ScenarioSeaState.Clamp(maxLaunchSeaState),
            MaxRecoverySeaState: ScenarioSeaState.Clamp(maxRecoverySeaState),
            PersonnelEmbarked: personnelEmbarked < 0 ? 0 : personnelEmbarked,
            PersonnelCapacity: personnelCapacity < 0 ? 0 : personnelCapacity,
            CargoTons: cargoTons < 0 ? 0 : cargoTons,
            CargoCapacityTons: cargoCapacityTons < 0 ? 0 : cargoCapacityTons,
            TimeToReadyTicks: 0,
            CanAbortLaunch: false);

    /// <summary>Factory for a craft already waterborne under a host.</summary>
    public static BoatOpsUnitState Waterborne(
        string craftId,
        string? hostId = null,
        int maxLaunchSeaState = 4,
        int maxRecoverySeaState = 3,
        int personnelEmbarked = 0,
        int personnelCapacity = 12,
        double cargoTons = 0,
        double cargoCapacityTons = 2.0) =>
        new(
            CraftId: craftId,
            HostId: hostId,
            Phase: BoatOpsPhase.Waterborne,
            MaxLaunchSeaState: ScenarioSeaState.Clamp(maxLaunchSeaState),
            MaxRecoverySeaState: ScenarioSeaState.Clamp(maxRecoverySeaState),
            PersonnelEmbarked: personnelEmbarked < 0 ? 0 : personnelEmbarked,
            PersonnelCapacity: personnelCapacity < 0 ? 0 : personnelCapacity,
            CargoTons: cargoTons < 0 ? 0 : cargoTons,
            CargoCapacityTons: cargoCapacityTons < 0 ? 0 : cargoCapacityTons,
            TimeToReadyTicks: 0,
            CanAbortLaunch: false);

    /// <summary>Factory for a maintenance-bay craft (launch refused).</summary>
    public static BoatOpsUnitState InMaintenance(
        string craftId,
        string? hostId = null,
        int maxLaunchSeaState = 4,
        int maxRecoverySeaState = 3) =>
        new(
            CraftId: craftId,
            HostId: hostId,
            Phase: BoatOpsPhase.Maintenance,
            MaxLaunchSeaState: ScenarioSeaState.Clamp(maxLaunchSeaState),
            MaxRecoverySeaState: ScenarioSeaState.Clamp(maxRecoverySeaState),
            PersonnelEmbarked: 0,
            PersonnelCapacity: 0,
            CargoTons: 0,
            CargoCapacityTons: 0,
            TimeToReadyTicks: 0,
            CanAbortLaunch: false);

    /// <summary>True when embarked personnel or cargo exceeds craft capacity.</summary>
    public bool IsEmbarkedOverloaded =>
        PersonnelEmbarked > PersonnelCapacity || CargoTons > CargoCapacityTons;
}
