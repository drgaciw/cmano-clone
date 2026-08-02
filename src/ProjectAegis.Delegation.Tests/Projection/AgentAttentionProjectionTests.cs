using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AgentAttentionProjectionTests
{
    private static AgentController MakeAgent(double budget = 20.0)
    {
        return new AgentController(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(42, 0),
            new StubPatrolPolicy(),
            budget);
    }

    [Test]
    public void Project_null_agent_returns_unknown_row()
    {
        var row = AgentAttentionProjection.Project(null!, null);
        Assert.That(row, Is.EqualTo(AgentAttentionRow.Unknown));
    }

    [Test]
    public void Project_no_evaluation_returns_zero_load()
    {
        var agent = MakeAgent(budget: 20);
        var row = AgentAttentionProjection.Project(agent, null);

        Assert.That(row.AgentId, Is.EqualTo("a1"));
        Assert.That(row.Budget, Is.EqualTo(20.0));
        Assert.That(row.Load, Is.EqualTo(0.0));
        Assert.That(row.IsOverloaded, Is.False);
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: —"));
        Assert.That(row.LoadBadge, Does.Contain("LOAD:"));
    }

    [Test]
    public void Project_nominal_load_shows_percentage()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 10,
            Degradation: new AttentionDegradation(false, false, false));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.IsOverloaded, Is.False);
        Assert.That(row.StatusLabel, Does.Contain("50"));
        Assert.That(row.LoadBadge, Does.Contain("10.0"));
        Assert.That(row.LoadBadge, Does.Contain("20.0"));
    }

    [Test]
    public void Project_overloaded_shows_elevated_status()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 22,
            Degradation: new AttentionDegradation(SlowerReactions: true, NarrowedFocus: false, SimplerDecisions: false));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.IsOverloaded, Is.True);
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: ELEVATED"));
    }

    [Test]
    public void Project_critical_load_shows_critical()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 35,
            Degradation: new AttentionDegradation(SlowerReactions: true, NarrowedFocus: true, SimplerDecisions: true));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.StatusLabel, Is.EqualTo("ATT: CRITICAL"));
    }

    [Test]
    public void Summarize_empty_returns_empty_summary()
    {
        var summary = AgentAttentionProjection.Summarize(null);
        Assert.That(summary, Is.EqualTo(AgentAttentionSummary.Empty));

        var summary2 = AgentAttentionProjection.Summarize(
            Array.Empty<(AgentController, AttentionEvaluation?)>());
        Assert.That(summary2, Is.EqualTo(AgentAttentionSummary.Empty));
    }

    [Test]
    public void Summarize_all_nominal_reports_nominal()
    {
        var agent = MakeAgent();
        var eval = new AttentionEvaluation(20, 5,
            new AttentionDegradation(false, false, false));
        var pairs = new (AgentController, AttentionEvaluation?)[] { (agent, eval) };

        var summary = AgentAttentionProjection.Summarize(pairs);

        Assert.That(summary.OverloadedCount, Is.EqualTo(0));
        Assert.That(summary.SummaryLine, Does.Contain("nominal"));
        Assert.That(summary.Rows, Has.Count.EqualTo(1));
    }

    [Test]
    public void Summarize_one_overloaded_reports_count()
    {
        var agent = MakeAgent();
        var overloaded = new AttentionEvaluation(20, 25,
            new AttentionDegradation(SlowerReactions: true, NarrowedFocus: false, SimplerDecisions: false));
        var pairs = new (AgentController, AttentionEvaluation?)[] { (agent, overloaded) };

        var summary = AgentAttentionProjection.Summarize(pairs);

        Assert.That(summary.OverloadedCount, Is.EqualTo(1));
        Assert.That(summary.SummaryLine, Does.Contain("overloaded"));
    }
}
