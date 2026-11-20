using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Scenario;

public sealed class WithdrawReadinessTrialResolverTests
{
    [Test]
    public void Catalog_withdraw_builds_trials_with_neutral_readiness_when_damage_absent()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u1", 80),
            ]);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = WithdrawReadinessTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(trials[0].CatalogResolved, Is.False);
        Assert.That(trials[0].WithdrawRecommended, Is.False);
    }

    [Test]
    public void Explicit_withdraw_trials_take_precedence_over_catalog()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            withdrawReadinessTrials:
            [
                new ScenarioWithdrawReadinessTrial("u1", 0.25, WithdrawRecommended: true, CatalogResolved: true),
            ],
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u2", 10),
            ]);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = WithdrawReadinessTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].ReadinessScore, Is.EqualTo(0.25));
        Assert.That(trials[0].WithdrawRecommended, Is.True);
    }

    [Test]
    public void Catalog_withdraw_without_damage_rows_emits_neutral_unresolved_trial()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u1", 15),
            ]);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();

        var trials = WithdrawReadinessTrialResolver.Resolve(profile, catalog);

        AssertEx.Single(trials);
        Assert.That(trials[0].CatalogResolved, Is.False);
        Assert.That(trials[0].WithdrawRecommended, Is.False);
        Assert.That(trials[0].ReadinessScore, Is.EqualTo(PhaseBCatalogDamageReadinessStub.NeutralReadinessScore));
    }

    [Test]
    public void Readiness_policy_evaluator_merges_scenario_launch_readiness_with_catalog_trial()
    {
        var profile = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            unitReadiness: new Dictionary<string, bool> { ["u1"] = false },
            catalogWithdrawTargets:
            [
                new ScenarioCatalogWithdrawTarget("u1", 20),
            ]);
        var catalog = new InMemoryCatalogReader(
            [new CatalogSensorBinding("u1", "radar-1", 1.0, "fixture-radar1")],
            damage: [new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25)]);

        var effective = ReadinessPolicyEvaluator.EvaluateUnit("u1", profile, catalog);

        Assert.That(effective.ReadyForLaunch, Is.False);
        Assert.That(effective.CatalogResolved, Is.True);
        Assert.That(effective.WithdrawRecommended, Is.True);
        Assert.That(effective.ReadinessScore, Is.Not.EqualTo(1.0));
    }
}