namespace ProjectAegis.Sim.Swarm;

/// <summary>
/// Pure deterministic gates for SWARM-13 / DRG-97: regenerate drones near a host with stores.
/// Geometry uses the same range model as <see cref="SwarmLinkEvaluator.RangeDeg"/>.
/// </summary>
public static class SwarmRegenEvaluator
{
    /// <summary>Degrees of lat/lon within which the swarm is considered near the host for regen.</summary>
    public const double DefaultMaxRangeDeg = 0.5;

    /// <summary>Default drones restored per authorized regen pulse.</summary>
    public const int DefaultDronesPerPulse = 1;

    /// <summary>
    /// Returns true when all regen gates pass: host alive, has stores, within range,
    /// and room remains under maxDrones.
    /// </summary>
    public static bool CanRegen(
        double? rangeDeg,
        bool hostAlive,
        bool hostHasStores,
        int droneCount,
        int maxDrones,
        double maxRangeDeg = DefaultMaxRangeDeg)
    {
        if (!hostAlive || !hostHasStores)
        {
            return false;
        }

        if (maxDrones <= 0 || droneCount >= maxDrones)
        {
            return false;
        }

        if (rangeDeg is not double range || range > maxRangeDeg || range < 0)
        {
            return false;
        }

        return true;
    }
}
