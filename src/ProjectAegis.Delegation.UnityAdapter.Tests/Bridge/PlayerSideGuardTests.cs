using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

/// <summary>UAT D22 re-fix: side comes from the side registry, never from agent naming.</summary>
[TestFixture]
public sealed class PlayerSideGuardTests
{
    private static AgentController Agent(string id) => new(
        new AgentId(id),
        PersonalityCatalog.All[0].Traits,
        AutonomyLevel.FullAutonomous,
        new SeededRng(1, 0),
        new PatrolCandidateEngagePolicy(EngagePrimaryMode.BlueForce),
        attentionBudget: 1);

    private sealed class Snap(params string[] alive) : ISimWorldSnapshot
    {
        public double SimTime => 1;
        public int ContactCount => 2;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => new("hostile-1");
        public bool HasFireControlTrackOnPrimaryContact => true;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => alive.Contains(memberId.Value);
    }

    [Test]
    public void Player_side_unit_delegated_to_arbitrary_agent_id_stays_commandable()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        unit.Target.Slot.SetActive(Agent("agent-7"));
        bridge.BeginExecution();
        var snapshot = new Snap("u1");

        Assert.That(PlayerSideGuard.IsOpposingAiUnit(bridge, new EntityKey(1)), Is.False);
        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, new EntityKey(1)), Is.True);
        var menu = PlayerSideGuard.GetAttackMenuOptions(bridge, "u1", snapshot);
        Assert.That(menu.Any(o => o.Enabled), Is.True);
        Assert.That(menu.Any(o => o.DisabledReason == "NOT_PLAYER_CONTROLLED"), Is.False);
    }

    [Test]
    public void Red_unit_is_refused_even_when_agent_id_looks_friendly()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var hostile = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        hostile.Target.Slot.SetActive(Agent("friendly-0"));
        bridge.BeginExecution();

        Assert.That(PlayerSideGuard.IsOpposingAiUnit(bridge, new EntityKey(2)), Is.True);
        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, new EntityKey(2)), Is.False);
        Assert.That(
            PlayerSideGuard.TryEnqueueAttackOption(bridge, new EntityKey(2), "fire-single", new Snap("hostile-1"), out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo("NOT_PLAYER_CONTROLLED"));
    }

    [Test]
    public void Configured_side_membership_controls_access_for_custom_ai_units()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "custom-patrol");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "custom-interceptor");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.AgentVsAgent, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: PersonalityCatalog.All[0].Traits);

        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, friendly.Entity), Is.True);
        Assert.That(PlayerSideGuard.IsOpposingAiUnit(bridge, opposing.Entity), Is.True);
        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, opposing.Entity), Is.False);
        Assert.That(
            PlayerSideGuard.TryEnqueueAttackOption(
                bridge, opposing.Entity, "fire-single", new Snap("custom-interceptor"), out var reason),
            Is.False);
        Assert.That(reason, Is.EqualTo(C2PlayerCommandBridge.ReasonNotPlayerControlled));
    }

    [Test]
    public void Opposing_player_cannot_command_blue_ai_and_defaults_to_their_own_unit()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "blue-patrol");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: false),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: PersonalityCatalog.All[0].Traits);
        bridge.BeginExecution();
        var snapshot = new Snap(friendly.TargetId.Value, opposing.TargetId.Value);

        Assert.That(PlayerSideGuard.IsOpposingAiUnit(bridge, friendly.Entity), Is.True);
        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, friendly.Entity), Is.False);
        Assert.That(PlayerSideGuard.IsPlayerControlledUnit(bridge, opposing.Entity), Is.True);
        var menu = PlayerSideGuard.GetAttackMenuOptions(bridge, friendly.TargetId.Value, snapshot);
        Assert.That(menu, Is.Not.Empty);
        Assert.That(menu.All(option => !option.Enabled
            && option.DisabledReason == C2PlayerCommandBridge.ReasonNotPlayerControlled), Is.True);
        var oob = OobTreeBridge.Build(snapshot, bridge.Registry);
        Assert.That(C2SelectionResolver.ResolveDefaultFriendlyUnit(oob), Is.EqualTo(opposing.TargetId.Value));
    }

    [Test]
    public void Default_selection_never_picks_a_red_unit()
    {
        var oob = new[] { new OobTreeEntry("hostile-1", true), new OobTreeEntry("u1", true) };

        Assert.That(C2SelectionResolver.ResolveDefaultFriendlyUnit(oob), Is.EqualTo("u1"));
    }
}
