using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>S40-03: Catalog/Import provenance + quarantine read-model surfacing (projection-side only).</summary>
[TestFixture]
public sealed class CatalogImportSurfacingProjectionTests
{
    [Test]
    public void Provenance_line_includes_batch_source_and_review_fields()
    {
        var binding = new CatalogSensorBinding(
            "u1",
            "radar-1",
            0.85,
            SourceFactId: "fact-42",
            Confidence: 0.9,
            ImportBatchId: "batch-2026",
            SourceFile: "sensors_baltic.json",
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: 7);

        var line = CatalogImportProvenanceProjection.FormatBinding(binding);

        Assert.That(line, Does.Contain("u1/radar-1"));
        Assert.That(line, Does.Contain("batch=batch-2026"));
        Assert.That(line, Does.Contain("file=sensors_baltic.json"));
        Assert.That(line, Does.Contain("source=fact-42"));
        Assert.That(line, Does.Contain("review=approved"));
        Assert.That(line, Does.Contain("trl=7"));
    }

    [Test]
    public void Quarantine_row_surfaces_rejection_reason_from_import_gate()
    {
        var binding = new CatalogSensorBinding(
            "u9",
            "radar-low",
            0.2,
            Confidence: 0.2,
            ReviewState: "pending",
            TrlLevel: 2,
            ImportBatchId: "sample-batch");
        var quarantined = new QuarantinedCatalogBinding(binding, "confidence_below_minimum");

        var line = CatalogImportQuarantineProjection.FormatRow(quarantined);

        Assert.That(line, Does.StartWith("QUARANTINE u9/radar-low"));
        Assert.That(line, Does.Contain("reason=confidence_below_minimum"));
        Assert.That(line, Does.Contain("review=pending"));
    }

    [Test]
    public void Bind_partition_reports_quarantine_counts_for_editor_panel()
    {
        var bindings = new[]
        {
            new CatalogSensorBinding("u1", "ok", 1.0, Confidence: 1.0, TrlLevel: 9, ReviewState: CatalogReviewStates.Approved),
            new CatalogSensorBinding("u2", "bad", 0.1, Confidence: 0.1, TrlLevel: 9, ReviewState: CatalogReviewStates.Approved),
        };

        var panel = CatalogImportQuarantineProjection.BindPartition(bindings);

        Assert.That(panel.ApprovedCount, Is.EqualTo(1));
        Assert.That(panel.QuarantinedCount, Is.EqualTo(1));
        Assert.That(panel.HasQuarantine, Is.True);
        Assert.That(panel.StatusLine, Does.Contain("1 approved, 1 quarantined"));
        Assert.That(panel.QuarantineLines, Has.Count.EqualTo(1));
    }

    [Test]
    public void For_platform_filters_provenance_lines_in_stable_order()
    {
        var bindings = new[]
        {
            new CatalogSensorBinding("u1", "b", 1.0),
            new CatalogSensorBinding("u1", "a", 1.0),
            new CatalogSensorBinding("u2", "a", 1.0),
        };

        var lines = CatalogImportProvenanceProjection.ForPlatform(bindings, "u1");

        Assert.That(lines, Has.Count.EqualTo(2));
        Assert.That(lines[0], Does.Contain("u1/a"));
        Assert.That(lines[1], Does.Contain("u1/b"));
    }

    [Test]
    public void Projection_types_do_not_reference_write_gate()
    {
        foreach (var type in new[]
                 {
                     typeof(CatalogImportProvenanceProjection),
                     typeof(CatalogImportQuarantineProjection),
                     typeof(MountLoadoutQuarantineProjection),
                 })
        {
            var source = type.Assembly.GetName().Name;
            Assert.That(source, Is.EqualTo("ProjectAegis.Delegation"));
            Assert.That(type.FullName, Does.Not.Contain("WriteGate"));
        }
    }
}
