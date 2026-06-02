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
}