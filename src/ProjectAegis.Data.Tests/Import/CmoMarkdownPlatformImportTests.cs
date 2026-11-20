using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

[Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownPlatformImportTests
{
    [Test]
    public void ProposePlatformsFromMarkdown_stages_ship_slice100_and_approve_commits()
    {
        var markdown = CmoMarkdownImporter.ResolveShipSlice100FixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-ship-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdown,
                mapBalticPlatformIds: false,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(2714));

            Assert.That(result.ParsedCount, Is.EqualTo(100));
            Assert.That(result.ApprovedCount, Is.EqualTo(100));
            Assert.That(result.QuarantinedCount, Is.EqualTo(0));
            Assert.That(result.Batches, Has.Exactly(1).Items);
            Assert.That(result.Batches[0].RecordCount, Is.EqualTo(100));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2715)))
            {
                Assert.That(gate.ApproveBatch(result.Batches[0].BatchId, "human", "s27-14-ship").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM platform WHERE platform_id LIKE 'test-ship-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            Assert.That(count, Is.EqualTo(100));
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
    public void Reference_ship_markdown_parses_4844_records_for_off_ci_nightly_scale()
    {
        var path = CmoMarkdownImporter.ResolveReferenceShipMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path);
        Assert.That(platforms.Count, Is.EqualTo(4844));

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(platforms, chunkSize: 500);
        Assert.That(chunks.Length, Is.EqualTo(10));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[8].Length, Is.EqualTo(500));
        Assert.That(chunks[9].Length, Is.EqualTo(344));
    }

    [Test]
    public void Reference_ground_unit_markdown_parses_3289_records_for_off_ci_nightly_scale()
    {
        var path = CmoMarkdownImporter.ResolveReferenceGroundUnitMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path);
        Assert.That(platforms.Count, Is.EqualTo(3289));

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(platforms, chunkSize: 500);
        Assert.That(chunks.Length, Is.EqualTo(7));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[5].Length, Is.EqualTo(500));
        Assert.That(chunks[6].Length, Is.EqualTo(289));
    }

    [Test]
    public void ChunkPlatforms_with_501_rows_produces_two_batches_at_chunk_size_500()
    {
        var rows = Enumerable.Range(0, 501)
            .Select(i => new CatalogPlatformBinding(
                $"test-ship-chunk-{i:D4}",
                DisplayName: $"Test Ship Chunk {i}",
                Domain: "surface",
                PlatformClass: "Frigate",
                Nationality: "NATO",
                ReviewState: CatalogReviewStates.Provisional,
                TrlLevel: 9,
                ValueTier: CatalogProvenanceTier.InterpretedValue,
                CitationRef: $"/ship/{5000 + i}/",
                SourceFactId: $"cmano-db:ship/{5000 + i}",
                ImportBatchId: "chunk-test",
                SourceFile: "chunk-test.md"))
            .ToArray();

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(rows, chunkSize: 500);

        Assert.That(chunks.Length, Is.EqualTo(2));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[1], Has.Exactly(1).Items);
        Assert.That(chunks[0][0].PlatformId, Is.EqualTo("test-ship-chunk-0000"));
    }

    [Test]
    public void ProposePlatformsFromMarkdown_emits_fitting_quarantine_json_for_orphan_weapon()
    {
        var markdownPath = WritePlatformOrphanFixture();
        var reportPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-platform-quarantine-{Guid.NewGuid():N}.json");
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s27-platform-quarantine-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdownPath,
                mapBalticPlatformIds: false,
                chunkSize: 500,
                clock: new FixedCatalogClock(2716));

            Assert.That(result.ParsedCount, Is.EqualTo(1));
            Assert.That(result.QuarantinedCount, Is.EqualTo(1));
            Assert.That(result.FittingQuarantineReport, Has.Exactly(1).Items);
            Assert.That(result.FittingQuarantineReport[0].Reason, Is.EqualTo("orphan_weapon_id"));

            var payload = new
            {
                quarantinedCount = result.QuarantinedCount,
                fittingQuarantineReport = result.FittingQuarantineReport
                    .Select(q => new
                    {
                        platformId = q.PlatformId,
                        loadoutId = q.LoadoutId,
                        mountId = q.MountId,
                        weaponRef = q.WeaponRef,
                        reason = q.Reason,
                        sourceFile = q.SourceFile,
                    })
                    .ToArray(),
            };
            File.WriteAllText(reportPath, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

            Assert.That(File.Exists(reportPath), Is.True);
            var json = File.ReadAllText(reportPath);
            Assert.That(json, Does.Contain("orphan_weapon_id"));
            Assert.That(json, Does.Contain("nonexistent-orphan-missile"));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }

            if (File.Exists(markdownPath))
            {
                File.Delete(markdownPath);
            }

            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }
        }
    }

    [Test]
    public void ProposePlatformsFromMarkdown_stages_baltic_fixture_with_mount_batches()
    {
        var markdown = CmoMarkdownImporter.ResolveBalticPlatformFixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s26-platform-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdown,
                mapBalticPlatformIds: true,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(2603));

            Assert.That(result.ParsedCount, Is.EqualTo(3));
            Assert.That(result.Batches.Count, Is.EqualTo(3));
            Assert.That(result.Batches[0].RecordCount, Is.EqualTo(3));
            Assert.That(result.Batches[1].RecordCount, Is.EqualTo(4));
            Assert.That(result.Batches[2].RecordCount, Is.EqualTo(3));
            Assert.That(result.QuarantinedCount, Is.EqualTo(4));
            Assert.That(result.FittingQuarantineReport.Count, Is.EqualTo(4));

            foreach (var batch in result.Batches)
            {
                using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2604));
                Assert.That(gate.ApproveBatch(batch.BatchId, "human", "s26-platform").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using (var platformCmd = connection.CreateCommand())
            {
                platformCmd.CommandText =
                    """
                    SELECT COUNT(*) FROM platform
                    WHERE platform_id IN ('u1', 'hostile-1', 'hostile-far')
                    """;
                Assert.That(Convert.ToInt32(platformCmd.ExecuteScalar(), CultureInfo.InvariantCulture), Is.EqualTo(3));
            }

            using (var mountCmd = connection.CreateCommand())
            {
                mountCmd.CommandText = "SELECT COUNT(*) FROM platform_mount";
                // Seed (gun-76, vls-fwd) + 4 CMO Baltic fixture mounts.
                Assert.That(Convert.ToInt32(mountCmd.ExecuteScalar(), CultureInfo.InvariantCulture), Is.EqualTo(6));
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

    private static string WritePlatformOrphanFixture()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-platform-orphan-{Guid.NewGuid():N}.md");
        File.WriteAllText(
            path,
            """
            # Platform quarantine fixture (S27-14)

            ### Orphan Mount Ship , Fixture
            <sub>[/ship/95001/](https://cmano-db.com/ship/95001/)</sub>

            | Field | Value |
            |---|---|
            | Type | Frigate |
            | Nationality | NATO |

            **Weapons**

            - Nonexistent Orphan Missile — Guided Weapon — Surface Max: 100 km.

            """);
        return path;
    }
}