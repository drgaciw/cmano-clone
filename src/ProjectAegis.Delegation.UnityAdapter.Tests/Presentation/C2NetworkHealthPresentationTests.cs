using NUnit.Framework;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.C2Network;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class C2NetworkHealthPresentationTests
{
    [Test]
    public void Null_snapshot_returns_empty_presentation()
    {
        Assert.That(
            C2NetworkHealthPresenter.Build(null),
            Is.EqualTo(C2NetworkHealthPresentation.Empty));
    }

    [Test]
    public void Healthy_mesh_surfaces_live_links_without_last_known_rows()
    {
        var snapshot = HealthySnapshot();
        var presentation = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(presentation.AdvisoryBadge, Does.Contain("never fabricated"));
        Assert.That(presentation.NetworkHealthLine, Does.Contain("HEALTHY"));
        Assert.That(presentation.CommsStateLine, Is.EqualTo("COMMS: NOMINAL"));
        Assert.That(presentation.CommsNodeLine, Does.Contain("c2-net"));
        Assert.That(presentation.LinkRows, Has.Count.EqualTo(2));
        Assert.That(presentation.LinkRows.All(row => row.CapabilityLabel == "LIVE"), Is.True);
        Assert.That(presentation.ContributorRows, Is.Empty);
        Assert.That(presentation.LostPathRows, Is.Empty);
        Assert.That(presentation.ContributorCountLine, Does.Contain("none"));
        Assert.That(presentation.CssClass, Is.EqualTo("c2-comms--healthy"));
        Assert.That(presentation.NextActionLine, Does.Contain("healthy"));
    }

    [Test]
    public void Partitioned_mesh_surfaces_last_known_contributors_and_lost_paths()
    {
        var snapshot = PartitionedSnapshot();
        var presentation = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(presentation.NetworkHealthLine, Does.Contain("PARTITIONED"));
        Assert.That(presentation.SummaryLine, Does.Contain("PARTITIONED").And.Contain("partitioned"));
        Assert.That(presentation.LinkRows.Single(row => row.EndpointLine.Contains("u2 ↔ u3")).CapabilityLabel,
            Is.EqualTo("LAST-KNOWN ONLY"));
        Assert.That(presentation.LinkRows.Single(row => row.EndpointLine.Contains("u2 ↔ u3")).AffectedLine,
            Does.Contain("u3"));
        Assert.That(presentation.ContributorRows, Has.Count.EqualTo(1));
        Assert.That(presentation.ContributorRows[0].ContactLine, Does.Contain("dl-hostile-1"));
        Assert.That(presentation.LostPathRows, Has.Count.EqualTo(1));
        Assert.That(presentation.LostPathRows[0].PathLine, Does.Contain("u2 ↔ u3"));
        Assert.That(presentation.CssClass, Is.EqualTo("c2-comms--partitioned"));
        Assert.That(presentation.NextActionLine, Does.Contain("partitioned"));
    }

    [Test]
    public void Degraded_mesh_reports_degraded_css_without_fabricating_last_known_rows()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1.0, 1, "brigade-net", CommsState.Nominal, CommsState.Degraded, "jamming"));

        var snapshot = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2"],
            BalticLinks(),
            currentSimTick: 2);

        var presentation = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(presentation.NetworkHealthLine, Does.Contain("DEGRADED"));
        Assert.That(presentation.CommsStateLine, Is.EqualTo("COMMS: DEGRADED"));
        Assert.That(presentation.LinkRows[0].HealthLabel, Is.EqualTo("DEGRADED"));
        Assert.That(presentation.LinkRows[0].CapabilityLabel, Is.EqualTo("LIVE"));
        Assert.That(presentation.ContributorRows, Is.Empty);
        Assert.That(presentation.CssClass, Is.EqualTo("c2-comms--degraded"));
        Assert.That(presentation.NextActionLine, Does.Contain("stale"));
    }

    [Test]
    public void Global_denied_surfaces_denied_comms_and_partitioned_network()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            0, 1.0, 1, "brigade-net", CommsState.Nominal, CommsState.Denied, "emp"));

        var snapshot = C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2"],
            BalticLinks(),
            currentSimTick: 2);

        var presentation = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(presentation.CommsStateLine, Is.EqualTo("COMMS: DENIED"));
        Assert.That(presentation.NetworkHealthLine, Does.Contain("PARTITIONED"));
        Assert.That(presentation.LinkRows[0].CapabilityLabel, Is.EqualTo("LAST-KNOWN ONLY"));
        Assert.That(presentation.NextActionLine, Does.Contain("organic sensors"));
    }

    [Test]
    public void Panel_binder_maps_positional_props_without_reprojecting()
    {
        var presentation = C2NetworkHealthPresenter.Build(PartitionedSnapshot());
        var labels = C2NetworkHealthPanelBinder.Bind(presentation);

        Assert.That(labels.HeaderLine, Is.EqualTo(presentation.HeaderLine));
        Assert.That(labels.NetworkHealthLine, Is.EqualTo(presentation.NetworkHealthLine));
        Assert.That(labels.CommsStateLine, Is.EqualTo(presentation.CommsStateLine));
        Assert.That(labels.SummaryLine, Is.EqualTo(presentation.SummaryLine));
        Assert.That(labels.CssClass, Is.EqualTo(presentation.CssClass));
        Assert.That(labels.LinkLines, Has.Count.EqualTo(presentation.LinkRows.Count));
        Assert.That(labels.LinkLines[0], Does.Contain("↔"));
        Assert.That(labels.ContributorLines, Has.Count.EqualTo(presentation.ContributorRows.Count));
        Assert.That(labels.LostPathLines, Has.Count.EqualTo(presentation.LostPathRows.Count));
    }

    [Test]
    public void Fingerprint_matches_headless_projection_for_replay_stability()
    {
        var snapshot = PartitionedSnapshot();
        var expected = C2NetworkHealthFingerprint.Compute(snapshot);

        var first = C2NetworkHealthPresenter.Build(snapshot);
        var second = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(first.Fingerprint, Is.EqualTo(expected));
        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
    }

    [Test]
    public void Repeated_projection_is_replay_stable()
    {
        var snapshot = PartitionedSnapshot();
        var first = C2NetworkHealthPresenter.Build(snapshot);
        var second = C2NetworkHealthPresenter.Build(snapshot);

        Assert.That(second.Fingerprint, Is.EqualTo(first.Fingerprint));
        Assert.That(second.SummaryLine, Is.EqualTo(first.SummaryLine));
        Assert.That(second.NextActionLine, Is.EqualTo(first.NextActionLine));
        Assert.That(second.LinkRows.Select(row => row.EndpointLine),
            Is.EqualTo(first.LinkRows.Select(row => row.EndpointLine)));
    }

    [Test]
    public void Summary_line_helper_reports_unknown_for_empty_presentation()
    {
        Assert.That(
            C2NetworkHealthPresenter.FormatSummaryLine(C2NetworkHealthPresentation.Empty),
            Does.Contain("UNKNOWN"));
    }

    private static C2NetworkHealthSnapshot HealthySnapshot()
    {
        var log = new DecisionLog();
        log.AppendContactChange(new ContactChangeRecord(
            0, 1.0, 1, "u1", "c1", "hostile-1", "Unknown", "Detected"));
        log.AppendContactChange(new ContactChangeRecord(
            0, 2.0, 2, "u2", "dl-hostile-1", "hostile-1", "Unknown", "Detected"));

        return C2NetworkHealthProjector.Project(
            log,
            ["u3", "u1", "u2"],
            BalticLinks(),
            currentSimTick: 3);
    }

    private static C2NetworkHealthSnapshot PartitionedSnapshot()
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

        return C2NetworkHealthProjector.Project(
            log,
            ["u1", "u2", "u3"],
            BalticLinks(),
            currentSimTick: 5,
            linkStatusOverrides:
            [
                new C2NetworkHealthProjector.LinkStatusOverride("u2", "u3", DatalinkPictureProjection.StatusDown),
            ]);
    }

    private static IReadOnlyList<CatalogLinkEntry> BalticLinks() =>
    [
        new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, 50),
        new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, 250),
    ];
}
