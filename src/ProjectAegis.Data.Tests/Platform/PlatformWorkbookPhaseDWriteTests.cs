using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S28-04 Phase D: export→edit→propose→approve round-trip on Baltic fixture via
/// <see cref="PlatformWorkbookWriteService"/> and <see cref="CatalogWriteGate"/> (extend-only).
/// </summary>
[Runner.CatalogSqliteCollection]
public sealed class PlatformWorkbookPhaseDWriteTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private readonly PlatformWorkbookWriteService _writeService = new();

    [Test]
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

            Assert.That(result.Import.Plan.SnapshotResolved, Is.True);
            Assert.That(result.Import.Plan.HasChanges, Is.False);
            Assert.That(result.Proposed, Is.False);
            Assert.That(result.BatchIds, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void E2E_sensor_export_edit_propose_approve_readback_via_write_service()
    {
        var dbPath = CreateTempDbPath("phase-d-sensor-e2e");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9710));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.55");


            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9711),
                "human",
                "drgamtd",
                "phase d sensor edit");
            Assert.That(propose.Proposed, Is.True);
            var batchId = AssertEx.Single(propose.BatchIds);

            var approve = _writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9712),
                "human",
                "qa-reviewer");
            Assert.That(approve.AllCommitted, Is.True);
            Assert.That(AssertEx.Single(approve.CommittedBatchIds), Is.EqualTo(batchId));

            using var reader = new SqliteCatalogReader(dbPath, "phase-d-sensor-readback");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.55).Within(0.000005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Reject_batch_discards_staging_without_live_commit()
    {
        var dbPath = CreateTempDbPath("phase-d-reject");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var readerBefore = new SqliteCatalogReader(dbPath, "phase-d-reject-before");
            Assert.That(readerBefore.TryGetBasePd("u1", "radar-1", out var originalBasePd), Is.True);

            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9720));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.33");


            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9721),
                "human",
                "drgamtd",
                "phase d reject");
            var batchId = AssertEx.Single(propose.BatchIds);

            var reject = _writeService.RejectBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9722),
                "human",
                "qa-reviewer",
                "reject phase d batch");
            Assert.That(reject.AllCommitted, Is.False);
            Assert.That(reject.CommittedBatchIds, Is.Empty);
            Assert.That(AssertEx.Single(reject.ProcessedBatchIds), Is.EqualTo(batchId));

            using var readerAfter = new SqliteCatalogReader(dbPath, "phase-d-reject-after");
            Assert.That(readerAfter.TryGetBasePd("u1", "radar-1", out var unchangedBasePd), Is.True);
            Assert.That(unchangedBasePd, Is.EqualTo(originalBasePd).Within(0.000005));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountRows(connection, "catalog_staging_sensor"), Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void E2E_multi_entity_propose_orders_batches_deterministically()
    {
        var dbPath = CreateTempDbPath("phase-d-multi");
        try
        {
            SeedPhaseAAndBDatabase(dbPath);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9730));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.61");

            edited = WithSheetCell(edited, "Mobility", 0, "MaxSpeedKnots", "34");

            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9731),
                "human",
                "drgamtd",
                "phase d multi entity");
            Assert.That(propose.Proposed, Is.True);
            Assert.That(propose.BatchIds.Count, Is.EqualTo(2));
            Assert.That(propose.Import.SensorBatchId, Is.Not.Null);
            Assert.That(propose.Import.MobilityBatchId, Is.Not.Null);

            var approve = _writeService.ApproveBatches(
                dbPath,
                propose.BatchIds,
                new FixedCatalogClock(9732),
                "human",
                "qa-reviewer");
            Assert.That(approve.AllCommitted, Is.True);

            using var reader = new SqliteCatalogReader(dbPath, "phase-d-multi-readback");
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.61).Within(0.000005));
            Assert.That(reader.TryGetMobility("u1", out var mobility), Is.True);
            Assert.That(mobility.MaxSpeedKnots, Is.EqualTo(34).Within(0.005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    /// <summary>
    /// PLE-3.5: successful Excel-path ApproveBatches records immutable db_release / snapshot via
    /// existing CatalogSnapshotBinder + DbSnapshotStore (single release train).
    /// </summary>
    [Test]
    public void ApproveBatches_records_db_release_and_snapshot_PLE_3_5()
    {
        var dbPath = CreateTempDbPath("phase-d-release-golden");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = _writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9740));
            var edited = WithSheetCell(exported, "Sensors", FindSheetRow(exported, "Sensors", "SensorId", "radar-1"), "BasePd", "0.57");


            var propose = _writeService.Propose(
                dbPath,
                edited,
                new FixedCatalogClock(9741),
                "human",
                "drgamtd",
                "phase d release golden");
            Assert.That(propose.Proposed, Is.True);
            var batchId = AssertEx.Single(propose.BatchIds);

            var approve = _writeService.ApproveBatches(
                dbPath,
                [batchId],
                new FixedCatalogClock(9742),
                "human",
                "qa-reviewer");
            Assert.That(approve.AllCommitted, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(approve.ReleaseVersion), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(approve.SnapshotId), Is.False);
            Assert.That(approve.ContentHashSha256!, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(approve.ReleaseVersion, Does.StartWith("platform-workbook-"));

            using var store = new DbSnapshotStore(dbPath);
            Assert.That(store.TryGetContentHash(approve.SnapshotId!, out var storedHash), Is.True);
            Assert.That(storedHash, Is.EqualTo(approve.ContentHashSha256));

            var releases = store.GetSortedReleases();
            Assert.That(releases.Any(r =>
                string.Equals(r.ReleaseVersion, approve.ReleaseVersion, StringComparison.Ordinal)
                && string.Equals(r.SnapshotId, approve.SnapshotId, StringComparison.Ordinal)), Is.True);
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