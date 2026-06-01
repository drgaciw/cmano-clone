namespace ProjectAegis.Delegation.Tests.Trust;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using NUnit.Framework;

[TestFixture]
public sealed class TrustSignalEmitterTests
{
    [Test]
    public void FinalizeScenario_emits_mvp_trust_metrics_without_affecting_ticks()
    {
        var orchestrator = new DelegationOrchestrator(1);
        orchestrator.BeginExecution();
        orchestrator.DecisionLog.Append(new DecisionRecord(
            0,
            new AgentId("a1"),
            new TargetId("t1"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "hold",
            1,
            20,
            0.5));
        orchestrator.DecisionLog.AppendControllerChange(new ControllerChangeRecord(
            0,
            1,
            new TargetId("t1"),
            "Agent",
            "Human",
            new AgentId("a1")));

        var signals = orchestrator.FinalizeScenario(missionSucceeded: true, objectivesMetRatio: 0.8);

        Assert.That(signals, Is.Not.Empty);
        Assert.That(signals.Any(s => s.Metric == "missions_succeeded" && s.Value == 1.0), Is.True);
        Assert.That(signals.Any(s => s.Metric == "objectives_met_ratio" && s.Value == 0.8), Is.True);
        Assert.That(signals.Any(s => s.Metric == "player_override_rate"), Is.True);
    }
}
