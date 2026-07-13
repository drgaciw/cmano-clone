namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Scenario;

/// <summary>
/// ADR-009 bounded gate: blocks engage when catalog-resolved damage trials recommend withdraw.
/// Additive-only — absent or unresolved trials never block.
/// </summary>
public static class CatalogDamageWithdrawEngageGate
{
    public static bool BlocksEngage(
        string platformId,
        IReadOnlyList<ScenarioWithdrawReadinessTrial> trials) =>
        TryGetTrial(platformId, trials, out var trial)
        && trial.CatalogResolved
        && trial.WithdrawRecommended;

    public static bool TryGetTrial(
        string platformId,
        IReadOnlyList<ScenarioWithdrawReadinessTrial> trials,
        out ScenarioWithdrawReadinessTrial trial)
    {
        foreach (var candidate in trials)
        {
            if (string.Equals(candidate.PlatformId, platformId, StringComparison.Ordinal))
            {
                trial = candidate;
                return true;
            }
        }

        trial = default!;
        return false;
    }

    public static EngagementAbortReason? Evaluate(in EngageContext ctx) =>
        ctx.CatalogDamageWithdrawBlocked
            ? EngagementAbortReason.DamageWithdrawRecommended
            : null;
}