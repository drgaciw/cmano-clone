using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class MountLoadoutQuarantineProjectionTests
{
    [Test]
    public void Domain_summary_line_includes_mount_loadout_and_repair_counts()
    {
        var counts = new MountLoadoutDomainQuarantineCounts(
            MountLoadoutQuarantineDomain.Platform,
            MountQuarantined: 2,
            LoadoutQuarantined: 1,
            FittingQuarantined: 0,
            Repairable: 2,
            OutOfEnvelope: 1);

        var line = MountLoadoutQuarantineProjection.FormatDomainSummary(counts);

        Assert.That(line, Does.Contain("DOMAIN platform"));
        Assert.That(line, Does.Contain("mount=2"));
        Assert.That(line, Does.Contain("loadout=1"));
        Assert.That(line, Does.Contain("repairable=2"));
        Assert.That(line, Does.Contain("out_of_envelope=1"));
    }

    [Test]
    public void Quarantine_row_includes_reason_and_optional_repair_rule()
    {
        var row = new MountLoadoutQuarantineRow(
            MountLoadoutQuarantineDomain.Platform,
            "mount",
            "batch-1",
            "u-hull",
            "mount-1",
            MountLoadoutQuarantineRepairEnvelope.ReasonOrphanPlatform,
            RepairRule: null);

        var line = MountLoadoutQuarantineProjection.FormatQuarantineRow(row);

        Assert.That(line, Does.Contain("platform=u-hull"));
        Assert.That(line, Does.Contain("reason=orphan_platform"));
        Assert.That(line, Does.Contain("repair=none"));
    }

    [Test]
    public void Bind_from_triage_builds_editor_panel_state()
    {
        var before = new[]
        {
            new MountLoadoutDomainQuarantineCounts(
                MountLoadoutQuarantineDomain.Platform,
                1,
                1,
                0,
                2,
                0),
        };
        var remaining = new[]
        {
            new MountLoadoutQuarantineRow(
                MountLoadoutQuarantineDomain.Platform,
                "mount",
                "batch-m",
                "u1",
                "mount-1",
                "pending_fk",
                MountLoadoutQuarantineRepairEnvelope.RuleLivePlatformFk),
        };
        var result = new MountLoadoutQuarantineTriageResult(
            Ok: false,
            DryRun: true,
            DatabasePath: "/tmp/test.db",
            Before: before,
            After: before,
            RemainingQuarantine: remaining,
            RepairedBatchIds: [],
            AdvisoryNotes: ["Dry-run only — no WriteGate commits."]);

        var panel = MountLoadoutQuarantineProjection.BindFromTriage(result);

        Assert.That(panel.DryRun, Is.True);
        Assert.That(panel.TotalQuarantined, Is.EqualTo(2));
        Assert.That(panel.Repairable, Is.EqualTo(2));
        Assert.That(panel.DomainSummaryLines, Has.Count.EqualTo(1));
        Assert.That(panel.QuarantineDetailLines, Has.Count.EqualTo(1));
        Assert.That(panel.StatusLine, Does.Contain("dry-run triage"));
        Assert.That(panel.StatusLine, Does.Contain("notes=1"));
    }

    [Test]
    public void Bind_from_audit_reports_empty_state_when_no_quarantine()
    {
        var panel = MountLoadoutQuarantineProjection.BindFromAudit(Array.Empty<MountLoadoutDomainQuarantineCounts>());

        Assert.That(panel.TotalQuarantined, Is.EqualTo(0));
        Assert.That(panel.StatusLine, Does.Contain("no quarantined child rows"));
        Assert.That(panel.DomainSummaryLines, Is.Empty);
    }
}
