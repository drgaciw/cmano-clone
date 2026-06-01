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
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// Headless equivalent of <c>SimplePlayModeSimHost</c> (multi-tick snapshot + sink loop).
/// </summary>
[TestFixture]
public sealed class PlayModeSmokeHarnessTests
{
    [Test]
    public void Multi_tick_loop_applies_orders_like_play_mode_host()
    {
        var bridge = new DelegationBridge(42);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "friendly-1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "opposing-1");

        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: [friendly.Target],
            opposing: [opposing.Target],
            defaultTraits: ProjectAegis.Delegation.Traits.PersonalityCatalog.All[0].Traits);

        bridge.BeginExecution();

        var harness = new PlayModeHarness(contactCount: 2);
        for (var frame = 0; frame < 30; frame++)
        {
            harness.AdvanceTime(1.0 / 60.0);
            bridge.Tick(harness, harness);
        }

        Assert.That(harness.AppliedOrders, Is.Not.Empty);
        Assert.That(harness.SimTime, Is.EqualTo(30.0 / 60.0).Within(1e-6));
    }

    [Test]
    public void Engage_scenario_multi_tick_writes_stable_engagement_log()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: true, scenarioPolicyId: "baltic-patrol");
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

        var harness = new PlayModeHarness(contactCount: 2);
        for (var frame = 0; frame < 5; frame++)
        {
            harness.AdvanceTime(1.0);
            bridge.Tick(harness, harness);
        }

        Assert.That(bridge.Orchestrator.DecisionLog.Engagements, Is.Not.Empty);
        Assert.That(
            bridge.Orchestrator.DecisionLog.Engagements.Any(e =>
                e.Launched && e.AbortReasonCode == EngagementAbortReasonCodes.Launched),
            Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.ComputeFingerprint(), Does.Contain("Engagement|"));
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

    private sealed class PlayModeHarness : ISimWorldSnapshot, IOrderSink
    {
        private readonly int _contactCount;
        private double _simTime;
        private readonly List<(EntityKey Entity, Order Order)> _applied = new();

        public PlayModeHarness(int contactCount) => _contactCount = contactCount;

        public double SimTime => _simTime;

        public int ContactCount => _contactCount;

        public int ActiveEngagementCount => 0;

        public IReadOnlyList<(EntityKey Entity, Order Order)> AppliedOrders => _applied;

        public void AdvanceTime(double delta) => _simTime += delta;

        public bool IsMemberAlive(TargetId memberId) => true;

        public void ApplyOrder(EntityKey entity, in Order order) =>
            _applied.Add((entity, order));
    }
}
