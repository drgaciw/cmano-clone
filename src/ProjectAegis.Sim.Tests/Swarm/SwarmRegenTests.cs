using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-97 / SWARM-B4: regen near host with stores (SWARM-13).</summary>
public sealed class SwarmRegenTests
{
    private static SwarmUnitIntegrity Sample(
        string id = "swarm-1",
        int drones = 20,
        int max = 40) =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, max);

    private static SwarmController NearHostController(
        int drones = 20,
        int max = 40,
        double swarmLat = 57.0,
        double swarmLon = 20.0,
        double hostLat = 57.0,
        double hostLon = 20.1,
        bool hostAlive = true,
        string hostId = "host-1")
    {
        var c = new SwarmController(SimSeed.FromScenario(97));
        c.Register(Sample(drones: drones, max: max), swarmLat, swarmLon);
        c.BindHost("swarm-1", hostId);
        c.PublishHostState(hostId, hostLat, hostLon, alive: hostAlive);
        return c;
    }

    [Fact]
    public void Regen_increases_count_near_host_with_stores()
    {
        var c = NearHostController(drones: 20, max: 40);

        Assert.True(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: true,
            simTick: 1,
            simTime: 1.0,
            out var change));

        Assert.Equal(20, change.PreviousDroneCount);
        Assert.Equal(21, change.NewDroneCount);
        Assert.Equal(0, change.DronesLost);
        Assert.Equal(SwarmController.RegenReasonCode, change.ReasonCode);
        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(21, integrity.DroneCount);
    }

    [Fact]
    public void No_regen_without_stores()
    {
        var c = NearHostController(drones: 20, max: 40);

        Assert.False(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: false,
            simTick: 1,
            simTime: 1.0,
            out _));

        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(20, integrity.DroneCount);
        Assert.Empty(c.IntegrityTimeline);
    }

    [Fact]
    public void No_regen_when_far_from_host()
    {
        // Default max range 0.5 deg; place host 1.0 deg away.
        var c = NearHostController(
            drones: 20,
            max: 40,
            swarmLat: 57.0,
            swarmLon: 20.0,
            hostLat: 57.0,
            hostLon: 21.0);

        Assert.False(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: true,
            simTick: 1,
            simTime: 1.0,
            out _));

        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(20, integrity.DroneCount);
    }

    [Fact]
    public void No_regen_above_maxDrones()
    {
        var c = NearHostController(drones: 40, max: 40);

        Assert.False(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: true,
            simTick: 1,
            simTime: 1.0,
            out _));

        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(40, integrity.DroneCount);
        Assert.Empty(c.IntegrityTimeline);
    }

    [Fact]
    public void No_regen_when_host_dead()
    {
        var c = NearHostController(drones: 20, max: 40, hostAlive: false);

        Assert.False(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: true,
            simTick: 1,
            simTime: 1.0,
            out _));

        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(20, integrity.DroneCount);
    }

    [Fact]
    public void Regen_appears_on_IntegrityTimeline_with_reason_regen_host()
    {
        var c = NearHostController(drones: 10, max: 40);

        Assert.True(c.TryRegenNearHost(
            "swarm-1",
            hostHasStores: true,
            simTick: 5,
            simTime: 5.5,
            out _));

        Assert.Single(c.IntegrityTimeline);
        var entry = c.IntegrityTimeline[0];
        Assert.Equal(SwarmController.RegenReasonCode, entry.ReasonCode);
        Assert.Equal(10, entry.PreviousDroneCount);
        Assert.Equal(11, entry.NewDroneCount);
        Assert.Equal(0, entry.DronesLost);
        Assert.Equal(5UL, entry.SimTick);
        Assert.Equal(5.5, entry.SimTime);
    }

    [Fact]
    public void Same_seed_path_is_deterministic()
    {
        const ulong seed = 97001UL;

        (ulong Hash, int Count, string Reason) Run()
        {
            var c = new SwarmController(SimSeed.FromScenario(seed));
            c.Register(Sample("s1", drones: 15, max: 40), 57.0, 20.0);
            c.BindHost("s1", "host-cvn");
            c.PublishHostState("host-cvn", 57.0, 20.05, alive: true);

            for (ulong tick = 1; tick <= 5; tick++)
            {
                c.TryRegenNearHost(
                    "s1",
                    hostHasStores: true,
                    simTick: tick,
                    simTime: tick,
                    out _);
            }

            Assert.True(c.TryGetIntegrity("s1", out var integrity));
            var reason = c.IntegrityTimeline.Count > 0
                ? c.IntegrityTimeline[0].ReasonCode
                : string.Empty;
            return (c.ComputeIntegrityTimelineHash(), integrity.DroneCount, reason);
        }

        var a = Run();
        var b = Run();
        Assert.Equal(a.Hash, b.Hash);
        Assert.Equal(a.Count, b.Count);
        Assert.Equal(20, a.Count); // 15 + 5 pulses
        Assert.Equal(SwarmController.RegenReasonCode, a.Reason);
        Assert.NotEqual(0UL, a.Hash);
    }

    [Fact]
    public void CanRegen_evaluator_gates_are_pure()
    {
        Assert.True(SwarmRegenEvaluator.CanRegen(
            rangeDeg: 0.1,
            hostAlive: true,
            hostHasStores: true,
            droneCount: 10,
            maxDrones: 40));

        Assert.False(SwarmRegenEvaluator.CanRegen(
            rangeDeg: 0.1,
            hostAlive: true,
            hostHasStores: false,
            droneCount: 10,
            maxDrones: 40));

        Assert.False(SwarmRegenEvaluator.CanRegen(
            rangeDeg: 0.9,
            hostAlive: true,
            hostHasStores: true,
            droneCount: 10,
            maxDrones: 40));

        Assert.False(SwarmRegenEvaluator.CanRegen(
            rangeDeg: 0.1,
            hostAlive: false,
            hostHasStores: true,
            droneCount: 10,
            maxDrones: 40));

        Assert.False(SwarmRegenEvaluator.CanRegen(
            rangeDeg: 0.1,
            hostAlive: true,
            hostHasStores: true,
            droneCount: 40,
            maxDrones: 40));

        Assert.False(SwarmRegenEvaluator.CanRegen(
            rangeDeg: null,
            hostAlive: true,
            hostHasStores: true,
            droneCount: 10,
            maxDrones: 40));
    }

    [Fact]
    public void TryApplyIntegrityRegen_clamps_to_max()
    {
        var c = new SwarmController(SimSeed.FromScenario(1));
        c.Register(Sample(drones: 38, max: 40), 0, 0);

        Assert.True(c.TryApplyIntegrityRegen(
            "swarm-1",
            dronesGained: 10,
            simTick: 1,
            simTime: 1.0,
            reasonCode: SwarmController.RegenReasonCode,
            out var change));

        Assert.Equal(38, change.PreviousDroneCount);
        Assert.Equal(40, change.NewDroneCount);
        Assert.Equal(0, change.DronesLost);
    }
}
