using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm;

/// <summary>DRG-94 / SWARM-B1: modes, host, linkState (SWARM-10/11/12).</summary>
public sealed class SwarmModeHostLinkTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1") =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, 40, 40);

    [Fact]
    public void IssueMode_logs_mode_change_and_is_readable()
    {
        var c = new SwarmController(SimSeed.FromScenario(1));
        c.Register(Sample(), 57.0, 20.0);

        var seq = c.IssueMode("swarm-1", SwarmOperationalMode.Assault, simTick: 1, simTime: 1.0);

        Assert.Equal(1UL, seq);
        Assert.Equal(SwarmOperationalMode.Assault, c.GetMode("swarm-1"));
        Assert.Single(c.ModeOrderLog);
        Assert.Equal(SwarmOperationalMode.Assault, c.ModeOrderLog[0].Mode);
    }

    [Fact]
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

        Assert.True(c.TryGetCentroid("swarm-1", out var lat, out var lon));
        Assert.Equal(57.0, lat, 5);
        Assert.Equal(20.5, lon, 5);
        Assert.Equal("host-cvn", c.GetHostId("swarm-1"));
    }

    [Fact]
    public void Link_evaluator_is_deterministic_for_range_and_jam()
    {
        Assert.Equal(SwarmLinkState.Connected, SwarmLinkEvaluator.Evaluate(0.1, hostAlive: true, jammed: false));
        Assert.Equal(SwarmLinkState.Degraded, SwarmLinkEvaluator.Evaluate(1.2, hostAlive: true, jammed: false));
        Assert.Equal(SwarmLinkState.Lost, SwarmLinkEvaluator.Evaluate(3.0, hostAlive: true, jammed: false));
        Assert.Equal(SwarmLinkState.Lost, SwarmLinkEvaluator.Evaluate(0.0, hostAlive: true, jammed: true));
        Assert.Equal(SwarmLinkState.Lost, SwarmLinkEvaluator.Evaluate(0.0, hostAlive: false, jammed: false));
    }

    [Fact]
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

    [Fact]
    public void RefreshLinkState_uses_host_geometry()
    {
        var c = new SwarmController(SimSeed.FromScenario(4));
        c.Register(Sample(), latDeg: 57.0, lonDeg: 20.0);
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 20.3, alive: true);

        Assert.Equal(SwarmLinkState.Connected, c.RefreshLinkState("swarm-1", jammed: false));

        c.PublishHostState("host-1", latDeg: 57.0, lonDeg: 22.5, alive: true);
        Assert.Equal(SwarmLinkState.Lost, c.RefreshLinkState("swarm-1", jammed: false));
    }

    [Fact]
    public void Host_loss_stub_forces_hold_and_lost_link()
    {
        var c = new SwarmController(SimSeed.FromScenario(5));
        c.Register(Sample(), 57.0, 20.0);
        c.BindHost("swarm-1", "host-1");
        c.PublishHostState("host-1", 57.0, 20.0, alive: true);
        c.IssueMode("swarm-1", SwarmOperationalMode.Assault, 1, 0);

        c.NotifyHostLost("swarm-1");

        Assert.Equal(SwarmLinkState.Lost, c.GetLinkState("swarm-1"));
        Assert.Equal(SwarmOperationalMode.Hold, c.GetMode("swarm-1"));
        Assert.Equal(SwarmIntentKind.Hold, c.GetIntent("swarm-1"));
    }

    [Fact]
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

        Assert.Equal(SwarmIntentKind.Attack, c.GetIntent("a"));
        Assert.Equal(SwarmOperationalMode.Assault, c.GetMode("a"));
        Assert.Equal(SwarmIntentKind.Hold, c.GetIntent("b"));
        Assert.Equal(SwarmOperationalMode.Hold, c.GetMode("b"));
    }
}
