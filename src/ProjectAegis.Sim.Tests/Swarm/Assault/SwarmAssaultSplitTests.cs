using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Assault;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm.Assault;

/// <summary>DRG-106 / SWARM-C2: multi-axis auto-split assault planner (SWARM-17).</summary>
public sealed class SwarmAssaultSplitTests
{
    [Fact]
    public void Assault_splits_mass_across_K_axes_summing_to_droneCount()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 40,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 106,
            doctrineAllowSplit: true,
            targetBearingDeg: 90.0);

        Assert.True(plan.SplitApplied);
        Assert.Equal(3, plan.RequestedAxisCount);
        Assert.Equal(3, plan.EffectiveAxisCount);
        Assert.Equal(3, plan.Axes.Count);
        Assert.Equal(40, plan.TotalDroneShare);

        foreach (var axis in plan.Axes)
        {
            Assert.True(axis.DroneShare >= 1);
        }

        Assert.Equal(0, plan.Axes[0].AxisIndex);
        Assert.Equal(1, plan.Axes[1].AxisIndex);
        Assert.Equal(2, plan.Axes[2].AxisIndex);
    }

    [Fact]
    public void Default_two_axis_assault_applies_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 10,
            axisCount: SwarmAssaultAxisSplitter.DefaultAxisCount,
            mode: SwarmOperationalMode.Assault,
            seed: 1);

        Assert.True(plan.SplitApplied);
        Assert.Equal(2, plan.EffectiveAxisCount);
        Assert.Equal(10, plan.TotalDroneShare);
        Assert.Equal(5, plan.Axes[0].DroneShare);
        Assert.Equal(5, plan.Axes[1].DroneShare);
    }

    [Fact]
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

            Assert.False(plan.SplitApplied);
            Assert.Equal(1, plan.EffectiveAxisCount);
            Assert.Single(plan.Axes);
            Assert.Equal(20, plan.Axes[0].DroneShare);
            Assert.Equal(45.0, plan.Axes[0].ApproachBearingDeg);
        }
    }

    [Fact]
    public void Doctrine_disallow_returns_single_axis_without_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 30,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 7,
            doctrineAllowSplit: false,
            targetBearingDeg: 180.0);

        Assert.False(plan.SplitApplied);
        Assert.Equal(1, plan.EffectiveAxisCount);
        Assert.Single(plan.Axes);
        Assert.Equal(30, plan.Axes[0].DroneShare);
        Assert.Equal(180.0, plan.Axes[0].ApproachBearingDeg);
    }

    [Fact]
    public void Axis_count_below_two_does_not_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 12,
            axisCount: 1,
            mode: SwarmOperationalMode.Assault,
            seed: 3,
            doctrineAllowSplit: true);

        Assert.False(plan.SplitApplied);
        Assert.Equal(1, plan.EffectiveAxisCount);
        Assert.Equal(12, plan.TotalDroneShare);
    }

    [Fact]
    public void Reduces_K_when_droneCount_less_than_requested_axes()
    {
        // 3 drones, K=5 → effective K=3, min 1 per axis.
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 3,
            axisCount: 5,
            mode: SwarmOperationalMode.Assault,
            seed: 99,
            doctrineAllowSplit: true);

        Assert.True(plan.SplitApplied);
        Assert.Equal(5, plan.RequestedAxisCount);
        Assert.Equal(3, plan.EffectiveAxisCount);
        Assert.Equal(3, plan.Axes.Count);
        Assert.Equal(3, plan.TotalDroneShare);
        Assert.All(plan.Axes, a => Assert.Equal(1, a.DroneShare));
    }

    [Fact]
    public void Single_drone_cannot_multi_axis_split()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 1,
            axisCount: 4,
            mode: SwarmOperationalMode.Assault,
            seed: 11,
            doctrineAllowSplit: true);

        Assert.False(plan.SplitApplied);
        Assert.Equal(1, plan.EffectiveAxisCount);
        Assert.Equal(1, plan.TotalDroneShare);
    }

    [Fact]
    public void Zero_drones_returns_empty_plan()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 0,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            doctrineAllowSplit: true);

        Assert.False(plan.SplitApplied);
        Assert.Equal(0, plan.EffectiveAxisCount);
        Assert.Empty(plan.Axes);
        Assert.Equal(0, plan.TotalDroneShare);
    }

    [Fact]
    public void Negative_drones_returns_empty_plan()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: -5,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 1);

        Assert.False(plan.SplitApplied);
        Assert.Empty(plan.Axes);
    }

    [Fact]
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

        Assert.Equal(a.SplitApplied, b.SplitApplied);
        Assert.Equal(a.EffectiveAxisCount, b.EffectiveAxisCount);
        Assert.Equal(a.Axes.Count, b.Axes.Count);
        for (var i = 0; i < a.Axes.Count; i++)
        {
            Assert.Equal(a.Axes[i].AxisIndex, b.Axes[i].AxisIndex);
            Assert.Equal(a.Axes[i].DroneShare, b.Axes[i].DroneShare);
            Assert.Equal(a.Axes[i].ApproachBearingDeg, b.Axes[i].ApproachBearingDeg);
        }
    }

    [Fact]
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

            Assert.True(plan.SplitApplied);
            Assert.Equal(10, plan.TotalDroneShare);
            var key = string.Join(",", plan.Axes.Select(a => a.DroneShare));
            seen.Add(key);
        }

        // At least two distinct remainder assignments across seeds.
        Assert.True(seen.Count >= 2, $"expected seed variety, got: {string.Join(" | ", seen)}");
    }

    [Fact]
    public void Approach_bearings_fan_around_target_bearing()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 30,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            targetBearingDeg: 90.0);

        Assert.True(plan.SplitApplied);
        var step = SwarmAssaultAxisSplitter.DefaultAxisSpreadDeg;
        Assert.Equal(90.0 - step, plan.Axes[0].ApproachBearingDeg, 6);
        Assert.Equal(90.0, plan.Axes[1].ApproachBearingDeg, 6);
        Assert.Equal(90.0 + step, plan.Axes[2].ApproachBearingDeg, 6);
    }

    [Fact]
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
        Assert.Equal(360.0 - half, plan.Axes[0].ApproachBearingDeg, 6);
        Assert.Equal(half, plan.Axes[1].ApproachBearingDeg, 6);
    }

    [Fact]
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
            Assert.InRange(axis.ApproachBearingDeg, 0.0, 360.0 - double.Epsilon);
        }

        // 350 - 15 = 335; 350 + 15 = 365 → 5
        Assert.Equal(335.0, plan.Axes[0].ApproachBearingDeg, 6);
        Assert.Equal(5.0, plan.Axes[1].ApproachBearingDeg, 6);
    }

    [Fact]
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

                Assert.True(plan.EffectiveAxisCount >= 2);
                Assert.Equal(drones, plan.TotalDroneShare);
                Assert.All(plan.Axes, a => Assert.True(a.DroneShare >= 1));
                Assert.Equal(Math.Min(k, drones), plan.EffectiveAxisCount);
            }
        }
    }

    [Fact]
    public void Null_target_bearing_defaults_to_zero_base()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 4,
            axisCount: 2,
            mode: SwarmOperationalMode.Assault,
            seed: 1,
            targetBearingDeg: null);

        var half = SwarmAssaultAxisSplitter.DefaultAxisSpreadDeg / 2.0;
        Assert.Equal(360.0 - half, plan.Axes[0].ApproachBearingDeg, 6);
        Assert.Equal(half, plan.Axes[1].ApproachBearingDeg, 6);
    }
}
