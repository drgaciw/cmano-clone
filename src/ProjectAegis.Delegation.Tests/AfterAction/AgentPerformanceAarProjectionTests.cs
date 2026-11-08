using NUnit.Framework;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.AfterAction;

public sealed class AgentPerformanceAarProjectionTests
{
    private static DecisionRecord Intent(string agent, double t) =>
        new(t, new AgentId(agent), new TargetId("u1"), AutonomyLevel.SemiAutonomous, OrderKind.Engage,
            Array.Empty<ScoredIntent>(), "r", 0, 1, 0.5, (ulong)t);

    private static DecisionLog BuildLog()
    {
        var log = new DecisionLog();
        log.Append(Intent("alpha", 1));
        log.Append(Intent("alpha", 2));
        log.Append(Intent("alpha", 3));
        log.Append(Intent("alpha", 4));
        log.Append(Intent("bravo", 2));
        log.AppendPolicyDenial(new PolicyDenialRecord(0, 2, 2, new AgentId("bravo"), new TargetId("u2"), 1,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));
        log.AppendControllerChange(new ControllerChangeRecord(0, 3, new TargetId("u1"), "Agent:alpha", "Human", new AgentId("alpha")));
        log.AppendControllerChange(new ControllerChangeRecord(0, 4, new TargetId("u1"), "Human", "Agent:alpha", new AgentId("alpha")));
        log.AppendControllerChange(new ControllerChangeRecord(0, 5, new TargetId("u3"), "Human", "Agent:charlie", new AgentId("charlie")));
        return log;
    }

    [Test]
    public void Project_EmitsPerAgentIntentsDenialsAndOverrideRate()
    {
        var rows = AgentPerformanceAarProjection.Project(BuildLog()).Rows;

        Assert.That(rows.Select(r => r.AgentId), Is.EqualTo(new[] { "alpha", "bravo" }));
        Assert.That(rows[0], Is.EqualTo(new AgentPerformanceRow("alpha", 4, 0, 1)));
        Assert.That(rows[0].OverrideRate, Is.EqualTo(0.25));
        Assert.That(rows[1], Is.EqualTo(new AgentPerformanceRow("bravo", 1, 1, 0)));
    }

    [Test]
    public void Project_IsReadOnlyAndDeterministic()
    {
        var log = BuildLog();
        var before = log.ComputeFingerprint();
        var a = AgentPerformanceAarProjection.ToCsv(AgentPerformanceAarProjection.Project(log));
        var b = AgentPerformanceAarProjection.ToCsv(AgentPerformanceAarProjection.Project(BuildLog()));

        Assert.That(a, Is.EqualTo(b));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(before));
        Assert.That(a, Is.EqualTo("agentId,intents,policyDenials,overrides,overrideRate\nalpha,4,0,1,0.25\nbravo,1,1,0,0\n"));
    }

    [Test]
    public void Project_EmptyOrNullLog_ReturnsEmpty()
    {
        Assert.That(AgentPerformanceAarProjection.Project(null).Rows, Is.Empty);
        Assert.That(AgentPerformanceAarProjection.Project(new DecisionLog()).Rows, Is.Empty);
    }


    [Test]
    public void Project_ExcludesInfrastructureDenialIds_AndCreditsOwningAgent()
    {
        var log = new DecisionLog();
        log.Append(Intent("alpha", 1));
        log.AppendControllerChange(new ControllerChangeRecord(
            0, 1, new TargetId("u1"), "Human", "Agent:alpha", new AgentId("alpha")));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 2, 2, new AgentId(AgentPerformanceAarProjection.CommsGuardAgentId), new TargetId("u1"), 0,
            FireAbortReason.CommsDenied, OrderKind.Engage));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 3, 3, new AgentId(AgentPerformanceAarProjection.CommsGridAgentId), new TargetId("u9"), 0,
            FireAbortReason.OffGrid, OrderKind.Engage));

        var rows = AgentPerformanceAarProjection.Project(log).Rows;

        Assert.That(rows.Select(r => r.AgentId), Is.EqualTo(new[] { "alpha" }));
        Assert.That(rows[0], Is.EqualTo(new AgentPerformanceRow("alpha", 1, 1, 0)));
        Assert.That(rows.Any(r => r.AgentId is "comms-grid" or "comms-guard"), Is.False);
    }

    [Test]
    public void Project_HumanOnlyInfrastructureDenial_ProducesNoAgentRow()
    {
        var log = new DecisionLog();
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId(AgentPerformanceAarProjection.CommsGridAgentId), new TargetId("u1"), 0,
            FireAbortReason.OffGrid, OrderKind.Move));

        Assert.That(AgentPerformanceAarProjection.Project(log).Rows, Is.Empty);
    }

    [Test]
    public void SourceFence_NoUnityOrMutatingRefs()
    {
        var src = File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,
            "..", "..", "..", "..", "ProjectAegis.Delegation", "AfterAction", "AgentPerformanceAarProjection.cs"));
        foreach (var banned in new[] { "UnityEngine", "MonoBehaviour", "log.Append", "SimulationSession", "DelegationBridge", "IOrderLog " })
        {
            Assert.That(src, Does.Not.Contain(banned), banned);
        }
    }
}
