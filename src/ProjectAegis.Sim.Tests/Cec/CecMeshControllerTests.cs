using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Cec;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Cec;

/// <summary>DRG-102 / SWARM-B6a: CEC mesh health + composite track (SWARM-31 mesh half).</summary>
public sealed class CecMeshControllerTests
{
    private const string Blue = "blue";
    private const string Target = "hostile-1";

    private static CecNodeRegistration UsnCecNode(
        string unitId,
        double lat,
        double lon,
        bool alive = true) =>
        new(
            unitId,
            Blue,
            CecCapable: true,
            LatDeg: lat,
            LonDeg: lon,
            IsAlive: alive,
            IsSwarm: true);

    private static CecNodeRegistration NonCecNode(
        string unitId,
        double lat,
        double lon) =>
        new(
            unitId,
            Blue,
            CecCapable: false,
            LatDeg: lat,
            LonDeg: lon,
            IsAlive: true,
            IsSwarm: true);

    [Test]
    public void Non_CEC_cannot_join_mesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-1", 57.0, 20.0));
        c.Register(NonCecNode("generic-1", 57.0, 20.1));
        c.Register(NonCecNode("generic-2", 57.0, 20.2));

        c.Refresh();

        Assert.That(c.GetMeshState("generic-1"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.GetMeshState("generic-2"), Is.EqualTo(CecMeshState.OutOfMesh));
        // Lone CEC node with only non-CEC neighbors has no CEC peer.
        Assert.That(c.GetMeshState("usn-1"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.ContributeOrganic(Blue, "generic-1", Target, 0.9), Is.False);
    }

    [Test]
    public void Two_USN_CEC_nodes_in_range_are_both_InMesh()
    {
        var c = new CecMeshController();
        // Catalog exemplar ids referenced for documentation; mesh gate is CecCapable flag.
        _ = CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId;
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5)); // 0.5 deg < DefaultConnectedRangeDeg 2.0

        c.Refresh();

        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.InMesh));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.InMesh));
        Assert.That(c.MeshEventLog.Any(e => e.UnitId == "usn-a" && e.Kind == CecMeshEventKind.Join), Is.True);
        Assert.That(c.MeshEventLog.Any(e => e.UnitId == "usn-b" && e.Kind == CecMeshEventKind.Join), Is.True);
    }

    [Test]
    public void Jam_forces_OutOfMesh_without_any_Swarm_types()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh(jammed: false);
        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.InMesh));

        c.Refresh(jammed: true);

        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.OutOfMesh));
        // Independence: this test file must not import ProjectAegis.Sim.Swarm.
        Assert.That(c.MeshEventLog.Any(e => e.Kind == CecMeshEventKind.Leave), Is.True);
    }

    [Test]
    public void Range_stretch_moves_InMesh_to_Degraded_then_OutOfMesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();
        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.InMesh));

        // Stretch into degraded band only (connected 2.0 < range <= 4.0).
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 23.0); // range 3.0
        c.Refresh();
        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.Degraded));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.Degraded));

        // Stretch beyond degraded band.
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 25.0); // range 5.0
        c.Refresh();
        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.OutOfMesh));
    }

    [Test]
    public void Composite_track_forms_when_two_nodes_contribute_same_target()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();

        Assert.That(c.ContributeOrganic(Blue, "usn-a", Target, 0.8), Is.True);
        Assert.That(c.ContributeOrganic(Blue, "usn-b", Target, 0.7), Is.True);

        var tracks = c.TryGetCompositeTracks(Blue);
        AssertEx.Single(tracks);
        var track = tracks[0];
        Assert.That(track.TargetId, Is.EqualTo(Target));
        Assert.That(track.SideId, Is.EqualTo(Blue));
        Assert.That(track.ContributorCount, Is.EqualTo(2));
        Assert.That(track.TrackId, Is.EqualTo("cec-blue-hostile-1"));
        Assert.That(track.PrimaryContributorUnitId, Is.EqualTo("usn-a")); // higher quality
        Assert.That(track.FireControlQuality, Is.True);
        Assert.That(
            Math.Round(track.Quality, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.75, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Fire_control_quality_false_when_only_degraded_mesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();
        Assert.That(c.ContributeOrganic(Blue, "usn-a", Target, 0.9), Is.True);
        Assert.That(c.ContributeOrganic(Blue, "usn-b", Target, 0.85), Is.True);

        var live = c.TryGetCompositeTracks(Blue);
        AssertEx.Single(live);
        Assert.That(live[0].FireControlQuality, Is.True);

        // Stretch to degraded-only mesh; organics remain but FC quality drops.
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 23.0);
        c.Refresh();
        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.Degraded));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.Degraded));

        // New organics rejected while degraded.
        Assert.That(c.ContributeOrganic(Blue, "usn-a", "hostile-2", 0.9), Is.False);

        var degraded = c.TryGetCompositeTracks(Blue);
        AssertEx.Single(degraded);
        Assert.That(degraded[0].FireControlQuality, Is.False);
        Assert.That(degraded[0].ContributorCount, Is.EqualTo(2));
    }

    [Test]
    public void Deterministic_refresh_order_produces_stable_event_log()
    {
        CecMeshController BuildAndRefresh()
        {
            var c = new CecMeshController();
            // Register in reverse id order — Refresh must sort by unit id.
            c.Register(UsnCecNode("usn-z", 57.0, 20.5));
            c.Register(UsnCecNode("usn-a", 57.0, 20.0));
            c.Register(UsnCecNode("usn-m", 57.1, 20.2));
            c.Refresh();
            return c;
        }

        var a = BuildAndRefresh();
        var b = BuildAndRefresh();

        Assert.That(b.ComputeEventLogFingerprint(), Is.EqualTo(a.ComputeEventLogFingerprint()));
        Assert.That(a.MeshEventLog.Count, Is.EqualTo(3));
        // Events follow unit-id ordinal order of evaluation.
        Assert.That(a.MeshEventLog[0].UnitId, Is.EqualTo("usn-a"));
        Assert.That(a.MeshEventLog[1].UnitId, Is.EqualTo("usn-m"));
        Assert.That(a.MeshEventLog[2].UnitId, Is.EqualTo("usn-z"));
        AssertEx.All(a.MeshEventLog, e => Assert.That(e.Kind, Is.EqualTo(CecMeshEventKind.Join)));
        Assert.That(a.MeshEventLog[0].SequenceId, Is.EqualTo(1UL));
        Assert.That(a.MeshEventLog[1].SequenceId, Is.EqualTo(2UL));
        Assert.That(a.MeshEventLog[2].SequenceId, Is.EqualTo(3UL));
    }

    [Test]
    public void Dead_node_is_OutOfMesh_and_cannot_contribute()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0, alive: true));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5, alive: false));
        c.Refresh();

        Assert.That(c.GetMeshState("usn-a"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.GetMeshState("usn-b"), Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(c.ContributeOrganic(Blue, "usn-b", Target, 0.9), Is.False);
    }

    [Test]
    public void Evaluator_pure_rules_match_bands()
    {
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: false, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: false, alive: true),
            Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: true, alive: true),
            Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: false, alive: false),
            Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: false, bestPeerRangeDeg: null, jammed: false, alive: true),
            Is.EqualTo(CecMeshState.OutOfMesh));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 1.5, jammed: false, alive: true),
            Is.EqualTo(CecMeshState.InMesh));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 3.0, jammed: false, alive: true),
            Is.EqualTo(CecMeshState.Degraded));
        Assert.That(
            CecMeshEvaluator.EvaluateMeshState(
                        cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 5.0, jammed: false, alive: true),
            Is.EqualTo(CecMeshState.OutOfMesh));
    }
}
