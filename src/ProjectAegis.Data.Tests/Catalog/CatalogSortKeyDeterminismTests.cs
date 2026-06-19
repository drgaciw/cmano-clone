using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

/// <summary>
/// S23-06 / DBI-7.3: golden-pinned sort-key ordering for Catalog* types across propose, export, and read paths.
/// </summary>
[Collection("CatalogSqlite")]
public sealed class CatalogSortKeyDeterminismTests
{
    private static CatalogSortKeyFixture PlatformEditorSample() => new(
        Sensors:
        [
            new CatalogSensorBinding("u1", "cmo-sensor-2", 0.40),
            new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85, CitationRef: "/sensor/1/"),
        ],
        Platforms:
        [
            new CatalogPlatformBinding("hostile-1", "Hostile Corvette"),
            new CatalogPlatformBinding("u1", "Patrol Frigate"),
        ],
        Weapons:
        [
            new CatalogWeaponRecord("mvp-weapon", "MVP Missile"),
            new CatalogWeaponRecord("rim-66", "RIM-66"),
        ],
        Mounts:
        [
            new CatalogMount("u1", "gun-1", "gun", 270.0, 1),
            new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32),
        ],
        Loadouts:
        [
            new CatalogLoadout("u1", "asuw-default", "ASuW Strike", "asuw", IsDefault: true),
        ],
        Magazines:
        [
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32),
        ],
        Comms:
        [
            new CatalogCommsBinding("u1", "NATO_TADIL_J", "txrx", SatcomCapable: false),
        ]);

    [Fact]
    public void CatalogSortKey_fixture_ordering_hash_is_stable_across_runs()
    {
        var fixture = PlatformEditorSample();
        var first = CatalogSortKeyComparer.ComputeOrderingHash(fixture);
        var second = CatalogSortKeyComparer.ComputeOrderingHash(fixture);
        Assert.Equal(first, second);
        Assert.Equal(CatalogSortKeyGoldenHashes.PlatformEditorSample, first);
    }

    [Theory]
    [InlineData("sensor", "catalog_staging_sensor", "platform_id || '/' || sensor_id")]
    [InlineData("platform", "catalog_staging_platform", "platform_id")]
    [InlineData("weapon", "catalog_staging_weapon", "weapon_id")]
    [InlineData("mount", "catalog_staging_mount", "platform_id || '/' || mount_id")]
    [InlineData("loadout", "catalog_staging_loadout", "platform_id || '/' || loadout_id")]
    [InlineData("magazine", "catalog_staging_magazine", "platform_id || '/' || loadout_id || '/' || mount_id || '/' || weapon_id")]
    [InlineData("comms", "catalog_staging_comms", "platform_id || '/' || link_id")]
    public void ProposeBatch_staging_insert_order_is_stable_for_shuffled_input(
        string entity,
        string stagingTable,
        string keyExpr)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-sortkey-{entity}-{Guid.NewGuid():N}.db");
        try
        {
            var fixture = PlatformEditorSample();
            var shuffledA = ShuffleForEntity(entity, fixture, seed: 17);
            var shuffledB = ShuffleForEntity(entity, fixture, seed: 91);

            string batchA;
            string batchB;
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9301)))
            {
                batchA = ProposeForEntity(gate, entity, shuffledA);
                batchB = ProposeForEntity(gate, entity, shuffledB);
            }

            var orderA = ReadStagingKeys(dbPath, stagingTable, keyExpr, batchA);
            var orderB = ReadStagingKeys(dbPath, stagingTable, keyExpr, batchB);
            Assert.Equal(orderA, orderB);
            Assert.Equal(orderA, orderA.OrderBy(k => k, StringComparer.Ordinal).ToArray());
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Display_name_alias_does_not_rewrite_platform_sort_key()
    {
        var original = new CatalogPlatformBinding("u1", "Patrol Frigate", PlatformClass: "Frigate");
        var aliased = original with { DisplayName = "Renamed Frigate (alias only)" };

        Assert.Equal(
            CatalogSortKeyComparer.FormatPlatformKey(original),
            CatalogSortKeyComparer.FormatPlatformKey(aliased));
        Assert.Equal("u1", CatalogSortKeyComparer.FormatPlatformKey(aliased));
    }

    [Fact]
    public void Export_workbook_row_keys_match_catalog_sort_keys()
    {
        var fixture = PlatformEditorSample();
        var data = new PlatformCatalogExportData(
            Platforms:
            [
                new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
                new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
            ],
            Sensors: fixture.Sensors,
            Mounts: fixture.Mounts,
            Loadouts: fixture.Loadouts,
            Magazines: fixture.Magazines,
            Comms: fixture.Comms);

        var workbook = new PlatformWorkbookExporter().Export(data, "baltic_patrol", new FixedCatalogClock(0));

        Assert.Equal(
            CatalogSortKeyComparer.SortPlatforms(fixture.Platforms).Select(CatalogSortKeyComparer.FormatPlatformKey).ToArray(),
            SheetKeys(workbook, "Platforms", 0));

        Assert.Equal(
            CatalogSortKeyComparer.SortSensors(fixture.Sensors).Select(CatalogSortKeyComparer.FormatSensorKey).ToArray(),
            SheetKeys(workbook, "Sensors", 0, 1));

        Assert.Equal(
            CatalogSortKeyComparer.SortMounts(fixture.Mounts).Select(CatalogSortKeyComparer.FormatMountKey).ToArray(),
            SheetKeys(workbook, "Mounts", 0, 1));

        Assert.Equal(
            CatalogSortKeyComparer.SortLoadouts(fixture.Loadouts).Select(CatalogSortKeyComparer.FormatLoadoutKey).ToArray(),
            SheetKeys(workbook, "Loadouts", 0, 1));

        Assert.Equal(
            CatalogSortKeyComparer.SortMagazines(fixture.Magazines).Select(CatalogSortKeyComparer.FormatMagazineKey).ToArray(),
            SheetKeys(workbook, "Magazines", 0, 1, 2, 3));

        Assert.Equal(
            CatalogSortKeyComparer.SortComms(fixture.Comms).Select(CatalogSortKeyComparer.FormatCommsKey).ToArray(),
            SheetKeys(workbook, "Comms", 0, 1));

        Assert.NotNull(workbook.FindSheet("LinkCatalog"));
    }

    [Fact]
    public void Export_workbook_hash_matches_pinned_golden()
    {
        var fixture = PlatformEditorSample();
        var data = new PlatformCatalogExportData(
            Platforms:
            [
                new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
                new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
            ],
            Sensors: fixture.Sensors,
            Mounts: fixture.Mounts,
            Loadouts: fixture.Loadouts,
            Magazines: fixture.Magazines,
            Comms: fixture.Comms);

        var workbook = new PlatformWorkbookExporter().Export(data, "baltic_patrol", new FixedCatalogClock(0));
        var withoutMeta = new PlatformWorkbook(workbook.Sheets.Where(s => s.Name != PlatformWorkbookHash.MetaSheetName).ToArray());
        var hash = PlatformWorkbookHash.Compute(withoutMeta);
        Assert.Equal(hash, PlatformWorkbookHash.Compute(withoutMeta));
        Assert.Equal(CatalogSortKeyGoldenHashes.PlatformEditorWorkbook, hash);
    }

    private static string ProposeForEntity(CatalogWriteGate gate, string entity, object rows) => entity switch
    {
        "sensor" => gate.ProposeSensorBatch((IReadOnlyList<CatalogSensorBinding>)rows, "agent", "sortkey-test"),
        "platform" => gate.ProposePlatformBatch((IReadOnlyList<CatalogPlatformBinding>)rows, "agent", "sortkey-test"),
        "weapon" => gate.ProposeWeaponBatch((IReadOnlyList<CatalogWeaponRecord>)rows, "agent", "sortkey-test"),
        "mount" => gate.ProposeMountBatch((IReadOnlyList<CatalogMount>)rows, "agent", "sortkey-test"),
        "loadout" => gate.ProposeLoadoutBatch((IReadOnlyList<CatalogLoadout>)rows, "agent", "sortkey-test"),
        "magazine" => gate.ProposeMagazineBatch((IReadOnlyList<CatalogMagazineEntry>)rows, "agent", "sortkey-test"),
        "comms" => gate.ProposeCommsBatch((IReadOnlyList<CatalogCommsBinding>)rows, "agent", "sortkey-test"),
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown entity"),
    };

    private static object ShuffleForEntity(string entity, CatalogSortKeyFixture fixture, int seed) => entity switch
    {
        "sensor" => Shuffle(CatalogSortKeyComparer.SortSensors(fixture.Sensors), seed),
        "platform" => Shuffle(CatalogSortKeyComparer.SortPlatforms(fixture.Platforms), seed),
        "weapon" => Shuffle(CatalogSortKeyComparer.SortWeapons(fixture.Weapons), seed),
        "mount" => Shuffle(CatalogSortKeyComparer.SortMounts(fixture.Mounts), seed),
        "loadout" => Shuffle(CatalogSortKeyComparer.SortLoadouts(fixture.Loadouts), seed),
        "magazine" => Shuffle(CatalogSortKeyComparer.SortMagazines(fixture.Magazines), seed),
        "comms" => Shuffle(CatalogSortKeyComparer.SortComms(fixture.Comms), seed),
        _ => throw new ArgumentOutOfRangeException(nameof(entity), entity, "Unknown entity"),
    };

    private static T[] Shuffle<T>(IReadOnlyList<T> sorted, int seed)
    {
        var list = sorted.ToList();
        var rng = new Random(seed);
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }

        return list.ToArray();
    }

    private static string[] ReadStagingKeys(string dbPath, string table, string keyExpr, string batchId)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            $"""
             SELECT {keyExpr}
             FROM {table}
             WHERE batch_id = $batch
             ORDER BY rowid ASC
             """;
        cmd.Parameters.AddWithValue("$batch", batchId);
        using var reader = cmd.ExecuteReader();
        var keys = new List<string>();
        while (reader.Read())
        {
            keys.Add(reader.GetString(0));
        }

        return keys.ToArray();
    }

    private static string[] SheetKeys(PlatformWorkbook workbook, string sheetName, params int[] keyColumns)
    {
        var sheet = workbook.FindSheet(sheetName);
        Assert.NotNull(sheet);
        return sheet!.Rows
            .Select(row => string.Join('\t', keyColumns.Select(i => row[i])))
            .ToArray();
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