namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
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

        var harness = new PlayModeHarness(contactCount: 2);
        for (var frame = 0; frame < 30; frame++)
        {
            harness.AdvanceTime(1.0 / 60.0);
            bridge.Tick(harness, harness);
        }

        Assert.That(harness.AppliedOrders, Is.Not.Empty);
        Assert.That(harness.SimTime, Is.EqualTo(30.0 / 60.0).Within(1e-6));
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
