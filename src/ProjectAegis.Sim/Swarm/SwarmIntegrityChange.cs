namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// Authorized integrity delta for aggregate swarm SoT (SWARM-02 / SWARM-07).
/// Only produced by <see cref="SwarmController.TryApplyIntegrityDamage"/>.
/// </summary>
public sealed record SwarmIntegrityChange(
    ulong SequenceId,
    ulong SimTick,
    double SimTime,
    string UnitId,
    int PreviousDroneCount,
    int NewDroneCount,
    int DronesLost,
    string ReasonCode);
