using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AgentRosterProjectionTests
{
    [Test]
    public void Project_from_tuples_orders_by_unit_id_and_maps_labels()
    {
        var rows = new (string unitId, string controllerKind, string? agentId, string autonomy)[]
        {
            ("u2", "Human", "agent-b", "Manual"),
            ("u1", "Agent", "agent-a", "FullAutonomous"),
            ("u3", "None", null, "Assisted"),
        };

        var roster = AgentRosterProjection.Project(rows);

        Assert.That(roster.Select(r => r.UnitId), Is.EqualTo(new[] { "u1", "u2", "u3" }));
        Assert.That(roster[0].AgentId, Is.EqualTo("agent-a"));
        Assert.That(roster[0].StatusLabel, Is.EqualTo("Active"));
        Assert.That(roster[0].ModeLabel, Is.EqualTo("Agent"));
        Assert.That(roster[0].AutonomyLabel, Is.EqualTo("FullAutonomous"));
        Assert.That(roster[0].AttentionLabel, Is.EqualTo(AgentRosterProjection.DefaultAttentionLabel));

        Assert.That(roster[1].StatusLabel, Is.EqualTo("Suspended"));
        Assert.That(roster[1].ModeLabel, Is.EqualTo("Human"));

        Assert.That(roster[2].AgentId, Is.EqualTo(AgentRosterProjection.MissingAgentId));
        Assert.That(roster[2].StatusLabel, Is.EqualTo("—"));
        Assert.That(roster[2].ModeLabel, Is.EqualTo("None"));
    }

    [Test]
    public void Project_AgentSuspended_is_Suspended_status()
    {
        var rows = new (string unitId, string controllerKind, string? agentId, string autonomy)[]
        {
            ("blue-1", "AgentSuspended", "agent-x", "SemiAutonomous"),
        };
        var roster = AgentRosterProjection.Project(rows);

        Assert.That(roster, Has.Count.EqualTo(1));
        Assert.That(roster[0].StatusLabel, Is.EqualTo("Suspended"));
        Assert.That(roster[0].ModeLabel, Is.EqualTo("AgentSuspended"));
        Assert.That(roster[0].AutonomyLabel, Is.EqualTo("SemiAutonomous"));
    }

    [Test]
    public void Project_with_attention_marks_Attention_status()
    {
        var rows = new (string unitId, string controllerKind, string? agentId, string autonomy, string? attention)[]
        {
            ("u1", "Agent", "a1", "FullAutonomous", "Overloaded"),
        };
        var roster = AgentRosterProjection.Project(rows);

        Assert.That(roster[0].StatusLabel, Is.EqualTo("Attention"));
        Assert.That(roster[0].AttentionLabel, Is.EqualTo("ATTENTION: Overloaded"));
    }

    [Test]
    public void Project_func_lookups_walk_unit_ids()
    {
        var roster = AgentRosterProjection.Project(
            new[] { "b", "a" },
            controllerKind: id => id == "a" ? "Agent" : "Human",
            agentId: id => id == "a" ? "ag-a" : "ag-b",
            autonomy: _ => AgentRosterProjection.FormatAutonomyLabel(AutonomyLevel.Assisted));

        Assert.That(roster.Select(r => r.UnitId), Is.EqualTo(new[] { "a", "b" }));
        Assert.That(roster[0].AutonomyLabel, Is.EqualTo("Assisted"));
        Assert.That(roster[0].StatusLabel, Is.EqualTo("Active"));
        Assert.That(roster[1].StatusLabel, Is.EqualTo("Suspended"));
    }

    [Test]
    public void Project_TargetId_overload_uses_lookups()
    {
        var roster = AgentRosterProjection.Project(
            new[] { new TargetId("t1") },
            controllerKind: _ => "Agent",
            agentId: id => $"agent-{id.Value}",
            autonomy: _ => "Manual",
            attention: _ => null);

        Assert.That(roster, Has.Count.EqualTo(1));
        Assert.That(roster[0].AgentId, Is.EqualTo("agent-t1"));
        Assert.That(roster[0].AttentionLabel, Is.EqualTo("ATTENTION: —"));
    }

    [Test]
    public void Project_empty_or_null_returns_empty()
    {
        Assert.That(AgentRosterProjection.Project(
            Array.Empty<(string, string, string?, string)>()), Is.Empty);
        Assert.That(AgentRosterProjection.Project(
            (IReadOnlyList<(string, string, string?, string)>)null!), Is.Empty);
    }

    [Test]
    public void ApplyState_formats_lines_for_binding()
    {
        var rows = new (string unitId, string controllerKind, string? agentId, string autonomy)[]
        {
            ("u1", "Agent", "a1", "FullAutonomous"),
        };
        var entries = AgentRosterProjection.Project(rows);

        var presentation = AgentRosterApplyState.Apply(entries);

        Assert.That(presentation.Count, Is.EqualTo(1));
        Assert.That(presentation.Lines, Has.Count.EqualTo(1));
        Assert.That(presentation.Lines[0], Does.Contain("u1"));
        Assert.That(presentation.Lines[0], Does.Contain("agent=a1"));
        Assert.That(presentation.Lines[0], Does.Contain("[Active]"));
        Assert.That(presentation.Lines[0], Does.Contain("FullAutonomous"));
        Assert.That(presentation.Rows[0].DisplayLine, Is.EqualTo(presentation.Lines[0]));
    }

    [Test]
    public void ApplyState_null_or_empty_returns_Empty()
    {
        Assert.That(AgentRosterApplyState.Apply(null).Count, Is.EqualTo(0));
        Assert.That(AgentRosterApplyState.Apply(Array.Empty<AgentRosterEntry>()), Is.SameAs(AgentRosterPresentation.Empty));
        Assert.That(AgentRosterPresentation.Empty.Lines, Is.Empty);
    }

    [Test]
    public void FormatAutonomyLabel_uses_enum_names()
    {
        Assert.That(AgentRosterProjection.FormatAutonomyLabel(AutonomyLevel.Manual), Is.EqualTo("Manual"));
        Assert.That(AgentRosterProjection.FormatAutonomyLabel(AutonomyLevel.FullAutonomous), Is.EqualTo("FullAutonomous"));
        Assert.That(AgentRosterProjection.FormatAutonomyLabel(AutonomyLevel.SemiAutonomous), Is.EqualTo("SemiAutonomous"));
        Assert.That(AgentRosterProjection.FormatAutonomyLabel(AutonomyLevel.Assisted), Is.EqualTo("Assisted"));
    }
}
