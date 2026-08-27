using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.C2Network;

/// <summary>DRG-214: headless C2 network health projection for Combat UX Slice A wave 2.</summary>
[TestFixture]
public sealed class C2NetworkHealthProjectorTests
{
    private static IReadOnlyList<CatalogLinkEntry> BalticLinks() =>
    [
        new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50),
        new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, 250),
    ];

    [Test]
    public void Healthy_mesh_all_links_live_with_zero_staleness()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, 2.0, 2, "u2", "dl-hostile-1", "hostile-1", "Unknown", "Detected"));

        var snapshot = C2NetworkHealthProjector.Project(
            log,
            ["u3", "u1", "u2"],
            BalticLinks(),
            currentSimTick: 3);

        Assert.That(snapshot.NetworkHealth, Is.EqualTo(C2NetworkHealthLevel.Healthy));
        Assert.That(snapshot.CommsState, Is.EqualTo(CommsState.Nominal));
        Assert.That(snapshot.Links, Has.Count.EqualTo(2));
        Assert.That(snapshot.Links.All(l => l.Health == C2LinkHealth.Healthy), Is.True);
        Assert.That(snapshot.Links.All(l => l.IsLiveCapability), Is.True);
        Assert.That(snapshot.Links.All(l => l.StalenessTicks == 0), Is.True);
        Assert.That(snapshot.Links.All(l => l.AffectedContributorUnitIds.Count == 0), Is.True);
        Assert.That(snapshot.LastKnownContributors, Is.Empty);
        Assert.That(snapshot.LostPaths, Is.Empty);
    }

    [Test]
    public void Partitioned_link_marks_affected_contributors_and_lost_path_without_fabricating_live_capability()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, 2.0, 2, "u2", "dl-hostile-1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, 3.0, 3, "u3", "dl-hostile-1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, 4.0, 4, "u3", "dl-hostile-1", "hostile-1", "Detected", "Classified"));

        var snapshot = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2", "u3"],
            BalticLinks(),
            currentSimTick: 5,
            linkStatusOverrides:
            [
                new C2NetworkHealthProjector.LinkStatusOverride("u2", "u3", DatalinkPictureProjection.StatusDown),
            ]);

        Assert.That(snapshot.NetworkHealth, Is.EqualTo(C2NetworkHealthLevel.Partitioned));

        var partitionedLink = snapshot.Links.Single(l => l.FromUnitId == "u2" && l.ToUnitId == "u3");
        Assert.That(partitionedLink.Health, Is.EqualTo(C2LinkHealth.Partitioned));
        Assert.That(partitionedLink.IsLiveCapability, Is.False);
        Assert.That(partitionedLink.AffectedContributorUnitIds, Is.EqualTo(new[] { "u3" }));
        Assert.That(partitionedLink.StalenessTicks, Is.GreaterThan(0));

        var healthyLink = snapshot.Links.Single(l => l.FromUnitId == "u1" && l.ToUnitId == "u2");
        Assert.That(healthyLink.Health, Is.EqualTo(C2LinkHealth.Healthy));
        Assert.That(healthyLink.IsLiveCapability, Is.True);

        Assert.That(snapshot.LastKnownContributors, Has.Count.EqualTo(1));
        var contributor = snapshot.LastKnownContributors[0];
        Assert.That(contributor.UnitId, Is.EqualTo("u3"));
        Assert.That(contributor.ContactId, Is.EqualTo("dl-hostile-1"));
        Assert.That(contributor.TargetId, Is.EqualTo("hostile-1"));
        Assert.That(contributor.LifecycleState, Is.EqualTo("Classified"));
        Assert.That(contributor.LastKnownSimTick, Is.EqualTo(4));
        Assert.That(contributor.IsLiveCapability, Is.False);

        Assert.That(snapshot.LostPaths, Has.Count.EqualTo(1));
        Assert.That(snapshot.LostPaths[0].FromUnitId, Is.EqualTo("u2"));
        Assert.That(snapshot.LostPaths[0].ToUnitId, Is.EqualTo("u3"));
        Assert.That(snapshot.LostPaths[0].LinkType, Is.EqualTo(CatalogLinkTypes.Tactical));
        Assert.That(snapshot.LostPaths[0].LastKnownSimTick, Is.EqualTo(4));
    }

    [Test]
    public void Global_comms_degraded_projects_degraded_mesh_without_last_known_rows()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1.0, 1, "brigade-net", CommsState.Nominal, CommsState.Degraded, "jamming"));

        var snapshot = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2"],
            BalticLinks(),
            currentSimTick: 2);

        Assert.That(snapshot.NetworkHealth, Is.EqualTo(C2NetworkHealthLevel.Degraded));
        Assert.That(snapshot.CommsState, Is.EqualTo(CommsState.Degraded));
        Assert.That(snapshot.Links, Has.Count.EqualTo(1));
        Assert.That(snapshot.Links[0].Health, Is.EqualTo(C2LinkHealth.Degraded));
        Assert.That(snapshot.Links[0].IsLiveCapability, Is.False);
        Assert.That(snapshot.LastKnownContributors, Is.Empty);
        Assert.That(snapshot.LostPaths, Is.Empty);
    }

    [Test]
    public void Project_null_log_throws()
    {
        try
        {
            C2NetworkHealthProjector.Project(null!, ["u1"], BalticLinks(), 0);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException ex)
        {
            Assert.That(ex.ParamName, Is.EqualTo("log"));
        }
    }
}
