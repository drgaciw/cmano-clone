using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.WriteGate;

[Collection("CatalogSqlite")]
public sealed class CatalogWriteGateTests
{
    [Fact]
    public void Propose_approve_writes_sensor_and_change_log()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-gate-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(42)))
            {
                var proposal = new CatalogSensorBinding(
                    "u-test",
                    "radar-test",
                    0.66,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9,
                    ValueTier: CatalogProvenanceTier.InterpretedValue,
                    ReviewerId: "test-reviewer",
                    CitationRef: "unit-test");
                var batchId = gate.ProposeSensorBatch([proposal], "agent", "test-agent");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "gate-test");
            Assert.True(reader.TryGetBasePd("u-test", "radar-test", out var basePd));
            Assert.Equal(0.66, basePd);
            var binding = reader.GetSortedSensorBindings().First(b => b.SensorId == "radar-test");
            Assert.Equal(CatalogProvenanceTier.InterpretedValue, binding.ValueTier);
            Assert.Equal("test-reviewer", binding.ReviewerId);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM catalog_change_log WHERE batch_id LIKE 'batch-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.True(count >= 1);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void Reject_batch_discards_staging_without_commit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-reject-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(1)))
            {
                var batchId = gate.ProposeSensorBatch(
                    [new CatalogSensorBinding("u9", "radar-z", 0.4, ReviewState: CatalogReviewStates.Approved)],
                    "agent",
                    "reject-test");
                var decision = gate.RejectBatch(batchId, "human", "qa");
                Assert.False(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "reject-test");
            Assert.False(reader.TryGetBasePd("u9", "radar-z", out _));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void ApproveBatch_AfterPropose_RecordsStableSnapshotHash()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-snapshot-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(1000)))
            {
                var proposal = new CatalogSensorBinding(
                    "u-snap",
                    "radar-snap",
                    0.77,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 8,
                    ValueTier: CatalogProvenanceTier.InterpretedValue,
                    ReviewerId: "s18-plan",
                    CitationRef: "phase2-test");
                var batchId = gate.ProposeSensorBatch([proposal], "agent", "s18-catalog-p2");
                var decision = gate.ApproveBatch(batchId, "human", "s18-reviewer");
                Assert.True(decision.Committed);

                var store = new DbSnapshotStore(dbPath);
                // TDD red: RecordApprovedImport does not exist yet on DbSnapshotStore (and may need DbSnapshotRecord type)
                var proposedIds = new[] { "radar-snap" }; // from the proposal we just approved
                var snap = store.RecordApprovedImport(proposedIds, sourceFile: "sensor.md:phase2", importBatchId: batchId);
                Assert.NotNull(snap);
                Assert.False(string.IsNullOrEmpty(snap.ContentHash));

                var snap2 = store.RecordApprovedImport(proposedIds, sourceFile: "sensor.md:phase2", importBatchId: batchId);
                Assert.Equal(snap.ContentHash, snap2.ContentHash); // stable across calls

                // P2-3: snapshot visible in store list (proves write)
                var snaps = store.GetSortedSnapshotIds();
                Assert.Contains(snap.Id, snaps);

                // Also verify reader sees committed rows (P2-2)
                using var reader = new SqliteCatalogReader(dbPath, "s18-snap-test");
                Assert.True(reader.TryGetBasePd("u-snap", "radar-snap", out var pd));
                Assert.Equal(0.77, pd);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
            catch (IOException) { /* lock from prior test or sqlite; ok for test env */ }
        }
    }
}