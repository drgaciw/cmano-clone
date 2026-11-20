using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class PhaseBDamageCatalogConsumerTests
{
    private static ScenarioPolicyProfile CatalogProfile(double currentHpPct = 20) =>
        new(
            EffectivePolicy.DefaultFree,
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u1", currentHpPct),
            ]);

    private static InMemoryCatalogReader DamageFixture(
        double withdrawThresholdPct = 25,
        int criticalFlags = 0) =>
        new(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            "phase-b-damage-fixture",
            damage:
            [
                new CatalogPlatformDamage(
                    "u1",
                    MaxHp: 100,
                    WithdrawThresholdPct: withdrawThresholdPct,
                    CriticalFlags: criticalFlags),
            ]);

    private static InMemoryCatalogReader BalticPatrolWithSeededDamage(
        double withdrawThresholdPct = 25,
        int criticalFlags = 0)
    {
        var baltic = InMemoryCatalogReader.BalticPatrolFixture();
        return new InMemoryCatalogReader(
            baltic.GetSortedSensorBindings(),
            $"{baltic.LayerVersion}+damage",
            CatalogValidationDefaults.BalticPlatforms(),
            damage:
            [
                new CatalogPlatformDamage(
                    "u1",
                    MaxHp: 100,
                    WithdrawThresholdPct: withdrawThresholdPct,
                    CriticalFlags: criticalFlags),
            ]);
    }

    [Test]
    public void PhaseB_Legacy_catalog_without_damage_preserves_baseline_readiness()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 15, catalog);

        Assert.That(trial.CatalogResolved, Is.False);
        Assert.That(trial.ReadinessScore, Is.EqualTo(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore));
        Assert.That(trial.WithdrawRecommended, Is.False);
        Assert.That(catalog.GetSortedPlatformDamage(), Is.Empty);
    }

    [Test]
    public void PhaseB_Legacy_catalog_preserves_baseline_through_readiness_policy_resolver()
    {
        var profile = CatalogProfile(15);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = WithdrawReadinessTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].CatalogResolved, Is.False);
        Assert.That(trials[0].ReadinessScore, Is.EqualTo(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore));
        Assert.That(trials[0].WithdrawRecommended, Is.False);
    }

    [Test]
    public void PhaseB_Committed_platform_damage_changes_withdraw_trial_outcome()
    {
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withDamage = DamageFixture();

        var baseline = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 20, legacy);
        var modified = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 20, withDamage);

        Assert.That(baseline.CatalogResolved, Is.False);
        Assert.That(baseline.WithdrawRecommended, Is.False);
        Assert.That(modified.CatalogResolved, Is.True);
        Assert.That(modified.WithdrawRecommended, Is.True);
        Assert.That(modified.ReadinessScore, Is.Not.EqualTo(baseline.ReadinessScore));
    }

    [Test]
    public void PhaseB_Committed_platform_damage_changes_readiness_policy_resolver_outcome()
    {
        var profile = CatalogProfile(20);
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withDamage = DamageFixture();

        var baseline = WithdrawReadinessTrialResolver.Resolve(profile, legacy);
        var modified = WithdrawReadinessTrialResolver.Resolve(profile, withDamage);

        Assert.That(baseline[0].CatalogResolved, Is.False);
        Assert.That(baseline[0].WithdrawRecommended, Is.False);
        Assert.That(modified[0].CatalogResolved, Is.True);
        Assert.That(modified[0].WithdrawRecommended, Is.True);
        Assert.That(modified[0].ReadinessScore, Is.Not.EqualTo(baseline[0].ReadinessScore));
    }

    [TestCase(25, true)]
    [TestCase(24.99, true)]
    [TestCase(25.01, false)]
    public void PhaseB_Withdraw_threshold_boundary_applies_predictably(double currentHpPct, bool expectedWithdraw)
    {
        var catalog = DamageFixture(withdrawThresholdPct: 25);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", currentHpPct, catalog);

        Assert.That(trial.WithdrawRecommended, Is.EqualTo(expectedWithdraw));
    }

    [TestCase(0, 0.0)]
    [TestCase(50, 0.5)]
    [TestCase(100, 1.0)]
    public void PhaseB_Readiness_score_tracks_hp_pct_when_catalog_resolves(double currentHpPct, double expectedScore)
    {
        var catalog = DamageFixture(withdrawThresholdPct: 0);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", currentHpPct, catalog);

        Assert.That(trial.CatalogResolved, Is.True);
        Assert.That(
            Math.Round((double)(trial.ReadinessScore), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(expectedScore), 6, MidpointRounding.AwayFromZero)));
        Assert.That(trial.WithdrawRecommended, Is.False);
    }

    [Test]
    public void PhaseB_Unreferenced_damage_platform_leaves_trial_unchanged()
    {
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            damage: [new CatalogPlatformDamage("other-platform", MaxHp: 50, WithdrawThresholdPct: 10)]);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 5, catalog);

        Assert.That(trial.CatalogResolved, Is.False);
        Assert.That(trial.ReadinessScore, Is.EqualTo(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore));
        Assert.That(trial.WithdrawRecommended, Is.False);
    }

    [Test]
    public void PhaseB_Critical_flags_reduce_readiness_score_deterministically()
    {
        var damage = new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 0, CriticalFlags: 2);
        var baseline = PhaseBCatalogDamageReadinessStub.ComputeReadinessScore(80, damage);
        var unflagged = PhaseBCatalogDamageReadinessStub.ComputeReadinessScore(
            80,
            damage with { CriticalFlags = 0 });

        Assert.That(
            Math.Round((double)(unflagged), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.8), 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round((double)(baseline), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(0.6), 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void PhaseB_Damage_rows_sort_by_platform_id()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage:
            [
                new CatalogPlatformDamage("u2", MaxHp: 80),
                new CatalogPlatformDamage("u1", MaxHp: 100),
            ]);

        Assert.That(catalog.GetSortedPlatformDamage().Select(d => d.PlatformId).ToArray(), Is.EqualTo(new[] { "u1", "u2" }));
    }

    [Test]
    public void PhaseB_Zero_withdraw_threshold_never_recommends_withdraw()
    {
        var catalog = DamageFixture(withdrawThresholdPct: 0);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 0, catalog);

        Assert.That(trial.CatalogResolved, Is.True);
        Assert.That(trial.WithdrawRecommended, Is.False);
    }

    [Test]
    public void PhaseB_Baltic_seeded_damage_fixture_changes_readiness_policy_outcome()
    {
        var profile = CatalogProfile(20);
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withDamage = BalticPatrolWithSeededDamage();

        var baseline = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, legacy);
        var modified = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, withDamage);

        Assert.That(baseline.CatalogResolved, Is.False);
        Assert.That(baseline.WithdrawRecommended, Is.False);
        Assert.That(modified.CatalogResolved, Is.True);
        Assert.That(modified.WithdrawRecommended, Is.True);
        Assert.That(modified.ReadinessScore, Is.Not.EqualTo(baseline.ReadinessScore));
    }

    [Test]
    public void PhaseB_Baltic_seeded_damage_fixture_blocks_engage_gate_when_withdraw_recommended()
    {
        var profile = CatalogProfile(20);
        var catalog = BalticPatrolWithSeededDamage();
        var trials = ReadinessPolicyEvaluator.ResolveCatalogTrials(profile, catalog);

        Assert.That(CatalogDamageWithdrawEngageGate.BlocksEngage("u1", trials), Is.True);
    }

    [Test]
    public void PhaseB_Try_resolve_scenario_trial_returns_null_when_damage_absent()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trial = PhaseBCatalogDamageReadinessStub.TryResolveScenarioTrial("u1", 10, catalog);

        Assert.That(trial, Is.Null);
    }

    [Test]
    public void PhaseB_Hot_tick_ledger_refresh_changes_withdraw_trial_after_ambient_drain()
    {
        var catalog = DamageFixture();
        var ledger = PlatformHpLedger.SeedFromWithdrawTargets(
            [new ScenarioCatalogWithdrawTarget("u1", 26)]);

        CatalogDamageHotTickApplier.ApplyAmbientTickDrain(ledger, catalog);
        var trials = CatalogDamageHotTickApplier.ResolveWithdrawTrials(ledger, catalog);

        Assert.That(trials[0].CatalogResolved, Is.True);
        Assert.That(trials[0].WithdrawRecommended, Is.True);
        Assert.That(CatalogDamageWithdrawEngageGate.BlocksEngage("u1", trials), Is.True);
    }
}