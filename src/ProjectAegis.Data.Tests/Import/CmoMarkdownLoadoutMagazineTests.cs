using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S27-03 — CMO platform markdown loadout + magazine import via write gate.</summary>
[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownLoadoutMagazineTests
{
    [Test]
    public void ProposePlatformWeaponMounts_stages_loadout_and_magazine_batches_with_baltic_fixture()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-fittings-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformWeaponMounts(
                dbPath,
                platformPath,
                weaponPath,
                mapBalticPlatformIds: true,
                clock: new FixedCatalogClock(27031));

            Assert.That(proposed.LoadoutCount, Is.EqualTo(3));
            Assert.That(proposed.MagazineCount, Is.EqualTo(3));
            Assert.That(proposed.FittingQuarantinedCount, Is.EqualTo(1));
            Assert.That(proposed.LoadoutBatchId, Is.Not.Null);
            Assert.That(proposed.MagazineBatchId, Is.Not.Null);
            Assert.That(proposed.FittingQuarantineReport, Has.Exactly(1).Items);
            Assert.That(proposed.FittingQuarantineReport[0].Reason, Is.EqualTo("orphan_weapon_id"));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(27032)))
            {
                Assert.That(gate.ApproveBatch(proposed.PlatformBatchId!, "human", "s27-fittings").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.WeaponBatchId!, "human", "s27-fittings").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.MountBatchId!, "human", "s27-fittings").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.LoadoutBatchId!, "human", "s27-fittings").Committed, Is.True);
                Assert.That(gate.ApproveBatch(proposed.MagazineBatchId!, "human", "s27-fittings").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();

            using (var loadoutCmd = connection.CreateCommand())
            {
                loadoutCmd.CommandText = "SELECT COUNT(*) FROM platform_loadout";
                // Seed asuw-default + CMO fixture loadouts (3) → 4 rows total.
                Assert.That(Convert.ToInt32(loadoutCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(4));
            }

            using (var magazineCmd = connection.CreateCommand())
            {
                magazineCmd.CommandText = "SELECT COUNT(*) FROM platform_magazine";
                // Seed (2) + CMO fixture magazines after approve.
                Assert.That(Convert.ToInt32(magazineCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(5));
            }
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

    [Test]
    public void ProposePlatformsFromMarkdown_stages_loadout_magazine_when_weapon_fixture_supplied()
    {
        var platformPath = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var weaponPath = CmoMarkdownImporter.ResolveMiniWeaponFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-platform-cli-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                platformPath,
                mapBalticPlatformIds: true,
                weaponMarkdownPath: weaponPath,
                chunkSize: 500,
                clock: new FixedCatalogClock(27033));

            Assert.That(result.Batches.Count, Is.EqualTo(4));
            Assert.That(result.QuarantinedCount, Is.EqualTo(1));
            Assert.That(result.FittingQuarantineReport, Has.Exactly(1).Items);

            foreach (var batch in result.Batches)
            {
                using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(27034));
                Assert.That(gate.ApproveBatch(batch.BatchId, "human", "s27-platform").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText =
                """
                SELECT platform_id, loadout_id, mount_id, weapon_id
                FROM platform_magazine
                ORDER BY platform_id ASC, loadout_id ASC, mount_id ASC, weapon_id ASC
                """;
            using var reader = cmd.ExecuteReader();
            var rows = new List<string>();
            while (reader.Read())
            {
                rows.Add($"{reader.GetString(0)}:{reader.GetString(1)}:{reader.GetString(2)}:{reader.GetString(3)}");
            }

            Assert.That(                rows, Is.EqualTo(                [
                    "hostile-1:default:ss-n-25-switchblade:cmo-weapon-2002",
                    "u1:asuw-default:gun-76:baltic-oto-76",
                    "u1:asuw-default:vls-fwd:baltic-rim-66",
                    "u1:default:76mm-oto-melara:cmo-weapon-2003",
                    "u1:default:rim-66-standard-mr:cmo-weapon-2001",
                ]));
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
}