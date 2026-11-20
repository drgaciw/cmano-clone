using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.WriteGate;

/// <summary>S25-04: Phase B damage write-gate staging commit (extend-only CatalogWriteGate).</summary>
[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CatalogWriteGatePhaseBDamageTests
{
    [Test]
    public void CatalogPhaseB_ApproveBatch_damage_staging_commits_and_reader_read_back()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-approve-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var damage = new CatalogPlatformDamage(
                "u1",
                MaxHp: 88,
                WithdrawThresholdPct: 30,
                CriticalFlags: 2,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: "unit-test");

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9601)))
            {
                var batchId = gate.ProposePlatformDamageBatch([damage], "agent", "phase-b-damage-gate");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.That(decision.Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-damage-readback");
            Assert.That(reader.TryGetPlatformDamage("u1", out var committed), Is.True);
            Assert.That(committed.MaxHp, Is.EqualTo(88).Within(0.005));
            Assert.That(committed.WithdrawThresholdPct, Is.EqualTo(30).Within(0.005));
            Assert.That(committed.CriticalFlags, Is.EqualTo(2));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_RejectBatch_discards_damage_staging_without_live_commit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-reject-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9602)))
            {
                var batchId = gate.ProposePlatformDamageBatch(
                    [new CatalogPlatformDamage("u1", MaxHp: 99)],
                    "agent",
                    "reject-test");
                Assert.That(gate.RejectBatch(batchId, "human", "qa").Committed, Is.False);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-damage-reject");
            Assert.That(reader.TryGetPlatformDamage("u1", out var seed), Is.True);
            Assert.That(seed.MaxHp, Is.EqualTo(100).Within(0.005));
            Assert.That(seed.WithdrawThresholdPct, Is.EqualTo(25).Within(0.005));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.That(CountRows(connection, "catalog_staging_damage"), Is.EqualTo(0));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_ApproveBatch_rejects_orphan_platform_damage()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-orphan-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9603)))
            {
                var batchId = gate.ProposePlatformDamageBatch(
                    [new CatalogPlatformDamage("orphan-platform", MaxHp: 10)],
                    "agent",
                    "orphan-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa");
                Assert.That(decision.Committed, Is.False);
                Assert.That(decision.Errors.Any(e => e.Contains("orphan_platform", StringComparison.Ordinal)), Is.True);
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_ProposePlatformDamageBatch_rejects_empty_batch()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9604));
            var ex = Assert.Throws<ArgumentException>(() =>
                gate.ProposePlatformDamageBatch([], "agent", "empty-test"));
            Assert.That(ex!.Message, Does.Contain("damage row").IgnoreCase);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_sensor_approve_regression_unchanged_after_damage_extend()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-sensor-damage-regression-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9605)))
            {
                var proposal = new CatalogSensorBinding(
                    "u-test",
                    "radar-damage-regression",
                    0.71,
                    ReviewState: CatalogReviewStates.Approved);
                var batchId = gate.ProposeSensorBatch([proposal], "agent", "regression-test");
                Assert.That(gate.ApproveBatch(batchId, "human", "qa").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "sensor-damage-regression");
            Assert.That(reader.TryGetBasePd("u-test", "radar-damage-regression", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.71).Within(0.005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_mobility_approve_regression_unchanged_after_damage_extend()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-mobility-damage-regression-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9606)))
            {
                var batchId = gate.ProposeMobilityBatch(
                    [new CatalogMobility("u1", MaxSpeedKnots: 22)],
                    "agent",
                    "mobility-regression");
                Assert.That(gate.ApproveBatch(batchId, "human", "qa").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "mobility-damage-regression");
            Assert.That(reader.TryGetMobility("u1", out var mobility), Is.True);
            Assert.That(mobility.MaxSpeedKnots, Is.EqualTo(22).Within(0.005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

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