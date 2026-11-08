using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Sim;

/// <summary>DRG-100 / SWARM-B8: agent delegation for swarm intents (SWARM-23).</summary>
public sealed class SwarmAgentIntentIssuerTests
{
    private static SwarmController CreateControllerWithSwarm(string unitId = "swarm-1")
    {
        var c = new SwarmController(SimSeed.FromScenario(42));
        c.Register(new SwarmUnitIntegrity(unitId, "plat-swarm", 10, 20), 57.0, 20.0);
        return c;
    }

    [Test]
    public void Agent_issues_Move()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1",
            SwarmIntentKind.Move,
            SwarmOrderActor.Agent,
            1,
            1.0,
            new AgentId("agent-alpha"),
            TargetLatDeg: 57.5,
            TargetLonDeg: 20.5));

        Assert.That(result.Success, Is.True);
        Assert.That(c.GetIntent("swarm-1"), Is.EqualTo(SwarmIntentKind.Move));
        Assert.That(result.Actor, Is.EqualTo(SwarmOrderActor.Agent));
        Assert.That(result.AgentId!.Value.Value, Is.EqualTo("agent-alpha"));
        Assert.That(issuer.AttributionLog, Has.Count.EqualTo(1));
        Assert.That(issuer.AttributionLog[0].Fingerprint(), Does.Contain("agent-alpha"));
    }

    [Test]
    public void Agent_issues_Attack()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1",
            SwarmIntentKind.Attack,
            SwarmOrderActor.Agent,
            2,
            2.0,
            new AgentId("agent-alpha"),
            AttackTargetUnitId: "hostile-1"));

        Assert.That(result.Success, Is.True);
        Assert.That(c.GetIntent("swarm-1"), Is.EqualTo(SwarmIntentKind.Attack));
    }

    [Test]
    public void Agent_issues_Hold()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1", SwarmIntentKind.Move, SwarmOrderActor.Agent, 1, 1.0,
            new AgentId("a"), TargetLatDeg: 58, TargetLonDeg: 21));
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1", SwarmIntentKind.Hold, SwarmOrderActor.Agent, 2, 2.0, new AgentId("a")));
        Assert.That(result.Success, Is.True);
        Assert.That(c.GetIntent("swarm-1"), Is.EqualTo(SwarmIntentKind.Hold));
    }

    [Test]
    public void Agent_issues_Mode_Assault()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssueMode(
            "swarm-1",
            SwarmOperationalMode.Assault,
            SwarmOrderActor.Agent,
            1,
            1.0,
            new AgentId("agent-bravo"));

        Assert.That(result.Success, Is.True);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Assault));
        Assert.That(result.Actor, Is.EqualTo(SwarmOrderActor.Agent));
    }

    [Test]
    public void Agent_issues_Mode_Screen()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssueMode(
            "swarm-1",
            SwarmOperationalMode.Screen,
            SwarmOrderActor.Agent,
            1,
            1.0,
            new AgentId("agent-bravo"));
        Assert.That(result.Success, Is.True);
        Assert.That(c.GetMode("swarm-1"), Is.EqualTo(SwarmOperationalMode.Screen));
    }

    [Test]
    public void Player_actor_has_null_agent_id()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1", SwarmIntentKind.Hold, SwarmOrderActor.Player, 1, 1.0));
        Assert.That(result.Success, Is.True);
        Assert.That(result.AgentId, Is.Null);
        Assert.That(issuer.AttributionLog[0].Actor, Is.EqualTo(SwarmOrderActor.Player));
    }

    [Test]
    public void Missing_agent_id_when_Actor_Agent_fails()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1", SwarmIntentKind.Hold, SwarmOrderActor.Agent, 1, 1.0, AgentId: null));
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SwarmAgentIntentIssuer.ReasonMissingAgentId));
    }

    [Test]
    public void Unknown_unit_fails_cleanly()
    {
        var c = CreateControllerWithSwarm();
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "missing", SwarmIntentKind.Hold, SwarmOrderActor.Player, 1, 1.0));
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SwarmAgentIntentIssuer.ReasonUnknownUnit));
    }

    [Test]
    public void Link_lost_maps_to_failure_reason()
    {
        var c = CreateControllerWithSwarm();
        c.SetLinkState("swarm-1", SwarmLinkState.Lost);
        var issuer = new SwarmAgentIntentIssuer(c);
        var result = issuer.TryIssue(new SwarmAgentOrderRequest(
            "swarm-1", SwarmIntentKind.Move, SwarmOrderActor.Agent, 1, 1.0,
            new AgentId("a"), TargetLatDeg: 58, TargetLonDeg: 21));
        Assert.That(result.Success, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SwarmAgentIntentIssuer.ReasonLinkLost));
    }
}
