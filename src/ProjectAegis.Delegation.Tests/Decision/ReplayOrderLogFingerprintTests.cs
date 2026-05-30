using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
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

        for (var tick = 0; tick < 6; tick++)
        {
            orchestrator.Tick(new ObservedState(tick, 2 + tick % 2, tick % 2, new Dictionary<TargetId, bool>()));
        }

        return orchestrator.DecisionLog.ComputeFingerprint();
    }
}
