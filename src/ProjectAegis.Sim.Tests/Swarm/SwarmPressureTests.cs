using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Assault;
using ProjectAegis.Sim.Swarm.Formation;
using ProjectAegis.Sim.Swarm.SoftKill;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>
/// S117 / DRG-150: swarm pressure gauntlet — multi-seed attrition × mode × link ×
/// formation × soft-kill matrix. Complements A2–C3 AC suites; saboteur --swarm-filter
/// kill path targets these assertions.
/// </summary>
public sealed class SwarmPressureTests
{
    private static SwarmUnitIntegrity Sample(
        string unitId = "swarm-1",
        int drones = 40,
        int max = 40) =>
        new(unitId, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, max);

    [Theory]
    [InlineData(42UL)]
    [InlineData(7UL)]
    [InlineData(123UL)]
    [InlineData(999UL)]
    public void Multi_seed_attrition_mode_link_matrix_is_deterministic(ulong seed)
    {
        var a = RunPressureScenario(seed);
        var b = RunPressureScenario(seed);

        Assert.Equal(a.IntegrityHash, b.IntegrityHash);
        Assert.Equal(a.OrderFingerprint, b.OrderFingerprint);
        Assert.Equal(a.FinalAlpha, b.FinalAlpha);
        Assert.Equal(a.FinalBravo, b.FinalBravo);
        Assert.True(a.FinalAlpha > 0 && a.FinalAlpha < 40);
        Assert.True(a.FinalBravo > 0 && a.FinalBravo < 40);
    }

    [Fact]
    public void Extreme_attrition_to_zero_stops_motion_and_further_damage()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(11), speedDegPerSecond: 0.5);
        ctl.Register(Sample(drones: 10, max: 10), latDeg: 10.0, lonDeg: 20.0);
        ctl.IssueMove("swarm-1", 11.0, 21.0, simTick: 1, simTime: 1.0);

        Assert.True(ctl.TryApplyIntegrityDamage("swarm-1", 100, 2, 2.0, "hard-aa", out var wipe));
        Assert.Equal(0, wipe.NewDroneCount);
        Assert.Equal(10, wipe.DronesLost);
        Assert.True(ctl.TryGetIntegrity("swarm-1", out var zero));
        Assert.Equal(0, zero.DroneCount);

        ctl.Tick(deltaSeconds: 5.0);
        Assert.True(ctl.TryGetCentroid("swarm-1", out var lat, out var lon));
        Assert.Equal(10.0, lat);
        Assert.Equal(20.0, lon);

        Assert.False(ctl.TryApplyIntegrityDamage("swarm-1", 1, 3, 3.0, "extra", out _));
    }

    [Fact]
    public void Emp_freezes_mode_switches_for_duration()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(3));
        ctl.Register(Sample(), 0, 0);
        var soft = new SwarmSoftKillApplicator(ctl);

        Assert.True(soft.ApplyEmp("swarm-1", simTick: 1, simTime: 10.0, freezeDurationSeconds: 30.0));
        Assert.True(soft.IsModeFrozen("swarm-1", simTime: 20.0));
        Assert.False(soft.IsModeFrozen("swarm-1", simTime: 50.0));
        Assert.Contains(soft.EventLog, e => e.Kind == SwarmSoftKillKind.Emp);
    }

    [Fact]
    public void Design_max_concurrent_swarms_under_mixed_attrition_stays_aggregate()
    {
        var result = SwarmReplayHarness.RunDesignMaxStress(seed: 7);
        Assert.Equal(SwarmPerformanceCaps.DesignMaxConcurrentSwarms, result.ConcurrentSwarms);
        Assert.Equal(result.ConcurrentSwarms * result.Ticks, result.EngagementWorkUnits);
        Assert.True(result.EngagementWorkUnits < result.LogicalDronesAtStart * result.Ticks);
        var again = SwarmReplayHarness.RunDesignMaxStress(seed: 7);
        Assert.Equal(result.FinalIntegrityHash, again.FinalIntegrityHash);
    }

    [Fact]
    public void Logical_caps_clamp_on_register_and_regen()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(5));
        ctl.Register(Sample(drones: 50, max: 50), 0, 0);
        Assert.True(ctl.TryGetIntegrity("swarm-1", out var afterReg));
        Assert.Equal(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm, afterReg.MaxDrones);
        Assert.Equal(afterReg.MaxDrones, afterReg.DroneCount);

        Assert.True(ctl.TryApplyIntegrityDamage("swarm-1", 2, 1, 1.0, "aa", out _));
        Assert.True(ctl.TryApplyIntegrityRegen(
            "swarm-1",
            dronesGained: 10,
            simTick: 2,
            simTime: 2.0,
            reasonCode: SwarmController.RegenReasonCode,
            out var regen));
        Assert.Equal(afterReg.MaxDrones - 2, regen.PreviousDroneCount);
        Assert.Equal(afterReg.MaxDrones, regen.NewDroneCount);
    }

    [Fact]
    public void Link_lost_does_not_mutate_integrity_hash()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(8));
        ctl.Register(Sample(), 0, 0);
        var hashBefore = ctl.ComputeIntegrityTimelineHash();
        ctl.SetLinkState("swarm-1", SwarmLinkState.Lost);
        Assert.Equal(SwarmLinkState.Lost, ctl.GetLinkState("swarm-1"));
        Assert.Equal(hashBefore, ctl.ComputeIntegrityTimelineHash());
    }

    [Fact]
    public void Assault_split_under_attrition_shares_sum_to_living_count()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 17,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 42,
            doctrineAllowSplit: true);
        Assert.True(plan.SplitApplied);
        Assert.Equal(17, plan.TotalDroneShare);
        Assert.Equal(plan.EffectiveAxisCount, plan.Axes.Count);
        Assert.All(plan.Axes, a => Assert.True(a.DroneShare >= 1));
    }

    [Fact]
    public void Caps_clamp_identity_is_false_for_oversize()
    {
        Assert.True(SwarmPerformanceCaps.ExceedsLogicalCap(41));
        Assert.Equal(40, SwarmPerformanceCaps.ClampLogicalMaxDrones(99));
        Assert.Equal(0, SwarmPerformanceCaps.ClampLogicalMaxDrones(-1));
    }

    [Fact]
    public void Softkill_emp_and_attrition_compose_without_integrity_side_channel()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(19));
        ctl.Register(Sample(drones: 20, max: 20), 1.0, 2.0);
        var soft = new SwarmSoftKillApplicator(ctl);
        soft.ApplyEmp("swarm-1", 1, 1.0, freezeDurationSeconds: 10.0);
        Assert.True(ctl.TryApplyIntegrityDamage("swarm-1", 4, 2, 2.0, "aa", out var change));
        Assert.Equal(16, change.NewDroneCount);
        Assert.True(soft.IsModeFrozen("swarm-1", 5.0));
        Assert.Single(ctl.IntegrityTimeline);
    }

    private static (
        ulong IntegrityHash,
        ulong OrderFingerprint,
        int FinalAlpha,
        int FinalBravo) RunPressureScenario(ulong seed)
    {
        var ctl = new SwarmController(SimSeed.FromScenario(seed), speedDegPerSecond: 0.05);
        ctl.Register(Sample("alpha", 40, 40), latDeg: 57.0, lonDeg: 20.0);
        ctl.Register(Sample("bravo", 30, 30), latDeg: 57.05, lonDeg: 20.05);
        ctl.PublishHostState("host-1", 57.0, 20.0, alive: true);
        ctl.BindHost("alpha", "host-1");
        ctl.BindHost("bravo", "host-1");

        ctl.IssueMode("alpha", SwarmOperationalMode.Assault, 1, 1.0);
        ctl.IssueSetFormation("alpha", SwarmFormation.Spear, 2, 2.0);
        ctl.IssueMove("alpha", 57.2, 20.2, 3, 3.0);
        ctl.IssueMode("bravo", SwarmOperationalMode.Screen, 4, 4.0);

        var soft = new SwarmSoftKillApplicator(ctl);
        soft.ApplyEmp("alpha", 5, 5.0, freezeDurationSeconds: 15.0);

        Assert.True(ctl.TryApplyIntegrityDamage("alpha", 8, 6, 6.0, "aa-point", out _));
        Assert.True(ctl.TryApplyIntegrityDamage("bravo", 5, 7, 7.0, "aa-area", out _));
        ctl.Tick(1.0);
        ctl.SetLinkState("bravo", SwarmLinkState.Degraded);
        ctl.TryRegenNearHost("alpha", hostHasStores: true, simTick: 8, simTime: 8.0, out _);
        ctl.Tick(1.0);

        Assert.True(ctl.TryGetIntegrity("alpha", out var a));
        Assert.True(ctl.TryGetIntegrity("bravo", out var b));
        return (ctl.ComputeIntegrityTimelineHash(), ctl.ComputeOrderLogFingerprint(),
            a.DroneCount, b.DroneCount);
    }
}
