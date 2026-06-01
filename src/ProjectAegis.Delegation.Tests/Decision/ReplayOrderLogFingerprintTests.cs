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

/// <summary>/replay-verify style gate on unified order log fingerprint.</summary>
[TestFixture]
public sealed class ReplayOrderLogFingerprintTests
{
    [Test]
    public void Unified_order_log_fingerprint_is_stable_across_runs()
    {
        var a = RunScenarioFingerprint(globalSeed: 4242);
        var b = RunScenarioFingerprint(globalSeed: 4242);
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Is.Not.Empty);
    }

    [Test]
    public void Mvp_engage_fingerprint_is_stable_with_scenario_defaults()
    {
        var a = RunMvpEngageFingerprint(42, "baltic-patrol");
        var b = RunMvpEngageFingerprint(42, "baltic-patrol");
        Assert.That(a, Is.EqualTo(b));
        Assert.That(a, Does.Contain("Engagement|"));
    }

    [Test]
    public void Unified_fingerprint_differs_when_seed_differs()
    {
        var a = RunScenarioFingerprint(1);
        var b = RunScenarioFingerprint(999983);
        Assert.That(a, Is.Not.EqualTo(b));
    }

    private static string RunScenarioFingerprint(int globalSeed)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed);
        foreach (var name in new[] { "a1", "a2" })
        {
            var unit = new UnitTarget(new TargetId($"u-{name}"));
            var agent = orchestrator.CreateAgent(
                new AgentId(name),
                PersonalityCatalog.All[0].Traits,
                AutonomyLevel.FullAutonomous);
            orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
            orchestrator.Register(unit);
        }

        orchestrator.BeginExecution();

        for (var tick = 0; tick < 6; tick++)
        {
            orchestrator.Tick(new ObservedState(tick, 2 + tick % 2, tick % 2, new Dictionary<TargetId, bool>()));
        }

        return orchestrator.DecisionLog.ComputeFingerprint();
    }

    private static string RunMvpEngageFingerprint(int globalSeed, string scenarioPolicyId)
    {
        var orchestrator = new DelegationOrchestrator(globalSeed);
        var session = SimulationSession.BindMvpEngagementForScenario(orchestrator, scenarioPolicyId);
        var unit = new UnitTarget(new TargetId("u1"));
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        orchestrator.Register(unit);
        session.BeginExecution();

        for (var tick = 0; tick < 4; tick++)
        {
            session.Tick(new ObservedState(tick, 2, 0, new Dictionary<TargetId, bool>()));
        }

        return orchestrator.DecisionLog.ComputeFingerprint();
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
