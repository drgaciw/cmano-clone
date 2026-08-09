namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Swarm;

/// <summary>
/// SWARM-07 / DRG-88: apply engagement integrity losses only via authorized
/// <see cref="SwarmController.TryApplyIntegrityDamage"/> (aggregate SoT).
/// </summary>
public static class SwarmEngagementIntegrityApplier
{
    public const string ReasonPointFire = "swarm-aa-point";
    public const string ReasonAreaAa = "swarm-aa-area";

    public static string ReasonCode(SwarmAaProfileKind profile) =>
        profile == SwarmAaProfileKind.AreaAa ? ReasonAreaAa : ReasonPointFire;

    /// <summary>
    /// Applies profiled drones-lost for one hit through the authorized integrity API.
    /// Returns false when unit missing, destroyed, or dronesLost invalid.
    /// </summary>
    public static bool TryApplyHit(
        SwarmController controller,
        string unitId,
        SwarmAaProfileKind profile,
        ulong simTick,
        double simTime,
        out SwarmIntegrityChange change)
    {
        change = default!;
        if (controller is null || string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        var lost = SwarmHardCounterAa.DronesLostPerHit(profile);
        return controller.TryApplyIntegrityDamage(
            unitId,
            lost,
            simTick,
            simTime,
            ReasonCode(profile),
            out change);
    }

    /// <summary>
    /// Applies <paramref name="hitCount"/> successive hits (sorted by tick order caller provides).
    /// </summary>
    public static int ApplyHits(
        SwarmController controller,
        string unitId,
        SwarmAaProfileKind profile,
        int hitCount,
        ulong startTick,
        double startSimTime,
        double tickDeltaSeconds = 1.0)
    {
        if (controller is null || hitCount <= 0)
        {
            return 0;
        }

        var applied = 0;
        for (var i = 0; i < hitCount; i++)
        {
            var tick = startTick + (ulong)(uint)i;
            var time = startSimTime + (i * tickDeltaSeconds);
            if (!TryApplyHit(controller, unitId, profile, tick, time, out _))
            {
                break;
            }

            applied++;
            if (controller.TryGetIntegrity(unitId, out var integrity) && integrity.IsDestroyed)
            {
                break;
            }
        }

        return applied;
    }
}
