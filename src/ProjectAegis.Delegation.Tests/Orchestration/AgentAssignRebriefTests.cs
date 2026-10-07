using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Hindsight;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

/// <summary>
/// S124-05 W2-DEL-04 initial Assign Agent and S124-06 W2-DEL-02 rebrief success path on the
/// orchestrator façade. The legacy deny-only <see cref="DelegationOrchestrator.TryRebindAgentTraits"/>
/// path is asserted unchanged alongside the new Rebrief Agent action.
/// </summary>
[TestFixture]
public sealed class AgentAssignRebriefTests
{
    private static readonly PersonalityPreset Aggressive = PersonalityCatalog.All[0];
    private static readonly PersonalityPreset Cautious = PersonalityCatalog.All[2];

    [Test]
    public void TryAssignAgentController_replaces_human_and_logs_controller_change()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var unit = RegisterHumanUnit(orchestrator, "u1");
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);

        var verdict = orchestrator.TryAssignAgentController(unit, agent, isFriendly: true, simTime: 3.0);

        Assert.That(verdict.Allowed, Is.True, verdict.DenialReason);
        Assert.That(unit.Slot.Active, Is.SameAs(agent));
        Assert.That(agent.PolicySnapshotId, Is.GreaterThan(0UL));
        var change = orchestrator.DecisionLog.ControllerChanges.Single();
        Assert.That(change.TargetId, Is.EqualTo(unit.Id));
        Assert.That(change.PreviousKind, Is.EqualTo("Human"));
        Assert.That(change.NewKind, Is.EqualTo("Agent"));
        Assert.That(change.AgentId, Is.EqualTo(agent.Id));
        Assert.That(change.SimTime, Is.EqualTo(3.0));
    }

    [Test]
    public void TryAssignAgentController_assigns_to_group_targets()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var group = new GroupTarget(new TargetId("g1"));
        group.Slot.SetActive(new HumanController());
        orchestrator.Register(group);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a-g1"), Aggressive, AutonomyLevel.FullAutonomous);

        var verdict = orchestrator.TryAssignAgentController(group, agent, isFriendly: true);

        Assert.That(verdict.Allowed, Is.True, verdict.DenialReason);
        Assert.That(group.Slot.Active, Is.SameAs(agent));
    }

    [Test]
    public void TryAssignAgentController_denies_while_human_orders_are_pending_and_keeps_the_queue()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var unit = RegisterHumanUnit(orchestrator, "u1");
        var human = (HumanController)unit.Slot.Active!;
        human.Enqueue(new Order(new OrderId(1), unit.Id, 0, OrderKind.Hold, RiskLevel.Low), executeSimTick: 99);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);

        var verdict = orchestrator.TryAssignAgentController(unit, agent, isFriendly: true, simTime: 3.0);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.DenialReason, Does.Contain(DelegationOrchestrator.PendingHumanOrdersCause));
        Assert.That(unit.Slot.Active, Is.SameAs(human));
        Assert.That(human.PendingOrderCount, Is.EqualTo(1));
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);

        Assert.That(human.DrainIssuedOrders(99), Has.Count.EqualTo(1));
        var after = orchestrator.TryAssignAgentController(unit, agent, isFriendly: true, simTime: 4.0);
        Assert.That(after.Allowed, Is.True, after.DenialReason);
        Assert.That(unit.Slot.Active, Is.SameAs(agent));
    }

    [Test]
    public void TryAssignAgentController_denies_when_agent_already_active_without_mutation()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var unit = RegisterHumanUnit(orchestrator, "u1");
        var first = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);
        Assume.That(orchestrator.TryAssignAgentController(unit, first, isFriendly: true).Allowed, Is.True);
        var second = orchestrator.CreateAgentFromPreset(new AgentId("a2"), Cautious, AutonomyLevel.SemiAutonomous);

        var verdict = orchestrator.TryAssignAgentController(unit, second, isFriendly: true);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.DenialReason, Does.Contain("already"));
        Assert.That(unit.Slot.Active, Is.SameAs(first));
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(1));
    }

    [Test]
    public void TryAssignAgentController_denies_unregistered_target_and_replay_viewer()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var stray = new UnitTarget(new TargetId("stray"));
        stray.Slot.SetActive(new HumanController());
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);

        Assert.That(orchestrator.TryAssignAgentController(stray, agent, isFriendly: true).Allowed, Is.False);

        var unit = RegisterHumanUnit(orchestrator, "u1");
        orchestrator.AttachReplayViewer = true;
        var viewerVerdict = orchestrator.TryAssignAgentController(unit, agent, isFriendly: true);

        Assert.That(viewerVerdict.Allowed, Is.False);
        Assert.That(unit.Slot.Active, Is.InstanceOf<HumanController>());
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Is.Empty);
    }

    [Test]
    public void TryAssignAgentController_denies_while_unit_is_under_direct_control_with_suspended_agent()
    {
        var orchestrator = new DelegationOrchestrator(42);
        var unit = new UnitTarget(new TargetId("u1"));
        orchestrator.Register(unit);
        var original = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);
        orchestrator.AssignAgentToTarget(original, unit, EffectivePolicy.DefaultFree);
        Assume.That(orchestrator.TryTakeDirectControl(unit, simTime: 1.0), Is.True);
        var changesBefore = orchestrator.DecisionLog.ControllerChanges.Count;
        var replacement = orchestrator.CreateAgentFromPreset(new AgentId("a2"), Cautious, AutonomyLevel.SemiAutonomous);

        var verdict = orchestrator.TryAssignAgentController(unit, replacement, isFriendly: true);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(unit.Slot.SuspendedAgent, Is.SameAs(original));
        Assert.That(orchestrator.DecisionLog.ControllerChanges, Has.Count.EqualTo(changesBefore));
    }

    [Test]
    public void TryRebriefAgent_succeeds_under_tieredRebrief_at_semi_autonomous_where_rebind_is_denied()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.TieredRebrief);
        var unit = RegisterHumanUnit(orchestrator, "u1");
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);
        Assume.That(orchestrator.TryAssignAgentController(unit, agent, isFriendly: true).Allowed, Is.True);

        var legacy = orchestrator.TryRebindAgentTraits(agent, Cautious.Traits);
        Assert.That(legacy.Allowed, Is.False, "DEL-02 deny-only rebind path must still deny at Semi-Autonomous+.");
        Assert.That(legacy.DenialReason, Does.Contain("Rebrief Agent required"));
        Assert.That(agent.Traits, Is.EqualTo(Aggressive.Traits));

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious, simTime: 5.0);

        Assert.That(verdict.Allowed, Is.True, verdict.DenialReason);
        Assert.That(agent.Traits, Is.EqualTo(Cautious.Traits));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Cautious.Name));
        var update = orchestrator.DecisionLog.PolicyUpdates.Single(u => u.Field == DelegationOrchestrator.RebriefPolicyField);
        Assert.That(update.PreviousValue, Is.EqualTo(Aggressive.Name));
        Assert.That(update.NewValue, Is.EqualTo(Cautious.Name));
        Assert.That(update.SimTime, Is.EqualTo(5.0));
        Assert.That(update.SimTick, Is.EqualTo(5UL));
        Assert.That(update.PolicySnapshotId, Is.EqualTo(agent.PolicySnapshotId));
    }

    [Test]
    public void TryRebriefAgent_denied_under_planningOnly_while_executing_without_mutation_or_log()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.PlanningOnly);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);
        var updatesBefore = orchestrator.DecisionLog.PolicyUpdates.Count;

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious, simTime: 2.0);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(verdict.DenialReason, Is.EqualTo("Personality locked after Begin Execution."));
        Assert.That(agent.Traits, Is.EqualTo(Aggressive.Traits));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive.Name));
        Assert.That(orchestrator.DecisionLog.PolicyUpdates, Has.Count.EqualTo(updatesBefore));
    }

    [Test]
    public void TryRebriefAgent_applies_the_new_preset_attention_budget()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.Anytime);
        var swarm = PersonalityCatalog.All.Single(p => p.Name == "SwarmCoordinator");
        var ew = PersonalityCatalog.All.Single(p => p.Name == "EwSpecialist");
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);
        Assert.That(agent.AttentionBudget, Is.EqualTo(PersonalityCatalog.ResolveAttentionBudget(Aggressive)));

        var toSwarm = orchestrator.TryRebriefAgent(agent, swarm, simTime: 1.0);

        Assert.That(toSwarm.Allowed, Is.True, toSwarm.DenialReason);
        Assert.That(agent.AttentionBudget, Is.EqualTo(PersonalityCatalog.ResolveAttentionBudget(swarm)));
        Assert.That(agent.AttentionBudget, Is.EqualTo(PersonalityCatalog.DefaultAttentionBudget * 1.25));

        var toEw = orchestrator.TryRebriefAgent(agent, ew, simTime: 2.0);

        Assert.That(toEw.Allowed, Is.True, toEw.DenialReason);
        Assert.That(agent.AttentionBudget, Is.EqualTo(PersonalityCatalog.ResolveAttentionBudget(ew)));
        Assert.That(agent.AttentionBudget, Is.EqualTo(PersonalityCatalog.DefaultAttentionBudget * 0.9));
    }

    [Test]
    public void TryRebriefAgent_denied_leaves_attention_budget_unchanged()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.PlanningOnly);
        var swarm = PersonalityCatalog.All.Single(p => p.Name == "SwarmCoordinator");
        var ew = PersonalityCatalog.All.Single(p => p.Name == "EwSpecialist");
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), swarm, AutonomyLevel.FullAutonomous);
        var budget = agent.AttentionBudget;

        var verdict = orchestrator.TryRebriefAgent(agent, ew, simTime: 2.0);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(agent.AttentionBudget, Is.EqualTo(budget));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(swarm.Name));
    }

    [Test]
    public void TryRebriefAgent_reregisters_hindsight_personality_after_success()
    {
        var orchestrator = ExecutingWithHindsight(PersonalityEditPolicy.Anytime);
        var unit = RegisterHumanUnit(orchestrator, "u1");
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.SemiAutonomous);
        Assume.That(orchestrator.TryAssignAgentController(unit, agent, isFriendly: true).Allowed, Is.True);
        var hook = (HindsightOrderLogHook)orchestrator.Hindsight!.OrderLogHook;
        Assert.That(hook.TryGetRegisteredPersonality(agent.Id, out var assigned), Is.True);
        Assert.That(assigned, Is.EqualTo(Aggressive.Name));

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious, simTime: 5.0);

        Assert.That(verdict.Allowed, Is.True, verdict.DenialReason);
        Assert.That(hook.TryGetRegisteredPersonality(agent.Id, out var rebriefed), Is.True);
        Assert.That(rebriefed, Is.EqualTo(Cautious.Name));
    }

    [Test]
    public void TryRebriefAgent_denied_does_not_reregister_hindsight_personality()
    {
        var orchestrator = ExecutingWithHindsight(PersonalityEditPolicy.PlanningOnly);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);
        var hook = (HindsightOrderLogHook)orchestrator.Hindsight!.OrderLogHook;
        hook.RegisterAgent(agent.Id, Aggressive.Name);

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious, simTime: 2.0);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(hook.TryGetRegisteredPersonality(agent.Id, out var slug), Is.True);
        Assert.That(slug, Is.EqualTo(Aggressive.Name));
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Aggressive.Name));
    }

    [Test]
    public void TryRebriefAgent_succeeds_when_hindsight_is_null()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.Anytime);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);
        Assert.That(orchestrator.Hindsight, Is.Null);

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious, simTime: 1.0);

        Assert.That(verdict.Allowed, Is.True, verdict.DenialReason);
        Assert.That(agent.PersonalitySlug, Is.EqualTo(Cautious.Name));
        Assert.That(orchestrator.Hindsight, Is.Null);
    }

    [Test]
    public void TryRebriefAgent_denies_same_preset_as_no_change()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.Anytime);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);

        var verdict = orchestrator.TryRebriefAgent(agent, Aggressive);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(orchestrator.DecisionLog.PolicyUpdates, Is.Empty);
    }

    [Test]
    public void TryRebriefAgent_denied_while_replay_viewer_attached()
    {
        var orchestrator = ExecutingWithPolicy(PersonalityEditPolicy.Anytime);
        var agent = orchestrator.CreateAgentFromPreset(new AgentId("a1"), Aggressive, AutonomyLevel.FullAutonomous);
        orchestrator.AttachReplayViewer = true;

        var verdict = orchestrator.TryRebriefAgent(agent, Cautious);

        Assert.That(verdict.Allowed, Is.False);
        Assert.That(agent.Traits, Is.EqualTo(Aggressive.Traits));
    }

    private static DelegationOrchestrator ExecutingWithHindsight(PersonalityEditPolicy editPolicy)
    {
        var orchestrator = new DelegationOrchestrator(
            42,
            policyEvaluator: null,
            hindsight: new HindsightOptions
            {
                Enabled = true,
                BaseUrl = "http://127.0.0.1:9",
                RetainAgentDecisions = true,
                FinalizeAarBank = false,
                FinalizeCampaignExperience = false,
            })
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                personalityEditPolicy: editPolicy),
        };
        orchestrator.BeginExecution();
        return orchestrator;
    }

    private static DelegationOrchestrator ExecutingWithPolicy(PersonalityEditPolicy editPolicy)
    {
        var orchestrator = new DelegationOrchestrator(42)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                personalityEditPolicy: editPolicy),
        };
        orchestrator.BeginExecution();
        return orchestrator;
    }

    private static UnitTarget RegisterHumanUnit(DelegationOrchestrator orchestrator, string id)
    {
        var unit = new UnitTarget(new TargetId(id));
        unit.Slot.SetActive(new HumanController());
        orchestrator.Register(unit);
        return unit;
    }
}
