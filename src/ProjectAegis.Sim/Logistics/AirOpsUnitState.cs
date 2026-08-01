namespace ProjectAegis.Sim.Logistics;

/// <summary>
/// Immutable per-unit air-ops state for the LOG-08 FSM (CMD-24 Phase N).
/// </summary>
public sealed record AirOpsUnitState(
    string UnitId,
    AirOpsPhase Phase,
    bool ReadyForLaunch,
    int TimeToReadyTicks,
    string? HostId,
    bool CanAbortLaunch)
{
    /// <summary>Factory for a grounded airframe (default Phase A readiness mapping).</summary>
    public static AirOpsUnitState OnGround(string unitId, bool readyForLaunch = true, string? hostId = null) =>
        new(
            UnitId: unitId,
            Phase: AirOpsPhase.OnGround,
            ReadyForLaunch: readyForLaunch,
            TimeToReadyTicks: 0,
            HostId: hostId,
            CanAbortLaunch: false);

    /// <summary>Factory for a maintenance bay airframe (launch refused).</summary>
    public static AirOpsUnitState InMaintenance(string unitId, string? hostId = null) =>
        new(
            UnitId: unitId,
            Phase: AirOpsPhase.Maintenance,
            ReadyForLaunch: false,
            TimeToReadyTicks: 0,
            HostId: hostId,
            CanAbortLaunch: false);

    /// <summary>Factory for an already-airborne airframe.</summary>
    public static AirOpsUnitState Airborne(string unitId, string? hostId = null) =>
        new(
            UnitId: unitId,
            Phase: AirOpsPhase.Airborne,
            ReadyForLaunch: false,
            TimeToReadyTicks: 0,
            HostId: hostId,
            CanAbortLaunch: false);
}
