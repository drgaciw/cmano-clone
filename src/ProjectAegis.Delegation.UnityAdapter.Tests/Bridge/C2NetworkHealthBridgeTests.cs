using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class C2NetworkHealthBridgeTests
{
    [Test]
    public void Build_projects_healthy_mesh_for_marked_friendly_units_only()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var friendlyTwo = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(3), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: new[] { friendly.Target, friendlyTwo.Target },
            opposing: new[] { opposing.Target },
            defaultTraits: PersonalityCatalog.All[0].Traits);

        var snapshot = new NetworkHealthStubSnapshot(
            SimTime: 1,
            ContactCount: 0,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [friendlyTwo.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var health = C2NetworkHealthBridge.Build(
            bridge.Orchestrator.DecisionLog,
            bridge.Registry,
            snapshot,
            InMemoryCatalogReader.BalticPatrolFixture(),
            currentSimTick: 1);

        Assert.That(health.NetworkHealth, Is.EqualTo(C2NetworkHealthLevel.Healthy));
        Assert.That(health.Links, Is.Not.Empty);
        Assert.That(
            health.Links.Select(link => link.FromUnitId).Concat(health.Links.Select(link => link.ToUnitId)),
            Is.All.Not.EqualTo("hostile-1"));
    }

    [Test]
    public void Build_excludes_alive_opposing_unit_even_when_side_blind_oob_would_include_it()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: new[] { friendly.Target },
            opposing: new[] { opposing.Target },
            defaultTraits: PersonalityCatalog.All[0].Traits);

        var snapshot = new NetworkHealthStubSnapshot(
            SimTime: 1,
            ContactCount: 0,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var health = C2NetworkHealthBridge.Build(
            bridge.Orchestrator.DecisionLog,
            bridge.Registry,
            snapshot,
            InMemoryCatalogReader.BalticPatrolFixture(),
            currentSimTick: 1);

        Assert.That(
            health.Links.Select(link => link.FromUnitId).Concat(health.Links.Select(link => link.ToUnitId)),
            Is.All.Not.EqualTo("hostile-1"));
    }

    [Test]
    public void Build_null_log_throws()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var snapshot = new NetworkHealthStubSnapshot(0, 0, 0, new Dictionary<TargetId, bool>());
        try
        {
            C2NetworkHealthBridge.Build(null!, bridge.Registry, snapshot, null, 0);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("log"));
        }
    }

    private sealed class NetworkHealthStubSnapshot(
        double SimTime,
        int ContactCount,
        int ActiveEngagementCount,
        IReadOnlyDictionary<TargetId, bool> Alive) : ISimWorldSnapshot
    {
        public double SimTime { get; } = SimTime;

        public int ContactCount { get; } = ContactCount;

        public int ActiveEngagementCount { get; } = ActiveEngagementCount;

        public TargetId? PrimaryHostileContactId => null;

        public bool HasFireControlTrackOnPrimaryContact => false;

        public bool ObserverRadarEmconActive => true;

        public bool IsMemberAlive(TargetId memberId) =>
            Alive.TryGetValue(memberId, out var alive) && alive;
    }
}
