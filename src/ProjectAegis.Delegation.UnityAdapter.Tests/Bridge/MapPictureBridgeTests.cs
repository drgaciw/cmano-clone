namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using NUnit.Framework;

/// <summary>
/// Headless dogfood for map presentation bridge (UCA-M5 / DRG-123).
/// Proves Build is projection-only: snapshot + registry + log → IReadOnlyList<MapSymbolEntry>.
/// </summary>
[TestFixture]
public sealed class MapPictureBridgeTests
{
    [Test]
    public void Build_registered_unit_appears_as_friendly_symbol()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        var symbols = MapPictureBridge.Build(
            snapshot,
            bridge.Registry,
            bridge.Orchestrator.DecisionLog,
            layoutSeed: 7);

        Assert.That(symbols, Has.Count.EqualTo(1));
        Assert.That(symbols[0].SymbolId, Is.EqualTo("u1"));
        Assert.That(symbols[0].Affiliation, Is.EqualTo("Friendly"));
        Assert.That(symbols[0].IsDestroyed, Is.False);
    }

    [Test]
    public void Build_dead_member_marks_symbol_destroyed()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0, memberAlive: false);
        var symbols = MapPictureBridge.Build(
            snapshot,
            bridge.Registry,
            bridge.Orchestrator.DecisionLog,
            layoutSeed: 1);

        Assert.That(symbols, Has.Count.EqualTo(1));
        Assert.That(symbols[0].IsDestroyed, Is.True);
    }

    [Test]
    public void Build_returns_readonly_list_same_seed_stable_order()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        bridge.Registry.RegisterUnit(new EntityKey(2), "u2");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);

        var a = MapPictureBridge.Build(snapshot, bridge.Registry, bridge.Orchestrator.DecisionLog, 3);
        var b = MapPictureBridge.Build(snapshot, bridge.Registry, bridge.Orchestrator.DecisionLog, 3);

        Assert.That(a, Is.InstanceOf<IReadOnlyList<ProjectAegis.Delegation.Projection.MapSymbolEntry>>());
        Assert.That(a.Select(s => s.SymbolId).ToArray(), Is.EqualTo(b.Select(s => s.SymbolId).ToArray()));
        Assert.That(a, Has.Count.EqualTo(2));
    }

    [Test]
    public void Build_null_snapshot_throws()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.Throws<ArgumentNullException>((Action)(() =>
            MapPictureBridge.Build(null!, bridge.Registry, bridge.Orchestrator.DecisionLog, 0)));
    }

    [Test]
    public void Build_null_registry_throws()
    {
        var snapshot = new SimWorldSnapshotStub();
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.Throws<ArgumentNullException>((Action)(() =>
            MapPictureBridge.Build(snapshot, null!, bridge.Orchestrator.DecisionLog, 0)));
    }

    [Test]
    public void Build_null_log_throws()
    {
        var snapshot = new SimWorldSnapshotStub();
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.Throws<ArgumentNullException>((Action)(() =>
            MapPictureBridge.Build(snapshot, bridge.Registry, null!, 0)));
    }

    [Test]
    public void Build_snapshot_pose_replaces_hash_placement()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var hash = ProjectAegis.Delegation.Projection.MapPictureProjection.Place("u1", 7);
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        snapshot.Poses["u1"] = new ProjectAegis.Delegation.Core.UnitKinematicPose(
            null, null, 0.22f, 0.33f, 90f, 18f);

        var symbols = MapPictureBridge.Build(
            snapshot,
            bridge.Registry,
            bridge.Orchestrator.DecisionLog,
            layoutSeed: 7);

        Assert.That(symbols[0].HasAuthoritativePose, Is.True);
        Assert.That(symbols[0].NormalizedX, Is.EqualTo(0.22f).Within(1e-6f));
        Assert.That(symbols[0].NormalizedY, Is.EqualTo(0.33f).Within(1e-6f));
        Assert.That(Math.Abs(symbols[0].NormalizedX - hash.X), Is.GreaterThan(1e-4f));
    }

    [Test]
    public void BuildCourses_projects_snapshot_waypoints()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Registry.RegisterUnit(new EntityKey(1), "u1");
        var snapshot = new SimWorldSnapshotStub(contactCount: 0);
        snapshot.Poses["u1"] = new ProjectAegis.Delegation.Core.UnitKinematicPose(
            null, null, 0.2f, 0.2f, 45f, 22f);
        snapshot.Courses["u1"] =
        [
            new ProjectAegis.Delegation.Core.CourseWaypoint(0.4f, 0.35f),
        ];

        var courses = MapPictureBridge.BuildCourses(
            snapshot,
            bridge.Registry,
            bridge.Orchestrator.DecisionLog,
            layoutSeed: 3);

        Assert.That(courses, Has.Count.EqualTo(1));
        Assert.That(courses[0].Vertices.Count, Is.GreaterThanOrEqualTo(2));
    }
}
