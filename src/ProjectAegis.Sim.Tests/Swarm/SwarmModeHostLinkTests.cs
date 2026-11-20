using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;
using ProjectAegis.Sim.Tests;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-94 / SWARM-B1: modes, host, linkState (SWARM-10/11/12).</summary>
public sealed class SwarmModeHostLinkTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1") =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40);

    [Test]
    public void IssueMode_logs_mode_change_and_is_readable()
    {
        var c = new SwarmController(SimSeed.FromScenario(1));
        c.Register(Sample(), 57.0, 20.0);

        var seq = c.IssueMode("swarm-1", SwarmOperationalMode.Assault, simTick: 1, simTime: 1.0);

        Assert.That(seq, Is.EqualTo(1UL));
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Assault));
        AssertEx.Single(c.ModeOrderLog);
        Assert.That(c.ModeOrderLog[0].Mode, Is.EqualTo(SwarmOperationalMode.Assault));
    }

    [Test]
    public void Screen_mode_gravitates_toward_host()
    {
        var c = new SwarmController(SimSeed.FromScenario(2), speedDegPerSecond: 0.5);
        c.Register(Sample(), latDeg: 57.0, lonDeg: 20.0);
        c.BindHost("swarm-1", "host-cvn");
        c.PublishHostState("host-cvn", latDeg: 57.0, lonDeg: 20.5, alive: true);
        c.IssueMode("swarm-1", SwarmOperationalMode.Screen, simTick: 1, simTime: 0.0);

        for (var i = 0; i < 20; i++)
        {
            c.Tick(1.0);
        }

        Assert.That(c.TryGetCentroid("swarm-1", out var lat, out var lon), Is.True);
        Assert.That(
            Math.Round((double)(lat), 5, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(57.0), 5, MidpointRounding.AwayFromZero)));
        Assert.That(
            Math.Round((double)(lon), 5, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round((double)(20.5), 5, MidpointRounding.AwayFromZero)));
        Assert.That(c.GetHostId("swarm-1"), Is.EqualTo("host-cvn"));
    }

    [Test]
    public void Link_evaluator_is_deterministic_for_range_and_jam()
    {
        Assert.That(SwarmLinkEvaluator.Evaluate(0.1, hostAlive: true, jammed: false), Is.EqualTo(SwarmLinkState.Connected));
        Assert.That(SwarmLinkEvaluator.Evaluate(1.2, hostAlive: true, jammed: false), Is.EqualTo(SwarmLinkState.Degraded));
        Assert.That(SwarmLinkEvaluator.Evaluate(3.0, hostAlive: true, jammed: false), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(SwarmLinkEvaluator.Evaluate(0.0, hostAlive: true, jammed: true), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(SwarmLinkEvaluator.Evaluate(0.0, hostAlive: false, jammed: false), Is.EqualTo(SwarmLinkState.Lost));
    }

    [Test]
    public void Lost_link_blocks_new_orders()
    {
        var c = new SwarmController(SimSeed.FromScenario(3));
        c.Register(Sample(), 57.0, 20.0);
        c.SetLinkState("swarm-1", SwarmLinkState.Lost);

        Assert.Throws<InvalidOperationException>(() =>
            c.IssueMove("swarm-1", 58.0, 21.0, simTick: 1, simTime: 1.0));
        Assert.Throws<InvalidOperationException>(() =>
            c.IssueMode("swarm-1", SwarmOperationalMode.Scatter, simTick: 2, simTime: 2.0));
    }

    [Test]
    public void RefreshLinkState_uses_host_geometry()
    {
        var c = new SwarmController(SimSeed.FromScenario(4));
        c.Register(Sample(), latDeg: 57.0, lonDeg: 20.0);
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 20.3, alive: true);

        Assert.That(c.RefreshLinkState("swarm-1", jammed: false), Is.EqualTo(SwarmLinkState.Connected));

        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 22.5, alive: true);
        Assert.That(c.RefreshLinkState("swarm-1", jammed: false), Is.EqualTo(SwarmLinkState.Lost));
    }

    [Test]
    public void Host_loss_stub_forces_hold_and_lost_link()
    {
        var c = new SwarmController(SimSeed.FromScenario(5));
        c.Register(Sample(), 57.0, 20.0);
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", 57.0, 20.0, alive: true);
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, 1, 0);

        c.NotifyHostLost("swarm-1");

        Assert.That(c.GetLinkState("swarm-1"), Is.EqualTo(SwarmLinkState.Lost));
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Hold));
        Assert.That(c.GetIntent("swarm-1"), Is.EqualTo(SwarmIntentKind.Hold));
    }

    [Test]
    public void Modes_differ_in_engagement_aggressiveness_proxy()
    {
        // Assault keeps Attack intent; Hold mode with Hold intent is less aggressive.
        var c = new SwarmController(SimSeed.FromScenario(6));
        c.Register(Sample("a"), 1, 1);
        c.Register(Sample("b"), 1, 1);
        c.IssueMode("a", SwarmOperationalMode.Assault, 1, 0);
        c.IssueAttack("a", "t", 2, 1, 1.1, 1.1);
        c.IssueMode("b", SwarmOperationalMode.Hold, 1, 0);
        c.IssueHold("b", 2, 1);

        Assert.That(c.GetIntent("a"), Is.EqualTo(SwarmIntentKind.Attack));
        Assert.That(c.GetMode("a"), Is.EqualTo(SwarmOperationalMode.Assault));
        Assert.That(c.GetIntent("b"), Is.EqualTo(SwarmIntentKind.Hold));
        Assert.That(c.GetMode("b"), Is.EqualTo(SwarmOperationalMode.Hold));
    }
}
