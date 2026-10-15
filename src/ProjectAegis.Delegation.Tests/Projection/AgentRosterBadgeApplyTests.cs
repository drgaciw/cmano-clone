using ProjectAegis.Delegation.Attention;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>
/// DRG-375: roster apply maps <see cref="ControllerBadgeProjection"/> onto display rows.
/// Presentation only (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
[TestFixture]
public sealed class AgentRosterBadgeApplyTests
{
    [Test]
    public void Apply_maps_each_roster_mode_to_controller_badge()
    {
        var entries = new[]
        {
            Entry("u-human", "agent-held", "Human"),
            Entry("u-agent", "agent-a", "Agent"),
            Entry("u-susp", "agent-s", "AgentSuspended"),
            Entry("u-none", "—", "None"),
        };

        var presentation = AgentRosterApplyState.Apply(entries);

        Assert.That(presentation.Rows, Has.Count.EqualTo(4));
        var badges = new ControllerBadge[entries.Length];
        for (var i = 0; i < entries.Length; i++)
        {
            var badge = RequireBadge(presentation.Rows[i]);
            badges[i] = badge;
            Assert.That(badge, Is.EqualTo(ControllerBadgeProjection.FromRosterEntry(entries[i])));
            Assert.That(badge.Shape, Is.Not.EqualTo(badge.ColorToken));
            Assert.That(badge.ShortText, Is.Not.Empty);
        }

        Assert.That(badges[0].Shape, Is.EqualTo("square"));
        Assert.That(badges[0].ShortText, Is.EqualTo("HUM"));
        Assert.That(badges[0].ColorToken, Is.EqualTo("badge-human"));
        Assert.That(badges[0].AccessibleLabel, Does.Contain("Human control"));

        Assert.That(badges[1].Shape, Is.EqualTo("diamond"));
        Assert.That(badges[1].ShortText, Is.EqualTo("AGT"));
        Assert.That(badges[1].ColorToken, Is.EqualTo("badge-agent"));

        Assert.That(badges[2].Shape, Is.EqualTo("diamond-dashed"));
        Assert.That(badges[2].ShortText, Is.EqualTo("AGT-SUSP"));
        Assert.That(badges[2].ColorToken, Is.EqualTo("badge-agent-suspended"));

        Assert.That(badges[3].Shape, Is.EqualTo("none"));
        Assert.That(badges[3].ShortText, Is.EqualTo("—"));
        Assert.That(badges[3].ColorToken, Is.EqualTo("badge-none"));
        Assert.That(badges[3].AccessibleLabel, Is.EqualTo("No controller"));
    }

    [Test]
    public void ApplyWithAttention_keeps_badge_from_mode_while_attention_changes()
    {
        var entry = Entry("u1", "agent-a", "Agent");
        var attention = new Dictionary<string, AgentAttentionRow>
        {
            ["agent-a"] = new AgentAttentionRow(
                "agent-a",
                20,
                22,
                true,
                true,
                AttentionTierName.SlowerReactions,
                "SlowerReactions",
                "ATT: SlowerReactions",
                "LOAD: 22.0/20.0",
                "Attention SlowerReactions; load 22.0 of budget 20.0"),
        };

        var presentation = AgentRosterApplyState.ApplyWithAttention(new[] { entry }, attention);

        Assert.That(RequireBadge(presentation.Rows[0]), Is.EqualTo(ControllerBadgeProjection.FromRosterEntry(entry)));
        Assert.That(presentation.Rows[0].AttentionLabel, Does.Contain("SlowerReactions"));
        Assert.That(entry.ModeLabel, Is.EqualTo("Agent"));
    }

    [Test]
    public void Apply_rereads_roster_after_take_control_and_return_without_mutating_entries()
    {
        var agent = Entry("u1", "agent-a", "Agent");
        var human = Entry("u1", "agent-a", "Human");
        var suspended = Entry("u1", "agent-a", "AgentSuspended");

        var commanding = AgentRosterApplyState.Apply(new[] { agent });
        var taken = AgentRosterApplyState.Apply(new[] { human });
        var returned = AgentRosterApplyState.Apply(new[] { suspended });

        Assert.That(RequireBadge(commanding.Rows[0]).Kind, Is.EqualTo(ControllerBadgeKind.Agent));
        var takenBadge = RequireBadge(taken.Rows[0]);
        Assert.That(takenBadge.Kind, Is.EqualTo(ControllerBadgeKind.Human));
        Assert.That(takenBadge.HasHeldAgent, Is.True);
        Assert.That(RequireBadge(returned.Rows[0]).Kind, Is.EqualTo(ControllerBadgeKind.AgentSuspended));
        Assert.That(agent.ModeLabel, Is.EqualTo("Agent"));
        Assert.That(human.ModeLabel, Is.EqualTo("Human"));
        Assert.That(suspended.ModeLabel, Is.EqualTo("AgentSuspended"));
    }

    [Test]
    public void Host_binds_badge_shape_text_color_and_accessible_label_without_a_second_binder()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        var host = File.ReadAllText(Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "Scripts",
            "Runtime",
            "AgentRosterPanelHost.cs"));
        var apply = File.ReadAllText(Path.Combine(
            root!,
            "src",
            "ProjectAegis.Delegation",
            "Projection",
            "AgentRosterApplyState.cs"));

        Assert.That(apply, Does.Contain("ControllerBadgeProjection.FromRosterEntry"));
        Assert.That(host, Does.Contain("AgentRosterApplyState.Apply(bridgeHost.LastAgentRoster)"));
        Assert.That(host, Does.Contain(".Badge"));
        Assert.That(host, Does.Contain("ShortText"));
        Assert.That(host, Does.Contain("ColorToken"));
        Assert.That(host, Does.Contain("Shape"));
        Assert.That(host, Does.Contain("AccessibleLabel"));
        Assert.That(host, Does.Not.Contain("ControllerBadgeBinder"));
        Assert.That(host, Does.Not.Contain("DelegationBridge.Tick"));
        Assert.That(host, Does.Not.Contain("CatalogWriteGate"));
        Assert.That(host, Does.Not.Contain("SimulationSession"));
        Assert.That(host, Does.Not.Contain("IOrderSink"));
    }

    [Test]
    public void Roster_stylesheet_carries_shape_and_color_channels()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null);
        var uss = File.ReadAllText(Path.Combine(
            root!,
            "unity",
            "ProjectAegis",
            "Assets",
            "UI",
            "AgentRoster",
            "AgentRosterPanel.uss"));

        Assert.That(uss, Does.Contain(".agent-roster-badge"));
        Assert.That(uss, Does.Contain(".badge-shape-square"));
        Assert.That(uss, Does.Contain(".badge-shape-diamond"));
        Assert.That(uss, Does.Contain(".badge-shape-diamond-dashed"));
        Assert.That(uss, Does.Contain(".badge-shape-none"));
        Assert.That(uss, Does.Contain(".badge-human"));
        Assert.That(uss, Does.Contain(".badge-agent"));
        Assert.That(uss, Does.Contain(".badge-agent-suspended"));
        Assert.That(uss, Does.Contain(".badge-none"));
    }

    private static ControllerBadge RequireBadge(AgentRosterDisplayRow row)
    {
        Assert.That(row.Badge, Is.Not.Null);
        return row.Badge!;
    }

    private static AgentRosterEntry Entry(string unitId, string agentId, string mode) =>
        new(agentId, unitId, "FullAutonomous", "Active", "ATTENTION: —", mode);

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
