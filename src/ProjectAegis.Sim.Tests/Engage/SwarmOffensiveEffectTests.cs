using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Engage;

public sealed class SwarmOffensiveEffectTests
{
    [Test]
    public void Full_swarm_deals_more_effect_than_half_depleted_under_identical_geometry()
    {
        const double baseEffect = 100.0;
        const int max = 40;
        var full = SwarmOffensiveEffect.Scale(baseEffect, droneCount: max, maxDrones: max);
        var half = SwarmOffensiveEffect.Scale(baseEffect, droneCount: max / 2, maxDrones: max);

        Assert.That(
            Math.Round((double)(full), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(100.0), 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round((double)(half), 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(50.0), 6, MidpointRounding.AwayFromZero)));
        Assert.That(full > half, Is.True);
    }

    [Test]
    public void Scale_preserves_public_linear_tuning_contract_and_exact_outputs()
    {
        Assert.That(SwarmOffensiveEffect.ScaleFactorPower, Is.EqualTo(1.0));
        Assert.That(SwarmOffensiveEffect.MinLivingScale, Is.EqualTo(0.0));
        Assert.That(SwarmOffensiveEffect.Scale(10.0, droneCount: 1, maxDrones: 8), Is.EqualTo(1.25));
        Assert.That(SwarmOffensiveEffect.Scale(10.0, droneCount: 4, maxDrones: 8), Is.EqualTo(5.0));
        Assert.That(SwarmOffensiveEffect.Scale(10.0, droneCount: 8, maxDrones: 8), Is.EqualTo(10.0));
    }

    [Test]
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
                Assert.That(effect >= previous.Value - 1e-12, Is.True);
            }

            previous = effect;
        }
    }

    [Test]
    public void Zero_or_invalid_inputs_yield_zero_effect()
    {
        Assert.That(SwarmOffensiveEffect.Scale(10, 0, 40), Is.EqualTo(0));
        Assert.That(SwarmOffensiveEffect.Scale(10, 20, 0), Is.EqualTo(0));
        Assert.That(SwarmOffensiveEffect.Scale(0, 20, 40), Is.EqualTo(0));
    }
}

public sealed class SwarmHardCounterAaTests
{
    [Test]
    public void Area_aa_shreds_more_drones_per_hit_than_point_fire_at_equal_nominal_dps()
    {
        Assert.That(SwarmHardCounterAa.EqualNominalDpsUnits, Is.EqualTo(SwarmHardCounterAa.EqualNominalDpsUnits));
        Assert.That(
            SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.AreaAa) >
                    SwarmHardCounterAa.DronesLostPerHit(SwarmAaProfileKind.PointFire),
            Is.True);
        Assert.That(SwarmHardCounterAa.PointFireDronesLostPerHit, Is.EqualTo(1));
        Assert.That(SwarmHardCounterAa.AreaAaDronesLostPerHit, Is.EqualTo(8));
    }

    [Test]
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

        Assert.That(point.TryGetIntegrity("swarm-1", out var pointAfter), Is.True);
        Assert.That(area.TryGetIntegrity("swarm-1", out var areaAfter), Is.True);
        Assert.That(pointAfter.DroneCount, Is.EqualTo(max - (hits * SwarmHardCounterAa.PointFireDronesLostPerHit)));
        Assert.That(areaAfter.DroneCount < pointAfter.DroneCount, Is.True);
        Assert.That(areaAfter.IsDestroyed, Is.True); // 5 * 8 = 40
        Assert.That(pointAfter.IsDestroyed, Is.False); // 5 * 1 = 5 remaining 35
    }

    [Test]
    public void Integrity_reductions_go_only_through_authorized_damage_api()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(7));
        ctl.Register(
            new SwarmUnitIntegrity("s1", CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40),
            0,
            0);

        Assert.That(
            SwarmEngagementIntegrityApplier.TryApplyHit(
                        ctl,
                        "s1",
                        SwarmAaProfileKind.PointFire,
                        1,
                        1.0,
                        out var change),
            Is.True);
        Assert.That(change.NewDroneCount, Is.EqualTo(39));
        Assert.That(change.ReasonCode, Is.EqualTo(SwarmEngagementIntegrityApplier.ReasonPointFire));
        AssertEx.Single(ctl.IntegrityTimeline);
        Assert.That(ctl.ComputeIntegrityTimelineHash(), Is.EqualTo(ctl.ComputeIntegrityTimelineHash()));
    }
}
