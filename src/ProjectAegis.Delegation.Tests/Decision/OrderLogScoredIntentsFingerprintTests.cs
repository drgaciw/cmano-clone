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

public sealed class OrderLogScoredIntentsFingerprintTests
{
    [Test]
    public void AgentDecision_fingerprint_includes_sorted_scored_intents()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new StubPatrolPolicy());
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        orchestrator.Register(unit);
        orchestrator.BeginExecution();
        orchestrator.Tick(new ObservedState(0, 1, 0, new Dictionary<TargetId, bool>(), false));

        var fingerprint = orchestrator.DecisionLog.ComputeFingerprint();
        Assert.That(fingerprint, Does.Contain("Hold:1:Low"));
        Assert.That(fingerprint, Does.Contain("Move:0.8:Low"));
        Assert.That(fingerprint, Does.Contain("Engage:0.6:High"));
    }

    [Test]
    public void Scored_intent_format_is_stable()
    {
        var a = ScoredIntentFingerprint.Format(StubPatrolPolicy.DefaultCandidates);
        var b = ScoredIntentFingerprint.Format(StubPatrolPolicy.DefaultCandidates);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.EqualTo("Move:0.8:Low|Hold:1:Low|Engage:0.6:High"));
    }
}