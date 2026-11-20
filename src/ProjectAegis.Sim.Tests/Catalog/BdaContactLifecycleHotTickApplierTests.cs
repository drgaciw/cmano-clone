using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Catalog;

public sealed class BdaContactLifecycleHotTickApplierTests
{
    private static PdDetectionContactSimulator DetectedHostileSimulator()
    {
        var sim = new PdDetectionContactSimulator(
            SimSeed.FromScenario(42),
            [new ScenarioDetectionTrial("u1", "radar-1", "hostile-1", "c1", 1.0)]);
        sim.Tick(1, 1.0);
        return sim;
    }

    [Test]
    public void Bda_lifecycle_disabled_when_combat_domains_flag_off()
    {
        Assert.That(BdaContactLifecycleHotTickApplier.IsEnabled(combatDomainsEnabled: false), Is.False);
    }

    [Test]
    public void Bda_lifecycle_enabled_when_combat_domains_flag_on()
    {
        Assert.That(BdaContactLifecycleHotTickApplier.IsEnabled(combatDomainsEnabled: true), Is.True);
    }

    [TestCase(2, 50.0, PlatformDamageChangeReasonCodes.Hit, false)]
    [TestCase(3, 25.0, PlatformDamageChangeReasonCodes.Hit, true)]
    [TestCase(2, 0.0, PlatformDamageChangeReasonCodes.Hit, true)]
    [TestCase(0, 0.0, PlatformDamageChangeReasonCodes.Kill, true)]
    [TestCase(0, 99.0, PlatformDamageChangeReasonCodes.AmbientTick, false)]
    public void ShouldPromoteToLost_matches_bda_projection_rules(
        int damageLevel,
        double newHpPct,
        string reasonCode,
        bool expected)
    {
        var apply = new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
            "hostile-1",
            damageLevel,
            newHpPct,
            reasonCode);

        Assert.That(BdaContactLifecycleHotTickApplier.ShouldPromoteToLost(apply), Is.EqualTo(expected));
    }

    [Test]
    public void ResolveSortedLostTargets_orders_platform_ids_deterministically()
    {
        var changes = new[]
        {
            new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
                "hostile-2",
                3,
                25,
                PlatformDamageChangeReasonCodes.Hit),
            new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
                "hostile-1",
                2,
                50,
                PlatformDamageChangeReasonCodes.Hit),
            new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
                "hostile-1",
                3,
                0,
                PlatformDamageChangeReasonCodes.Hit),
        };

        var lost = BdaContactLifecycleHotTickApplier.ResolveSortedLostTargets(changes);

        Assert.That(lost, Is.EqualTo(new[] { "hostile-1", "hostile-2" }));
    }

    [Test]
    public void ApplySortedTargets_promotes_contact_fsm_to_lost_at_damage_level_boundary()
    {
        var sim = DetectedHostileSimulator();
        Assert.That(sim.ActiveCount, Is.EqualTo(1));

        var transitions = BdaContactLifecycleHotTickApplier.ApplySortedTargets(
            sim,
            2,
            2.0,
            ["hostile-1"]);

        AssertEx.Single(transitions);
        Assert.That(transitions[0].NewState, Is.EqualTo(ContactLifecycleState.Lost));
        Assert.That(transitions[0].ContactId, Is.EqualTo("c1"));
        Assert.That(sim.ActiveCount, Is.EqualTo(0));
        Assert.That(sim.IsTargetDestroyed("hostile-1"), Is.False);
    }

    [Test]
    public void ApplySortedTargets_skips_damage_level_two_without_lost_transition()
    {
        var sim = DetectedHostileSimulator();

        var transitions = BdaContactLifecycleHotTickApplier.ApplySortedTargets(
            sim,
            2,
            2.0,
            BdaContactLifecycleHotTickApplier.ResolveSortedLostTargets(
            [
                new BdaContactLifecycleHotTickApplier.DamageLifecycleApply(
                    "hostile-1",
                    2,
                    50,
                    PlatformDamageChangeReasonCodes.Hit),
            ]));

        Assert.That(transitions, Is.Empty);
        Assert.That(sim.ActiveCount, Is.EqualTo(1));
    }

    [Test]
    public void ApplyFromRegistry_drains_pending_targets_once_per_promotion()
    {
        var sim = DetectedHostileSimulator();
        var registry = new BdaContactLifecycleRegistry();
        registry.MarkLost("hostile-1");
        registry.MarkLost("hostile-1");

        var first = BdaContactLifecycleHotTickApplier.ApplyFromRegistry(sim, 2, 2.0, registry);
        var second = BdaContactLifecycleHotTickApplier.ApplyFromRegistry(sim, 3, 3.0, registry);

        AssertEx.Single(first);
        Assert.That(second, Is.Empty);
        Assert.That(registry.PromotedCount, Is.EqualTo(1));
    }
}