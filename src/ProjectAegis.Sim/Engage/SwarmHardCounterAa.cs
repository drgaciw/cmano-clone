namespace ProjectAegis.Sim.Engage;

/// <summary>
/// SWARM-08 / DRG-88: hard-counter AA profiles vs swarm aggregate integrity.
/// Area-AA / flak / CIWS shreds more drones per engagement than equal-nominal point fire.
/// Integrity application must go through <see cref="SwarmEngagementIntegrityApplier"/>.
/// Per-scenario overrides ride on <see cref="EngageContext"/> (0 = use table defaults).
/// </summary>
public enum SwarmAaProfileKind
{
    /// <summary>Single-target / point-fire against one body in the cloud.</summary>
    PointFire = 0,

    /// <summary>Area AA / flak / CIWS-class volume fire against the swarm volume.</summary>
    AreaAa = 1,
}

/// <summary>Static profile table for Phase A hard-counter demo (gameplay abstraction).</summary>
public static class SwarmHardCounterAa
{
    /// <summary>
    /// Nominal DPS framing units for "equal nominal" comparisons (doc 22 non-normative).
    /// Both profiles share the same nominal DpsUnits; area shreds more drones per shot.
    /// </summary>
    public const double EqualNominalDpsUnits = 10.0;

    /// <summary>Point-fire drones lost per successful engagement against a swarm (default table).</summary>
    public const int PointFireDronesLostPerHit = 1;

    /// <summary>Area-AA drones lost per successful engagement (hard counter, default table).</summary>
    public const int AreaAaDronesLostPerHit = 8;

    public static int DronesLostPerHit(
        SwarmAaProfileKind profile,
        int pointFireOverride = 0,
        int areaAaOverride = 0)
    {
        var point = pointFireOverride > 0 ? pointFireOverride : PointFireDronesLostPerHit;
        var area = areaAaOverride > 0 ? areaAaOverride : AreaAaDronesLostPerHit;
        return profile switch
        {
            SwarmAaProfileKind.AreaAa => area,
            _ => point,
        };
    }

    /// <summary>
    /// Total drones lost after <paramref name="hitCount"/> successful hits of the given profile.
    /// Deterministic; no RNG (draw resolution lives elsewhere).
    /// </summary>
    public static int TotalDronesLost(
        SwarmAaProfileKind profile,
        int hitCount,
        int pointFireOverride = 0,
        int areaAaOverride = 0)
    {
        if (hitCount <= 0)
        {
            return 0;
        }

        return checked(DronesLostPerHit(profile, pointFireOverride, areaAaOverride) * hitCount);
    }

    public static int ResolveFromContext(in EngageContext ctx) =>
        DronesLostPerHit(ctx.TargetAaProfile, ctx.PointFireDronesLostPerHit, ctx.AreaAaDronesLostPerHit);
}
