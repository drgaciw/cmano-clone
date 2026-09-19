using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Policy;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class FriendlyMeshUnitIdsProjectionTests
{
    [Test]
    public void Collect_prefers_marked_friendly_mesh_members_over_registry_sweep()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        bridge.ConfigureSimulationMode(
            new SimulationModeProfile(SimulationModeKind.Mixed, PlayerControlsFriendlySide: true),
            friendly: new[] { friendly.Target },
            opposing: new[] { opposing.Target },
            defaultTraits: PersonalityCatalog.All[0].Traits);

        var snapshot = new FriendlyMeshStubSnapshot(
            SimTime: 1,
            ContactCount: 0,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var ids = FriendlyMeshUnitIdsProjection.Collect(bridge.Registry, snapshot);

        Assert.That(ids, Is.EquivalentTo(new[] { "u1" }));
        Assert.That(ids, Does.Not.Contain("hostile-1"));
    }

    [Test]
    public void Collect_infers_friendly_mesh_from_human_controller_when_unmarked()
    {
        var bridge = new DelegationBridge(1, mvpEngagement: false);
        var friendly = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var opposing = bridge.Registry.RegisterUnit(new EntityKey(2), "hostile-1");
        friendly.Target.Slot.SetActive(new HumanController());
        opposing.Target.Slot.SetActive(new AgentController(
            new AgentId("opp-0"),
            PersonalityCatalog.All[0].Traits,
            AutonomyLevel.FullAutonomous,
            new SeededRng(1, 0),
            new PatrolCandidateEngagePolicy(EngagePrimaryMode.BlueForce),
            attentionBudget: 1));

        var snapshot = new FriendlyMeshStubSnapshot(
            SimTime: 1,
            ContactCount: 0,
            ActiveEngagementCount: 0,
            Alive: new Dictionary<TargetId, bool>
            {
                [friendly.TargetId] = true,
                [opposing.TargetId] = true,
            });

        var ids = FriendlyMeshUnitIdsProjection.Collect(bridge.Registry, snapshot);

        Assert.That(ids, Is.EquivalentTo(new[] { "u1" }));
    }

    private sealed class FriendlyMeshStubSnapshot(
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
