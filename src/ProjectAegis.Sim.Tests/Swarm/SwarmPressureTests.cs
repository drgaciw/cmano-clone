using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Assault;
using ProjectAegis.Sim.Swarm.Formation;
using ProjectAegis.Sim.Swarm.SoftKill;
using NUnit.Framework;

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

    [TestCase(42UL)]
    [TestCase(7UL)]
    [TestCase(123UL)]
    [TestCase(999UL)]
    public void Multi_seed_attrition_mode_link_matrix_is_deterministic(ulong seed)
    {
        var a = RunPressureScenario(seed);
        var b = RunPressureScenario(seed);

        Assert.That(b.IntegrityHash, Is.EqualTo(a.IntegrityHash));
        Assert.That(b.OrderFingerprint, Is.EqualTo(a.OrderFingerprint));
        Assert.That(b.FinalAlpha, Is.EqualTo(a.FinalAlpha));
        Assert.That(b.FinalBravo, Is.EqualTo(a.FinalBravo));
        Assert.That(a.FinalAlpha > 0 && a.FinalAlpha < 40, Is.True);
        Assert.That(a.FinalBravo > 0 && a.FinalBravo < 40, Is.True);
    }

    [Test]
    public void Extreme_attrition_to_zero_stops_motion_and_further_damage()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(11), speedDegPerSecond: 0.5);
        ctl.Register(Sample(drones: 10, max: 10), latDeg: 10.0, lonDeg: 20.0);
        ctl.IssueMove("swarm-1", 11.0, 21.0, simTick: 1, simTime: 1.0);

        Assert.That(ctl.TryApplyIntegrityDamage("swarm-1", 100, 2, 2.0, "hard-aa", out var wipe), Is.True);
        Assert.That(wipe.NewDroneCount, Is.EqualTo(0));
        Assert.That(wipe.DronesLost, Is.EqualTo(10));
        Assert.That(ctl.TryGetIntegrity("swarm-1", out var zero), Is.True);
        Assert.That(zero.DroneCount, Is.EqualTo(0));

        ctl.Tick(deltaSeconds: 5.0);
        Assert.That(ctl.TryGetCentroid("swarm-1", out var lat, out var lon), Is.True);
        Assert.That(lat, Is.EqualTo(10.0));
        Assert.That(lon, Is.EqualTo(20.0));

        Assert.That(ctl.TryApplyIntegrityDamage("swarm-1", 1, 3, 3.0, "extra", out _), Is.False);
    }

    [Test]
    public void Emp_freezes_mode_switches_for_duration()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(3));
        ctl.Register(Sample(), 0, 0);
        var soft = new SwarmSoftKillApplicator(ctl);

        Assert.That(soft.ApplyEmp("swarm-1", simTick: 1, simTime: 10.0, freezeDurationSeconds: 30.0), Is.True);
        Assert.That(soft.IsModeFrozen("swarm-1", simTime: 20.0), Is.True);
        Assert.That(soft.IsModeFrozen("swarm-1", simTime: 50.0), Is.False);
        Assert.That(soft.EventLog.Any(e => e.Kind == SwarmSoftKillKind.Emp), Is.True);
    }

    [Test]
    public void Design_max_concurrent_swarms_under_mixed_attrition_stays_aggregate()
    {
        var result = SwarmReplayHarness.RunDesignMaxStress(seed: 7);
        Assert.That(result.ConcurrentSwarms, Is.EqualTo(SwarmPerformanceCaps.DesignMaxConcurrentSwarms));
        Assert.That(result.EngagementWorkUnits, Is.EqualTo(result.ConcurrentSwarms * result.Ticks));
        Assert.That(result.EngagementWorkUnits < result.LogicalDronesAtStart * result.Ticks, Is.True);
        var again = SwarmReplayHarness.RunDesignMaxStress(seed: 7);
        Assert.That(again.FinalIntegrityHash, Is.EqualTo(result.FinalIntegrityHash));
    }

    [Test]
    public void Logical_caps_clamp_on_register_and_regen()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(5));
        ctl.Register(Sample(drones: 50, max: 50), 0, 0);
        Assert.That(ctl.TryGetIntegrity("swarm-1", out var afterReg), Is.True);
        Assert.That(afterReg.MaxDrones, Is.EqualTo(SwarmPerformanceCaps.LogicalMaxDronesPerSwarm));
        Assert.That(afterReg.DroneCount, Is.EqualTo(afterReg.MaxDrones));

        Assert.That(ctl.TryApplyIntegrityDamage("swarm-1", 2, 1, 1.0, "aa", out _), Is.True);
        Assert.That(
            ctl.TryApplyIntegrityRegen(
                    "swarm-1",
                    dronesGained: 10,
                    simTick: 2,
                    simTime: 2.0,
                    reasonCode: SwarmController.RegenReasonCode,
                    out var regen),
            Is.True);
        Assert.That(regen.PreviousDroneCount, Is.EqualTo(afterReg.MaxDrones - 2));
        Assert.That(regen.NewDroneCount, Is.EqualTo(afterReg.MaxDrones));
    }

    [Test]
    public void Link_lost_does_not_mutate_integrity_hash()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(8));
        ctl.Register(Sample(), 0, 0);
        var hashBefore = ctl.ComputeIntegrityTimelineHash();
        ctl.SetLinkState("swarm-1", SwarmLinkState.Lost);
        Assert.That(ctl.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(ctl.ComputeIntegrityTimelineHash(), Is.EqualTo(hashBefore));
    }

    [Test]
    public void Assault_split_under_attrition_shares_sum_to_living_count()
    {
        var plan = SwarmAssaultAxisSplitter.Plan(
            droneCount: 17,
            axisCount: 3,
            mode: SwarmOperationalMode.Assault,
            seed: 42,
            doctrineAllowSplit: true);
        Assert.That(plan.SplitApplied, Is.True);
        Assert.That(plan.TotalDroneShare, Is.EqualTo(17));
        Assert.That(plan.Axes.Count, Is.EqualTo(plan.EffectiveAxisCount));
        AssertEx.All(plan.Axes, a => Assert.That(a.DroneShare >= 1, Is.True));
    }

    [Test]
    public void Caps_clamp_identity_is_false_for_oversize()
    {
        Assert.That(SwarmPerformanceCaps.ExceedsLogicalCap(41), Is.True);
        Assert.That(SwarmPerformanceCaps.ClampLogicalMaxDrones(99), Is.EqualTo(40));
        Assert.That(SwarmPerformanceCaps.ClampLogicalMaxDrones(-1), Is.EqualTo(0));
    }

    [Test]
    public void Softkill_emp_and_attrition_compose_without_integrity_side_channel()
    {
        var ctl = new SwarmController(SimSeed.FromScenario(19));
        ctl.Register(Sample(drones: 20, max: 20), 1.0, 2.0);
        var soft = new SwarmSoftKillApplicator(ctl);
        soft.ApplyEmp("swarm-1", 1, 1.0, freezeDurationSeconds: 10.0);
        Assert.That(ctl.TryApplyIntegrityDamage("swarm-1", 4, 2, 2.0, "aa", out var change), Is.True);
        Assert.That(change.NewDroneCount, Is.EqualTo(16));
        Assert.That(soft.IsModeFrozen("swarm-1", 5.0), Is.True);
        AssertEx.Single(ctl.IntegrityTimeline);
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

        Assert.That(ctl.TryApplyIntegrityDamage("alpha", 8, 6, 6.0, "aa-point", out _), Is.True);
        Assert.That(ctl.TryApplyIntegrityDamage("bravo", 5, 7, 7.0, "aa-area", out _), Is.True);
        ctl.Tick(1.0);
        ctl.SetLinkState("bravo", SwarmLinkState.Degraded);
        ctl.TryRegenNearHost("alpha", hostHasStores: true, simTick: 8, simTime: 8.0, out _);
        ctl.Tick(1.0);

        Assert.That(ctl.TryGetIntegrity("alpha", out var a), Is.True);
        Assert.That(ctl.TryGetIntegrity("bravo", out var b), Is.True);
        return (ctl.ComputeIntegrityTimelineHash(), ctl.ComputeOrderLogFingerprint(),
            a.DroneCount, b.DroneCount);
    }
}
