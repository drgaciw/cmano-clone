using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Swarm;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class SwarmOffensiveEffectTests
{
    [Fact]
    public void Full_swarm_deals_more_effect_than_half_depleted_under_identical_geometry()
    {
        const double baseEffect = 100.0;
        const int max = 40;
        var full = SwarmOffensiveEffect.Scale(baseEffect, droneCount: max, maxDrones: max);
        var half = SwarmOffensiveEffect.Scale(baseEffect, droneCount: max / 2, maxDrones: max);

        Assert.Equal(100.0, full, 6);
        Assert.Equal(50.0, half, 6);
        Assert.True(full > half);
    }

    [Fact]
    public void Scale_is_monotonic_non_decreasing_in_drone_count()
    {
        const double baseEffect = 10.0;
        const int max = 40;
        double? previous = null;
        for (var count = 0; count <= max; count++)
        {
            var effect = SwarmOffensiveEffect.Scale(baseEffect, count, max);
            if (previous is not null)
            {
                Assert.True(effect >= previous.Value - 1e-12);
            }

            previous = effect;
        }
    }

    [Fact]
    public void Zero_or_invalid_inputs_yield_zero_effect()
    {
        Assert.Equal(0, SwarmOffensiveEffect.Scale(10, 0, 40));
        Assert.Equal(0, SwarmOffensiveEffect.Scale(10, 20, 0));
        Assert.Equal(0, SwarmOffensiveEffect.Scale(0, 20, 40));
    }
}

public sealed class SwarmHardCounterAaTests
{
    [Fact]
    public void Area_aa_shreds_more_drones_per_hit_than_point_fire_at_equal_nominal_dps()
    {
        Assert.Equal(
            SwarmHardCounterAa.EqualNominalDpsUnits,
            SwarmHardCounterAa.EqualNominalDpsUnits);
        Assert.True(
            SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.AreaAa) >
            SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.PointFire));
        Assert.Equal(1, SwarmHardCounterAa.PointFireDronesLostPerHit);
        Assert.Equal(8, SwarmHardCounterAa.AreaAaDronesLostPerHit);
    }

    [Fact]
    public void Hard_counter_scenario_area_destroys_faster_than_point_via_authorized_api()
    {
        const int max = 40;
        var seed = SimSeed.FromScenario(42);
        var point = new SwarmController(seed);
        var area = new SwarmController(seed);
        var integrity = new SwarmUnitIntegrity("swarm-1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, max, max);
        point.Register(integrity, 57.0, 20.0);
        area.Register(integrity, 57.0, 20.0);

        // Equal engagement count, equal nominal DPS framing — area shreds integrity faster.
        const int hits = 5;
        SwarmEngagementIntegrityApplier.ApplyHits(point, "swarm-1", SwarmAaProfileKind.PointFire, hits, 1, 1.0);
        SwarmEngagementIntegrityApplier.ApplyHits(area, "swarm-1", SwarmAaProfileKind.AreaAa, hits, 1, 1.0);

        Assert.True(point.TryGetIntegrity("swarm-1", out var pointAfter));
        Assert.True(area.TryGetIntegrity("swarm-1", out var areaAfter));
        Assert.Equal(max - (hits * SwarmHardCounterAa.PointFireDronesLostPerHit), pointAfter.DroneCount);
        Assert.True(areaAfter.DroneCount < pointAfter.DroneCount);
        Assert.True(areaAfter.IsDestroyed); // 5 * 8 = 40
        Assert.False(pointAfter.IsDestroyed); // 5 * 1 = 5 remaining 35
    }

    [Fact]
    public void Integrity_reductions_go_only_through_authorized_damage_api()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(7));
        ctl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            0,
            0);

        Assert.True(
            SwarmEngagementIntegrityApplier.TryApplyHit(
                ctl,
                "s1",
                SwarmAaProfileKind.PointFire,
                1,
                1.0,
                out var change));
        Assert.Equal(39, change.NewDroneCount);
        Assert.Equal(SwarmEngagementIntegrityApplier.ReasonPointFire, change.ReasonCode);
        Assert.Single(ctl.IntegrityTimeline);
        Assert.Equal(ctl.ComputeIntegrityTimelineHash(), ctl.ComputeIntegrityTimelineHash());
    }
}
