using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class PolicyDenialLogTests
{
    [Test]
    public void HoldFire_assignment_logs_policy_denial_on_engage_intent()
    {
        var orchestrator = new DelegationOrchestrator(globalSeed: 42);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        orchestrator.AssignAgentToTarget(
            agent,
            unit,
            new EffectivePolicy(RoeLevel.HoldFire),
            capturedAtSimTick: 0);
        orchestrator.Register(unit);

        for (var tick = 0; tick < 12; tick++)
        {
            orchestrator.Tick(new ObservedState(tick, 3, 1, new Dictionary<TargetId, bool>()));
        }

        Assert.That(orchestrator.DecisionLog.PolicyDenials, Is.Not.Empty);
        Assert.That(
            orchestrator.DecisionLog.PolicyDenials.Any(d => d.Reason == FireAbortReason.RoeHoldFire),
            Is.True);
        Assert.That(orchestrator.DecisionLog.PolicyDenials[0].PolicySnapshotId, Is.GreaterThan(0));
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }
}
