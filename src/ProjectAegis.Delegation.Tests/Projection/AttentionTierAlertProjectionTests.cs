using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AttentionTierAlertProjectionTests
{
    private static AgentAttentionRow Row(string id, AttentionTierName tier, double load = 25, double budget = 20) =>
        new(
            id, budget, load,
            IsOverloaded: load > budget,
            HasSample: true,
            Tier: tier,
            TierLabel: AttentionTierNaming.DisplayName(tier),
            StatusLabel: $"ATT: {AttentionTierNaming.DisplayName(tier)}",
            LoadBadge: $"LOAD: {load:0.0}/{budget:0.0}",
            AccessibleLabel: AttentionTierNaming.AccessibleLabel(tier, load, budget));

    [Test]
    public void Diff_no_change_emits_nothing()
    {
        var current = new[] { Row("a1", AttentionTierName.SlowerReactions) };
        var previous = new Dictionary<string, AgentAttentionRow>
        {
            ["a1"] = Row("a1", AttentionTierName.SlowerReactions),
        };

        var alerts = AttentionTierAlertProjection.Diff(previous, current);
        Assert.That(alerts, Is.Empty);
    }

    [Test]
    public void Diff_upward_crossing_emits_alert_with_agent_and_tiers()
    {
        var current = new[] { Row("a1", AttentionTierName.NarrowedFocus, load: 26) };
        var previous = new Dictionary<string, AgentAttentionRow>
        {
            ["a1"] = Row("a1", AttentionTierName.SlowerReactions, load: 22),
        };

        var alerts = AttentionTierAlertProjection.Diff(previous, current);
        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0].AgentId, Is.EqualTo("a1"));
        Assert.That(alerts[0].PriorTier, Is.EqualTo(AttentionTierName.SlowerReactions));
        Assert.That(alerts[0].NewTier, Is.EqualTo(AttentionTierName.NarrowedFocus));
        Assert.That(alerts[0].Text, Does.Contain("a1"));
        Assert.That(alerts[0].AccessibleText, Does.Contain("NarrowedFocus"));
        Assert.That(alerts[0].Severity, Is.EqualTo(AlertSeverity.Notable));
    }

    [Test]
    public void Diff_downward_crossing_emits_alert()
    {
        var current = new[] { Row("a1", AttentionTierName.Nominal, load: 10) };
        var previous = new Dictionary<string, AgentAttentionRow>
        {
            ["a1"] = Row("a1", AttentionTierName.SimplerDecisions, load: 35),
        };

        var alerts = AttentionTierAlertProjection.Diff(previous, current);
        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0].NewTier, Is.EqualTo(AttentionTierName.Nominal));
        Assert.That(alerts[0].PriorTier, Is.EqualTo(AttentionTierName.SimplerDecisions));
        Assert.That(alerts[0].Severity, Is.EqualTo(AlertSeverity.Routine));
    }

    [Test]
    public void Diff_first_nominal_sample_does_not_alert()
    {
        var current = new[] { Row("a1", AttentionTierName.Nominal, load: 5) };
        var alerts = AttentionTierAlertProjection.Diff(null, current);
        Assert.That(alerts, Is.Empty);
    }

    [Test]
    public void Diff_first_degraded_sample_alerts()
    {
        var current = new[] { Row("a1", AttentionTierName.SimplerDecisions, load: 40) };
        var alerts = AttentionTierAlertProjection.Diff(null, current);
        Assert.That(alerts, Has.Count.EqualTo(1));
        Assert.That(alerts[0].PriorTier, Is.Null);
        Assert.That(alerts[0].Severity, Is.EqualTo(AlertSeverity.Critical));
    }

    [Test]
    public void ToMessageLogLine_uses_attention_category()
    {
        var alert = AttentionTierAlertProjection.BuildAlert(
            Row("a9", AttentionTierName.SlowerReactions),
            AttentionTierName.Nominal);
        var line = AttentionTierAlertProjection.ToMessageLogLine(alert, 42, 12.5);
        Assert.That(line.Category, Is.EqualTo(AttentionTierAlertProjection.Category));
        Assert.That(line.UnitId, Is.EqualTo("a9"));
        Assert.That(line.SequenceId, Is.EqualTo(42UL));
    }
}
