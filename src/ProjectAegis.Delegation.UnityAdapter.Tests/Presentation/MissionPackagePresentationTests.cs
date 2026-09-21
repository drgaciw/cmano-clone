using System.Xml.Linq;
using NUnit.Framework;
using ProjectAegis.Delegation.C2Nodes;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Catalog;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class MissionPackagePresentationTests
{
    [Test]
    public void Empty_snapshot_clears_presentation()
    {
        Assert.That(
            MissionPackagePresenter.Build(MissionPackageSnapshot.Empty),
            Is.EqualTo(MissionPackagePresentation.Empty));
    }

    [Test]
    public void Package_rows_bind_every_membership_field()
    {
        var snapshot = MissionPackageProjection.Project(
            new[] { BalticAsuwPackage() },
            currentSimTick: 4,
            currentSimTime: 4.0);
        var presentation = MissionPackagePresenter.Build(snapshot);

        Assert.That(presentation.Packages, Has.Count.EqualTo(1));
        var rollup = presentation.Packages[0];
        Assert.That(rollup.PackageId, Is.EqualTo("pkg-asuw-1"));
        Assert.That(rollup.PackageLabel, Is.EqualTo("Baltic ASuW Package"));
        Assert.That(rollup.ElementIds, Has.Count.EqualTo(4));
        Assert.That(rollup.UnitIds, Is.EqualTo(new[] { "u1", "u2", "u3" }));
        Assert.That(rollup.SummaryLine, Does.Contain("pkg-asuw-1").And.Contain("elements 4"));
    }

    [Test]
    public void Element_rows_bind_every_positional_record_property()
    {
        var log = new DecisionLog();
        log.AppendPlatformDamageChange(new PlatformDamageChangeRecord(
            0,
            3.0,
            3,
            new TargetId("u2"),
            100,
            0,
            PlatformDamageChangeReasonCodes.Hit,
            2));
        var snapshot = MissionPackageProjection.Project(
            new[] { BalticAsuwPackage() },
            log,
            currentSimTick: 3,
            currentSimTime: 3.0);
        var shooter = snapshot.Elements.Single(element => element.ElementId == "elem-shooter-1");
        var presentation = MissionPackagePresenter.Build(snapshot, selectedUnitId: "u2");
        var row = presentation.Elements.Single(element => element.ElementId == "elem-shooter-1");

        Assert.That(row.PlatformUnitId, Is.EqualTo("u2"));
        Assert.That(row.RoleLabel, Is.EqualTo("SHOOTER"));
        Assert.That(row.AvailabilityLabel, Is.EqualTo("UNAVAILABLE"));
        Assert.That(row.PackageId, Is.EqualTo("pkg-asuw-1"));
        Assert.That(row.PackageLabel, Is.EqualTo("Baltic ASuW Package"));
        Assert.That(row.MembershipKindLabel, Is.EqualTo("PACKAGE"));
        Assert.That(row.CapabilityScope, Is.EqualTo("package-engage"));
        Assert.That(row.TaskOrgDetached, Is.False);
        Assert.That(row.LastSimTick, Is.EqualTo(3UL));
        Assert.That(row.LastSimTime, Is.EqualTo(3.0));
        Assert.That(row.CorrelationSequenceLabel, Does.Contain(shooter.CorrelationSequenceId!.Value.ToString()));
        Assert.That(row.SourceRefs, Does.Contain("unit:u2"));
        Assert.That(row.DisplayLine, Does.Contain("elem-shooter-1").And.Contain("UNAVAILABLE"));
    }

    [Test]
    public void Organic_scope_surfaces_membership_kind_and_last_known_relay()
    {
        var log = new DecisionLog();
        log.AppendCommsStateChange(new CommsStateChangeRecord(
            1,
            1.0,
            1,
            "net",
            CommsState.Nominal,
            CommsState.Degraded,
            "jam"));
        var snapshot = MissionPackageProjection.Project(
            new[] { BalticAsuwPackage() },
            log,
            currentSimTick: 1,
            currentSimTime: 1.0);
        var presentation = MissionPackagePresenter.Build(snapshot);
        var relay = presentation.Elements.Single(element => element.ElementId == "elem-relay-1");
        var organic = presentation.Elements.Single(element => element.ElementId == "elem-c2-1");

        Assert.That(relay.AvailabilityLabel, Is.EqualTo("LAST-KNOWN"));
        Assert.That(organic.MembershipKindLabel, Is.EqualTo("ORGANIC"));
        Assert.That(presentation.NextActionLine, Does.Contain("last-known"));
    }

    [Test]
    public void Fingerprint_matches_projection_for_replay_stability()
    {
        var snapshot = MissionPackageProjection.Project(new[] { BalticAsuwPackage() });
        var presentation = MissionPackagePresenter.Build(snapshot);
        Assert.That(presentation.Fingerprint, Is.EqualTo(MissionPackageProjection.ComputeFingerprint(snapshot)));
    }

    [Test]
    public void Panel_binder_exposes_all_lines_for_host_bind()
    {
        var snapshot = MissionPackageProjection.Project(new[] { BalticAsuwPackage() });
        var presentation = MissionPackagePresenter.Build(snapshot, selectedUnitId: "u1");
        var labels = MissionPackagePanelBinder.Bind(presentation);

        Assert.That(labels.HeaderLine, Does.Contain("MISSION PACKAGE"));
        Assert.That(labels.AdvisoryBadge, Does.Contain("IsOrder=false"));
        Assert.That(labels.ActivePackageLine, Does.Contain("pkg-asuw-1"));
        Assert.That(labels.SummaryLine, Does.Contain("unit u1"));
        Assert.That(labels.PackageLines, Has.Count.EqualTo(1));
        Assert.That(labels.ElementLines, Has.Count.EqualTo(2));
        Assert.That(labels.NextActionLine, Does.Contain("inspection"));
    }

    [Test]
    public void Panel_layout_exposes_package_and_element_lists()
    {
        var xml = XDocument.Parse(UiIaSourceReader.ReadUnder(
            "unity", "ProjectAegis", "Assets", "UI", "MissionPackage", "MissionPackagePanel.uxml"));
        foreach (var name in new[]
        {
            "mp-advisory-line", "mp-active-package-line", "mp-summary-line",
            "mp-package-list", "mp-element-list", "mp-next-action-line",
        })
        {
            Assert.That(xml.Descendants().Count(element => (string?)element.Attribute("name") == name), Is.EqualTo(1), name);
        }
    }

    private static PackageDefinition BalticAsuwPackage() =>
        new(
            "pkg-asuw-1",
            "Baltic ASuW Package",
            new[]
            {
                new PackageElementDefinition("elem-sensor-1", "u1", C2NodeRole.Sensor, "package-track-feed"),
                new PackageElementDefinition("elem-shooter-1", "u2", C2NodeRole.Shooter, "package-engage"),
                new PackageElementDefinition("elem-relay-1", "u3", C2NodeRole.Relay, "package-relay"),
                new PackageElementDefinition("elem-c2-1", "u1", C2NodeRole.C2, "organic-c2"),
            });
}
