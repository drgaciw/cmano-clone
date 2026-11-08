using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.WriteGate;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S125-07 / AUTH-12: the editor shows which catalog DB ref / snapshot it is bound to.</summary>
[TestFixture]
public sealed class EditorCatalogBindingProjectionTests
{
    [Test]
    public void Scenario_binding_shows_dbref_snapshot_and_tl_branch()
    {
        var metadata = new ScenarioMetadataDto { DbRef = "baltic_patrol", DbSnapshotId = "snap-42", TlBranch = "TL-0" };

        var state = EditorCatalogBindingProjection.ForScenario(metadata);

        Assert.That(state.Surface, Is.EqualTo(EditorSurface.MissionEditor));
        Assert.That(state.IsBound, Is.True);
        Assert.That(state.IsMismatch, Is.False);
        Assert.That(state.DbRef, Is.EqualTo("baltic_patrol"));
        Assert.That(state.SnapshotId, Is.EqualTo("snap-42"));
        Assert.That(state.BindingLabel, Is.EqualTo("DB: baltic_patrol · snapshot snap-42 · TL TL-0"));
        Assert.That(state.UssClass, Does.Contain("editor-db-binding--bound"));
    }

    [Test]
    public void Scenario_without_dbref_is_visibly_unbound()
    {
        var state = EditorCatalogBindingProjection.ForScenario(new ScenarioMetadataDto { TlBranch = "TL-0" });

        Assert.That(state.IsBound, Is.False);
        Assert.That(state.BindingLabel, Is.EqualTo("DB: unbound"));
        Assert.That(state.UssClass, Does.Contain("--unbound"));
    }

    [Test]
    public void Scenario_dbref_is_resolved_through_catalog()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assume.That(catalog.TryResolveDbRef("baltic_patrol", out var expected), Is.True);

        var state = EditorCatalogBindingProjection.ForScenario(
            ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol").Metadata,
            catalog);

        Assert.That(state.IsMismatch, Is.False);
        Assert.That(state.SnapshotId, Is.EqualTo(expected));
        Assert.That(state.DetailText, Does.Contain(catalog.LayerVersion));
    }

    [Test]
    public void Unresolvable_dbref_is_a_visible_mismatch()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assume.That(catalog.TryResolveDbRef("no-such-db", out _), Is.False);

        var state = EditorCatalogBindingProjection.ForScenario(new ScenarioMetadataDto { DbRef = "no-such-db" }, catalog);

        Assert.That(state.IsMismatch, Is.True);
        Assert.That(state.BindingLabel, Does.StartWith("DB MISMATCH"));
        Assert.That(state.DetailText, Does.Contain("DB_MISMATCH"));
        Assert.That(state.UssClass, Does.Contain("--mismatch"));
    }

    [Test]
    public void Workbook_binding_reads_meta_source_snapshot()
    {
        var workbook = new PlatformWorkbookExporter().Export(PlatformCatalogExportData.Empty, "snap-7", new FixedCatalogClock(0));

        var state = EditorCatalogBindingProjection.ForWorkbook(workbook);

        Assert.That(state.Surface, Is.EqualTo(EditorSurface.PlatformEditor));
        Assert.That(state.IsBound, Is.True);
        Assert.That(state.SnapshotId, Is.EqualTo("snap-7"));
        Assert.That(state.BindingLabel, Does.Contain("snapshot snap-7"));
    }

    [Test]
    public void Unresolved_import_snapshot_is_a_visible_mismatch()
    {
        var workbook = new PlatformWorkbookExporter().Export(PlatformCatalogExportData.Empty, "stale-snap", new FixedCatalogClock(0));
        var importer = new PlatformWorkbookImporter(_ => null, new FixedCatalogClock(0));

        var plan = importer.Plan(workbook);
        var state = EditorCatalogBindingProjection.ForImportPlan(plan, workbook);

        Assert.That(plan.SnapshotResolved, Is.False);
        Assert.That(state.IsMismatch, Is.True);
        Assert.That(state.SnapshotId, Is.EqualTo("stale-snap"));
        Assert.That(state.DetailText, Does.Contain("nothing will be staged"));
    }

    [Test]
    public void Workbook_without_meta_is_unbound()
    {
        var state = EditorCatalogBindingProjection.ForWorkbook(new PlatformWorkbook(Array.Empty<PlatformWorkbookSheet>()));

        Assert.That(state.IsBound, Is.False);
        Assert.That(state.DetailText, Does.Contain("SourceSnapshotId"));
    }
}
