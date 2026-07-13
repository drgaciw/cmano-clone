using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.WriteGate;

/// <summary>S25-04: Phase B damage write-gate staging commit (extend-only CatalogWriteGate).</summary>
[Collection("CatalogSqlite")]
public sealed class CatalogWriteGatePhaseBDamageTests
{
    [Fact]
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
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-damage-readback");
            Assert.True(reader.TryGetPlatformDamage("u1", out var committed));
            Assert.Equal(88, committed.MaxHp, precision: 3);
            Assert.Equal(30, committed.WithdrawThresholdPct, precision: 3);
            Assert.Equal(2, committed.CriticalFlags);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
                Assert.False(gate.RejectBatch(batchId, "human", "qa").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-damage-reject");
            Assert.True(reader.TryGetPlatformDamage("u1", out var seed));
            Assert.Equal(100, seed.MaxHp, precision: 3);
            Assert.Equal(25, seed.WithdrawThresholdPct, precision: 3);

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountRows(connection, "catalog_staging_damage"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
                Assert.False(decision.Committed);
                Assert.Contains(decision.Errors, e => e.Contains("orphan_platform", StringComparison.Ordinal));
            }
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_ProposePlatformDamageBatch_rejects_empty_batch()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9604));
            var ex = Assert.Throws<ArgumentException>(() =>
                gate.ProposePlatformDamageBatch([], "agent", "empty-test"));
            Assert.Contains("damage row", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
                Assert.True(gate.ApproveBatch(batchId, "human", "qa").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "sensor-damage-regression");
            Assert.True(reader.TryGetBasePd("u-test", "radar-damage-regression", out var basePd));
            Assert.Equal(0.71, basePd, precision: 3);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
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
                Assert.True(gate.ApproveBatch(batchId, "human", "qa").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "mobility-damage-regression");
            Assert.True(reader.TryGetMobility("u1", out var mobility));
            Assert.Equal(22, mobility.MaxSpeedKnots, precision: 3);
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