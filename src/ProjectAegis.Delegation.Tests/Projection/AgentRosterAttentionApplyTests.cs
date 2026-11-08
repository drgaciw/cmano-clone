using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AgentRosterAttentionApplyTests
{
    [Test]
    public void ApplyWithAttention_merges_tier_into_line_and_accessible_label()
    {
        var entries = new[]
        {
            new AgentRosterEntry("agent-a", "u1", "FullAutonomous", "Active", "ATTENTION: —", "Agent"),
        };
        var att = new Dictionary<string, AgentAttentionRow>
        {
            ["agent-a"] = new AgentAttentionRow(
                "agent-a", 20, 22, true, true,
                AttentionTierName.SlowerReactions,
                "SlowerReactions",
                "ATT: SlowerReactions",
                "LOAD: 22.0/20.0",
                "Attention SlowerReactions; load 22.0 of budget 20.0"),
        };

        var presentation = AgentRosterApplyState.ApplyWithAttention(entries, att);

        Assert.That(presentation.Count, Is.EqualTo(1));
        Assert.That(presentation.Lines[0], Does.Contain("SlowerReactions"));
        Assert.That(presentation.Lines[0], Does.Contain("LOAD:"));
        Assert.That(presentation.Rows[0].AttentionLabel, Does.Contain("SlowerReactions"));
        Assert.That(presentation.Rows[0].AccessibleAttentionLabel, Does.Contain("SlowerReactions"));
        Assert.That(presentation.Rows[0].AccessibleAttentionLabel, Does.Contain("22.0"));
    }

    [Test]
    public void ApplyWithAttention_missing_agent_keeps_default_attention()
    {
        var entries = new[]
        {
            new AgentRosterEntry("—", "u1", "Manual", "Suspended", AgentRosterProjection.DefaultAttentionLabel, "Human"),
        };
        var presentation = AgentRosterApplyState.ApplyWithAttention(entries, null);
        Assert.That(presentation.Rows[0].AttentionLabel, Is.EqualTo(AgentRosterProjection.DefaultAttentionLabel));
    }
}
