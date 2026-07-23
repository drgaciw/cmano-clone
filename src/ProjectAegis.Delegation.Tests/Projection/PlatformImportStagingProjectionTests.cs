using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.Validation;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>PE-UX-W1: Section/DiffKind/UssClass + section filter for Platform Import staging.</summary>
[TestFixture]
public sealed class PlatformImportStagingProjectionTests
{
    [Test]
    public void BuildDiffRows_damage_cell_change_sets_section_and_changed_kind()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Platforms", PlatformWorkbookChangeKind.CellChanged, 0, "MaxHp: '100' -> '85'"),
        };

        var rows = PlatformImportStagingProjection.BuildDiffRows(changes);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Section, Is.EqualTo(PlatformImportStagingSection.Damage));
        Assert.That(rows[0].DiffKind, Is.EqualTo(PlatformImportStagingDiffKind.Changed));
        Assert.That(rows[0].UssClass, Is.EqualTo("platform-import-diff-row--changed"));
        Assert.That(rows[0].SummaryLine, Does.Contain("DAMAGE"));
    }

    [Test]
    public void BuildDiffRows_comms_row_added_sets_added_kind()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Comms", PlatformWorkbookChangeKind.RowAdded, 1, "u1|LINK_NATO|Tx|true"),
        };

        var rows = PlatformImportStagingProjection.BuildDiffRows(changes);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Section, Is.EqualTo(PlatformImportStagingSection.Comms));
        Assert.That(rows[0].DiffKind, Is.EqualTo(PlatformImportStagingDiffKind.Added));
        Assert.That(rows[0].UssClass, Is.EqualTo("platform-import-diff-row--added"));
    }

    [Test]
    public void BuildDiffRows_link_row_removed_sets_removed_kind()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("LinkCatalog", PlatformWorkbookChangeKind.RowRemoved, 2, "LINK_OLD"),
        };

        var rows = PlatformImportStagingProjection.BuildDiffRows(changes);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].Section, Is.EqualTo(PlatformImportStagingSection.Link));
        Assert.That(rows[0].DiffKind, Is.EqualTo(PlatformImportStagingDiffKind.Removed));
        Assert.That(rows[0].UssClass, Is.EqualTo("platform-import-diff-row--removed"));
    }

    [Test]
    public void FilterBySection_damage_excludes_comms_and_other()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Platforms", PlatformWorkbookChangeKind.CellChanged, 0, "MaxHp: '100' -> '85'"),
            new PlatformWorkbookChange("Comms", PlatformWorkbookChangeKind.CellChanged, 0, "LinkId: 'A' -> 'B'"),
            new PlatformWorkbookChange("Sensors", PlatformWorkbookChangeKind.CellChanged, 0, "BasePd: '0.8' -> '0.7'"),
        };

        var all = PlatformImportStagingProjection.BuildDiffRows(changes);
        var damageOnly = PlatformImportStagingProjection.FilterBySection(all, PlatformImportStagingSection.Damage);

        Assert.That(all, Has.Count.EqualTo(3));
        Assert.That(damageOnly, Has.Count.EqualTo(1));
        Assert.That(damageOnly[0].Section, Is.EqualTo(PlatformImportStagingSection.Damage));
    }

    [Test]
    public void FilterBySection_null_returns_all_rows()
    {
        var changes = new[]
        {
            new PlatformWorkbookChange("Platforms", PlatformWorkbookChangeKind.CellChanged, 0, "MaxHp: '100' -> '85'"),
            new PlatformWorkbookChange("Sensors", PlatformWorkbookChangeKind.CellChanged, 0, "BasePd: '0.8' -> '0.7'"),
        };

        var all = PlatformImportStagingProjection.BuildDiffRows(changes);
        var filtered = PlatformImportStagingProjection.FilterBySection(all, section: null);

        Assert.That(filtered.Count, Is.EqualTo(all.Count));
    }

    [Test]
    public void Bind_blocked_plan_sets_IsBlocked()
    {
        var findings = new[]
        {
            new ValidationFinding("LINK_FK", ValidationSeverity.Error, "unknown LinkId"),
        };
        var changes = new[]
        {
            new PlatformWorkbookChange("Comms", PlatformWorkbookChangeKind.CellChanged, 0, "LinkId: 'bad' -> 'worse'"),
        };
        var plan = new PlatformImportPlan(
            SourceSnapshotId: "snap-test",
            SnapshotResolved: true,
            Changes: changes,
            Findings: findings,
            SupportedChanges: changes,
            UnsupportedChanges: Array.Empty<PlatformWorkbookChange>(),
            RequiresHumanApproval: true);
        var import = new PlatformImportResult(
            plan,
            Staged: false,
            SensorBatchId: null,
            MountBatchId: null,
            LoadoutBatchId: null,
            MagazineBatchId: null,
            CommsBatchId: null,
            LinkBatchId: null,
            MobilityBatchId: null,
            SignatureBatchId: null,
            EmconBatchId: null,
            DamageBatchId: null,
            Notes: Array.Empty<string>());
        var propose = PlatformWorkbookWriteResult.FromImport(import, BalanceDriftReport.EmptyDisabled);

        var panel = PlatformImportStagingProjection.Bind(propose, reviewAcknowledged: false);

        Assert.That(panel.IsBlocked, Is.True);
        Assert.That(panel.ApproveEnabled, Is.False);
        Assert.That(panel.StatusLine, Does.Contain("blocked"));
        Assert.That(panel.DiffRows, Is.Not.Empty);
        Assert.That(panel.DiffRows[0].DiffKind, Is.EqualTo(PlatformImportStagingDiffKind.Blocked));
        Assert.That(panel.DiffRows[0].UssClass, Is.EqualTo("platform-import-diff-row--blocked"));
    }

    [Test]
    public void UssClassFor_maps_diff_kinds()
    {
        Assert.That(
            PlatformImportStagingProjection.UssClassFor(PlatformImportStagingDiffKind.Added),
            Is.EqualTo("platform-import-diff-row--added"));
        Assert.That(
            PlatformImportStagingProjection.UssClassFor(PlatformImportStagingDiffKind.Changed),
            Is.EqualTo("platform-import-diff-row--changed"));
        Assert.That(
            PlatformImportStagingProjection.UssClassFor(PlatformImportStagingDiffKind.Removed),
            Is.EqualTo("platform-import-diff-row--removed"));
        Assert.That(
            PlatformImportStagingProjection.UssClassFor(PlatformImportStagingDiffKind.Blocked),
            Is.EqualTo("platform-import-diff-row--blocked"));
        Assert.That(
            PlatformImportStagingProjection.UssClassFor(PlatformImportStagingDiffKind.Info),
            Is.EqualTo("platform-import-diff-row--info"));
    }

    [Test]
    public void TextTagFor_and_FormatDisplayLine_prefix_non_color_indicator()
    {
        Assert.That(
            PlatformImportStagingProjection.TextTagFor(PlatformImportStagingDiffKind.Added),
            Is.EqualTo("ADDED"));
        Assert.That(
            PlatformImportStagingProjection.TextTagFor(PlatformImportStagingDiffKind.Blocked),
            Is.EqualTo("BLOCKED"));

        var row = new PlatformImportStagingRow(
            "Comms",
            1,
            "COMMS · +1",
            PlatformImportStagingSection.Comms,
            PlatformImportStagingDiffKind.Added,
            "platform-import-diff-row--added");

        Assert.That(
            PlatformImportStagingProjection.FormatDisplayLine(row),
            Is.EqualTo("[ADDED] COMMS · +1"));
    }
}
