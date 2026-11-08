namespace ProjectAegis.Sim.Engage;

/// <summary>
/// Optional production hook: engagement hit/kill against a swarm target reduces aggregate integrity.
/// Implementations typically forward to <see cref="Swarm.SwarmController.TryApplyIntegrityDamage"/>.
/// </summary>
public interface ISwarmIntegrityDamageSink
{
    /// <summary>
    /// Apply drones lost against target unit key (string form of sim target id, or scenario unit id).
    /// </summary>
    bool TryApply(
        string targetUnitId,
        int dronesLost,
        ulong simTick,
        double simTime,
        string reasonCode);
}
