using ProjectAegis.Sim.Cec;
using ProjectAegis.Sim.Engage;
using Xunit;

namespace ProjectAegis.Sim.Tests.Engage;

/// <summary>DRG-103 / SWARM-B6b: CEC remote engage-on-remote-data (SWARM-31 engage half).</summary>
public sealed class CecRemoteEngageTests
{
    private const string Blue = "blue";
    private const string Target = "hostile-1";

    private static CecNodeRegistration Usn(
        string unitId,
        double lat,
        double lon,
        bool isSwarm = false) =>
        new(unitId, Blue, CecCapable: true, LatDeg: lat, LonDeg: lon, IsAlive: true, IsSwarm: isSwarm);

    [Fact]
    public void Organic_only_fails_when_no_FC_and_no_remote()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(request, new EngageContext(50_000, new WeaponEnvelope(1_000, 100_000), 2, HasFireControlTrack: false));

        var result = resolver.Resolve(request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.NoFireControlTrack, result.AbortReason);
    }

    [Fact]
    public void Ship_plus_CEC_swarm_composite_allows_third_shooter_remote_engage()
    {
        // Two mesh contributors form FC-quality composite; third CEC shooter has no organic FC.
        var mesh = new CecMeshController();
        mesh.Register(Usn("ship-sensor", 57.0, 20.0));
        mesh.Register(Usn("swarm-isr", 57.0, 20.1, isSwarm: true));
        mesh.Register(Usn("shooter", 57.0, 20.15));
        mesh.Refresh();
        Assert.Equal(CecMeshState.InMesh, mesh.GetMeshState("ship-sensor"));
        Assert.Equal(CecMeshState.InMesh, mesh.GetMeshState("swarm-isr"));
        Assert.Equal(CecMeshState.InMesh, mesh.GetMeshState("shooter"));

        Assert.True(mesh.ContributeOrganic(Blue, "ship-sensor", Target, 0.9));
        Assert.True(mesh.ContributeOrganic(Blue, "swarm-isr", Target, 0.85));

        Assert.True(CecRemoteEngageGate.TryResolveRemoteEligibility(
            mesh, Blue, "shooter", Target, out var track));
        Assert.NotNull(track);
        Assert.True(track!.FireControlQuality);
        Assert.NotEqual("shooter", track.PrimaryContributorUnitId);

        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(3, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(3, 99, 0, 0);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                2,
                HasFireControlTrack: false,
                UsesRemoteCecTrack: true,
                CecRemoteFireControlEligible: true,
                ShooterCecCapable: true));

        var result = resolver.Resolve(request);
        Assert.True(result.Launched);
        Assert.Equal(EngagementAbortReason.None, result.AbortReason);
        Assert.Equal(1, magazines.GetRounds(3, 0));
    }

    [Fact]
    public void Mesh_loss_aborts_remote_with_CecRemoteTrackUnavailable()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                2,
                HasFireControlTrack: false,
                UsesRemoteCecTrack: true,
                CecRemoteFireControlEligible: false,
                ShooterCecCapable: true));

        var result = resolver.Resolve(request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.CecRemoteTrackUnavailable, result.AbortReason);
        Assert.Equal(2, magazines.GetRounds(1, 0));
    }

    [Fact]
    public void Non_CEC_shooter_cannot_remote_engage()
    {
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 2);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                2,
                HasFireControlTrack: false,
                UsesRemoteCecTrack: true,
                CecRemoteFireControlEligible: true,
                ShooterCecCapable: false));

        var result = resolver.Resolve(request);
        Assert.False(result.Launched);
        Assert.Equal(EngagementAbortReason.CecRemoteTrackUnavailable, result.AbortReason);
    }

    [Fact]
    public void Aggregate_swarm_SoT_unchanged_target_integrity_fields_still_apply()
    {
        // Remote engage does not invent per-drone FC — target swarm still uses aggregate fields.
        var world = new DictionaryEngageWorldQuery();
        var magazines = new MagazineLedger();
        magazines.SetRounds(1, 0, 4);
        var resolver = new MvpEngagementResolver(world, magazines);
        var request = new EngageRequest(1, 2, 0, 0);
        world.Set(
            request,
            new EngageContext(
                50_000,
                new WeaponEnvelope(1_000, 100_000),
                4,
                HasFireControlTrack: false,
                TargetMaxDrones: 20,
                TargetDroneCount: 20,
                TargetAaProfile: SwarmAaProfileKind.AreaAa,
                UsesRemoteCecTrack: true,
                CecRemoteFireControlEligible: true,
                ShooterCecCapable: true));

        var result = resolver.Resolve(request);
        Assert.True(result.Launched);
    }

    [Fact]
    public void Gate_denies_when_primary_contributor_is_self()
    {
        var mesh = new CecMeshController();
        mesh.Register(Usn("a", 57.0, 20.0));
        mesh.Register(Usn("b", 57.0, 20.1));
        mesh.Refresh();
        mesh.ContributeOrganic(Blue, "a", Target, 0.95);
        mesh.ContributeOrganic(Blue, "b", Target, 0.5);

        // Primary is highest quality = a; a cannot treat as remote.
        Assert.False(CecRemoteEngageGate.TryResolveRemoteEligibility(mesh, Blue, "a", Target, out _));
        Assert.True(CecRemoteEngageGate.TryResolveRemoteEligibility(mesh, Blue, "b", Target, out _));
    }

    [Fact]
    public void Jam_drops_remote_eligibility_without_organic()
    {
        var mesh = new CecMeshController();
        mesh.Register(Usn("ship-sensor", 57.0, 20.0));
        mesh.Register(Usn("swarm-isr", 57.0, 20.1, isSwarm: true));
        mesh.Register(Usn("shooter", 57.0, 20.15));
        mesh.Refresh();
        mesh.ContributeOrganic(Blue, "ship-sensor", Target, 0.9);
        mesh.ContributeOrganic(Blue, "swarm-isr", Target, 0.85);
        Assert.True(CecRemoteEngageGate.TryResolveRemoteEligibility(mesh, Blue, "shooter", Target, out _));

        mesh.Refresh(jammed: true);
        Assert.Equal(CecMeshState.OutOfMesh, mesh.GetMeshState("shooter"));
        Assert.False(CecRemoteEngageGate.TryResolveRemoteEligibility(mesh, Blue, "shooter", Target, out _));
    }

    [Fact]
    public void Evaluate_organic_path_returns_null()
    {
        Assert.Null(CecRemoteEngageGate.Evaluate(
            hasOrganicFireControlTrack: true,
            usesRemoteCecTrack: false,
            shooterCecCapable: false,
            cecRemoteFireControlEligible: false));
    }
}
