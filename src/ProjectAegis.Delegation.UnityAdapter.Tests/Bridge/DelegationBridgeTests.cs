namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

[TestFixture]
public sealed class DelegationBridgeTests
{
    [Test]
    public void Tick_is_no_op_while_planning()
    {
        var bridge = new DelegationBridge(42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        var sink = new RecordingSink();
        var result = bridge.Tick(
            new StubSnapshot(1, 2, 0, new Dictionary<TargetId, bool>()),
            sink);

        Assert.That(bridge.Phase, Is.EqualTo(SimulationPhase.Planning));
        Assert.That(result.ExecutedOrders, Is.Empty);
        Assert.That(sink.Applied, Is.Empty);
    }

    [Test]
    public void Tick_builds_observed_state_and_dispatches_orders_to_sink()
    {
        var bridge = new DelegationBridge(globalSeed: 42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        bridge.BeginExecution();

        var snapshot = new StubSnapshot(
            SimTime: 1,
            ContactCount: 2,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var sink = new RecordingSink();
        var result = bridge.Tick(snapshot, sink);

        Assert.That(result.ExecutedOrders, Is.Not.Empty);
        Assert.That(result.DispatchedToSim, Is.EqualTo(result.ExecutedOrders.Count));
        Assert.That(sink.Applied, Has.Count.EqualTo(result.DispatchedToSim));
        Assert.That(sink.Applied[0].Entity, Is.EqualTo(new EntityKey(2)));
    }

    [Test]
    public void TryEnqueueHumanOrder_only_when_human_controls_entity()
    {
        var bridge = new DelegationBridge(1);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(10), "u10");
        unit.Target.Slot.SetActive(new HumanController());

        Assert.That(
            bridge.TryEnqueueHumanOrder(new EntityKey(10), OrderKind.Hold, simTime: 0),
            Is.True);

        var sink = new RecordingSink();
        bridge.BeginExecution();
        bridge.Tick(new StubSnapshot(0, 0, 0, new Dictionary<TargetId, bool>()), sink);

        Assert.That(sink.Applied, Has.Count.EqualTo(1));
        Assert.That(sink.Applied[0].Order.Kind, Is.EqualTo(OrderKind.Hold));
    }

    [Test]
    public void Tick_with_mvp_session_logs_engagement_when_agent_fires_engage()
    {
        var bridge = new DelegationBridge(99, mvpEngagement: true);
        Assert.That(bridge.Session, Is.Not.Null);

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);
        bridge.BeginExecution();

        var sink = new RecordingSink();
        for (var t = 0; t < 5; t++)
        {
            bridge.Tick(
                new StubSnapshot(t, 2, 0, new Dictionary<TargetId, bool>()),
                sink);
        }

        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Is.Not.Empty);
    }

    [Test]
    public void Tick_without_mvp_session_skips_engagement_log()
    {
        var bridge = new DelegationBridge(99, mvpEngagement: false);
        Assert.That(bridge.Session, Is.Null);

        var unit = new UnitTarget(new TargetId("u1"));
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit, EffectivePolicy.DefaultFree);
        bridge.Orchestrator.Register(unit);
        bridge.BeginExecution();

        var sink = new RecordingSink();
        bridge.Tick(new StubSnapshot(0, 2, 0, new Dictionary<TargetId, bool>()), sink);

        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Is.Empty);
    }

    [Test]
    public void ObservedStateBuilder_includes_registered_member_alive_flags()
    {
        var bridge = new DelegationBridge(1);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var member = bridge.Registry.RegisterUnit(new EntityKey(101), "u1");
        bridge.Registry.LinkGroupMember(group.TargetId, member.TargetId);

        var snapshot = new StubSnapshot(
            5,
            3,
            1,
            new Dictionary<TargetId, bool> { [member.TargetId] = false });

        var observed = ObservedStateBuilder.Build(snapshot, bridge.Registry.CollectMemberIds());

        Assert.That(observed.SimTime, Is.EqualTo(5));
        Assert.That(observed.MemberAlive[member.TargetId], Is.False);
    }

    private sealed class StubSnapshot(
        double SimTime,
        int ContactCount,
        int ActiveEngagementCount,
        IReadOnlyDictionary<TargetId, bool> Alive) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;

        public int ContactCount { get; } = ContactCount;

        public int ActiveEngagementCount { get; } = ActiveEngagementCount;

        public bool IsMemberAlive(TargetId memberId) =>
            Alive.TryGetValue(memberId, out var alive) && alive;
    }

    private sealed class RecordingSink : IOrderSink
    {
        public List<(EntityKey Entity, Order Order)> Applied { get; } = new();

        public void ApplyOrder(EntityKey entity, in Order order) =>
            Applied.Add((entity, order));
    }

    private sealed class EngageOnlyPolicy : IPolicy
    {
        public IReadOnlyList<ScoredIntent> GenerateCandidates(PerceivedState perceived, TraitVector traits)
        {
            _ = perceived;
            _ = traits;
            return [new ScoredIntent(OrderKind.Engage, 1.0, RiskLevel.High)];
        }
    }
}
