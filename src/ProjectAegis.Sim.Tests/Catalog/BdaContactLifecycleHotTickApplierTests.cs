using ProjectAegis.Sim.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;
using Xunit;

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

    [Fact]
    public void Bda_lifecycle_disabled_when_combat_domains_flag_off()
    {
        Assert.False(BdaContactLifecycleHotTickApplier.IsEnabled(combatDomainsEnabled: false));
    }

    [Fact]
    public void Bda_lifecycle_enabled_when_combat_domains_flag_on()
    {
        Assert.True(BdaContactLifecycleHotTickApplier.IsEnabled(combatDomainsEnabled: true));
    }

    [Theory]
    [InlineData(2, 50.0, PlatformDamageChangeReasonCodes.Hit, false)]
    [InlineData(3, 25.0, PlatformDamageChangeReasonCodes.Hit, true)]
    [InlineData(2, 0.0, PlatformDamageChangeReasonCodes.Hit, true)]
    [InlineData(0, 0.0, PlatformDamageChangeReasonCodes.Kill, true)]
    [InlineData(0, 99.0, PlatformDamageChangeReasonCodes.AmbientTick, false)]
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

        Assert.Equal(expected, BdaContactLifecycleHotTickApplier.ShouldPromoteToLost(apply));
    }

    [Fact]
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

        Assert.Equal(["hostile-1", "hostile-2"], lost);
    }

    [Fact]
    public void ApplySortedTargets_promotes_contact_fsm_to_lost_at_damage_level_boundary()
    {
        var sim = DetectedHostileSimulator();
        Assert.Equal(1, sim.ActiveCount);

        var transitions = BdaContactLifecycleHotTickApplier.ApplySortedTargets(
            sim,
            2,
            2.0,
            ["hostile-1"]);

        Assert.Single(transitions);
        Assert.Equal(ContactLifecycleState.Lost, transitions[0].NewState);
        Assert.Equal("c1", transitions[0].ContactId);
        Assert.Equal(0, sim.ActiveCount);
        Assert.False(sim.IsTargetDestroyed("hostile-1"));
    }

    [Fact]
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

        Assert.Empty(transitions);
        Assert.Equal(1, sim.ActiveCount);
    }

    [Fact]
    public void ApplyFromRegistry_drains_pending_targets_once_per_promotion()
    {
        var sim = DetectedHostileSimulator();
        var registry = new BdaContactLifecycleRegistry();
        registry.MarkLost("hostile-1");
        registry.MarkLost("hostile-1");

        var first = BdaContactLifecycleHotTickApplier.ApplyFromRegistry(sim, 2, 2.0, registry);
        var second = BdaContactLifecycleHotTickApplier.ApplyFromRegistry(sim, 3, 3.0, registry);

        Assert.Single(first);
        Assert.Empty(second);
        Assert.Equal(1, registry.PromotedCount);
    }
}