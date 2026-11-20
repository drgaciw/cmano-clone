using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.WriteGate;

[Runner.CatalogSqliteCollection]
public sealed class CatalogWriteGateTests
{
    [Test]
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
                Assert.That(decision.Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "gate-test");
            Assert.That(reader.TryGetBasePd("u-test", "radar-test", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.66));
            var binding = reader.GetSortedSensorBindings().First(b => b.SensorId == "radar-test");
            Assert.That(binding.ValueTier, Is.EqualTo(CatalogProvenanceTier.InterpretedValue));
            Assert.That(binding.ReviewerId, Is.EqualTo("test-reviewer"));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM catalog_change_log WHERE batch_id LIKE 'batch-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(count >= 1, Is.True);
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

    [Test]
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
                Assert.That(decision.Committed, Is.False);
            }

            using var reader = new SqliteCatalogReader(dbPath, "reject-test");
            Assert.That(reader.TryGetBasePd("u9", "radar-z", out _), Is.False);
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

    /// <summary>
    /// Adversarial: propose without approve must never surface staged sensors as live catalog reads.
    /// Doc 06 dual-track / write-gate contract (staging ≠ live).
    /// </summary>
    [Test]
    public void ProposeSensorBatch_without_ApproveBatch_not_readable_as_live_catalog()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-propose-only-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var staged = new CatalogSensorBinding(
                "u-staged-only",
                "radar-staged",
                0.88,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 7,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                ReviewerId: "agent",
                CitationRef: "no-human-approve");

            string batchId;
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(7)))
            {
                batchId = gate.ProposeSensorBatch([staged], "agent", "no-approve");
                var pending = gate.ListPendingBatches();
                Assert.That(pending.Any(p => p.BatchId == batchId && p.ApprovalState == "proposed"), Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "propose-only");
            Assert.That(reader.TryGetBasePd("u-staged-only", "radar-staged", out _), Is.False);
            Assert.That(                reader.GetSortedSensorBindings().Any(                b => b.PlatformId == "u-staged-only" && b.SensorId == "radar-staged"), Is.False);
            Assert.That(reader.TryGetBasePd("u1", "radar-1", out var pd), Is.True);
            Assert.That(pd, Is.EqualTo(1.0));
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

    [Test]
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
                Assert.That(decision.Committed, Is.True);

                var store = new DbSnapshotStore(dbPath);
                // TDD red: RecordApprovedImport does not exist yet on DbSnapshotStore (and may need DbSnapshotRecord type)
                var proposedIds = new[] { "radar-snap" }; // from the proposal we just approved
                var snap = store.RecordApprovedImport(proposedIds, sourceFile: "sensor.md:phase2", importBatchId: batchId);
                Assert.That(snap, Is.Not.Null);
                Assert.That(string.IsNullOrEmpty(snap.ContentHash), Is.False);

                var snap2 = store.RecordApprovedImport(proposedIds, sourceFile: "sensor.md:phase2", importBatchId: batchId);
                Assert.That(snap2.ContentHash, Is.EqualTo(snap.ContentHash)); // stable across calls

                // P2-3: snapshot visible in store list (proves write)
                var snaps = store.GetSortedSnapshotIds();
                Assert.That(snaps, Does.Contain(snap.Id));

                // Also verify reader sees committed rows (P2-2)
                using var reader = new SqliteCatalogReader(dbPath, "s18-snap-test");
                Assert.That(reader.TryGetBasePd("u-snap", "radar-snap", out var pd), Is.True);
                Assert.That(pd, Is.EqualTo(0.77));
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