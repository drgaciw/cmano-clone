using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S31-09: balance drift advisory on nightly approve summary (default off).</summary>
[Collection("CatalogSqlite")]
public sealed class NightlyApproveBalanceDriftSummaryTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static BalanceDriftOptions FastOptions => new()
    {
        MinimumSampleRuns = 100,
        WinRateDriftThreshold = 0.08,
        DefaultExpectedWinRate = 0.5,
    };

    [Fact]
    public void Nightly_approve_summary_default_disabled_omits_advisory()
    {
        var dbPath = CreateTempDbPath("nightly-balance-default");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var writeService = new PlatformWorkbookWriteService();
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(31100));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.55");

            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(31101),
                "human",
                "nightly-balance");
            var batchId = Assert.Single(propose.BatchIds);

            var advisory = NightlyApproveBalanceDriftSummary.EvaluateForBatch(
                dbPath,
                batchId,
                CatalogBalanceDriftPipelineSettings.Disabled);
            Assert.False(advisory.DriftDetectionEnabled);
            Assert.Null(NightlyApproveBalanceDriftSummary.ToDtoOrNull(advisory));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_approve_summary_surfaces_drift_advisory_when_enabled_beyond_eight_percent()
    {
        var dbPath = CreateTempDbPath("nightly-balance-enabled");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var settings = EnabledSettings(sink);
            var writeService = new PlatformWorkbookWriteService(settings);
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(31110));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.57");

            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(31111),
                "human",
                "nightly-balance");
            var batchId = Assert.Single(propose.BatchIds);

            var advisory = NightlyApproveBalanceDriftSummary.EvaluateForBatch(
                dbPath,
                batchId,
                settings,
                new FixedCatalogClock(31112));
            var dto = NightlyApproveBalanceDriftSummary.ToDto(advisory);
            var finding = Assert.Single(dto.Findings);
            Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
            Assert.Equal("u1", finding.EntityId);
            Assert.Contains(dto.AdvisoryNotes, note => note.Contains("u1", StringComparison.Ordinal));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(31113)))
            {
                Assert.True(gate.ApproveBatch(batchId, "human", "nightly-balance").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "nightly-balance-readback");
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
            Assert.Equal(0.57, basePd, precision: 6);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_approve_multi_batch_summary_aggregates_entity_advisory_when_enabled()
    {
        var dbPath = CreateTempDbPath("nightly-balance-multi");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var settings = EnabledSettings(sink);
            var writeService = new PlatformWorkbookWriteService(settings);
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(31120));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.58");

            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(31121),
                "human",
                "nightly-balance");
            var batchId = Assert.Single(propose.BatchIds);

            var advisory = NightlyApproveBalanceDriftSummary.EvaluateForBatches(
                dbPath,
                [batchId],
                settings,
                new FixedCatalogClock(31122));
            var dto = NightlyApproveBalanceDriftSummary.ToDto(advisory);

            Assert.True(dto.DriftDetectionEnabled);
            var finding = Assert.Single(dto.Findings);
            Assert.Equal("u1", finding.EntityId);
            Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
            Assert.NotEmpty(dto.AdvisoryNotes);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_approve_enabled_empty_diff_emits_no_findings()
    {
        var dbPath = CreateTempDbPath("nightly-balance-empty");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var settings = EnabledSettings(sink);
            var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
            var proposed = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 1,
                chunkSize: 500,
                clock: new FixedCatalogClock(31130),
                balanceDrift: settings);

            var batchId = proposed.Batches[0].BatchId;
            var advisory = NightlyApproveBalanceDriftSummary.EvaluateForBatch(
                dbPath,
                batchId,
                settings,
                new FixedCatalogClock(31131));

            Assert.True(advisory.DriftDetectionEnabled);
            Assert.Empty(advisory.Findings);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Nightly_sensor_slice_advisory_does_not_block_write_gate_commit()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var dbPath = CreateTempDbPath("nightly-balance-sensor");
        try
        {
            var sink = SeedDriftSink("platform-1", wins: 70, total: 100);
            var settings = EnabledSettings(sink);
            var proposed = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 3,
                chunkSize: 500,
                clock: new FixedCatalogClock(31140),
                balanceDrift: settings);

            var batchId = proposed.Batches[0].BatchId;
            var advisory = NightlyApproveBalanceDriftSummary.EvaluateForBatch(
                dbPath,
                batchId,
                settings,
                new FixedCatalogClock(31141));
            Assert.True(advisory.DriftDetectionEnabled);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(31142)))
            {
                Assert.True(gate.ApproveBatch(batchId, "human", "nightly-curator").Committed);
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static CatalogBalanceDriftPipelineSettings EnabledSettings(IBalanceTelemetrySink sink) =>
        new(enableBalanceDrift: true, options: FastOptions, telemetrySink: sink);

    private static BalanceTelemetryAccumulator SeedDriftSink(string entityId, int wins, int total)
    {
        var sink = new BalanceTelemetryAccumulator(FastOptions);
        for (var i = 0; i < total; i++)
        {
            sink.RecordOutcome(entityId, BalanceEntityKind.Platform, won: i < wins);
        }

        return sink;
    }

    private static PlatformWorkbook WithSheetCell(
        PlatformWorkbook workbook,
        string sheetName,
        int rowIndex,
        string columnName,
        string value)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, sheetName, StringComparison.Ordinal))
            {
                return sheet;
            }

            var colIndex = Array.IndexOf(sheet.Header.ToArray(), columnName);
            Assert.True(colIndex >= 0, $"Column '{columnName}' not found on sheet '{sheetName}'.");

            var rows = sheet.Rows.Select((row, i) =>
            {
                if (i != rowIndex)
                {
                    return row;
                }

                var cells = row.ToList();
                while (cells.Count <= colIndex)
                {
                    cells.Add(string.Empty);
                }

                cells[colIndex] = value;
                return (IReadOnlyList<string>)cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return workbook with { Sheets = sheets };
    }

    private static string CreateTempDbPath(string label) =>
        Path.Combine(Path.GetTempPath(), $"aegis-{label}-{Guid.NewGuid():N}.db");

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}