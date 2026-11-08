using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Cec;
using Xunit;

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

    [Fact]
    public void Non_CEC_cannot_join_mesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-1", 57.0, 20.0));
        c.Register(NonCecNode("generic-1", 57.0, 20.1));
        c.Register(NonCecNode("generic-2", 57.0, 20.2));

        c.Refresh();

        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("generic-1"));
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("generic-2"));
        // Lone CEC node with only non-CEC neighbors has no CEC peer.
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-1"));
        Assert.False(c.ContributeOrganic(Blue, "generic-1", Target, 0.9));
    }

    [Fact]
    public void Two_USN_CEC_nodes_in_range_are_both_InMesh()
    {
        var c = new CecMeshController();
        // Catalog exemplar ids referenced for documentation; mesh gate is CecCapable flag.
        _ = CatalogSwarmPlatformDefaults.UsnCecSwarmPlatformId;
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5)); // 0.5 deg < DefaultConnectedRangeDeg 2.0

        c.Refresh();

        Assert.Equal(CecMeshState.InMesh, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.InMesh, c.GetMeshState("usn-b"));
        Assert.Contains(c.MeshEventLog, e => e.UnitId == "usn-a" && e.Kind == CecMeshEventKind.Join);
        Assert.Contains(c.MeshEventLog, e => e.UnitId == "usn-b" && e.Kind == CecMeshEventKind.Join);
    }

    [Fact]
    public void Jam_forces_OutOfMesh_without_any_Swarm_types()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh(jammed: false);
        Assert.Equal(CecMeshState.InMesh, c.GetMeshState("usn-a"));

        c.Refresh(jammed: true);

        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-b"));
        // Independence: this test file must not import ProjectAegis.Sim.Swarm.
        Assert.Contains(c.MeshEventLog, e => e.Kind == CecMeshEventKind.Leave);
    }

    [Fact]
    public void Range_stretch_moves_InMesh_to_Degraded_then_OutOfMesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();
        Assert.Equal(CecMeshState.InMesh, c.GetMeshState("usn-a"));

        // Stretch into degraded band only (connected 2.0 < range <= 4.0).
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 23.0); // range 3.0
        c.Refresh();
        Assert.Equal(CecMeshState.Degraded, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.Degraded, c.GetMeshState("usn-b"));

        // Stretch beyond degraded band.
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 25.0); // range 5.0
        c.Refresh();
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-b"));
    }

    [Fact]
    public void Composite_track_forms_when_two_nodes_contribute_same_target()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();

        Assert.True(c.ContributeOrganic(Blue, "usn-a", Target, 0.8));
        Assert.True(c.ContributeOrganic(Blue, "usn-b", Target, 0.7));

        var tracks = c.TryGetCompositeTracks(Blue);
        Assert.Single(tracks);
        var track = tracks[0];
        Assert.Equal(Target, track.TargetId);
        Assert.Equal(Blue, track.SideId);
        Assert.Equal(2, track.ContributorCount);
        Assert.Equal("cec-blue-hostile-1", track.TrackId);
        Assert.Equal("usn-a", track.PrimaryContributorUnitId); // higher quality
        Assert.True(track.FireControlQuality);
        Assert.Equal(0.75, track.Quality, 6);
    }

    [Fact]
    public void Fire_control_quality_false_when_only_degraded_mesh()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5));
        c.Refresh();
        Assert.True(c.ContributeOrganic(Blue, "usn-a", Target, 0.9));
        Assert.True(c.ContributeOrganic(Blue, "usn-b", Target, 0.85));

        var live = c.TryGetCompositeTracks(Blue);
        Assert.Single(live);
        Assert.True(live[0].FireControlQuality);

        // Stretch to degraded-only mesh; organics remain but FC quality drops.
        c.UpdateNode("usn-b", latDeg: 57.0, lonDeg: 23.0);
        c.Refresh();
        Assert.Equal(CecMeshState.Degraded, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.Degraded, c.GetMeshState("usn-b"));

        // New organics rejected while degraded.
        Assert.False(c.ContributeOrganic(Blue, "usn-a", "hostile-2", 0.9));

        var degraded = c.TryGetCompositeTracks(Blue);
        Assert.Single(degraded);
        Assert.False(degraded[0].FireControlQuality);
        Assert.Equal(2, degraded[0].ContributorCount);
    }

    [Fact]
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

        Assert.Equal(a.ComputeEventLogFingerprint(), b.ComputeEventLogFingerprint());
        Assert.Equal(3, a.MeshEventLog.Count);
        // Events follow unit-id ordinal order of evaluation.
        Assert.Equal("usn-a", a.MeshEventLog[0].UnitId);
        Assert.Equal("usn-m", a.MeshEventLog[1].UnitId);
        Assert.Equal("usn-z", a.MeshEventLog[2].UnitId);
        Assert.All(a.MeshEventLog, e => Assert.Equal(CecMeshEventKind.Join, e.Kind));
        Assert.Equal(1UL, a.MeshEventLog[0].SequenceId);
        Assert.Equal(2UL, a.MeshEventLog[1].SequenceId);
        Assert.Equal(3UL, a.MeshEventLog[2].SequenceId);
    }

    [Fact]
    public void Dead_node_is_OutOfMesh_and_cannot_contribute()
    {
        var c = new CecMeshController();
        c.Register(UsnCecNode("usn-a", 57.0, 20.0, alive: true));
        c.Register(UsnCecNode("usn-b", 57.0, 20.5, alive: false));
        c.Refresh();

        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-a"));
        Assert.Equal(CecMeshState.OutOfMesh, c.GetMeshState("usn-b"));
        Assert.False(c.ContributeOrganic(Blue, "usn-b", Target, 0.9));
    }

    [Fact]
    public void Evaluator_pure_rules_match_bands()
    {
        Assert.Equal(
            CecMeshState.OutOfMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: false, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: false, alive: true));
        Assert.Equal(
            CecMeshState.OutOfMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: true, alive: true));
        Assert.Equal(
            CecMeshState.OutOfMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 0.1, jammed: false, alive: false));
        Assert.Equal(
            CecMeshState.OutOfMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: false, bestPeerRangeDeg: null, jammed: false, alive: true));
        Assert.Equal(
            CecMeshState.InMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 1.5, jammed: false, alive: true));
        Assert.Equal(
            CecMeshState.Degraded,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 3.0, jammed: false, alive: true));
        Assert.Equal(
            CecMeshState.OutOfMesh,
            CecMeshEvaluator.EvaluateMeshState(
                cecCapable: true, hasPeerInRange: true, bestPeerRangeDeg: 5.0, jammed: false, alive: true));
    }
}
