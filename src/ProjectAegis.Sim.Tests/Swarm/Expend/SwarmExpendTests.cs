using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Swarm;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Swarm.Expend;

/// <summary>DRG-108 / SWARM-C4: expend/kamikaze pulse (SWARM-19).</summary>
public sealed class SwarmExpendTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1", int drones = 40) =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, drones);

    [Test]
    public void IssueExpend_authorized_reduces_integrity_and_logs()
    {
        var c = new SwarmController(SimSeed.FromScenario(19));
        c.Register(Sample(drones: 20), 57.0, 20.0);

        var result = c.IssueExpend(
            "swarm-1",
            dronesToExpend: 5,
            expendAuthorized: true,
            simTick: 1,
            simTime: 1.0,
            targetUnitId: "hostile-1");

        Assert.That(result.Applied, Is.True);
        Assert.That(result.DronesExpended, Is.EqualTo(5));
        Assert.That(result.PreviousDroneCount, Is.EqualTo(20));
        Assert.That(result.NewDroneCount, Is.EqualTo(15));
        Assert.That(result.DenyReason, Is.Null);
        Assert.That(result.OrderSequenceId, Is.EqualTo(1UL));

        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(15));

        AssertEx.Single(c.ExpendOrderLog);
        Assert.That(c.ExpendOrderLog[0].DronesExpended, Is.EqualTo(5));
        Assert.That(c.ExpendOrderLog[0].TargetUnitId, Is.EqualTo("hostile-1"));

        AssertEx.Single(c.IntegrityTimeline);
        Assert.That(c.IntegrityTimeline[0].ReasonCode, Is.EqualTo(SwarmController.ExpendReasonCode));
        Assert.That(c.IntegrityTimeline[0].DronesLost, Is.EqualTo(5));
    }

    [Test]
    public void IssueExpend_denied_when_not_authorized()
    {
        var c = new SwarmController(SimSeed.FromScenario(20));
        c.Register(Sample(drones: 20), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", 5, expendAuthorized: false, simTick: 1, simTime: 1.0);

        Assert.That(result.Applied, Is.False);
        Assert.That(result.DenyReason, Is.EqualTo("expend-unauthorized"));
        Assert.That(result.DronesExpended, Is.EqualTo(0));
        Assert.That(c.ExpendOrderLog, Is.Empty);
        Assert.That(c.IntegrityTimeline, Is.Empty);
        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(20));
    }

    [Test]
    public void IssueExpend_clamps_to_remaining_drones()
    {
        var c = new SwarmController(SimSeed.FromScenario(21));
        c.Register(Sample(drones: 3), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", dronesToExpend: 10, expendAuthorized: true, simTick: 1, simTime: 1.0);

        Assert.That(result.Applied, Is.True);
        Assert.That(result.DronesExpended, Is.EqualTo(3));
        Assert.That(result.NewDroneCount, Is.EqualTo(0));
    }

    [Test]
    public void IssueExpend_blocked_when_link_lost()
    {
        var c = new SwarmController(SimSeed.FromScenario(22));
        c.Register(Sample(), 57.0, 20.0);
        c.SetLinkState("swarm-1", SwarmLinkState.Lost);

        Assert.Throws<InvalidOperationException>(() =>
            c.IssueExpend("swarm-1", 2, expendAuthorized: true, simTick: 1, simTime: 1.0));
    }

    [Test]
    public void IssueExpend_invalid_count_denied()
    {
        var c = new SwarmController(SimSeed.FromScenario(23));
        c.Register(Sample(), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", 0, expendAuthorized: true, simTick: 1, simTime: 1.0);
        Assert.That(result.Applied, Is.False);
        Assert.That(result.DenyReason, Is.EqualTo("expend-count-invalid"));
    }

    [Test]
    public void Doctrine_ExpendAuthorized_gates_call_site()
    {
        // Integration with B7: caller evaluates Policy, then passes flag.
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: true);
        var evaluator = new PolicyEvaluator(_ => policy);
        var verdict = evaluator.Evaluate(
            new PolicyContext(1, 0, 0, policy),
            new ActionRequest(ActionKind.FireGuided, 1, 0, IsExpend: true));
        Assert.That(verdict.Allowed, Is.True);

        var deniedPolicy = new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: false);
        var denied = new PolicyEvaluator(_ => deniedPolicy).Evaluate(
            new PolicyContext(1, 0, 0, deniedPolicy),
            new ActionRequest(ActionKind.FireGuided, 1, 0, IsExpend: true));
        Assert.That(denied.Allowed, Is.False);
        Assert.That(denied.Reason, Is.EqualTo(FireAbortReason.ExpendUnauthorized));

        var c = new SwarmController(SimSeed.FromScenario(24));
        c.Register(Sample(drones: 10), 57.0, 20.0);
        var applied = c.IssueExpend("swarm-1", 2, expendAuthorized: verdict.Allowed, simTick: 1, simTime: 1.0);
        Assert.That(applied.Applied, Is.True);
        var blocked = c.IssueExpend("swarm-1", 2, expendAuthorized: denied.Allowed, simTick: 2, simTime: 2.0);
        Assert.That(blocked.Applied, Is.False);
    }

    [Test]
    public void IssueExpend_is_irreversible_no_auto_regen()
    {
        var c = new SwarmController(SimSeed.FromScenario(25));
        c.Register(Sample(drones: 10), 57.0, 20.0);
        c.IssueExpend("swarm-1", 4, expendAuthorized: true, simTick: 1, simTime: 1.0);
        c.Tick(1.0);
        c.Tick(1.0);
        Assert.That(c.TryGetIntegrity("swarm-1", out var integrity), Is.True);
        Assert.That(integrity.DroneCount, Is.EqualTo(6));
    }
}
