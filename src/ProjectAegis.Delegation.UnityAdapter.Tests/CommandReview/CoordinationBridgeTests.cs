namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.MissionIntent;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;
using NUnit.Framework;

[TestFixture]
public sealed class CoordinationBridgeTests
{
    [Test]
    public void Build_without_authored_facts_reports_actual_members_and_unknown_roles_and_coverage()
    {
        var (bridge, snapshot) = CreateGroup();

        var frame = CoordinationBridge.Build(bridge, snapshot);

        Assert.That(frame.Groups, Has.Count.EqualTo(1));
        Assert.That(frame.Groups[0].Coordination.Members, Is.EqualTo(new[] { "u1", "u2" }));
        Assert.That(frame.Groups[0].Effects, Has.Count.EqualTo(2));
        Assert.That(frame.Groups[0].Effects.All(e => e.Role is null), Is.True);
        Assert.That(frame.Groups[0].Effects.All(e => e.Coverage.Status == CoverageStatus.Unknown), Is.True);
        Assert.That(frame.Groups[0].Effects.All(e => e.Coverage.Geometry is null), Is.True);
    }

    [Test]
    public void Build_composes_package_roles_intent_and_explicit_coverage_facts()
    {
        var (bridge, snapshot) = CreateGroup();
        var facts = new TestFacts(
            [Package()],
            [new CoverageFact(
                "sensor",
                "sector-north",
                "North sector track custody",
                new CoverageAreaGeometry(
                    [new(0.1, 0.1), new(0.8, 0.1), new(0.5, 0.7)],
                    "scenario:coverage/sector-north"))],
            [new MissionIntentInput("g1", "", MissionIntentCode.Hold, [MissionIntentConstraintCode.NoStrike])]);

        var frame = CoordinationBridge.Build(bridge, snapshot, facts);
        var group = frame.Groups.Single();

        Assert.That(group.Coordination.AssignedPackageId, Is.EqualTo("pkg-1"));
        Assert.That(group.Intent.IntentCode, Is.EqualTo(MissionIntentCode.Hold));
        Assert.That(group.Effects.Single(e => e.UnitId == "u1").Role, Is.EqualTo(C2NodeRole.Sensor));
        Assert.That(group.Effects.Single(e => e.UnitId == "u1").Coverage.Status, Is.EqualTo(CoverageStatus.Covered));
        Assert.That(group.Effects.Single(e => e.UnitId == "u1").Coverage.Geometry?.Boundary, Has.Count.EqualTo(3));
        Assert.That(group.Effects.Single(e => e.UnitId == "u1").Coverage.Geometry?.SourceRef, Is.EqualTo("scenario:coverage/sector-north"));
        Assert.That(group.Effects.Single(e => e.UnitId == "u2").Coverage.Status, Is.EqualTo(CoverageStatus.Unknown));
    }

    [Test]
    public void Build_lost_and_split_elements_surface_named_gaps_without_dropping_responsibility()
    {
        var (bridge, snapshot) = CreateGroup(alive: new Dictionary<string, bool> { ["u1"] = true, ["u2"] = false });
        Assert.That(bridge.TryTakeDirectControl(new EntityKey(2), 1), Is.True);
        var facts = new TestFacts(
            [Package()],
            [new CoverageFact("shooter", "fires", "Package fires")],
            []);

        var frame = CoordinationBridge.Build(bridge, snapshot, facts);
        var group = frame.Groups.Single();

        Assert.That(group.Coordination.GapCode, Is.EqualTo("SPLIT"));
        Assert.That(group.Gaps.Select(g => g.Code), Does.Contain("DETACHED_SHOOTER"));
        Assert.That(group.Effects.Single(e => e.UnitId == "u2").State, Is.EqualTo(CoordinationEffectState.Detached));
        Assert.That(group.Effects.Single(e => e.UnitId == "u2").Coverage.Status, Is.EqualTo(CoverageStatus.Gap));
    }

    [Test]
    public void Build_shared_platform_responsibilities_surface_scarcity_conflict()
    {
        var (bridge, snapshot) = CreateGroup();
        var package = new PackageDefinition(
            "pkg-1",
            "Package One",
            [
                new PackageElementDefinition("sensor", "u1", C2NodeRole.Sensor, "organic-radar"),
                new PackageElementDefinition("c2", "u1", C2NodeRole.C2, "organic-c2"),
                new PackageElementDefinition("shooter", "u2", C2NodeRole.Shooter, "package-engage"),
            ]);

        var group = CoordinationBridge.Build(bridge, snapshot, new TestFacts([package], [], [])).Groups.Single();

        Assert.That(group.Gaps.Select(g => g.Code), Does.Contain("SCARCE_SHARED_UNIT"));
        Assert.That(group.Effects.Count(e => e.UnitId == "u1"), Is.EqualTo(2));
    }

    [Test]
    public void Build_filters_assigned_package_elements_outside_actual_group_scope()
    {
        var (bridge, snapshot) = CreateGroup();
        var package = new PackageDefinition(
            "pkg-1",
            "Package One",
            [
                new PackageElementDefinition("sensor", "u1", C2NodeRole.Sensor, "organic-radar"),
                new PackageElementDefinition("outsider", "u9", C2NodeRole.C2, "organic-c2"),
            ]);

        var group = CoordinationBridge.Build(bridge, snapshot, new TestFacts([package], [], [])).Groups.Single();

        Assert.That(group.Effects.Select(e => e.UnitId), Does.Not.Contain("u9"));
        Assert.That(group.Coordination.GapCode, Is.EqualTo("NO_C2"));
    }

    [Test]
    public void Build_unknown_role_preserves_lost_member_state()
    {
        var (bridge, snapshot) = CreateGroup(alive: new Dictionary<string, bool> { ["u1"] = true, ["u2"] = false });

        var group = CoordinationBridge.Build(bridge, snapshot).Groups.Single();

        Assert.That(group.Effects.Single(e => e.UnitId == "u2").State, Is.EqualTo(CoordinationEffectState.Lost));
        Assert.That(group.Gaps.Select(g => g.Code), Does.Contain("LOST_MEMBER"));
        Assert.That(group.Coordination.GapCode, Is.EqualTo("UNKNOWN"));
    }

    [Test]
    public void Build_last_known_c2_and_coverage_remain_unknown_under_degraded_comms()
    {
        var (bridge, snapshot) = CreateGroup();
        bridge.Orchestrator.DecisionLog.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1, 1, "g1", CommsState.Nominal, CommsState.Degraded, "test"));
        var package = new PackageDefinition(
            "pkg-1", "Package One",
            [new PackageElementDefinition("c2", "u1", C2NodeRole.C2, "package-c2")]);
        var facts = new TestFacts(
            [package],
            [new CoverageFact("c2", "sector", "Sector", new CoverageAreaGeometry(
                [new(0, 0), new(1, 0), new(0, 1)], "fixture:last-known"))],
            []);

        var group = CoordinationBridge.Build(bridge, snapshot, facts).Groups.Single();

        Assert.That(group.Effects.Single(e => e.ElementId == "c2").State, Is.EqualTo(CoordinationEffectState.UnknownAvailability));
        Assert.That(group.Effects.Single(e => e.ElementId == "c2").Coverage.Status, Is.EqualTo(CoverageStatus.Unknown));
        Assert.That(group.Effects.Single(e => e.ElementId == "c2").Coverage.Geometry, Is.Null);
        Assert.That(group.Coordination.GapCode, Is.EqualTo("NO_C2"));
    }

    [Test]
    public void Build_does_not_fold_damage_from_after_snapshot_time()
    {
        var (bridge, snapshot) = CreateGroup();
        bridge.Orchestrator.DecisionLog.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0, 11, 11, new TargetId("u1"), 100, 0, "future"));
        var package = new PackageDefinition(
            "pkg-1", "Package One",
            [new PackageElementDefinition("c2", "u1", C2NodeRole.C2, "organic-c2")]);

        var group = CoordinationBridge.Build(bridge, snapshot, new TestFacts([package], [], [])).Groups.Single();

        Assert.That(group.Effects.Single(e => e.UnitId == "u1").State, Is.EqualTo(CoordinationEffectState.Available));
    }

    private static (DelegationBridge Bridge, Snapshot Snapshot) CreateGroup(
        IReadOnlyDictionary<string, bool>? alive = null)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var group = bridge.Registry.RegisterGroup(new EntityKey(100), "g1");
        var u1 = bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var u2 = bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        bridge.Registry.LinkGroupMember(group.TargetId, u1.TargetId);
        bridge.Registry.LinkGroupMember(group.TargetId, u2.TargetId);
        return (bridge, new Snapshot(alive ?? new Dictionary<string, bool> { ["u1"] = true, ["u2"] = true }));
    }

    private static PackageDefinition Package() => new(
        "pkg-1",
        "Package One",
        [
            new PackageElementDefinition("sensor", "u1", C2NodeRole.Sensor, "organic-radar"),
            new PackageElementDefinition("shooter", "u2", C2NodeRole.Shooter, "package-engage"),
        ]);

    private sealed class Snapshot(IReadOnlyDictionary<string, bool> alive) : ISimWorldSnapshot
    {
        public double SimTime => 2;
        public int ContactCount => 0;
        public int ActiveEngagementCount => 0;
        public TargetId? PrimaryHostileContactId => null;
        public bool HasFireControlTrackOnPrimaryContact => false;
        public bool ObserverRadarEmconActive => true;
        public bool IsMemberAlive(TargetId memberId) => alive.TryGetValue(memberId.Value, out var value) && value;
    }

    private sealed class TestFacts(
        IReadOnlyList<PackageDefinition> packages,
        IReadOnlyList<CoverageFact> coverage,
        IReadOnlyList<MissionIntentInput> intents) : ICoordinationFacts
    {
        public IReadOnlyList<PackageDefinition> Packages => packages;
        public IReadOnlyList<CoverageFact> Coverage => coverage;
        public IReadOnlyList<MissionIntentInput> Intents => intents;
        public IReadOnlyList<CoordinationGroupPackageAssignment> Assignments =>
            packages.Count == 1
                ? [new CoordinationGroupPackageAssignment("g1", packages[0].PackageId)]
                : [];
    }
}
