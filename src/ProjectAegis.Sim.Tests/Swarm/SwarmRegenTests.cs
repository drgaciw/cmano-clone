using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;

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

    [Test]
    public void Regen_increases_count_near_host_with_stores()
    {
        var c = NearHostController(drones: 20, max: 40);

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: true,
                    simTick: 1,
                    simTime: 1.0,
                    out var change),
            Is.True);

        Assert.That(change.PreviousDroneCount, Is.EqualTo(20));
        Assert.That(change.NewDroneCount, Is.EqualTo(21));
        Assert.That(change.DronesLost, Is.EqualTo(0));
        Assert.That(change.ReasonCode, Is.EqualTo(SwarmController.RegenReasonCode));
        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(21));
    }

    [Test]
    public void No_regen_without_stores()
    {
        var c = NearHostController(drones: 20, max: 40);

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: false,
                    simTick: 1,
                    simTime: 1.0,
                    out _),
            Is.False);

        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(20));
        Assert.That(c.IntegrityTimeline, Is.Empty);
    }

    [Test]
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

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: true,
                    simTick: 1,
                    simTime: 1.0,
                    out _),
            Is.False);

        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(20));
    }

    [Test]
    public void No_regen_above_maxDrones()
    {
        var c = NearHostController(drones: 40, max: 40);

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: true,
                    simTick: 1,
                    simTime: 1.0,
                    out _),
            Is.False);

        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(40));
        Assert.That(c.IntegrityTimeline, Is.Empty);
    }

    [Test]
    public void No_regen_when_host_dead()
    {
        var c = NearHostController(drones: 20, max: 40, hostAlive: false);

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: true,
                    simTick: 1,
                    simTime: 1.0,
                    out _),
            Is.False);

        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(20));
    }

    [Test]
    public void Regen_appears_on_IntegrityTimeline_with_reason_regen_host()
    {
        var c = NearHostController(drones: 10, max: 40);

        Assert.That(
            c.TryRegenNearHost(
                    "swarm-1",
                    hostHasStores: true,
                    simTick: 5,
                    simTime: 5.5,
                    out _),
            Is.True);

        AssertEx.Single(c.IntegrityTimeline);
        var entry = c.IntegrityTimeline[0];
        Assert.That(entry.ReasonCode, Is.EqualTo(SwarmController.RegenReasonCode));
        Assert.That(entry.PreviousDroneCount, Is.EqualTo(10));
        Assert.That(entry.NewDroneCount, Is.EqualTo(11));
        Assert.That(entry.DronesLost, Is.EqualTo(0));
        Assert.That(entry.SimTick, Is.EqualTo(5UL));
        Assert.That(entry.SimTime, Is.EqualTo(5.5));
    }

    [Test]
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

            Assert.That(c.TryGetIntegrity("s1", out var integrity), Is.True);
            var reason = c.IntegrityTimeline.Count > 0
                ? c.IntegrityTimeline[0].ReasonCode
                : string.Empty;
            return (c.ComputeIntegrityTimelineHash(), integrity.DroneCount, reason);
        }

        var a = Run();
        var b = Run();
        Assert.That(b.Hash, Is.EqualTo(a.Hash));
        Assert.That(b.Count, Is.EqualTo(a.Count));
        Assert.That(a.Count, Is.EqualTo(20)); // 15 + 5 pulses
        Assert.That(a.Reason, Is.EqualTo(SwarmController.RegenReasonCode));
        Assert.That(a.Hash, Is.Not.EqualTo(0UL));
    }

    [Test]
    public void CanRegen_evaluator_gates_are_pure()
    {
        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: 0.1,
                    hostAlive: true,
                    hostHasStores: true,
                    droneCount: 10,
                    maxDrones: 40),
            Is.True);

        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: 0.1,
                    hostAlive: true,
                    hostHasStores: false,
                    droneCount: 10,
                    maxDrones: 40),
            Is.False);

        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: 0.9,
                    hostAlive: true,
                    hostHasStores: true,
                    droneCount: 10,
                    maxDrones: 40),
            Is.False);

        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: 0.1,
                    hostAlive: false,
                    hostHasStores: true,
                    droneCount: 10,
                    maxDrones: 40),
            Is.False);

        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: 0.1,
                    hostAlive: true,
                    hostHasStores: true,
                    droneCount: 40,
                    maxDrones: 40),
            Is.False);

        Assert.That(
            SwarmRegenEvaluator.CanRegen(
                    rangeDeg: null,
                    hostAlive: true,
                    hostHasStores: true,
                    droneCount: 10,
                    maxDrones: 40),
            Is.False);
    }

    [Test]
    public void TryApplyIntegrityRegen_clamps_to_max()
    {
        var c = new SwarmController(SimSeed.FromScenario(1));
        c.Register(Sample(drones: 38, max: 40), 0, 0);

        Assert.That(
            c.TryApplyIntegrityRegen(
                    "swarm-1",
                    dronesGained: 10,
                    simTick: 1,
                    simTime: 1.0,
                    reasonCode: SwarmController.RegenReasonCode,
                    out var change),
            Is.True);

        Assert.That(change.PreviousDroneCount, Is.EqualTo(38));
        Assert.That(change.NewDroneCount, Is.EqualTo(40));
        Assert.That(change.DronesLost, Is.EqualTo(0));
    }
}
