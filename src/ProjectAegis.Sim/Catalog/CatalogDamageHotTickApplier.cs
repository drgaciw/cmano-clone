namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;

/// <summary>
/// ADR-009 bounded hot-tick catalog damage apply — ledger mutation + withdraw trial refresh.
/// Additive-only; requires gate-approved <see cref="ICatalogReader"/> snapshot (no hot-path SQLite).
/// </summary>
public static class CatalogDamageHotTickApplier
{
    /// <summary>Bounded light damage per Hit outcome (MVP platform level).</summary>
    public const double HitDamageHpPct = 25.0;

    public sealed record OutcomeApply(
        string VictimPlatformId,
        ulong EngagementId,
        ulong SequenceId,
        string OutcomeCode);

    public sealed record DamageChange(
        string PlatformId,
        double PreviousHpPct,
        double NewHpPct,
        string ReasonCode);

    public static bool IsEnabled(bool combatDomainsEnabled, int catalogWithdrawTargetCount) =>
        combatDomainsEnabled && catalogWithdrawTargetCount > 0;

    /// <summary>Catalog-scaled ambient drain: one MaxHp unit per tick as HP%.</summary>
    public static double AmbientTickDrainHpPct(CatalogPlatformDamage damage) =>
        damage.MaxHp <= 0 ? 0.0 : 100.0 / damage.MaxHp;

    public static IReadOnlyList<DamageChange> ApplyAmbientTickDrain(
        PlatformHpLedger ledger,
        ICatalogReader catalog)
    {
        var changes = new List<DamageChange>();
        foreach (var platformId in ledger.GetSortedPlatformIds())
        {
            if (!ledger.TryGetHpPct(platformId, out var previousHp) || previousHp <= 0)
            {
                continue;
            }

            if (!catalog.TryGetPlatformDamage(platformId, out var damage))
            {
                continue;
            }

            var drain = AmbientTickDrainHpPct(damage);
            if (drain <= 0)
            {
                continue;
            }

            var newHp = Math.Max(0.0, previousHp - drain);
            if (Math.Abs(newHp - previousHp) < 1e-9)
            {
                continue;
            }

            ledger.SetHpPct(platformId, newHp);
            changes.Add(new DamageChange(
                platformId,
                previousHp,
                newHp,
                PlatformDamageChangeReasonCodes.AmbientTick));
        }

        return changes;
    }

    public static IReadOnlyList<DamageChange> ApplySortedOutcomes(
        PlatformHpLedger ledger,
        ICatalogReader catalog,
        IReadOnlyList<OutcomeApply> outcomes)
    {
        if (outcomes.Count == 0)
        {
            return Array.Empty<DamageChange>();
        }

        var damageOutcomes = outcomes
            .Select(o => new EngagementDamageOutcome(o.EngagementId, o.SequenceId, o.OutcomeCode))
            .Where(o => o.OutcomeCode is EngagementOutcomeCodes.Hit or EngagementOutcomeCodes.Kill)
            .ToArray();
        if (damageOutcomes.Length == 0)
        {
            return Array.Empty<DamageChange>();
        }

        var sorted = DeterministicDamageApplyBatch.Sort(damageOutcomes);
        var changes = new List<DamageChange>(sorted.Count);
        foreach (var outcome in sorted)
        {
            var apply = outcomes.First(o =>
                o.EngagementId == outcome.EngagementId &&
                o.SequenceId == outcome.SequenceId);
            var platformId = apply.VictimPlatformId;
            if (!ledger.TryGetHpPct(platformId, out var previousHp))
            {
                continue;
            }

            if (!catalog.TryGetPlatformDamage(platformId, out _))
            {
                continue;
            }

            var newHp = outcome.OutcomeCode switch
            {
                EngagementOutcomeCodes.Kill => 0.0,
                EngagementOutcomeCodes.Hit => Math.Max(0.0, previousHp - HitDamageHpPct),
                _ => previousHp,
            };
            if (Math.Abs(newHp - previousHp) < 1e-9)
            {
                continue;
            }

            ledger.SetHpPct(platformId, newHp);
            changes.Add(new DamageChange(
                platformId,
                previousHp,
                newHp,
                outcome.OutcomeCode));
        }

        return changes;
    }

    public static IReadOnlyList<ScenarioWithdrawReadinessTrial> ResolveWithdrawTrials(
        PlatformHpLedger ledger,
        ICatalogReader catalog)
    {
        var trials = new List<ScenarioWithdrawReadinessTrial>();
        foreach (var platformId in ledger.GetSortedPlatformIds())
        {
            var hpPct = ledger.TryGetHpPct(platformId, out var hp) ? hp : 100.0;
            if (PhaseBCatalogDamageReadinessStub.TryResolveScenarioTrial(platformId, hpPct, catalog) is { } trial)
            {
                trials.Add(trial);
                continue;
            }

            trials.Add(new ScenarioWithdrawReadinessTrial(
                platformId,
                PhaseBCatalogDamageReadinessStub.NeutralReadinessScore,
                WithdrawRecommended: false,
                CatalogResolved: false));
        }

        return trials;
    }
}

/// <summary>Order-log reason codes for bounded platform damage apply.</summary>
public static class PlatformDamageChangeReasonCodes
{
    public const string AmbientTick = "CATALOG_AMBIENT_TICK";

    public const string Hit = EngagementOutcomeCodes.Hit;

    public const string Kill = EngagementOutcomeCodes.Kill;
}