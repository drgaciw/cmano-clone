using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using Xunit;

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

    [Fact]
    public void PhaseB_Legacy_catalog_without_damage_preserves_baseline_readiness()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 15, catalog);

        Assert.False(trial.CatalogResolved);
        Assert.Equal(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore, trial.ReadinessScore);
        Assert.False(trial.WithdrawRecommended);
        Assert.Empty(catalog.GetSortedPlatformDamage());
    }

    [Fact]
    public void PhaseB_Legacy_catalog_preserves_baseline_through_readiness_policy_resolver()
    {
        var profile = CatalogProfile(15);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = WithdrawReadinessTrialResolver.Resolve(profile, catalog);

        Assert.Single(trials);
        Assert.False(trials[0].CatalogResolved);
        Assert.Equal(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore, trials[0].ReadinessScore);
        Assert.False(trials[0].WithdrawRecommended);
    }

    [Fact]
    public void PhaseB_Committed_platform_damage_changes_withdraw_trial_outcome()
    {
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withDamage = DamageFixture();

        var baseline = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 20, legacy);
        var modified = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 20, withDamage);

        Assert.False(baseline.CatalogResolved);
        Assert.False(baseline.WithdrawRecommended);
        Assert.True(modified.CatalogResolved);
        Assert.True(modified.WithdrawRecommended);
        Assert.NotEqual(baseline.ReadinessScore, modified.ReadinessScore);
    }

    [Fact]
    public void PhaseB_Committed_platform_damage_changes_readiness_policy_resolver_outcome()
    {
        var profile = CatalogProfile(20);
        var legacy = InMemoryCatalogReader.BalticPatrolFixture();
        var withDamage = DamageFixture();

        var baseline = WithdrawReadinessTrialResolver.Resolve(profile, legacy);
        var modified = WithdrawReadinessTrialResolver.Resolve(profile, withDamage);

        Assert.False(baseline[0].CatalogResolved);
        Assert.False(baseline[0].WithdrawRecommended);
        Assert.True(modified[0].CatalogResolved);
        Assert.True(modified[0].WithdrawRecommended);
        Assert.NotEqual(baseline[0].ReadinessScore, modified[0].ReadinessScore);
    }

    [Theory]
    [InlineData(25, true)]
    [InlineData(24.99, true)]
    [InlineData(25.01, false)]
    public void PhaseB_Withdraw_threshold_boundary_applies_predictably(double currentHpPct, bool expectedWithdraw)
    {
        var catalog = DamageFixture(withdrawThresholdPct: 25);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", currentHpPct, catalog);

        Assert.Equal(expectedWithdraw, trial.WithdrawRecommended);
    }

    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(50, 0.5)]
    [InlineData(100, 1.0)]
    public void PhaseB_Readiness_score_tracks_hp_pct_when_catalog_resolves(double currentHpPct, double expectedScore)
    {
        var catalog = DamageFixture(withdrawThresholdPct: 0);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", currentHpPct, catalog);

        Assert.True(trial.CatalogResolved);
        Assert.Equal(expectedScore, trial.ReadinessScore, precision: 6);
        Assert.False(trial.WithdrawRecommended);
    }

    [Fact]
    public void PhaseB_Unreferenced_damage_platform_leaves_trial_unchanged()
    {
        var catalog = new InMemoryCatalogReader(
            [
                new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1"),
            ],
            damage: [new CatalogPlatformDamage("other-platform", MaxHp: 50, WithdrawThresholdPct: 10)]);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 5, catalog);

        Assert.False(trial.CatalogResolved);
        Assert.Equal(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore, trial.ReadinessScore);
        Assert.False(trial.WithdrawRecommended);
    }

    [Fact]
    public void PhaseB_Critical_flags_reduce_readiness_score_deterministically()
    {
        var damage = new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 0, CriticalFlags: 2);
        var baseline = PhaseBCatalogDamageReadinessStub.ComputeReadinessScore(80, damage);
        var unflagged = PhaseBCatalogDamageReadinessStub.ComputeReadinessScore(
            80,
            damage with { CriticalFlags = 0 });

        Assert.Equal(0.8, unflagged, precision: 6);
        Assert.Equal(0.6, baseline, precision: 6);
    }

    [Fact]
    public void PhaseB_Damage_rows_sort_by_platform_id()
    {
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage:
            [
                new CatalogPlatformDamage("u2", MaxHp: 80),
                new CatalogPlatformDamage("u1", MaxHp: 100),
            ]);

        Assert.Equal(["u1", "u2"], catalog.GetSortedPlatformDamage().Select(d => d.PlatformId).ToArray());
    }

    [Fact]
    public void PhaseB_Zero_withdraw_threshold_never_recommends_withdraw()
    {
        var catalog = DamageFixture(withdrawThresholdPct: 0);

        var trial = PhaseBCatalogDamageReadinessStub.EvaluateWithdrawReadiness("u1", 0, catalog);

        Assert.True(trial.CatalogResolved);
        Assert.False(trial.WithdrawRecommended);
    }
}