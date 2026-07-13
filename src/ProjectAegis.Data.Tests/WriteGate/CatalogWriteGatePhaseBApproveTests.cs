using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.WriteGate;

/// <summary>S24-03: Phase B write-gate staging commit (extend-only CatalogWriteGate).</summary>
[Collection("CatalogSqlite")]
public sealed class CatalogWriteGatePhaseBApproveTests
{
    [Fact]
    public void CatalogPhaseB_ApproveBatch_mobility_staging_commits_and_reader_read_back()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-mobility-approve-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var mobility = new CatalogMobility(
                "u1",
                MaxSpeedKnots: 28,
                CruiseSpeedKnots: 16,
                RangeNm: 3900,
                ReviewState: CatalogReviewStates.Approved,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: "unit-test");

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9501)))
            {
                var batchId = gate.ProposeMobilityBatch([mobility], "agent", "phase-b-gate-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-gate-readback");
            Assert.True(reader.TryGetMobility("u1", out var committed));
            Assert.Equal(28, committed.MaxSpeedKnots, precision: 3);
            Assert.Equal(3900, committed.RangeNm, precision: 3);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_RejectBatch_discards_mobility_staging_without_live_commit()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-mobility-reject-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9502)))
            {
                var batchId = gate.ProposeMobilityBatch(
                    [new CatalogMobility("u1", MaxSpeedKnots: 99)],
                    "agent",
                    "reject-test");
                Assert.False(gate.RejectBatch(batchId, "human", "qa").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-reject");
            Assert.False(reader.TryGetMobility("u1", out _));

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            Assert.Equal(0, CountRows(connection, "catalog_staging_mobility"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_ApproveBatch_rejects_orphan_platform_mobility()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-mobility-orphan-{Guid.NewGuid():N}.db");
        try
        {
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9503)))
            {
                var batchId = gate.ProposeMobilityBatch(
                    [new CatalogMobility("orphan-platform", MaxSpeedKnots: 10)],
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
    public void CatalogPhaseB_sensor_approve_regression_unchanged_after_extend_only()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-sensor-regression-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9504)))
            {
                var proposal = new CatalogSensorBinding(
                    "u-test",
                    "radar-phase-b-regression",
                    0.71,
                    ReviewState: CatalogReviewStates.Approved);
                var batchId = gate.ProposeSensorBatch([proposal], "agent", "regression-test");
                Assert.True(gate.ApproveBatch(batchId, "human", "qa").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "sensor-regression");
            Assert.True(reader.TryGetBasePd("u-test", "radar-phase-b-regression", out var basePd));
            Assert.Equal(0.71, basePd, precision: 3);
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