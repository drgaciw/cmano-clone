using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S26-04 — import E2E golden hygiene: snapshot stability and catalog state unchanged on re-import.</summary>
[Collection("CatalogSqlite")]
public sealed class CmoMarkdownImportGoldenTests
{
    [Fact]
    public void Weapon_import_reapprove_identical_slice_preserves_catalog_ordering_hash()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s26-golden-{Guid.NewGuid():N}.db");

        try
        {
            var hashBefore = ImportApproveAndHashWeapons(dbPath, markdown, clockSeed: 26041);
            var hashAfter = ImportApproveAndHashWeapons(dbPath, markdown, clockSeed: 26042);
            Assert.Equal(hashBefore, hashAfter);
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
    public void Weapon_platform_combined_import_regression_WriteGate_and_replay_unchanged()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s26-regression-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
                dbPath,
                platformPath,
                weaponPath,
                mapBalticPlatformIds: true,
                clock: new FixedCatalogClock(26043));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(26044)))
            {
                Assert.True(gate.ApproveBatch(proposed.PlatformBatchId!, "human", "s26-regression").Committed);
                Assert.True(gate.ApproveBatch(proposed.WeaponBatchId!, "human", "s26-regression").Committed);
                Assert.True(gate.ApproveBatch(proposed.MountBatchId!, "human", "s26-regression").Committed);
                Assert.True(gate.ApproveBatch(proposed.LoadoutBatchId!, "human", "s26-regression").Committed);
                Assert.True(gate.ApproveBatch(proposed.MagazineBatchId!, "human", "s26-regression").Committed);
            }

            var weaponLookup = CmoMarkdownImporter.BuildWeaponNameLookup(CmoMarkdownImporter.ReadWeaponBindings(weaponPath));
            var (magazines, _) = CmoMarkdownImporter.PartitionPlatformMagazines(
                platformPath,
                mapBalticIds: true,
                weaponLookup,
                Path.GetFileName(platformPath));

            var fixture = new CatalogSortKeyFixture(
                Sensors: [],
                Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: true),
                Weapons: CmoMarkdownImporter.ReadWeaponBindings(weaponPath),
                Mounts: CmoMarkdownImporter.ReadPlatformMounts(platformPath, mapBalticIds: true),
                Loadouts: CmoMarkdownImporter.ReadPlatformLoadouts(platformPath, mapBalticIds: true),
                Magazines: magazines,
                Comms: []);

            Assert.Equal(CatalogSortKeyGoldenHashes.BalticCmoImportWithFittings, CatalogSortKeyComparer.ComputeOrderingHash(fixture));
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
    public void Platform_fittings_reimport_identical_slice_preserves_catalog_ordering_hash()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-golden-fittings-{Guid.NewGuid():N}.db");

        try
        {
            var hashBefore = ImportApproveAndHashFittings(dbPath, platformPath, weaponPath, clockSeed: 27041);
            var hashAfter = ImportApproveAndHashFittings(dbPath, platformPath, weaponPath, clockSeed: 27042);
            Assert.Equal(hashBefore, hashAfter);
            Assert.Equal(CatalogSortKeyGoldenHashes.BalticCmoImportWithFittings, hashBefore);
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
    public void Platform_ship_slice100_reimport_identical_slice_preserves_catalog_ordering_hash()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s28-golden-platform-{Guid.NewGuid():N}.db");

        try
        {
            var hashBefore = ImportApproveAndHashPlatforms(dbPath, platformPath, clockSeed: 28031);
            var hashAfter = ImportApproveAndHashPlatforms(dbPath, platformPath, clockSeed: 28032);
            Assert.Equal(hashBefore, hashAfter);
            Assert.Equal(CatalogSortKeyGoldenHashes.ShipSlice100PlatformV2, hashBefore);
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
    public void Platform_ship_slice100_corpus_roundtrip_through_WriteGate_pins_ordering_hash()
    {
        var platformPath = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s28-platform-roundtrip-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                mapBalticPlatformIds: false,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(28033));

            Assert.Equal(100, proposed.ParsedCount);
            Assert.Equal(100, proposed.ApprovedCount);
            Assert.Single(proposed.Batches);
            Assert.Equal(100, proposed.Batches[0].RecordCount);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(28034)))
            {
                Assert.True(gate.ApproveBatch(proposed.Batches[0].BatchId, "human", "s28-golden").Committed);
            }

            var fixture = new CatalogSortKeyFixture(
                Sensors: [],
                Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: false),
                Weapons: [],
                Mounts: [],
                Loadouts: [],
                Magazines: [],
                Comms: []);

            Assert.Equal(CatalogSortKeyGoldenHashes.ShipSlice100PlatformV2, CatalogSortKeyComparer.ComputeOrderingHash(fixture));
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
    public void Sensor_import_path_unchanged_after_weapon_platform_cli_additions()
    {
        var markdown = CmoMarkdownImporter.ResolveMiniFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s26-sensor-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposeFromMarkdown(
                dbPath,
                markdown,
                maxRecords: 12,
                chunkSize: 500,
                clock: new FixedCatalogClock(26045));

            Assert.Equal(12, result.ApprovedCount);
            Assert.Single(result.Batches);
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

    private static string ImportApproveAndHashWeapons(string dbPath, string markdownPath, long clockSeed)
    {
        var propose = CmoMarkdownImportProposer.ProposeWeaponsFromMarkdown(
            dbPath,
            markdownPath,
            maxRecords: null,
            chunkSize: 500,
            clock: new FixedCatalogClock(clockSeed));

        foreach (var batch in propose.Batches)
        {
            using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(clockSeed + 1));
            Assert.True(gate.ApproveBatch(batch.BatchId, "human", "s26-golden").Committed);
        }

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(markdownPath);
        var fixture = new CatalogSortKeyFixture(
            Sensors: [],
            Platforms: [],
            Weapons: weapons,
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: []);

        return CatalogSortKeyComparer.ComputeOrderingHash(fixture);
    }

    private static string ImportApproveAndHashFittings(
        string dbPath,
        string platformPath,
        string weaponPath,
        long clockSeed)
    {
        var proposed = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
            dbPath,
            platformPath,
            weaponPath,
            mapBalticPlatformIds: true,
            clock: new FixedCatalogClock(clockSeed));

        using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(clockSeed + 1)))
        {
            Assert.True(gate.ApproveBatch(proposed.PlatformBatchId!, "human", "s27-golden").Committed);
            Assert.True(gate.ApproveBatch(proposed.WeaponBatchId!, "human", "s27-golden").Committed);
            Assert.True(gate.ApproveBatch(proposed.MountBatchId!, "human", "s27-golden").Committed);
            Assert.True(gate.ApproveBatch(proposed.LoadoutBatchId!, "human", "s27-golden").Committed);
            Assert.True(gate.ApproveBatch(proposed.MagazineBatchId!, "human", "s27-golden").Committed);
        }

        var weaponLookup = CmoMarkdownImporter.BuildWeaponNameLookup(CmoMarkdownImporter.ReadWeaponBindings(weaponPath));
        var (magazines, _) = CmoMarkdownImporter.PartitionPlatformMagazines(
            platformPath,
            mapBalticIds: true,
            weaponLookup,
            Path.GetFileName(platformPath));

        var fixture = new CatalogSortKeyFixture(
            Sensors: [],
            Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: true),
            Weapons: CmoMarkdownImporter.ReadWeaponBindings(weaponPath),
            Mounts: CmoMarkdownImporter.ReadPlatformMounts(platformPath, mapBalticIds: true),
            Loadouts: CmoMarkdownImporter.ReadPlatformLoadouts(platformPath, mapBalticIds: true),
            Magazines: magazines,
            Comms: []);

        return CatalogSortKeyComparer.ComputeOrderingHash(fixture);
    }

    private static string ImportApproveAndHashPlatforms(string dbPath, string platformPath, long clockSeed)
    {
        var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
            dbPath,
            platformPath,
            mapBalticPlatformIds: false,
            maxRecords: null,
            chunkSize: 500,
            clock: new FixedCatalogClock(clockSeed));

        using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(clockSeed + 1)))
        {
            foreach (var batch in proposed.Batches)
            {
                Assert.True(gate.ApproveBatch(batch.BatchId, "human", "s28-golden").Committed);
            }
        }

        var fixture = new CatalogSortKeyFixture(
            Sensors: [],
            Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: false),
            Weapons: [],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: []);

        return CatalogSortKeyComparer.ComputeOrderingHash(fixture);
    }
}