namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Core;

/// <summary>Deterministic hit/miss after launch using Combat RNG domain (combat-outcomes MVP).</summary>
public static class CombatOutcomeResolver
{
    public static EngageResult Apply(
        SimSeed seed,
        in EngageRequest request,
        EngageResult launch,
        double pkBase)
    {
        if (!launch.Launched)
        {
            return launch;
        }

        var pk = Math.Clamp(pkBase, 0, 1);
        var draw = SeededRng.UnitFloat(seed, RngDomain.Combat, launch.EngagementId, request.SimTick, 0);
        var outcome = draw < pk ? EngagementOutcomeCodes.Hit : EngagementOutcomeCodes.Miss;
        return launch with { OutcomeCode = outcome, PkDraw = draw };
    }

    /// <summary>Second combat draw (salt 1): promote Hit to Kill when pkKill passes.</summary>
    public static EngageResult ApplyKillOnHit(
        SimSeed seed,
        in EngageRequest request,
        EngageResult afterHit,
        double pkKill)
    {
        if (afterHit.OutcomeCode != EngagementOutcomeCodes.Hit)
        {
            return afterHit;
        }

        var pk = Math.Clamp(pkKill, 0, 1);
        var draw = SeededRng.UnitFloat(seed, RngDomain.Combat, afterHit.EngagementId, request.SimTick, 1);
        if (draw < pk)
        {
            return afterHit with { OutcomeCode = EngagementOutcomeCodes.Kill };
        }

        return afterHit;
    }
}