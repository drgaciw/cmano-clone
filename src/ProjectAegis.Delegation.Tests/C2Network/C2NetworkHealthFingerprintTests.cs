using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.C2Network;

[TestFixture]
public sealed class C2NetworkHealthFingerprintTests
{
    [Test]
    public void Compute_is_deterministic_for_identical_snapshots()
    {
        var log = BuildPartitionedFixtureLog();
        var a = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2", "u3"],
            Links(),
            currentSimTick: 5,
            linkStatusOverrides:
            [
                new C2NetworkHealthProjector.LinkStatusOverride("u2", "u3", DatalinkPictureProjection.StatusDown),
            ]);
        var b = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2", "u3"],
            Links(),
            currentSimTick: 5,
            linkStatusOverrides:
            [
                new C2NetworkHealthProjector.LinkStatusOverride("u2", "u3", DatalinkPictureProjection.StatusDown),
            ]);

        Assert.That(C2NetworkHealthFingerprint.Compute(b), Is.EqualTo(C2NetworkHealthFingerprint.Compute(a)));
        Assert.That(C2NetworkHealthFingerprint.Compute(a), Does.StartWith("C2NetworkHealth|"));
    }

    [Test]
    public void Compute_changes_when_partition_override_differs()
    {
        var log = BuildPartitionedFixtureLog();
        var healthy = C2NetworkHealthProjector.Project(log, ["u1", "u2", "u3"], Links(), currentSimTick: 5);
        var partitioned = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2", "u3"],
            Links(),
            currentSimTick: 5,
            linkStatusOverrides:
            [
                new C2NetworkHealthProjector.LinkStatusOverride("u2", "u3", DatalinkPictureProjection.StatusDown),
            ]);

        Assert.That(
            C2NetworkHealthFingerprint.Compute(partitioned),
            Is.Not.EqualTo(C2NetworkHealthFingerprint.Compute(healthy)));
    }

    private static DecisionLog BuildPartitionedFixtureLog()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 3.0, 3, "u3", "dl-hostile-1", "hostile-1", "Unknown", "Detected"));
        return log;
    }

    private static IReadOnlyList<CatalogLinkEntry> Links() =>
    [
        new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50),
    ];
}
