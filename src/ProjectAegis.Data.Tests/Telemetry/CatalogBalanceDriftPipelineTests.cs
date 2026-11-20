using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Telemetry;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Telemetry;

/// <summary>S29-10: balance drift advisory on catalog import/approve diff paths.</summary>
[Runner.CatalogSqliteCollection]
public sealed class CatalogBalanceDriftPipelineTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static BalanceDriftOptions FastOptions => new()
    {
        MinimumSampleRuns = 100,
        WinRateDriftThreshold = 0.08,
        DefaultExpectedWinRate = 0.5,
    };

    [Test]
    public void Propose_diff_default_disabled_emits_empty_advisory()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-default");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var writeService = new PlatformWorkbookWriteService();
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9800));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.55");


            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9801),
                "human",
                "balance-pipeline");

            Assert.That(result.Proposed, Is.True);
            Assert.That(result.BalanceDriftAdvisory.DriftDetectionEnabled, Is.False);
            Assert.That(result.BalanceDriftAdvisory.Findings, Is.Empty);
            Assert.That(                result.Import.Notes.Any(                note => note.StartsWith("balance_drift_advisory:", StringComparison.Ordinal)), Is.False);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.55");


            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9811),
                "human",
                "balance-pipeline");

            var finding = AssertEx.Single(result.BalanceDriftAdvisory.Findings);
            Assert.That(finding.Code, Is.EqualTo("BALANCE_WIN_RATE_DRIFT"));
            Assert.That(finding.EntityId, Is.EqualTo("u1"));
            Assert.That(result.Import.Notes.Any(note => note.Contains("u1", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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

            Assert.That(result.Proposed, Is.False);
            Assert.That(result.BalanceDriftAdvisory.DriftDetectionEnabled, Is.True);
            Assert.That(result.BalanceDriftAdvisory.Findings, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Approve_diff_surfaces_drift_advisory_when_enabled()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-approve");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9830));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.57");


            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9831),
                "human",
                "balance-pipeline");
            var batchId = AssertEx.Single(propose.BatchIds);

            var approve = writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9832),
                "human",
                "qa-reviewer");

            Assert.That(approve.AllCommitted, Is.True);
            var finding = AssertEx.Single(approve.BalanceDriftAdvisory.Findings);
            Assert.That(finding.EntityId, Is.EqualTo("u1"));
            Assert.That(finding.Code, Is.EqualTo("BALANCE_WIN_RATE_DRIFT"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Approve_diff_advisory_does_not_block_write_gate_commit()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-no-bypass");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 70, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9840));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.59");


            var propose = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9841),
                "human",
                "balance-pipeline");
            var batchId = AssertEx.Single(propose.BatchIds);

            var approve = writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9842),
                "human",
                "qa-reviewer");

            Assert.That(approve.AllCommitted, Is.True);
            Assert.That(approve.BalanceDriftAdvisory.Findings, Is.Not.Empty);

            using var reader = new SqliteCatalogReader(dbPath, "balance-pipeline-readback");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.59).Within(0.000005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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

            Assert.That(proposed.ParsedCount, Is.EqualTo(1));
            var finding = AssertEx.Single(proposed.BalanceDriftAdvisory.Findings);
            Assert.That(finding.EntityId, Is.EqualTo(firstPlatformId));
            Assert.That(finding.Code, Is.EqualTo("BALANCE_WIN_RATE_DRIFT"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Drift_at_exactly_eight_percent_band_emits_no_pipeline_advisory()
    {
        var dbPath = CreateTempDbPath("balance-pipeline-threshold");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var sink = SeedDriftSink("u1", wins: 58, total: 100);
            var writeService = new PlatformWorkbookWriteService(EnabledSettings(sink));
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9860));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.52");


            var result = writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9861),
                "human",
                "balance-pipeline");

            Assert.That(result.BalanceDriftAdvisory.DriftDetectionEnabled, Is.True);
            Assert.That(result.BalanceDriftAdvisory.Findings, Is.Empty);
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

    private static int FindSheetRow(
        PlatformWorkbook workbook,
        string sheetName,
        string keyColumn,
        string keyValue)
    {
        var sheet = AssertEx.Single(workbook.Sheets, s => string.Equals(s.Name, sheetName, StringComparison.Ordinal));
        var colIndex = Array.IndexOf(sheet.Header.ToArray(), keyColumn);
        Assert.That(colIndex >= 0, Is.True, $"Column '{keyColumn}' not found on sheet '{sheetName}'.");
        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            var row = sheet.Rows[i];
            if (row.Count > colIndex &&
                string.Equals(row[colIndex], keyValue, StringComparison.Ordinal))
            {
                return i;
            }
        }

        throw new InvalidOperationException(
            $"Row with {keyColumn}='{keyValue}' not found on sheet '{sheetName}'.");
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
            Assert.That(colIndex >= 0, Is.True, $"Column '{columnName}' not found on sheet '{sheetName}'.");

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
                return cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return new PlatformWorkbook(sheets);
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