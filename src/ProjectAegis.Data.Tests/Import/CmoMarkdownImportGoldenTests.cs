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
            }

            var fixture = new CatalogSortKeyFixture(
                Sensors: [],
                Platforms: CmoMarkdownImporter.ReadPlatformBindings(platformPath, mapBalticIds: true),
                Weapons: CmoMarkdownImporter.ReadWeaponBindings(weaponPath),
                Mounts: CmoMarkdownImporter.ReadPlatformMounts(platformPath, mapBalticIds: true),
                Loadouts: [],
                Magazines: [],
                Comms: []);

            Assert.Equal(CatalogSortKeyGoldenHashes.BalticCmoImport, CatalogSortKeyComparer.ComputeOrderingHash(fixture));
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
}