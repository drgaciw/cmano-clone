namespace ProjectAegis.Sim.Swarm.SoftKill;

/// <summary>
/// Pure deterministic EMP soft-kill rules (SWARM-18 / DRG-107).
/// Freezes mode switches until a sim-time horizon; no RNG.
/// </summary>
public static class SwarmEmpEvaluator
{
    /// <summary>Default EMP mode-switch freeze duration in sim-seconds.</summary>
    public const double DefaultFreezeDurationSeconds = 30.0;

    public const string ReasonModeFreeze = "soft-kill-emp-mode-freeze";
    public const string ReasonRecommendScatter = "soft-kill-emp-recommend-scatter";
    public const string ReasonModeBlocked = "soft-kill-emp-mode-frozen";
    public const string ReasonClear = "soft-kill-emp-clear";

    /// <summary>Compute exclusive freeze-until simTime (modes blocked while simTime < freezeUntil).</summary>
    public static double ComputeFreezeUntil(double simTime, double freezeDurationSeconds)
    {
        if (freezeDurationSeconds <= 0)
        {
            return simTime;
        }

        return simTime + freezeDurationSeconds;
    }

    /// <summary>True when <paramref name="simTime"/> is still inside an active freeze window.</summary>
    public static bool IsModeFrozen(double simTime, double freezeUntilSimTime) =>
        freezeUntilSimTime > simTime;

    /// <summary>
    /// Merge overlapping EMP freezes by taking the later freeze-until (deterministic max).
    /// </summary>
    public static double MergeFreezeUntil(double existingFreezeUntil, double candidateFreezeUntil) =>
        candidateFreezeUntil > existingFreezeUntil ? candidateFreezeUntil : existingFreezeUntil;
}
