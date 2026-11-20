using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Swarm;
using ProjectAegis.Sim.Swarm.Formation;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Swarm.Formation;

/// <summary>DRG-105 / SWARM-C1: formations Cloud/Wall/Spear/Orbit (SWARM-16).</summary>
public sealed class SwarmFormationTests
{
    private static SwarmUnitIntegrity Sample(string id = "swarm-1", int drones = 8) =>
        new(id, CatalogSwarmPlatformDefaults.GenericSwarmPlatformId, drones, drones);

    [Test]
    public void Register_defaults_formation_to_Cloud()
    {
        var c = new SwarmController(SimSeed.FromScenario(1));
        c.Register(Sample(), 57.0, 20.0);

        Assert.That(c.GetFormation("swarm-1"), Is.EqualTo(SwarmFormation.Cloud));
    }

    [Test]
    public void IssueSetFormation_logs_and_is_readable()
    {
        var c = new SwarmController(SimSeed.FromScenario(2));
        c.Register(Sample(), 57.0, 20.0);

        var seq = c.IssueSetFormation("swarm-1", SwarmFormation.Spear, simTick: 1, simTime: 1.0);

        Assert.That(seq, Is.EqualTo(1UL));
        Assert.That(c.GetFormation("swarm-1"), Is.EqualTo(SwarmFormation.Spear));
        AssertEx.Single(c.FormationOrderLog);
        Assert.That(c.FormationOrderLog[0].Formation, Is.EqualTo(SwarmFormation.Spear));
        Assert.That(c.FormationOrderLog[0].UnitId, Is.EqualTo("swarm-1"));
        Assert.That(c.FormationOrderLog[0].SimTick, Is.EqualTo(1UL));
    }

    [Test]
    public void IssueSetFormation_blocked_when_link_lost()
    {
        var c = new SwarmController(SimSeed.FromScenario(3));
        c.Register(Sample(), 57.0, 20.0);
        c.SetLinkState("swarm-1", SwarmLinkState.Lost);

        Assert.Throws<InvalidOperationException>(() =>
            c.IssueSetFormation("swarm-1", SwarmFormation.Wall, simTick: 1, simTime: 1.0));
    }

    [Test]
    public void ComputeOffsets_is_deterministic_for_same_seed()
    {
        var a = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, droneCount: 12, seed: 99);
        var b = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, droneCount: 12, seed: 99);

        Assert.That(a.Count, Is.EqualTo(12));
        Assert.That(b.Count, Is.EqualTo(a.Count));
        for (var i = 0; i < a.Count; i++)
        {
            Assert.That(b[i].DxDeg, Is.EqualTo(a[i].DxDeg));
            Assert.That(b[i].DyDeg, Is.EqualTo(a[i].DyDeg));
        }
    }

    [Test]
    public void ComputeOffsets_Cloud_differs_by_seed()
    {
        var a = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, 10, seed: 1);
        var b = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, 10, seed: 2);

        var same = true;
        for (var i = 0; i < a.Count; i++)
        {
            if (a[i].DxDeg != b[i].DxDeg || a[i].DyDeg != b[i].DyDeg)
            {
                same = false;
                break;
            }
        }

        Assert.That(same, Is.False);
    }

    [Test]
    public void ComputeOffsets_Wall_is_line_perpendicular_to_bearing()
    {
        // Bearing due east (π/2): wall runs north-south (varying lat, ~0 lon span).
        var bearing = Math.PI / 2.0;
        var offsets = SwarmFormationLayout.ComputeOffsets(
            SwarmFormation.Wall,
            droneCount: 5,
            seed: 0,
            hostBearingRad: bearing);

        Assert.That(offsets.Count, Is.EqualTo(5));
        // Perpendicular to east is north-south: dirLat = -sin(π/2) = -1, dirLon = cos(π/2) = 0
        AssertEx.All(offsets, o => Assert.That(
    Math.Round(o.DyDeg, 9, MidpointRounding.AwayFromZero),
    Is.EqualTo(Math.Round(0.0, 9, MidpointRounding.AwayFromZero))));
        Assert.That(offsets[0].DxDeg < offsets[4].DxDeg || offsets[0].DxDeg > offsets[4].DxDeg, Is.True);
        // Centered: midpoint near origin
        Assert.That(
            Math.Round(offsets[2].DxDeg, 9, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.0, 9, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void ComputeOffsets_Spear_aligns_with_host_bearing()
    {
        // Bearing due east: spear along lon axis.
        var bearing = Math.PI / 2.0;
        var offsets = SwarmFormationLayout.ComputeOffsets(
            SwarmFormation.Spear,
            droneCount: 5,
            seed: 0,
            hostBearingRad: bearing);

        AssertEx.All(offsets, o => Assert.That(
    Math.Round(o.DxDeg, 9, MidpointRounding.AwayFromZero),
    Is.EqualTo(Math.Round(0.0, 9, MidpointRounding.AwayFromZero))));
        Assert.That(Math.Abs(offsets[0].DyDeg) > 0 || Math.Abs(offsets[4].DyDeg) > 0, Is.True);
        Assert.That(
            Math.Round(offsets[2].DyDeg, 9, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.0, 9, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void ComputeOffsets_Orbit_biases_toward_host_bearing()
    {
        var unbound = SwarmFormationLayout.ComputeOffsets(
            SwarmFormation.Orbit,
            droneCount: 8,
            seed: 0,
            hostBearingRad: null);

        var boundEast = SwarmFormationLayout.ComputeOffsets(
            SwarmFormation.Orbit,
            droneCount: 8,
            seed: 0,
            hostBearingRad: Math.PI / 2.0);

        // Unbound centroid of offsets ~0; bound should shift mean lon (dy) positive (east).
        var meanUnboundDy = unbound.Average(o => o.DyDeg);
        var meanBoundDy = boundEast.Average(o => o.DyDeg);
        Assert.That(
            Math.Round(meanUnboundDy, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(0.0, 6, MidpointRounding.AwayFromZero)));
        Assert.That(meanBoundDy > 0.005, Is.True, $"expected east bias, got mean Dy={meanBoundDy}");
    }

    [Test]
    public void Orbit_with_host_via_public_API_uses_centroid_and_host_publish()
    {
        var c = new SwarmController(SimSeed.FromScenario(42));
        c.Register(Sample(drones: 6), latDeg: 57.0, lonDeg: 20.0);
        c.BindHost("swarm-1", "host-cvn");
        c.PublishHostState("host-cvn", latDeg: 57.0, lonDeg: 20.5, alive: true);
        c.IssueSetFormation("swarm-1", SwarmFormation.Orbit, simTick: 1, simTime: 0.0);

        Assert.That(c.GetFormation("swarm-1"), Is.EqualTo(SwarmFormation.Orbit));
        Assert.That(c.GetHostId("swarm-1"), Is.EqualTo("host-cvn"));
        Assert.That(c.TryGetCentroid("swarm-1", out var lat, out var lon), Is.True);

        // Host east of centroid → bearing atan2(dLon, dLat) ≈ π/2
        const double hostLat = 57.0;
        const double hostLon = 20.5;
        var bearing = Math.Atan2(hostLon - lon, hostLat - lat);

        var offsets = SwarmFormationLayout.ComputeOffsets(
            c.GetFormation("swarm-1"),
            droneCount: 6,
            seed: c.Seed.Value,
            hostBearingRad: bearing);

        Assert.That(offsets.Count, Is.EqualTo(6));
        var meanDy = offsets.Average(o => o.DyDeg);
        Assert.That(meanDy > 0.0, Is.True, "orbit should bias toward host east of centroid");
    }

    [Test]
    public void ComputeOffsets_empty_for_zero_drones()
    {
        var offsets = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, droneCount: 0, seed: 1);
        Assert.That(offsets, Is.Empty);
    }

    [Test]
    public void Formations_produce_distinct_layouts()
    {
        const int n = 8;
        const ulong seed = 7;
        var cloud = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Cloud, n, seed);
        var wall = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Wall, n, seed, hostBearingRad: 0);
        var spear = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Spear, n, seed, hostBearingRad: 0);
        var orbit = SwarmFormationLayout.ComputeOffsets(SwarmFormation.Orbit, n, seed, hostBearingRad: 0);

        Assert.That(LayoutsEqual(cloud, wall), Is.False);
        Assert.That(LayoutsEqual(wall, spear), Is.False);
        Assert.That(LayoutsEqual(spear, orbit), Is.False);
        Assert.That(LayoutsEqual(cloud, orbit), Is.False);
    }

    private static bool LayoutsEqual(
        IReadOnlyList<(double DxDeg, double DyDeg)> a,
        IReadOnlyList<(double DxDeg, double DyDeg)> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (Math.Abs(a[i].DxDeg - b[i].DxDeg) > 1e-12 ||
                Math.Abs(a[i].DyDeg - b[i].DyDeg) > 1e-12)
            {
                return false;
            }
        }

        return true;
    }
}
