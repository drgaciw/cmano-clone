using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>S25-03: Phase B damage reader API + Platforms sheet export columns (Req-21 / PLE-1.3).</summary>
public sealed class CatalogPhaseBDamageReaderTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    [Fact]
    public void CatalogPhaseB_InMemoryReader_returns_empty_damage_collection()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.Empty(reader.GetSortedPlatformDamage());
        Assert.False(reader.TryGetPlatformDamage("u1", out _));
        Assert.True(reader.TryGetBasePd("u1", "radar-1", out _));
    }

    [Fact]
    public void CatalogPhaseB_SqliteReader_returns_empty_when_damage_table_has_no_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var reader = new SqliteCatalogReader(dbPath, "damage-empty");
            Assert.Empty(reader.GetSortedPlatformDamage());
            Assert.False(reader.TryGetPlatformDamage("u1", out _));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_SqliteReader_reads_baltic_damage_row()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-read-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var reader = new SqliteCatalogReader(dbPath, "damage-readback");
            Assert.True(reader.TryGetPlatformDamage("u1", out var damage));
            Assert.Equal(100, damage.MaxHp, precision: 3);
            Assert.Equal(25, damage.WithdrawThresholdPct, precision: 3);
            Assert.Equal(0, damage.CriticalFlags);
            Assert.Single(reader.GetSortedPlatformDamage());
            Assert.False(reader.TryGetPlatformDamage("missing-platform", out _));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void CatalogPhaseB_damage_rows_sort_by_platform_id()
    {
        var rows = new[]
        {
            new CatalogPlatformDamage("u2", MaxHp: 80),
            new CatalogPlatformDamage("u1", MaxHp: 100),
        };

        var sorted = CatalogSortKeyComparer.SortPlatformDamage(rows);
        Assert.Equal(["u1", "u2"], sorted.Select(r => r.PlatformId).ToArray());
    }

    [Fact]
    public void CatalogPhaseB_export_includes_damage_columns_on_platforms_sheet()
    {
        var data = new PlatformCatalogExportData(
            Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Sensors: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: [],
            Damage: new[] { new CatalogPlatformDamage("u1", MaxHp: 100, WithdrawThresholdPct: 25, CriticalFlags: 1) });

        var workbook = Export(data);
        var platforms = workbook.FindSheet("Platforms");
        Assert.NotNull(platforms);
        Assert.Equal(
            ["PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm", "MaxHp", "WithdrawThresholdPct", "CriticalFlags"],
            platforms!.Header);
        Assert.Single(platforms.Rows);
        Assert.Equal("u1", platforms.Rows[0][0]);
        Assert.Equal("100", platforms.Rows[0][4]);
        Assert.Equal("25", platforms.Rows[0][5]);
        Assert.Equal("1", platforms.Rows[0][6]);
    }

    [Fact]
    public void CatalogPhaseB_export_resolver_returns_damage_from_db()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-export-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var data));
            Assert.NotNull(data.Damage);
            Assert.Single(data.Damage!);
            Assert.Equal(25, data.Damage![0].WithdrawThresholdPct, precision: 3);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Meta_sheet_reports_schema_version_009()
    {
        var meta = Export(PlatformCatalogExportData.Empty).FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.NotNull(meta);
        Assert.Contains(meta!.Rows, row => row.Count >= 2
            && string.Equals(row[0], "SchemaVersion", StringComparison.Ordinal)
            && string.Equals(row[1], "009", StringComparison.Ordinal));
    }

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(42));

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}