using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>S25-03: Phase B damage reader API + Platforms sheet export columns (Req-21 / PLE-1.3).</summary>
public sealed class CatalogPhaseBDamageReaderTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    [Test]
    public void CatalogPhaseB_InMemoryReader_returns_empty_damage_collection()
    {
        var reader = InMemoryCatalogReader.BalticPatrolFixture();

        Assert.That(reader.GetSortedPlatformDamage(), Is.Empty);
        Assert.That(reader.TryGetPlatformDamage("u1", out _), Is.False);
        Assert.That(reader.TryGetBasePd("u1", "radar-1", out _), Is.True);
    }

    [Test]
    public void CatalogPhaseB_SqliteReader_returns_empty_when_damage_table_has_no_rows()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-empty-{Guid.NewGuid():N}.db");
        try
        {
            using var reader = new SqliteCatalogReader(dbPath, "damage-empty");
            Assert.That(reader.GetSortedPlatformDamage(), Is.Empty);
            Assert.That(reader.TryGetPlatformDamage("u1", out _), Is.False);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_SqliteReader_reads_baltic_damage_row()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-read-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var reader = new SqliteCatalogReader(dbPath, "damage-readback");
            Assert.That(reader.TryGetPlatformDamage("u1", out var damage), Is.True);
            Assert.That(damage.MaxHp, Is.EqualTo(100).Within(0.0005));
            Assert.That(damage.WithdrawThresholdPct, Is.EqualTo(25).Within(0.0005));
            Assert.That(damage.CriticalFlags, Is.EqualTo(0));
            Assert.That(reader.GetSortedPlatformDamage(), Has.Exactly(1).Items);
            Assert.That(reader.TryGetPlatformDamage("missing-platform", out _), Is.False);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void CatalogPhaseB_damage_rows_sort_by_platform_id()
    {
        var rows = new[]
        {
            new CatalogPlatformDamage("u2", MaxHp: 80),
            new CatalogPlatformDamage("u1", MaxHp: 100),
        };

        var sorted = CatalogSortKeyComparer.SortPlatformDamage(rows);
        Assert.That(sorted.Select(r => r.PlatformId).ToArray(), Is.EqualTo(["u1", "u2"]));
    }

    [Test]
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
        Assert.That(platforms, Is.Not.Null);
        Assert.That(            platforms!.Header, Is.EqualTo(            ["PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm", "MaxHp", "WithdrawThresholdPct", "CriticalFlags"]));
        Assert.That(platforms.Rows, Has.Exactly(1).Items);
        Assert.That(platforms.Rows[0][0], Is.EqualTo("u1"));
        Assert.That(platforms.Rows[0][4], Is.EqualTo("100"));
        Assert.That(platforms.Rows[0][5], Is.EqualTo("25"));
        Assert.That(platforms.Rows[0][6], Is.EqualTo("1"));
    }

    [Test]
    public void CatalogPhaseB_export_resolver_returns_damage_from_db()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-damage-export-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var data), Is.True);
            Assert.That(data.Damage, Is.Not.Null);
            Assert.That(data.Damage!, Has.Exactly(1).Items);
            Assert.That(data.Damage![0].WithdrawThresholdPct, Is.EqualTo(25).Within(0.0005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void Meta_sheet_reports_schema_version_010()
    {
        var meta = Export(PlatformCatalogExportData.Empty).FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.That(meta, Is.Not.Null);
        Assert.That(meta!.Rows.Any(row => row.Count >= 2
            && string.Equals(row[0], "SchemaVersion", StringComparison.Ordinal)
            && string.Equals(row[1], "010", StringComparison.Ordinal)), Is.True);
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