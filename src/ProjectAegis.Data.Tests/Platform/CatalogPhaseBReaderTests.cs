using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>S24-02: Phase B catalog reader API + export resolver (Req-21 / PLE-1.3).</summary>
public sealed class CatalogPhaseBReaderTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    [Test]
    public void CatalogPhaseB_InMemoryReader_returns_empty_Phase_B_collections()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.That(reader.GetSortedMobility(), Is.Empty);
        Assert.That(reader.GetSortedSignatures(), Is.Empty);
        Assert.That(reader.GetSortedEmcon(), Is.Empty);
        Assert.That(reader.GetSortedPlatformDamage(), Is.Empty);
        Assert.That(reader.TryGetMobility("u1", out _), Is.False);
        Assert.That(reader.TryGetPlatformDamage("u1", out _), Is.False);
        Assert.That(reader.TryGetSignature("u1", out _), Is.False);
        Assert.That(reader.TryGetEmcon("u1", "silent", "radar-1", out _), Is.False);
        Assert.That(reader.TryGetBasePd("u1", "radar-1", out _), Is.True);
    }

    [Test]
    public void CatalogPhaseB_SqliteReader_returns_empty_when_tables_have_no_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var reader = new SqliteCatalogReader(dbPath, "phase-b-empty");
            Assert.That(reader.GetSortedMobility(), Is.Empty);
            Assert.That(reader.GetSortedSignatures(), Is.Empty);
            Assert.That(reader.GetSortedEmcon(), Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
            Assert.That(reader.TryGetMobility("u1", out var mobility), Is.True);
            Assert.That(mobility.MaxSpeedKnots, Is.EqualTo(32).Within(0.005));
            Assert.That(mobility.RangeNm, Is.EqualTo(4200).Within(0.005));

            Assert.That(reader.TryGetSignature("u1", out var signature), Is.True);
            Assert.That(signature.RcsBandDbsm, Is.EqualTo(-12).Within(0.005));

            Assert.That(reader.TryGetEmcon("u1", "free", "radar-1", out var emcon), Is.True);
            Assert.That(emcon.Posture, Is.EqualTo("active"));
            Assert.That(reader.GetSortedEmcon().Count, Is.EqualTo(2));
            Assert.That(reader.TryGetMobility("missing-platform", out _), Is.False);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_emcon_rows_sort_by_platform_condition_emitter()
    {
        var rows = new[]
        {
            new CatalogEmcon("u2", "free", "emitter-b", "active"),
            new CatalogEmcon("u1", "silent", "emitter-a", "off"),
            new CatalogEmcon("u1", "free", "emitter-a", "active"),
        };

        var sorted = CatalogSortKeyComparer.SortEmcon(rows);
        Assert.That(sorted.Select(r => r.PlatformId).ToArray(), Is.EqualTo(["u1", "u1", "u2"]));
        Assert.That(sorted.Select(r => r.Condition).ToArray(), Is.EqualTo(["free", "silent", "free"]));
        Assert.That(sorted.Select(r => r.EmitterId).ToArray(), Is.EqualTo(["emitter-a", "emitter-a", "emitter-b"]));
    }

    [Test]
    public void CatalogPhaseB_export_resolver_returns_real_payload_when_snapshot_resolves()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-export-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            SeedPhaseBRows(dbPath);

            Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var data), Is.True);
            Assert.That(data.Signatures, Is.Not.Null);
            Assert.That(data.Signatures!, Has.Exactly(1).Items);
            Assert.That(data.Signatures![0].RcsBandDbsm, Is.EqualTo(-12).Within(0.005));
            Assert.That(data.Sensors.Count > 0, Is.True);
            Assert.That(data.Mobility?.Count, Is.EqualTo(1));
            Assert.That(data.Emcon?.Count, Is.EqualTo(2));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_export_resolver_falls_back_when_db_missing()
    {
        Assert.That(PlatformCatalogExportResolver.TryResolve(null, SnapshotId, out var data), Is.False);
        Assert.That(data, Is.SameAs(PlatformCatalogExportData.Empty));
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