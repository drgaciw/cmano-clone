using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>DRG-248: live message log follows the player-info filter; replay/AAR stays full.</summary>
[TestFixture]
public sealed class MessageLogLiveBindingTests
{
    [TestCase(PlayerInfoModel.DelegationFog)]
    [TestCase(PlayerInfoModel.TieredByAutonomy)]
    public void ProjectLive_omits_filtered_agent_decisions_but_keeps_alerts(PlayerInfoModel model)
    {
        var log = SampleLog();

        var live = MessageLogProjection.ProjectLive(log, model);

        var decisions = live.Where(l => l.Category == "AGENT_DECISION").ToList();
        Assert.That(decisions, Has.Count.EqualTo(1));
        Assert.That(decisions[0].Text, Does.Contain("assisted-agent"));
        Assert.That(live.Any(l => l.Text.Contains("full-agent")), Is.False);
        Assert.That(live.Any(l => l.Category == "POLICY_DENIAL"), Is.True);
    }

    [TestCase(PlayerInfoModel.DelegationFog)]
    [TestCase(PlayerInfoModel.TieredByAutonomy)]
    public void Full_projection_still_reads_every_decision_after_live_projection(PlayerInfoModel model)
    {
        var log = SampleLog();
        var entriesBefore = log.ChronologicalEntries().Count;

        _ = MessageLogProjection.ProjectLive(log, model);
        var full = MessageLogProjection.Project(log);

        Assert.That(log.ChronologicalEntries(), Has.Count.EqualTo(entriesBefore));
        Assert.That(full.Count(l => l.Category == "AGENT_DECISION"), Is.EqualTo(2));
        Assert.That(full.Any(l => l.Text.Contains("full-agent")), Is.True);
    }

    [Test]
    public void ProjectLive_FullTransparency_matches_full_projection()
    {
        var log = SampleLog();

        var live = MessageLogProjection.ProjectLive(log, PlayerInfoModel.FullTransparency);
        var full = MessageLogProjection.Project(log);

        Assert.That(live.Select(l => (l.SequenceId, l.Category, l.Text)),
            Is.EqualTo(full.Select(l => (l.SequenceId, l.Category, l.Text))));
    }

    [Test]
    public void ProjectLive_null_log_throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            MessageLogProjection.ProjectLive(null!, PlayerInfoModel.DelegationFog));
    }

    [Test]
    public void Bind_defaults_to_full_decision_log_source()
    {
        var panel = MessageLogPanelBinder.Bind(MessageLogProjection.Project(SampleLog()));

        Assert.That(panel.Source, Is.EqualTo(MessageLogBindingSource.FullDecisionLog));
    }

    [Test]
    public void Bind_with_live_source_marks_panel_live()
    {
        var lines = MessageLogProjection.ProjectLive(SampleLog(), PlayerInfoModel.DelegationFog);

        var panel = MessageLogPanelBinder.Bind(lines, MessageLogBindingSource.LiveOrderLogView);

        Assert.That(panel.Source, Is.EqualTo(MessageLogBindingSource.LiveOrderLogView));
        Assert.That(panel.Rows, Has.Count.EqualTo(lines.Count));
    }

    private static DecisionLog SampleLog()
    {
        var log = new DecisionLog();
        log.Append(MakeDecision("assisted-agent", AutonomyLevel.Assisted));
        log.Append(MakeDecision("full-agent", AutonomyLevel.FullAutonomous));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId("full-agent"), new TargetId("t1"), 1,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));
        return log;
    }

    private static DecisionRecord MakeDecision(string agentId, AutonomyLevel autonomy) =>
        new(
            0,
            new AgentId(agentId),
            new TargetId("t1"),
            autonomy,
            OrderKind.Hold,
            [],
            "test",
            1,
            10,
            0.5);
}
