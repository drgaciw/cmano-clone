namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

/// <summary>
/// ADR-019 / Req 20 P0 T4: additive <see cref="DelegationBridge.TryPauseAgent"/>,
/// <see cref="DelegationBridge.TryResumeAgent"/>, <see cref="DelegationBridge.TrySetAutonomyLevel"/> —
/// session intents + AgentGate stack routing without Tick / order-log mutation.
/// </summary>
[TestFixture]
public sealed class DelegationBridgePauseAutonomyTests
{
    [Test]
    public void TryPauseAgent_pushes_AgentGate_and_projects_Paused()
    {
        var bridge = CreateBridgeWithAgent(out var entity, out _);
        var stack = new PauseReasonStack();

        Assert.That(bridge.TryPauseAgent(entity, simTime: 5.0, stack, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(stack.Contains(PauseReasonIds.AgentGate), Is.True);
        Assert.That(stack.IsPaused, Is.True);
        Assert.That(bridge.IsAgentPaused("u1"), Is.True);
        Assert.That(bridge.AgentPauseIntents, Has.Count.EqualTo(1));
        Assert.That(bridge.AgentPauseIntents[0].UnitId, Is.EqualTo("u1"));

        Assert.That(bridge.TryProjectDelegationState(entity, out var projection), Is.True);
        Assert.That(projection!.Paused, Is.True);
        Assert.That(projection.Owner, Is.EqualTo(DelegationOwnerKind.Agent));

        // Session intents only — order-log fingerprint path untouched.
        Assert.That(bridge.Orchestrator.DecisionLog.ChronologicalEntries(), Is.Empty);
    }

    [Test]
    public void TryResumeAgent_pops_AgentGate_when_last_paused_unit()
    {
        var bridge = CreateBridgeWithAgent(out var entity, out _);
        var stack = new PauseReasonStack();
        Assert.That(bridge.TryPauseAgent(entity, 1.0, stack, out _), Is.True);

        Assert.That(bridge.TryResumeAgent(entity, 2.0, stack, out var reason), Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(stack.Contains(PauseReasonIds.AgentGate), Is.False);
        Assert.That(stack.IsPaused, Is.False);
        Assert.That(bridge.IsAgentPaused("u1"), Is.False);
        Assert.That(bridge.AgentResumeIntents, Has.Count.EqualTo(1));
        Assert.That(bridge.TryProjectDelegationState(entity, out var projection), Is.True);
        Assert.That(projection!.Paused, Is.False);
    }

    [Test]
    public void TryPauseAgent_second_unit_does_not_duplicate_AgentGate()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var e1 = new EntityKey(1);
        var e2 = new EntityKey(2);
        RegisterAgent(bridge, e1, "u1");
        RegisterAgent(bridge, e2, "u2");
        var stack = new PauseReasonStack();

        Assert.That(bridge.TryPauseAgent(e1, 1.0, stack, out _), Is.True);
        Assert.That(bridge.TryPauseAgent(e2, 1.5, stack, out _), Is.True);
        Assert.That(stack.Reasons.Count(r => r == PauseReasonIds.AgentGate), Is.EqualTo(1));

        Assert.That(bridge.TryResumeAgent(e1, 2.0, stack, out _), Is.True);
        Assert.That(stack.Contains(PauseReasonIds.AgentGate), Is.True, "second unit still paused");

        Assert.That(bridge.TryResumeAgent(e2, 2.5, stack, out _), Is.True);
        Assert.That(stack.Contains(PauseReasonIds.AgentGate), Is.False);
    }

    [Test]
    public void TryPauseAgent_rejects_human_double_pause_and_replay()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var human = bridge.Registry.RegisterUnit(new EntityKey(10), "human-1");
        human.Target.Slot.SetActive(new HumanController());
        var stack = new PauseReasonStack();

        Assert.That(bridge.TryPauseAgent(new EntityKey(10), 1.0, stack, out var noAgent), Is.False);
        Assert.That(noAgent, Is.EqualTo("no-active-agent"));

        var agentBridge = CreateBridgeWithAgent(out var entity, out _);
        Assert.That(agentBridge.TryPauseAgent(entity, 1.0, stack, out _), Is.True);
        Assert.That(agentBridge.TryPauseAgent(entity, 1.0, stack, out var already), Is.False);
        Assert.That(already, Is.EqualTo("already-paused"));

        agentBridge.AttachReplayViewer = true;
        Assert.That(agentBridge.TryResumeAgent(entity, 2.0, stack, out var replay), Is.False);
        Assert.That(replay, Is.EqualTo("replay"));
    }

    [Test]
    public void TrySetAutonomyLevel_updates_agent_and_logs_session_intent()
    {
        var bridge = CreateBridgeWithAgent(out var entity, out var agent);
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.FullAutonomous));

        Assert.That(
            bridge.TrySetAutonomyLevel(entity, AutonomyLevel.Assisted, simTime: 3.0, out var reason),
            Is.True);
        Assert.That(reason, Is.Null);
        Assert.That(agent.Autonomy, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(bridge.AutonomyChangeIntents, Has.Count.EqualTo(1));
        Assert.That(bridge.AutonomyChangeIntents[0].AutonomyLevel, Is.EqualTo(AutonomyLevel.Assisted));

        Assert.That(
            bridge.TrySetAutonomyLevel(entity, AutonomyLevel.Assisted, simTime: 4.0, out var unchanged),
            Is.False);
        Assert.That(unchanged, Is.EqualTo("unchanged"));

        Assert.That(bridge.TryProjectDelegationState(entity, out var projection), Is.True);
        Assert.That(projection!.AutonomyLevel, Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(bridge.Orchestrator.DecisionLog.ChronologicalEntries(), Is.Empty);
    }

    [Test]
    public void ProjectAllDelegationStates_and_OobFilter_roundtrip()
    {
        var bridge = new DelegationBridge(9, mvpEngagement: false);
        var human = bridge.Registry.RegisterUnit(new EntityKey(1), "h1");
        human.Target.Slot.SetActive(new HumanController());
        RegisterAgent(bridge, new EntityKey(2), "a1");

        var all = bridge.ProjectAllDelegationStates();
        Assert.That(all, Has.Count.EqualTo(2));

        var agents = DelegationOobFilter.Filter(all, DelegationOobFilterMode.AgentOnly);
        Assert.That(agents.Select(p => p.UnitId).ToArray(), Is.EqualTo(new[] { "a1" }));

        var humans = DelegationOobFilter.Filter(all, DelegationOobFilterMode.HumanOnly);
        Assert.That(humans.Select(p => p.UnitId).ToArray(), Is.EqualTo(new[] { "h1" }));
    }

    [Test]
    public void Pause_resume_autonomy_do_not_append_order_log_entries()
    {
        var bridge = CreateBridgeWithAgent(out var entity, out _);
        var stack = new PauseReasonStack();
        var before = bridge.Orchestrator.DecisionLog.ComputeFingerprint();

        bridge.TryPauseAgent(entity, 1.0, stack, out _);
        bridge.TrySetAutonomyLevel(entity, AutonomyLevel.SemiAutonomous, 1.5, out _);
        bridge.TryResumeAgent(entity, 2.0, stack, out _);

        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Is.EqualTo(before),
            "ADR-019 session intents must not alter order-log fingerprint (Baltic hash path)");
    }

    private static DelegationBridge CreateBridgeWithAgent(out EntityKey entity, out AgentController agent)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        entity = new EntityKey(1);
        agent = RegisterAgent(bridge, entity, "u1");
        return bridge;
    }

    private static AgentController RegisterAgent(DelegationBridge bridge, EntityKey entity, string unitId)
    {
        var binding = bridge.Registry.RegisterUnit(entity, unitId);
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId($"agent-{unitId}"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous);
        binding.Target.Slot.SetActive(agent);
        return agent;
    }
}
