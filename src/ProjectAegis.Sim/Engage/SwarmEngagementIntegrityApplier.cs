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

    public static bool TryApplyHit(
        SwarmController controller,
        string unitId,
        SwarmAaProfileKind profile,
        ulong simTick,
        double simTime,
        out SwarmIntegrityChange change,
        int pointFireOverride = 0,
        int areaAaOverride = 0)
    {
        change = default!;
        if (controller is null || string.IsNullOrWhiteSpace(unitId))
        {
            return false;
        }

        var lost = SwarmHardCounterAa.DronesLostPerHit(profile, pointFireOverride, areaAaOverride);
        return controller.TryApplyIntegrityDamage(
            unitId,
            lost,
            simTick,
            simTime,
            ReasonCode(profile),
            out change);
    }

    public static int ApplyHits(
        SwarmController controller,
        string unitId,
        SwarmAaProfileKind profile,
        int hitCount,
        ulong startTick,
        double startSimTime,
        double tickDeltaSeconds = 1.0,
        int pointFireOverride = 0,
        int areaAaOverride = 0)
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
            if (!TryApplyHit(
                    controller,
                    unitId,
                    profile,
                    tick,
                    time,
                    out _,
                    pointFireOverride,
                    areaAaOverride))
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

    /// <summary>
    /// Apply one engagement outcome against a swarm target using <see cref="EngageContext"/> profile fields.
    /// </summary>
    public static bool TryApplyFromEngageContext(
        ISwarmIntegrityDamageSink sink,
        string targetUnitId,
        in EngageContext ctx,
        ulong simTick,
        double simTime)
    {
        if (sink is null || string.IsNullOrWhiteSpace(targetUnitId) || ctx.TargetMaxDrones <= 0)
        {
            return false;
        }

        var lost = SwarmHardCounterAa.ResolveFromContext(in ctx);
        return sink.TryApply(
            targetUnitId,
            lost,
            simTick,
            simTime,
            ReasonCode(ctx.TargetAaProfile));
    }
}
