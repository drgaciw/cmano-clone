using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Traits;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 / ADR-019: pure ownership → <see cref="DelegationStateProjection"/> builder.</summary>
[TestFixture]
public sealed class DelegationStateProjectionBuilderTests
{
    [Test]
    public void FromSlot_human_only_projects_Manual_and_Human_owner()
    {
        var slot = new ControllerSlot();
        slot.SetActive(new HumanController());

        var state = DelegationStateProjectionBuilder.FromSlot("u1", slot, paused: false);

        Assert.That(state.UnitId, Is.EqualTo("u1"));
        Assert.That(state.Owner, Is.EqualTo(DelegationOwnerKind.Human));
        Assert.That(state.AutonomyLevel, Is.EqualTo(AutonomyLevel.Manual));
        Assert.That(state.PersonalityId, Is.EqualTo(string.Empty));
        Assert.That(state.Paused, Is.False);
    }

    [Test]
    public void FromSlot_agent_projects_agent_autonomy_and_personality()
    {
        var orchestrator = new DelegationOrchestrator(7);
        var agent = orchestrator.CreateAgentFromPreset(
            new AgentId("a1"),
            PersonalityCatalog.All[0],
            AutonomyLevel.SemiAutonomous);
        var slot = new ControllerSlot();
        slot.SetActive(agent);

        var state = DelegationStateProjectionBuilder.FromSlot("u2", slot, paused: true);

        Assert.That(state.Owner, Is.EqualTo(DelegationOwnerKind.Agent));
        Assert.That(state.AutonomyLevel, Is.EqualTo(AutonomyLevel.SemiAutonomous));
        Assert.That(state.PersonalityId, Is.EqualTo(PersonalityCatalog.All[0].Name));
        Assert.That(state.Paused, Is.True);
    }

    [Test]
    public void FromSlot_human_with_suspended_agent_is_Mixed()
    {
        var orchestrator = new DelegationOrchestrator(7);
        var agent = orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        var slot = new ControllerSlot();
        slot.SuspendAgent(agent);
        slot.SetActive(new HumanController());

        var state = DelegationStateProjectionBuilder.FromSlot("u3", slot, paused: false);

        Assert.That(state.Owner, Is.EqualTo(DelegationOwnerKind.Mixed));
        Assert.That(state.AutonomyLevel, Is.EqualTo(AutonomyLevel.FullAutonomous));
    }

    [Test]
    public void ResolveOwner_empty_slot_is_Human_default()
    {
        Assert.That(
            DelegationStateProjectionBuilder.ResolveOwner(new ControllerSlot()),
            Is.EqualTo(DelegationOwnerKind.Human));
    }

    [Test]
    public void ProjectAll_applies_per_unit_pause()
    {
        var humanSlot = new ControllerSlot();
        humanSlot.SetActive(new HumanController());
        var agentSlot = new ControllerSlot();
        var orchestrator = new DelegationOrchestrator(3);
        agentSlot.SetActive(orchestrator.CreateAgent(
            new AgentId("a2"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.Assisted));

        var rows = DelegationStateProjectionBuilder.ProjectAll(
            [("u-human", humanSlot), ("u-agent", agentSlot)],
            id => id == "u-agent");

        Assert.That(rows, Has.Count.EqualTo(2));
        Assert.That(rows[0].Paused, Is.False);
        Assert.That(rows[1].Paused, Is.True);
        Assert.That(rows[1].Owner, Is.EqualTo(DelegationOwnerKind.Agent));
    }
}
