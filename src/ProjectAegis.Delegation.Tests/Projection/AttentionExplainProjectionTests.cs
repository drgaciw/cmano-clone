using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AttentionExplainProjectionTests
{
    private static DecisionRecord Record(double load, double budget, string rationale = "test") =>
        new(
            SimTime: 42,
            AgentId: new AgentId("agent-x"),
            TargetId: new TargetId("u1"),
            AutonomyLevel: AutonomyLevel.FullAutonomous,
            ChosenKind: OrderKind.Hold,
            Alternatives: Array.Empty<ScoredIntent>(),
            Rationale: rationale,
            AttentionLoad: load,
            AttentionBudget: budget,
            RngDraw: 0.1);

    [Test]
    public void Project_null_record_returns_empty()
    {
        var s = AttentionExplainProjection.Project(null);
        Assert.That(s.StatusLine, Is.EqualTo(AttentionExplainProjection.NoRecordLabel));
        Assert.That(s.AffectedBehavior, Is.False);
    }

    [Test]
    public void Project_uses_decision_time_load_not_live_state()
    {
        var s = AttentionExplainProjection.Project(Record(load: 35, budget: 20));
        Assert.That(s.Load, Is.EqualTo(35));
        Assert.That(s.Budget, Is.EqualTo(20));
        Assert.That(s.Tier, Is.EqualTo(AttentionTierName.SimplerDecisions));
        Assert.That(s.AffectedBehavior, Is.True);
        Assert.That(s.ReasonPlain, Does.Contain("SimplerDecisions"));
        Assert.That(s.StatusLine, Does.Contain("SimplerDecisions"));
    }

    [Test]
    public void Project_nominal_does_not_claim_degradation()
    {
        var s = AttentionExplainProjection.Project(Record(load: 5, budget: 20));
        Assert.That(s.Tier, Is.EqualTo(AttentionTierName.Nominal));
        Assert.That(s.AffectedBehavior, Is.False);
        Assert.That(s.ReasonPlain, Does.Contain("Nominal"));
    }

    [Test]
    public void CombineWithRationale_appends_when_attention_affected()
    {
        var s = AttentionExplainProjection.Project(Record(load: 30, budget: 20));
        var combined = AttentionExplainProjection.CombineWithRationale("overload: narrowed focus applied", s);
        Assert.That(combined, Does.Contain("overload"));
        Assert.That(combined, Does.Contain("SimplerDecisions").Or.Contain("NarrowedFocus").Or.Contain("attention"));
    }

    [Test]
    public void CombineWithRationale_keeps_existing_when_nominal()
    {
        var s = AttentionExplainProjection.Project(Record(load: 2, budget: 20));
        var combined = AttentionExplainProjection.CombineWithRationale("nominal: trait-weighted", s);
        Assert.That(combined, Is.EqualTo("nominal: trait-weighted"));
    }
}
