namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// Authorized integrity delta for aggregate swarm SoT (SWARM-02 / SWARM-07 / SWARM-13).
/// Produced only by <see cref="SwarmController.TryApplyIntegrityDamage"/> (loss) or
/// <see cref="SwarmController.TryApplyIntegrityRegen"/> (gain).
/// For regen events, <see cref="DronesLost"/> is 0 and <see cref="NewDroneCount"/> >
/// <see cref="PreviousDroneCount"/> (reason typically <see cref="SwarmController.RegenReasonCode"/>).
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
