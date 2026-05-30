namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

[TestFixture]
public sealed class DelegationBridgeSimSessionTests
{
    [Test]
    public void EnableMvpEngagement_resolves_engages_via_sim_session()
    {
        var bridge = new DelegationBridge(99);
        bridge.EnableMvpEngagement();

        var unit = bridge.Registry.RegisterUnit(new EntityKey(1), "opp-1");
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a1"),
            ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit.Target, EffectivePolicy.DefaultFree);
        bridge.BeginExecution();

        var snapshot = new StubSnapshot(1, 2, 0, new Dictionary<TargetId, bool> { [unit.TargetId] = true });
        var sink = new RecordingSink();
        var result = bridge.Tick(snapshot, sink);

        Assert.That(bridge.SimSession, Is.Not.Null);
        Assert.That(result.EngagementsResolved, Is.GreaterThan(0));
        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Is.Not.Empty);
        Assert.That(sink.Applied, Is.Empty, "Engage orders stay in sim session, not IOrderSink");
    }

    [Test]
    public void Without_sim_session_dispatches_all_orders_including_engage()
    {
        var bridge = new DelegationBridge(1);
        var unit = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        var agent = bridge.Orchestrator.CreateAgent(
            new AgentId("a2"),
            ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            policy: new EngageOnlyPolicy());
        bridge.Orchestrator.AssignAgentToTarget(agent, unit.Target, EffectivePolicy.DefaultFree);
        bridge.BeginExecution();

        var sink = new RecordingSink();
        bridge.Tick(
            new StubSnapshot(0, 1, 0, new Dictionary<TargetId, bool> { [unit.TargetId] = true }),
            sink);

        Assert.That(sink.Applied, Is.Not.Empty);
        Assert.That(sink.Applied[0].Order.Kind, Is.EqualTo(OrderKind.Engage));
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
}
