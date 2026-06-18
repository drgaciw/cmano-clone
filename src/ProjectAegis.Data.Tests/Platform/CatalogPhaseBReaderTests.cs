using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>S24-02: Phase B catalog reader API + export resolver (Req-21 / PLE-1.3).</summary>
public sealed class CatalogPhaseBReaderTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    [Fact]
    public void CatalogPhaseB_InMemoryReader_returns_empty_Phase_B_collections()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.Empty(reader.GetSortedMobility());
        Assert.Empty(reader.GetSortedSignatures());
        Assert.Empty(reader.GetSortedEmcon());
        Assert.False(reader.TryGetMobility("u1", out _));
        Assert.False(reader.TryGetSignature("u1", out _));
        Assert.False(reader.TryGetEmcon("u1", "silent", "radar-1", out _));
        Assert.True(reader.TryGetBasePd("u1", "radar-1", out _));
    }

    [Fact]
    public void CatalogPhaseB_SqliteReader_returns_empty_when_tables_have_no_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var reader = new SqliteCatalogReader(dbPath, "phase-b-empty");
            Assert.Empty(reader.GetSortedMobility());
            Assert.Empty(reader.GetSortedSignatures());
            Assert.Empty(reader.GetSortedEmcon());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_SqliteReader_reads_mobility_signature_and_emcon_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-read-{Guid.NewGuid():N}.db");
        try
        {
            using (var seedReader = new SqliteCatalogReader(dbPath, "phase-b-seed"))
            {
                _ = seedReader.GetSortedSensorBindings();
            }

            SeedPhaseBRows(dbPath);

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-readback");
            Assert.True(reader.TryGetMobility("u1", out var mobility));
            Assert.Equal(32, mobility.MaxSpeedKnots, precision: 3);
            Assert.Equal(4200, mobility.RangeNm, precision: 3);

            Assert.True(reader.TryGetSignature("u1", out var signature));
            Assert.Equal(-12, signature.RcsBandDbsm, precision: 3);

            Assert.True(reader.TryGetEmcon("u1", "free", "radar-1", out var emcon));
            Assert.Equal("active", emcon.Posture);
            Assert.Equal(2, reader.GetSortedEmcon().Count);
            Assert.False(reader.TryGetMobility("missing-platform", out _));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_emcon_rows_sort_by_platform_condition_emitter()
    {
        var rows = new[]
        {
            new CatalogEmcon("u2", "free", "emitter-b", "active"),
            new CatalogEmcon("u1", "silent", "emitter-a", "off"),
            new CatalogEmcon("u1", "free", "emitter-a", "active"),
        };

        var sorted = CatalogSortKeyComparer.SortEmcon(rows);
        Assert.Equal(["u1", "u1", "u2"], sorted.Select(r => r.PlatformId).ToArray());
        Assert.Equal(["free", "silent", "free"], sorted.Select(r => r.Condition).ToArray());
        Assert.Equal(["emitter-a", "emitter-a", "emitter-b"], sorted.Select(r => r.EmitterId).ToArray());
    }

    [Fact]
    public void CatalogPhaseB_export_resolver_returns_real_payload_when_snapshot_resolves()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-export-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            SeedPhaseBRows(dbPath);

            Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var data));
            Assert.NotNull(data.Signatures);
            Assert.Single(data.Signatures!);
            Assert.Equal(-12, data.Signatures![0].RcsBandDbsm, precision: 3);
            Assert.True(data.Sensors.Count > 0);
            Assert.Equal(1, data.Mobility?.Count);
            Assert.Equal(2, data.Emcon?.Count);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_export_resolver_falls_back_when_db_missing()
    {
        Assert.False(PlatformCatalogExportResolver.TryResolve(null, SnapshotId, out var data));
        Assert.Same(PlatformCatalogExportData.Empty, data);
    }

    private static void SeedPhaseBRows(string dbPath)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mobility
                (platform_id, max_speed_knots, cruise_speed_knots, range_nm, review_state, trl_level, value_tier, citation_ref)
            VALUES ('u1', 32, 18, 4200, 'approved', 9, 'interpreted_value', 'unit-test');
            INSERT OR REPLACE INTO platform_signature
                (platform_id, rcs_band_dbsm, acoustic_signature_db, review_state, trl_level, value_tier, citation_ref)
            VALUES ('u1', -12, 88, 'approved', 9, 'interpreted_value', 'unit-test');
            INSERT OR REPLACE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
            VALUES ('u1', 'free', 'radar-1', 'active', 'approved');
            INSERT OR REPLACE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
            VALUES ('u1', 'silent', 'radar-1', 'off', 'approved');
            """;
        cmd.ExecuteNonQuery();
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