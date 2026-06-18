using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S28-04 Phase D: export→edit→propose→approve round-trip on Baltic fixture via
/// <see cref="PlatformWorkbookWriteService"/> and <see cref="CatalogWriteGate"/> (extend-only).
/// </summary>
[Collection("CatalogSqlite")]
public sealed class PlatformWorkbookPhaseDWriteTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private readonly PlatformWorkbookWriteService _writeService = new();

    [Fact]
    public void Propose_unedited_Baltic_round_trip_stages_nothing_empty_diff_golden()
    {
        var dbPath = CreateTempDbPath("phase-d-empty-diff");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9700));

            var result = _writeService.Propose(
                dbPath,
                exported,
                new FixedCatalogClock(9701),
                "human",
                "drgamtd",
                "phase d empty diff");

            Assert.True(result.Import.Plan.SnapshotResolved);
            Assert.False(result.Import.Plan.HasChanges);
            Assert.False(result.Proposed);
            Assert.Empty(result.BatchIds);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void E2E_sensor_export_edit_propose_approve_readback_via_write_service()
    {
        var dbPath = CreateTempDbPath("phase-d-sensor-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9710));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.55");

            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9711),
                "human",
                "drgamtd",
                "phase d sensor edit");
            Assert.True(propose.Proposed);
            var batchId = Assert.Single(propose.BatchIds);

            var approve = _writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9712),
                "human",
                "qa-reviewer");
            Assert.True(approve.AllCommitted);
            Assert.Equal(batchId, Assert.Single(approve.CommittedBatchIds));

            using var reader = new SqliteCatalogReader(dbPath, "phase-d-sensor-readback");
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
            Assert.Equal(0.55, basePd, precision: 6);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Reject_batch_discards_staging_without_live_commit()
    {
        var dbPath = CreateTempDbPath("phase-d-reject");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var readerBefore = new SqliteCatalogReader(dbPath, "phase-d-reject-before");
            Assert.True(readerBefore.TryGetBasePd("u1", "radar-1", out var originalBasePd));

            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9720));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.33");

            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9721),
                "human",
                "drgamtd",
                "phase d reject");
            var batchId = Assert.Single(propose.BatchIds);

            var reject = _writeService.RejectBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9722),
                "human",
                "qa-reviewer",
                "reject phase d batch");
            Assert.False(reject.AllCommitted);
            Assert.Empty(reject.CommittedBatchIds);
            Assert.Equal(batchId, Assert.Single(reject.ProcessedBatchIds));

            using var readerAfter = new SqliteCatalogReader(dbPath, "phase-d-reject-after");
            Assert.True(readerAfter.TryGetBasePd("u1", "radar-1", out var unchangedBasePd));
            Assert.Equal(originalBasePd, unchangedBasePd, precision: 6);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountRows(connection, "catalog_staging_sensor"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void E2E_multi_entity_propose_orders_batches_deterministically()
    {
        var dbPath = CreateTempDbPath("phase-d-multi");
        try
        {
            SeedPhaseAAndBDatabase(dbPath);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9730));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.61");
            edited = WithSheetCell(edited, "Mobility", 0, "MaxSpeedKnots", "34");

            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9731),
                "human",
                "drgamtd",
                "phase d multi entity");
            Assert.True(propose.Proposed);
            Assert.Equal(2, propose.BatchIds.Count);
            Assert.NotNull(propose.Import.SensorBatchId);
            Assert.NotNull(propose.Import.MobilityBatchId);

            var approve = _writeService.ApproveBatches(
                dbPath,
                propose.BatchIds,
                new FixedCatalogClock(9732),
                "human",
                "qa-reviewer");
            Assert.True(approve.AllCommitted);

            using var reader = new SqliteCatalogReader(dbPath, "phase-d-multi-readback");
            Assert.True(reader.TryGetBasePd("u1", "radar-1", out var basePd));
            Assert.Equal(0.61, basePd, precision: 6);
            Assert.True(reader.TryGetMobility("u1", out var mobility));
            Assert.Equal(34, mobility.MaxSpeedKnots, precision: 3);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void SeedPhaseAAndBDatabase(string dbPath)
    {
        CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mobility
                (platform_id, max_speed_knots, cruise_speed_knots, range_nm, review_state, trl_level, value_tier, citation_ref)
            VALUES ('u1', 32, 18, 4200, 'approved', 9, 'interpreted_value', 'unit-test');
            """;
        cmd.ExecuteNonQuery();
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

    private static int CountRows(SqliteConnection connection, string table)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}