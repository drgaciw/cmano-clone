using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Expend;
using Xunit;

namespace ProjectAegis.Sim.Tests.Swarm.Expend;

/// <summary>DRG-108 / SWARM-C4: expend/kamikaze pulse (SWARM-19).</summary>
public sealed class SwarmExpendTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1", int drones = 40) =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, drones);

    [Fact]
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

        Assert.True(result.Applied);
        Assert.Equal(5, result.DronesExpended);
        Assert.Equal(20, result.PreviousDroneCount);
        Assert.Equal(15, result.NewDroneCount);
        Assert.Null(result.DenyReason);
        Assert.Equal(1UL, result.OrderSequenceId);

        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(15, integrity.DroneCount);

        Assert.Single(c.ExpendOrderLog);
        Assert.Equal(5, c.ExpendOrderLog[0].DronesExpended);
        Assert.Equal("hostile-1", c.ExpendOrderLog[0].TargetUnitId);

        Assert.Single(c.IntegrityTimeline);
        Assert.Equal(SwarmController.ExpendReasonCode, c.IntegrityTimeline[0].ReasonCode);
        Assert.Equal(5, c.IntegrityTimeline[0].DronesLost);
    }

    [Fact]
    public void IssueExpend_denied_when_not_authorized()
    {
        var c = new SwarmController(SimSeed.FromScenario(20));
        c.Register(Sample(drones: 20), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", 5, expendAuthorized: false, simTick: 1, simTime: 1.0);

        Assert.False(result.Applied);
        Assert.Equal("expend-unauthorized", result.DenyReason);
        Assert.Equal(0, result.DronesExpended);
        Assert.Empty(c.ExpendOrderLog);
        Assert.Empty(c.IntegrityTimeline);
        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(20, integrity.DroneCount);
    }

    [Fact]
    public void IssueExpend_clamps_to_remaining_drones()
    {
        var c = new SwarmController(SimSeed.FromScenario(21));
        c.Register(Sample(drones: 3), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", dronesToExpend: 10, expendAuthorized: true, simTick: 1, simTime: 1.0);

        Assert.True(result.Applied);
        Assert.Equal(3, result.DronesExpended);
        Assert.Equal(0, result.NewDroneCount);
    }

    [Fact]
    public void IssueExpend_blocked_when_link_lost()
    {
        var c = new SwarmController(SimSeed.FromScenario(22));
        c.Register(Sample(), 57.0, 20.0);
        c.SetLinkState("swarm-1", SwarmLinkState.Lost);

        Assert.Throws<InvalidOperationException>(() =>
            c.IssueExpend("swarm-1", 2, expendAuthorized: true, simTick: 1, simTime: 1.0));
    }

    [Fact]
    public void IssueExpend_invalid_count_denied()
    {
        var c = new SwarmController(SimSeed.FromScenario(23));
        c.Register(Sample(), 57.0, 20.0);

        var result = c.IssueExpend("swarm-1", 0, expendAuthorized: true, simTick: 1, simTime: 1.0);
        Assert.False(result.Applied);
        Assert.Equal("expend-count-invalid", result.DenyReason);
    }

    [Fact]
    public void Doctrine_ExpendAuthorized_gates_call_site()
    {
        // Integration with B7: caller evaluates Policy, then passes flag.
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: true);
        var evaluator = new PolicyEvaluator(_ => policy);
        var verdict = evaluator.Evaluate(
            new PolicyContext(1, 0, 0, policy),
            new ActionRequest(ActionKind.FireGuided, 1, 0, IsExpend: true));
        Assert.True(verdict.Allowed);

        var deniedPolicy = new EffectivePolicy(RoeLevel.WeaponsFree, ExpendAuthorized: false);
        var denied = new PolicyEvaluator(_ => deniedPolicy).Evaluate(
            new PolicyContext(1, 0, 0, deniedPolicy),
            new ActionRequest(ActionKind.FireGuided, 1, 0, IsExpend: true));
        Assert.False(denied.Allowed);
        Assert.Equal(FireAbortReason.ExpendUnauthorized, denied.Reason);

        var c = new SwarmController(SimSeed.FromScenario(24));
        c.Register(Sample(drones: 10), 57.0, 20.0);
        var applied = c.IssueExpend("swarm-1", 2, expendAuthorized: verdict.Allowed, simTick: 1, simTime: 1.0);
        Assert.True(applied.Applied);
        var blocked = c.IssueExpend("swarm-1", 2, expendAuthorized: denied.Allowed, simTick: 2, simTime: 2.0);
        Assert.False(blocked.Applied);
    }

    [Fact]
    public void IssueExpend_is_irreversible_no_auto_regen()
    {
        var c = new SwarmController(SimSeed.FromScenario(25));
        c.Register(Sample(drones: 10), 57.0, 20.0);
        c.IssueExpend("swarm-1", 4, expendAuthorized: true, simTick: 1, simTime: 1.0);
        c.Tick(1.0);
        c.Tick(1.0);
        Assert.True(c.TryGetIntegrity("swarm-1", out var integrity));
        Assert.Equal(6, integrity.DroneCount);
    }
}
