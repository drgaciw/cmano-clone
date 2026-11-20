using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class CatalogDamageHotTickApplierTests
{
    private static InMemoryCatalogReader DamageFixture(double withdrawThresholdPct = 25) =>
        new(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            "hot-tick-damage-fixture",
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: withdrawThresholdPct)]);

    private static PlatformHpLedger Ledger(double hpPct = 26) =>
        PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", hpPct)]);

    [Test]
    public void HotTick_Disabled_when_combat_domains_flag_off()
    {
        Assert.That(CatalogDamageHotTickApplier.IsEnabled(combatDomainsEnabled: false, catalogWithdrawTargetCount: 1), Is.False);
    }

    [Test]
    public void HotTick_Enabled_when_combat_domains_on_and_catalog_withdraw_present()
    {
        Assert.That(CatalogDamageHotTickApplier.IsEnabled(combatDomainsEnabled: true, catalogWithdrawTargetCount: 1), Is.True);
    }

    [Test]
    public void HotTick_Ambient_drain_crosses_withdraw_threshold_deterministically()
    {
        var ledger = Ledger(hpPct: 26);
        var catalog = DamageFixture();

        var first = CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledger, catalog);
        var second = CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledger, catalog);

        AssertEx.Single(first);
        AssertEx.Single(second);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(24.0), 6, MidpointRounding.AwayFromZero)));
        var trials = CatalogDamageHotTickApplier.ResolveWithdrawTrials(ledger, catalog);
        Assert.That(trials[0].WithdrawRecommended, Is.True);
    }

    [Test]
    public void HotTick_Missing_damage_row_skips_apply_and_preserves_hp()
    {
        var ledger = Ledger(hpPct: 40);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var changes = CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledger, catalog);

        Assert.That(changes, Is.Empty);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(40.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void HotTick_Zero_max_hp_produces_zero_ambient_drain()
    {
        var ledger = Ledger(hpPct: 50);
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage: [new CatalogPlatformDamage("u1", MaxHp: 0, WithdrawThresholdPct: 0)]);

        Assert.That(CatalogDamageHotTickApplier.AmbientTickDrainHpPct(catalog.GetSortedPlatformDamage()[0]), Is.EqualTo(0.0));
        Assert.That(CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledger, catalog), Is.Empty);
    }

    [Test]
    public void HotTick_Hit_and_kill_outcomes_apply_in_sorted_order()
    {
        var ledger = Ledger(hpPct: 80);
        var catalog = DamageFixture(withdrawThresholdPct: 0);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 30, 1, EngagementOutcomeCodes.Miss),
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 10, 2, EngagementOutcomeCodes.Hit),
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 20, 3, EngagementOutcomeCodes.Hit),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[0].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(changes[1].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(30.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void HotTick_Repeat_apply_is_deterministic()
    {
        var ledgerA = Ledger(hpPct: 26);
        var ledgerB = Ledger(hpPct: 26);
        var catalog = DamageFixture();

        CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledgerA, catalog);
        CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledgerA, catalog);
        CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledgerB, catalog);
        CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledgerB, catalog);

        Assert.That(ledgerB.ComputeWorldHashMix(), Is.EqualTo(ledgerA.ComputeWorldHashMix()));
        Assert.That(
            CatalogDamageHotTickApplier.ResolveWithdrawTrials(ledgerB, catalog)[0].WithdrawRecommended,
            Is.EqualTo(CatalogDamageHotTickApplier.ResolveWithdrawTrials(ledgerA, catalog)[0].WithdrawRecommended));
    }

    [TestCase(25.01, false)]
    [TestCase(25.0, true)]
    [TestCase(24.99, true)]
    public void HotTick_Withdraw_boundary_tracks_live_hp(double hpPct, bool expectedWithdraw)
    {
        var ledger = Ledger(hpPct);
        var catalog = DamageFixture();

        var trial = CatalogDamageHotTickApplier.ResolveWithdrawTrials(ledger, catalog)[0];

        Assert.That(trial.WithdrawRecommended, Is.EqualTo(expectedWithdraw));
    }

    [Test]
    public void HotTick_Hit_damage_level_bounded_0_to_3_from_severity_and_resilience()
    {
        var ledger = Ledger(hpPct: 100);
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage:
            [
                new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 0, Resilience: 2.0),
            ]);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply(
                "u1",
                10,
                1,
                EngagementOutcomeCodes.Hit,
                HitSeverity: 1.0),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        AssertEx.Single(changes);
        Assert.That(changes[0].DamageLevel, Is.EqualTo(2));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(50.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void HotTick_Zero_resilience_hit_produces_no_hp_change()
    {
        var ledger = Ledger(hpPct: 80);
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage:
            [
                new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 0, Resilience: 0.0),
            ]);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 10, 1, EngagementOutcomeCodes.Hit),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        Assert.That(changes, Is.Empty);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(80.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void HotTick_Kill_outcome_zeroes_hp_after_prior_hit()
    {
        var ledger = Ledger(hpPct: 80);
        var catalog = DamageFixture(withdrawThresholdPct: 0);
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 10, 1, EngagementOutcomeCodes.Hit),
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 20, 2, EngagementOutcomeCodes.Kill),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        Assert.That(changes.Count, Is.EqualTo(2));
        Assert.That(changes[0].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Hit));
        Assert.That(changes[1].ReasonCode, Is.EqualTo(EngagementOutcomeCodes.Kill));
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.0), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void HotTick_Missing_damage_row_skips_hit_apply()
    {
        var ledger = Ledger(hpPct: 80);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var outcomes = new[]
        {
            new CatalogDamageHotTickApplier.OutcomeApply("u1", 10, 1, EngagementOutcomeCodes.Hit),
        };

        var changes = CatalogDamageHotTickApplier.ApplySortedOutcomes(ledger, catalog, outcomes);

        Assert.That(changes, Is.Empty);
        Assert.That(
            Math.Round((double)(ledger.TryGetHpPct("u1", out var hp) ? hp : -1), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(80.0), 6, MidpointRounding.AwayFromZero)));
    }
}