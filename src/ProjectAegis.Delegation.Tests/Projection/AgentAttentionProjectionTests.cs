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
    private static AgentController MakeAgent(string id = "a1", double budget = 20.0)
    {
        return new AgentController(
            new AgentId(id),
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
        Assert.That(row.HasSample, Is.False);
    }

    [Test]
    public void Project_no_evaluation_returns_zero_load_without_sample()
    {
        var agent = MakeAgent(budget: 20);
        var row = AgentAttentionProjection.Project(agent, null);

        Assert.That(row.AgentId, Is.EqualTo("a1"));
        Assert.That(row.Budget, Is.EqualTo(20.0));
        Assert.That(row.Load, Is.EqualTo(0.0));
        Assert.That(row.IsOverloaded, Is.False);
        Assert.That(row.HasSample, Is.False);
        Assert.That(row.Tier, Is.EqualTo(AttentionTierName.Nominal));
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: —"));
        Assert.That(row.AccessibleLabel, Does.Contain("unavailable").IgnoreCase);
    }

    [Test]
    public void Project_nominal_load_shows_percentage_and_named_nominal_tier()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 10,
            Degradation: new AttentionDegradation(false, false, false));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.HasSample, Is.True);
        Assert.That(row.IsOverloaded, Is.False);
        Assert.That(row.Tier, Is.EqualTo(AttentionTierName.Nominal));
        Assert.That(row.TierLabel, Is.EqualTo("Nominal"));
        Assert.That(row.StatusLabel, Does.Contain("50"));
        Assert.That(row.StatusLabel, Does.Contain("Nominal"));
        Assert.That(row.LoadBadge, Does.Contain("10.0"));
        Assert.That(row.AccessibleLabel, Does.Contain("Nominal"));
        Assert.That(row.AccessibleLabel, Does.Contain("10.0"));
    }

    [Test]
    public void Project_slower_reactions_named_tier()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 22,
            Degradation: new AttentionDegradation(SlowerReactions: true, NarrowedFocus: false, SimplerDecisions: false));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.IsOverloaded, Is.True);
        Assert.That(row.Tier, Is.EqualTo(AttentionTierName.SlowerReactions));
        Assert.That(row.TierLabel, Is.EqualTo("SlowerReactions"));
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: SlowerReactions"));
        Assert.That(row.AccessibleLabel, Does.Contain("SlowerReactions"));
    }

    [Test]
    public void Project_narrowed_focus_named_tier()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 26,
            Degradation: new AttentionDegradation(true, true, false));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.Tier, Is.EqualTo(AttentionTierName.NarrowedFocus));
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: NarrowedFocus"));
    }

    [Test]
    public void Project_simpler_decisions_named_tier()
    {
        var agent = MakeAgent(budget: 20);
        var eval = new AttentionEvaluation(
            Budget: 20,
            Load: 35,
            Degradation: new AttentionDegradation(true, true, true));

        var row = AgentAttentionProjection.Project(agent, eval);

        Assert.That(row.Tier, Is.EqualTo(AttentionTierName.SimplerDecisions));
        Assert.That(row.StatusLabel, Is.EqualTo("ATT: SimplerDecisions"));
        Assert.That(row.AccessibleLabel, Does.Contain("SimplerDecisions"));
    }

    [Test]
    public void ProjectFromLoadBudget_matches_calculator_thresholds()
    {
        var nominal = AgentAttentionProjection.ProjectFromLoadBudget("a1", 10, 20);
        Assert.That(nominal.Tier, Is.EqualTo(AttentionTierName.Nominal));

        var slower = AgentAttentionProjection.ProjectFromLoadBudget("a1", 21, 20);
        Assert.That(slower.Tier, Is.EqualTo(AttentionTierName.SlowerReactions));

        var narrow = AgentAttentionProjection.ProjectFromLoadBudget("a1", 26, 20);
        Assert.That(narrow.Tier, Is.EqualTo(AttentionTierName.NarrowedFocus));

        var simple = AgentAttentionProjection.ProjectFromLoadBudget("a1", 31, 20);
        Assert.That(simple.Tier, Is.EqualTo(AttentionTierName.SimplerDecisions));
    }

    [Test]
    public void Summarize_orders_by_agent_id_deterministically()
    {
        var a2 = MakeAgent("a2");
        var a1 = MakeAgent("a1");
        var eval = new AttentionEvaluation(20, 5, new AttentionDegradation(false, false, false));
        var pairs = new (AgentController, AttentionEvaluation?)[] { (a2, eval), (a1, eval) };

        var summary = AgentAttentionProjection.Summarize(pairs);

        Assert.That(summary.Rows.Select(r => r.AgentId), Is.EqualTo(new[] { "a1", "a2" }));
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
