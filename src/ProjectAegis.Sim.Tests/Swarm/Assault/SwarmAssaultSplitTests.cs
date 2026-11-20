using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Assault;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Swarm.Assault;

/// <summary>DRG-106 / SWARM-C2: multi-axis auto-split assault planner (SWARM-17).</summary>
public sealed class SwarmAssaultSplitTests
{
    [Test]
    public void Assault_splits_mass_across_K_axes_summing_to_droneCount()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 40,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 106,
            doctrineAllowSplit: true,
            targetBearingDeg: 90.0);

        Assert.That(plan.SplitApplied, Is.True);
        Assert.That(plan.RequestedAxisCount, Is.EqualTo(3));
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(3));
        Assert.That(plan.Axes.Count, Is.EqualTo(3));
        Assert.That(plan.TotalDroneShare, Is.EqualTo(40));

        foreach (var axis in plan.Axes)
        {
            Assert.That(axis.DroneShare >= 1, Is.True);
        }

        Assert.That(plan.Axes[0].AxisIndex, Is.EqualTo(0));
        Assert.That(plan.Axes[1].AxisIndex, Is.EqualTo(1));
        Assert.That(plan.Axes[2].AxisIndex, Is.EqualTo(2));
    }

    [Test]
    public void Default_two_axis_assault_applies_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 10,
            axisCount: SwarmAssaultAxisSplitter.DefaultAxisCount,
            mode: SwarmOperationalMode.Assault,
            seed: 1);

        Assert.That(plan.SplitApplied, Is.True);
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(2));
        Assert.That(plan.TotalDroneShare, Is.EqualTo(10));
        Assert.That(plan.Axes[0].DroneShare, Is.EqualTo(5));
        Assert.That(plan.Axes[1].DroneShare, Is.EqualTo(5));
    }

    [Test]
    public void Non_Assault_mode_returns_single_axis_without_split()
    {
        foreach (var mode in new[]
                 {
                     SwarmOperationalMode.Hold,
                     SwarmOperationalMode.Screen,
                     SwarmOperationalMode.Scatter,
                     SwarmOperationalMode.Rejoin,
                 })
        {
            var plan = SwarmAssaultAxisSplitter.Plan(
                droneCount: 20,
                axisCount: 4,
                mode: mode,
                seed: 42,
                doctrineAllowSplit: true,
                targetBearingDeg: 45.0);

            Assert.That(plan.SplitApplied, Is.False);
            Assert.That(plan.EffectiveAxisCount, Is.EqualTo(1));
            AssertEx.Single(plan.Axes);
            Assert.That(plan.Axes[0].DroneShare, Is.EqualTo(20));
            Assert.That(plan.Axes[0].ApproachBearingDeg, Is.EqualTo(45.0));
        }
    }

    [Test]
    public void Doctrine_disallow_returns_single_axis_without_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 30,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 7,
            doctrineAllowSplit: false,
            targetBearingDeg: 180.0);

        Assert.That(plan.SplitApplied, Is.False);
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(1));
        AssertEx.Single(plan.Axes);
        Assert.That(plan.Axes[0].DroneShare, Is.EqualTo(30));
        Assert.That(plan.Axes[0].ApproachBearingDeg, Is.EqualTo(180.0));
    }

    [Test]
    public void Axis_count_below_two_does_not_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 12,
            axisCount: 1,
            mode: SwarmOperationalMode.Assault,
            seed: 3,
            doctrineAllowSplit: true);

        Assert.That(plan.SplitApplied, Is.False);
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(1));
        Assert.That(plan.TotalDroneShare, Is.EqualTo(12));
    }

    [Test]
    public void Reduces_K_when_droneCount_less_than_requested_axes()
    {
        // 3 drones, K=5 → effective K=3, min 1 per axis.
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 3,
            axisCount: 5,
            mode: SwarmOperationalMode.Assault,
            seed: 99,
            doctrineAllowSplit: true);

        Assert.That(plan.SplitApplied, Is.True);
        Assert.That(plan.RequestedAxisCount, Is.EqualTo(5));
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(3));
        Assert.That(plan.Axes.Count, Is.EqualTo(3));
        Assert.That(plan.TotalDroneShare, Is.EqualTo(3));
        AssertEx.All(plan.Axes, a => Assert.That(a.DroneShare, Is.EqualTo(1)));
    }

    [Test]
    public void Single_drone_cannot_multi_axis_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 1,
            axisCount: 4,
            mode: SwarmOperationalMode.Assault,
            seed: 11,
            doctrineAllowSplit: true);

        Assert.That(plan.SplitApplied, Is.False);
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(1));
        Assert.That(plan.TotalDroneShare, Is.EqualTo(1));
    }

    [Test]
    public void Zero_drones_returns_empty_plan()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 0,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            doctrineAllowSplit: true);

        Assert.That(plan.SplitApplied, Is.False);
        Assert.That(plan.EffectiveAxisCount, Is.EqualTo(0));
        Assert.That(plan.Axes, Is.Empty);
        Assert.That(plan.TotalDroneShare, Is.EqualTo(0));
    }

    [Test]
    public void Negative_drones_returns_empty_plan()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: -5,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 1);

        Assert.That(plan.SplitApplied, Is.False);
        Assert.That(plan.Axes, Is.Empty);
    }

    [Test]
    public void Same_seed_is_deterministic()
    {
        var a = SwarmAssaultAxisSplitter.Plan(
            droneCount: 17,
            axisCount: 4,
            mode: SwarmOperationalMode.Assault,
            seed: 20260809,
            doctrineAllowSplit: true,
            targetBearingDeg: 12.5);

        var b = SwarmAssaultAxisSplitter.Plan(
            droneCount: 17,
            axisCount: 4,
            mode: SwarmOperationalMode.Assault,
            seed: 20260809,
            doctrineAllowSplit: true,
            targetBearingDeg: 12.5);

        Assert.That(b.SplitApplied, Is.EqualTo(a.SplitApplied));
        Assert.That(b.EffectiveAxisCount, Is.EqualTo(a.EffectiveAxisCount));
        Assert.That(b.Axes.Count, Is.EqualTo(a.Axes.Count));
        for (var i = 0; i < a.Axes.Count; i++)
        {
            Assert.That(b.Axes[i].AxisIndex, Is.EqualTo(a.Axes[i].AxisIndex));
            Assert.That(b.Axes[i].DroneShare, Is.EqualTo(a.Axes[i].DroneShare));
            Assert.That(b.Axes[i].ApproachBearingDeg, Is.EqualTo(a.Axes[i].ApproachBearingDeg));
        }
    }

    [Test]
    public void Different_seeds_can_vary_remainder_assignment()
    {
        // 10 drones / 3 axes → base 3, remainder 1 — seed picks who gets +1.
        var seen = new HashSet<string>();
        for (ulong seed = 0; seed < 64; seed++)
        {
            var plan = SwarmAssaultAxisSplitter.Plan(
                droneCount: 10,
                axisCount: 3,
                mode: SwarmOperationalMode.Assault,
                seed: seed);

            Assert.That(plan.SplitApplied, Is.True);
            Assert.That(plan.TotalDroneShare, Is.EqualTo(10));
            var key = string.Join(",", plan.Axes.Select(a => a.DroneShare));
            seen.Add(key);
        }

        // At least two distinct remainder assignments across seeds.
        Assert.That(seen.Count >= 2, Is.True, $"expected seed variety, got: {string.Join(" | ", seen)}");
    }

    [Test]
    public void Approach_bearings_fan_around_target_bearing()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 30,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            targetBearingDeg: 90.0);

        Assert.That(plan.SplitApplied, Is.True);
        var step = SwarmAssaultAxisSplitter.DefaultAxisSpreadDeg;
        Assert.That(
            Math.Round(plan.Axes[0].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(90.0 - step, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(plan.Axes[1].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(90.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(plan.Axes[2].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(90.0 + step, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Two_axis_bearings_are_symmetric_about_target()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 8,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 5,
            targetBearingDeg: 0.0);

        var half = SwarmAssaultAxisSplitter.DefaultAxisSpreadDeg / 2.0;
        // mid for K=2 is 0.5 → offsets -0.5*30 and +0.5*30; normalized to [0,360).
        Assert.That(
            Math.Round(plan.Axes[0].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(360.0 - half, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(plan.Axes[1].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(half, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Bearings_normalize_into_zero_three_sixty()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 6,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            targetBearingDeg: 350.0);

        foreach (var axis in plan.Axes)
        {
            Assert.That(axis.ApproachBearingDeg, Is.InRange(0.0, 360.0 - double.Epsilon));
        }

        // 350 - 15 = 335; 350 + 15 = 365 → 5
        Assert.That(
            Math.Round(plan.Axes[0].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(335.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(plan.Axes[1].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(5.0, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Min_one_drone_per_axis_when_split_applied()
    {
        for (var drones = 2; drones <= 20; drones++)
        {
            for (var k = 2; k <= 8; k++)
            {
                var plan = SwarmAssaultAxisSplitter.Plan(
                    droneCount: drones,
                    axisCount: k,
                    mode: SwarmOperationalMode.Assault,
                    seed: (ulong)(drones * 100 + k));

                if (!plan.SplitApplied)
                {
                    continue;
                }

                Assert.That(plan.EffectiveAxisCount >= 2, Is.True);
                Assert.That(plan.TotalDroneShare, Is.EqualTo(drones));
                AssertEx.All(plan.Axes, a => Assert.That(a.DroneShare >= 1, Is.True));
                Assert.That(plan.EffectiveAxisCount, Is.EqualTo(Math.Min(k, drones)));
            }
        }
    }

    [Test]
    public void Null_target_bearing_defaults_to_zero_base()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 4,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            targetBearingDeg: null);

        var half = SwarmAssaultAxisSplitter.DefaultAxisSpreadDeg / 2.0;
        Assert.That(
            Math.Round(plan.Axes[0].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(360.0 - half, 6, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round(plan.Axes[1].ApproachBearingDeg, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(half, 6, MidpointRounding.AwayFromZero)));
    }
}
