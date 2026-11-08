using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class ControllerBadgeProjectionTests
{
    [Test]
    public void Project_binds_each_roster_row_to_badge_in_roster_order()
    {
        var roster = AgentRosterProjection.Project(new (string unitId, string controllerKind, string? agentId, string autonomy)[]
        {
            ("u3", "AgentSuspended", "a3", "SemiAutonomous"),
            ("u1", "Agent", "a1", "FullAutonomous"),
            ("u2", "Human", null, "Manual"),
            ("u4", "None", null, "—"),
        });

        var badges = ControllerBadgeProjection.Project(roster);

        Assert.That(badges.Select(b => b.UnitId), Is.EqualTo(roster.Select(r => r.UnitId)));
        Assert.That(badges.Select(b => b.Kind), Is.EqualTo(new[]
        {
            ControllerBadgeKind.Agent,
            ControllerBadgeKind.Human,
            ControllerBadgeKind.AgentSuspended,
            ControllerBadgeKind.None,
        }));
        Assert.That(badges[0].AgentId, Is.EqualTo("a1"));
        Assert.That(badges[1].AgentId, Is.Null);
        Assert.That(badges[2].AgentId, Is.EqualTo("a3"));
    }

    [Test]
    public void Badges_are_distinguishable_without_colour()
    {
        var kinds = new[]
        {
            ControllerBadgeKind.None,
            ControllerBadgeKind.Human,
            ControllerBadgeKind.Agent,
            ControllerBadgeKind.AgentSuspended,
        };
        var badges = kinds
            .Select(k => ControllerBadgeProjection.FromRosterEntry(Entry("u", ControllerBadgeProjection.FormatKind(k), "a")))
            .ToList();

        Assert.That(badges.Select(b => b.Kind), Is.EqualTo(kinds));
        Assert.That(badges.Select(b => b.Shape).Distinct().Count(), Is.EqualTo(kinds.Length));
        Assert.That(badges.Select(b => b.ShortText).Distinct().Count(), Is.EqualTo(kinds.Length));
        Assert.That(badges.Select(b => b.ColorToken).Distinct().Count(), Is.EqualTo(kinds.Length));
        Assert.That(badges.Select(b => b.Shape), Has.None.Empty);
        Assert.That(badges.Select(b => b.ShortText), Has.None.Empty);
        Assert.That(badges.Select(b => b.AccessibleLabel), Has.None.Empty);
    }

    [Test]
    public void Human_with_suspended_agent_names_held_agent_in_text()
    {
        var roster = AgentRosterProjection.Project(new (string unitId, string controllerKind, string? agentId, string autonomy)[]
        {
            ("u1", "Human", "a1", "FullAutonomous"),
        });

        var badge = ControllerBadgeProjection.Project(roster).Single();

        Assert.That(badge.Kind, Is.EqualTo(ControllerBadgeKind.Human));
        Assert.That(badge.HasHeldAgent, Is.True);
        Assert.That(badge.AccessibleLabel, Does.Contain("a1"));
        Assert.That(badge.AccessibleLabel, Does.Contain("suspended"));
    }

    [Test]
    public void Attention_status_keeps_controller_kind()
    {
        var roster = AgentRosterProjection.Project(new (string unitId, string controllerKind, string? agentId, string autonomy, string? attention)[]
        {
            ("u1", "Agent", "a1", "FullAutonomous", "Overloaded"),
        });

        var badge = ControllerBadgeProjection.Project(roster).Single();

        Assert.That(roster[0].StatusLabel, Is.EqualTo("Attention"));
        Assert.That(badge.Kind, Is.EqualTo(ControllerBadgeKind.Agent));
    }

    [Test]
    public void Unknown_mode_label_falls_back_to_None()
    {
        var badge = ControllerBadgeProjection.FromRosterEntry(Entry("u1", "Mystery", null));

        Assert.That(badge.Kind, Is.EqualTo(ControllerBadgeKind.None));
    }

    [Test]
    public void Project_null_or_empty_returns_empty()
    {
        Assert.That(ControllerBadgeProjection.Project(null), Is.Empty);
        Assert.That(ControllerBadgeProjection.Project(Array.Empty<AgentRosterEntry>()), Is.Empty);
    }

    [Test]
    public void Badge_follows_assign_take_control_and_return_to_agent_without_mutating_slot()
    {
        var orchestrator = new DelegationOrchestrator(1);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);

        Assert.That(BadgeFor(unit).Kind, Is.EqualTo(ControllerBadgeKind.None));

        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        Assert.That(BadgeFor(unit).Kind, Is.EqualTo(ControllerBadgeKind.Agent));
        Assert.That(unit.Slot.Active, Is.SameAs(agent));

        Assert.That(orchestrator.TryTakeDirectControl(unit, simTime: 1), Is.True);
        var held = BadgeFor(unit);
        Assert.That(held.Kind, Is.EqualTo(ControllerBadgeKind.Human));
        Assert.That(held.HasHeldAgent, Is.True);
        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(agent));

        Assert.That(orchestrator.TryReleaseDirectControl(unit, simTime: 2), Is.True);
        Assert.That(BadgeFor(unit).Kind, Is.EqualTo(ControllerBadgeKind.Agent));
        Assert.That(unit.Slot.Active, Is.SameAs(agent));

        unit.Slot.SuspendAgent(agent);
        Assert.That(BadgeFor(unit).Kind, Is.EqualTo(ControllerBadgeKind.AgentSuspended));
        Assert.That(unit.Slot.Active, Is.Null);
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(agent));
    }

    private static ControllerBadge BadgeFor(UnitTarget unit)
    {
        var roster = AgentRosterProjection.Project(
            new[] { unit.Id },
            controllerKind: _ => DescribeKind(unit.Slot),
            agentId: _ => (unit.Slot.Active as AgentController)?.Id.Value ?? unit.Slot.SuspendedAgent?.Id.Value,
            autonomy: _ => "FullAutonomous");
        return ControllerBadgeProjection.Project(roster).Single();
    }

    private static string DescribeKind(ControllerSlot slot) => slot.Active switch
    {
        AgentController => "Agent",
        HumanController => "Human",
        null when slot.SuspendedAgent is not null => "AgentSuspended",
        _ => "None",
    };

    private static AgentRosterEntry Entry(string unitId, string mode, string? agentId) =>
        new(agentId ?? AgentRosterProjection.MissingAgentId, unitId, "—", "—", AgentRosterProjection.DefaultAttentionLabel, mode);
}
