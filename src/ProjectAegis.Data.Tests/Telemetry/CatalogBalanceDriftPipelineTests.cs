using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Telemetry;

/// <summary>S29-10: balance drift advisory on catalog import/approve diff paths.</summary>
[Collection("CatalogSqlite")]
public sealed class CatalogBalanceDriftPipelineTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static BalanceDriftOptions FastOptions => new()
    {
        MinimumSampleRuns = 100,
        WinRateDriftThreshold = 0.08,
        DefaultExpectedWinRate = 0.5,
    };

    [Fact]
    public void Propose_diff_default_disabled_emits_empty_advisory()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-default");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var writeService = new PlatformWorkbookWriteService();
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9800));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.55");

            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9801),
                "human",
                "balance-pipeline");

            Assert.True(result.Proposed);
            Assert.False(result.BalanceDriftAdvisory.DriftDetectionEnabled);
            Assert.Empty(result.BalanceDriftAdvisory.Findings);
            Assert.DoesNotContain(
                result.Import.Notes,
                note => note.StartsWith("balance_drift_advisory:", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Propose_diff_surfaces_drift_advisory_when_enabled_beyond_eight_percent()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-propose");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var settings = EnabledSettings(sink);
            var writeService = new PlatformWorkbookWriteService(settings);
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9810));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.55");

            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9811),
                "human",
                "balance-pipeline");

            var finding = Assert.Single(result.BalanceDriftAdvisory.Findings);
            Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
            Assert.Equal("u1", finding.EntityId);
            Assert.Contains(result.Import.Notes, note => note.Contains("u1", StringComparison.Ordinal));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Propose_empty_diff_emits_no_drift_findings_when_enabled()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-empty-diff");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9820));

            var result = writeService.Propose(
                dbPath,
                exported,
                new FixedCatalogClock(9821),
                "human",
                "balance-pipeline");

            Assert.False(result.Proposed);
            Assert.True(result.BalanceDriftAdvisory.DriftDetectionEnabled);
            Assert.Empty(result.BalanceDriftAdvisory.Findings);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Approve_diff_surfaces_drift_advisory_when_enabled()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-approve");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9830));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.57");

            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9831),
                "human",
                "balance-pipeline");
            var batchId = Assert.Single(propose.BatchIds);

            var approve = writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9832),
                "human",
                "qa-reviewer");

            Assert.True(approve.AllCommitted);
            var finding = Assert.Single(approve.BalanceDriftAdvisory.Findings);
            Assert.Equal("u1", finding.EntityId);
            Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Approve_diff_advisory_does_not_block_write_gate_commit()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-no-bypass");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9840));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.59");

            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9841),
                "human",
                "balance-pipeline");
            var batchId = Assert.Single(propose.BatchIds);

            var approve = writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9842),
                "human",
                "qa-reviewer");

            Assert.True(approve.AllCommitted);
            Assert.NotEmpty(approve.BalanceDriftAdvisory.Findings);

            using var reader = new SqliteCatalogReader(dbPath, "balance-pipeline-readback");
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
            Assert.Equal(0.59, basePd, precision: 6);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Import_propose_surfaces_drift_advisory_for_touched_platform()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var firstPlatformId = CmoMarkdownImporter
            .ReadPlatformBindings(platformPath)
            .Select(platform => platform.PlatformId)
            .First();
        var dbPath = CreateTempDbPath("balance-pipeline-import");
        try
        {
            var sink = SeedDriftSink(firstPlatformId, wins: 70, total: 100);
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                maxRecords: 1,
                chunkSize: 500,
                clock: new FixedCatalogClock(9850),
                balanceDrift: EnabledSettings(sink));

            Assert.Equal(1, proposed.ParsedCount);
            var finding = Assert.Single(proposed.BalanceDriftAdvisory.Findings);
            Assert.Equal(firstPlatformId, finding.EntityId);
            Assert.Equal("BALANCE_WIN_RATE_DRIFT", finding.Code);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Drift_at_exactly_eight_percent_band_emits_no_pipeline_advisory()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-threshold");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 58, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9860));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.52");

            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9861),
                "human",
                "balance-pipeline");

            Assert.True(result.BalanceDriftAdvisory.DriftDetectionEnabled);
            Assert.Empty(result.BalanceDriftAdvisory.Findings);
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