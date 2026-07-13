namespace ProjectAegis.Delegation.Sim;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Scenario;

/// <summary>Ledger-backed catalog hot-tick damage apply and withdraw trial refresh (S29-09).</summary>
public sealed class CatalogDamageHotTickTracker
{
    private readonly PlatformHpLedger _ledger;
    private readonly ICatalogReader _catalog;
    private readonly ScenarioMineHazardSettings? _mineHazard;
    private readonly SimSeed _seed;

    public CatalogDamageHotTickTracker(
        PlatformHpLedger ledger,
        ICatalogReader catalog,
        SimSeed seed,
        ScenarioMineHazardSettings? mineHazard = null)
    {
        _ledger = ledger;
        _catalog = catalog;
        _seed = seed;
        _mineHazard = mineHazard;
    }

    public static CatalogDamageHotTickTracker? TryCreate(
        ScenarioPolicyProfile? profile,
        bool combatDomainsEnabled,
        ICatalogReader? catalog,
        int globalSeed)
    {
        if (profile == null || catalog == null)
        {
            return null;
        }

        var catalogHotTickEnabled = CatalogDamageHotTickApplier.IsEnabled(
            combatDomainsEnabled,
            profile.CatalogWithdrawTargets.Count);
        var mineHazardEnabled = MineTransitHazardHotTickApplier.IsEnabled(
            combatDomainsEnabled,
            profile.MineHazard);
        if (!catalogHotTickEnabled && !mineHazardEnabled)
        {
            return null;
        }

        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(profile.CatalogWithdrawTargets);
        return new CatalogDamageHotTickTracker(
            ledger,
            catalog,
            SimSeed.FromScenario((ulong)globalSeed),
            profile.MineHazard);
    }

    public PlatformHpLedger Ledger => _ledger;

    public CatalogDamageHotTickTickResult ApplyTick(
        ulong simTick,
        double simTime,
        IReadOnlyList<CatalogDamageHotTickApplier.OutcomeApply> engagementOutcomes)
    {
        var changes = new List<PlatformDamageChangeRecord>();
        foreach (var change in CatalogDamageHotTickApplier.ApplyAmbientTickDrain(_ledger, _catalog))
        {
            changes.Add(ToRecord(simTick, simTime, change));
        }

        foreach (var change in CatalogDamageHotTickApplier.ApplySortedOutcomes(_ledger, _catalog, engagementOutcomes))
        {
            changes.Add(ToRecord(simTick, simTime, change));
        }

        if (_mineHazard != null)
        {
            foreach (var change in MineTransitHazardHotTickApplier.ApplyTransitHazardTick(
                         _seed,
                         simTick,
                         _ledger,
                         _catalog,
                         _mineHazard))
            {
                changes.Add(ToRecord(simTick, simTime, change));
            }
        }

        var trials = CatalogDamageHotTickApplier.ResolveWithdrawTrials(_ledger, _catalog);
        return new CatalogDamageHotTickTickResult(changes, trials, _ledger.ComputeWorldHashMix());
    }

    private static PlatformDamageChangeRecord ToRecord(
        ulong simTick,
        double simTime,
        CatalogDamageHotTickApplier.DamageChange change) =>
        new(
            SequenceId: 0,
            simTime,
            simTick,
            new TargetId(change.PlatformId),
            change.PreviousHpPct,
            change.NewHpPct,
            change.ReasonCode,
            change.DamageLevel);
}

public sealed record CatalogDamageHotTickTickResult(
    IReadOnlyList<PlatformDamageChangeRecord> Changes,
    IReadOnlyList<ScenarioWithdrawReadinessTrial> WithdrawTrials,
    ulong WorldHashMix);