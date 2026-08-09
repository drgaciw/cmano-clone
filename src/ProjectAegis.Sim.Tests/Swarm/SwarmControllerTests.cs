using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-87 / SWARM-A2 ACs: centroid move, logged intents, authorized integrity, determinism.</summary>
public sealed class SwarmControllerTests
{
    private static SwarmUnitIntegrity SampleIntegrity(
        string unitId = "swarm-1",
        int drones = 40,
        int max = 40) =>
        new(unitId, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, max);

    [Fact]
    public void Headless_move_command_advances_swarm_centroid()
    {
        var controller = new SwarmController(SimSeed.FromScenario(42), speedDegPerSecond: 0.1);
        controller.Register(SampleIntegrity(), latDeg: 57.0, lonDeg: 20.0);

        controller.IssueMove("swarm-1", targetLatDeg: 57.5, targetLonDeg: 20.5, simTick: 1, simTime: 1.0);

        // Diagonal ~0.707 deg; 20s * 0.1 deg/s = 2.0 deg travel — arrives and snaps.
        for (var i = 0; i < 20; i++)
        {
            controller.Tick(deltaSeconds: 1.0);
        }

        Assert.True(controller.TryGetCentroid("swarm-1", out var lat, out var lon));
        Assert.Equal(57.5, lat, 6);
        Assert.Equal(20.5, lon, 6);
        Assert.Equal(SwarmIntentKind.Move, controller.GetIntent("swarm-1"));
    }

    [Fact]
    public void Hold_keeps_centroid_stationary()
    {
        var controller = new SwarmController(SimSeed.FromScenario(7), speedDegPerSecond: 1.0);
        controller.Register(SampleIntegrity(), latDeg: 10.0, lonDeg: 30.0);

        controller.IssueHold("swarm-1", simTick: 1, simTime: 0.0);
        controller.Tick(deltaSeconds: 10.0);

        Assert.True(controller.TryGetCentroid("swarm-1", out var lat, out var lon));
        Assert.Equal(10.0, lat);
        Assert.Equal(30.0, lon);
    }

    [Fact]
    public void Attack_and_hold_intents_are_logged_and_replayable()
    {
        var source = new SwarmController(SimSeed.FromScenario(99));
        source.Register(SampleIntegrity("swarm-a"), latDeg: 1.0, lonDeg: 2.0);

        source.IssueHold("swarm-a", simTick: 1, simTime: 1.0);
        source.IssueAttack(
            "swarm-a",
            attackTargetUnitId: "hostile-1",
            simTick: 2,
            simTime: 2.0,
            targetLatDeg: 1.5,
            targetLonDeg: 2.5);
        source.IssueHold("swarm-a", simTick: 3, simTime: 3.0);

        Assert.Equal(3, source.OrderLog.Count);
        Assert.Equal(SwarmIntentKind.Hold, source.OrderLog[0].Intent);
        Assert.Equal(SwarmIntentKind.Attack, source.OrderLog[1].Intent);
        Assert.Equal("hostile-1", source.OrderLog[1].AttackTargetUnitId);
        Assert.Equal(SwarmIntentKind.Hold, source.OrderLog[2].Intent);

        var replay = new SwarmController(SimSeed.FromScenario(99));
        replay.Register(SampleIntegrity("swarm-a"), latDeg: 1.0, lonDeg: 2.0);
        SwarmController.ReplayOrders(replay, source.OrderLog);

        Assert.Equal(source.ComputeOrderLogFingerprint(), replay.ComputeOrderLogFingerprint());
        Assert.Equal(SwarmIntentKind.Hold, replay.GetIntent("swarm-a"));
        Assert.Equal(3, replay.OrderLog.Count);
        Assert.Equal("hostile-1", replay.OrderLog[1].AttackTargetUnitId);
    }

    [Fact]
    public void Integrity_updates_only_via_authorized_damage_api()
    {
        var controller = new SwarmController(SimSeed.FromScenario(1));
        controller.Register(SampleIntegrity(drones: 40, max: 40), latDeg: 0, lonDeg: 0);

        Assert.True(controller.TryGetIntegrity("swarm-1", out var before));
        Assert.Equal(40, before.DroneCount);

        // Unauthorized: reassignment of the returned record must not mutate controller state.
        before = before with { DroneCount = 1 };
        Assert.True(controller.TryGetIntegrity("swarm-1", out var stillFull));
        Assert.Equal(40, stillFull.DroneCount);

        Assert.True(controller.TryApplyIntegrityDamage(
            "swarm-1",
            dronesLost: 5,
            simTick: 10,
            simTime: 10.0,
            reasonCode: "test-aa",
            out var change));

        Assert.Equal(40, change.PreviousDroneCount);
        Assert.Equal(35, change.NewDroneCount);
        Assert.Equal(5, change.DronesLost);
        Assert.Equal("test-aa", change.ReasonCode);

        Assert.True(controller.TryGetIntegrity("swarm-1", out var after));
        Assert.Equal(35, after.DroneCount);
        Assert.Single(controller.IntegrityTimeline);

        Assert.False(controller.TryApplyIntegrityDamage(
            "swarm-1",
            dronesLost: 0,
            simTick: 11,
            simTime: 11.0,
            reasonCode: "noop",
            out _));
    }

    [Fact]
    public void Same_scenario_seed_yields_same_integrity_timeline()
    {
        const ulong seed = 12345UL;

        ulong Run()
        {
            var c = new SwarmController(SimSeed.FromScenario(seed));
            c.Register(SampleIntegrity("s1", drones: 40, max: 40), latDeg: 57.0, lonDeg: 20.0);
            c.Register(SampleIntegrity("s2", drones: 20, max: 20), latDeg: 58.0, lonDeg: 21.0);

            c.IssueMove("s1", 57.2, 20.2, simTick: 1, simTime: 1.0);
            c.IssueAttack("s2", "hostile-x", simTick: 1, simTime: 1.0, targetLatDeg: 58.1, targetLonDeg: 21.1);

            for (ulong tick = 2; tick <= 12; tick++)
            {
                c.Tick(1.0);
                // Deterministic damage schedule (same for both runs).
                if (tick is 4 or 8 or 12)
                {
                    c.TryApplyIntegrityDamage("s1", dronesLost: 3, simTick: tick, simTime: tick, reasonCode: "area-aa", out _);
                }

                if (tick is 6)
                {
                    c.TryApplyIntegrityDamage("s2", dronesLost: 7, simTick: tick, simTime: tick, reasonCode: "point-fire", out _);
                }
            }

            return c.ComputeIntegrityTimelineHash();
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a, b);
        Assert.NotEqual(0UL, a);

        // Different seed must not collide on the mix base even with same damage schedule shape.
        var other = new SwarmController(SimSeed.FromScenario(seed + 1));
        other.Register(SampleIntegrity("s1", drones: 40, max: 40), latDeg: 57.0, lonDeg: 20.0);
        other.Register(SampleIntegrity("s2", drones: 20, max: 20), latDeg: 58.0, lonDeg: 21.0);
        Assert.NotEqual(a, other.ComputeIntegrityTimelineHash());
    }

    [Fact]
    public void Integrity_clamps_at_zero_and_marks_destroyed()
    {
        var controller = new SwarmController(SimSeed.FromScenario(3));
        controller.Register(SampleIntegrity(drones: 4, max: 40), latDeg: 0, lonDeg: 0);

        Assert.True(controller.TryApplyIntegrityDamage(
            "swarm-1",
            dronesLost: 100,
            simTick: 1,
            simTime: 1.0,
            reasonCode: "overkill",
            out var change));

        Assert.Equal(0, change.NewDroneCount);
        Assert.Equal(4, change.DronesLost);
        Assert.True(controller.TryGetIntegrity("swarm-1", out var integrity));
        Assert.True(integrity.IsDestroyed);
    }

    [Fact]
    public void Destroyed_swarm_does_not_advance_centroid()
    {
        var controller = new SwarmController(SimSeed.FromScenario(4), speedDegPerSecond: 1.0);
        controller.Register(SampleIntegrity(drones: 1, max: 40), latDeg: 0, lonDeg: 0);
        controller.IssueMove("swarm-1", 10, 10, simTick: 1, simTime: 1.0);
        controller.TryApplyIntegrityDamage("swarm-1", 1, 2, 2.0, "kill", out _);
        controller.Tick(5.0);

        Assert.True(controller.TryGetCentroid("swarm-1", out var lat, out var lon));
        Assert.Equal(0.0, lat);
        Assert.Equal(0.0, lon);
    }
}
