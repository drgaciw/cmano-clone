using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownPlatformImportTests
{
    [Fact]
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

            Assert.Equal(100, result.ParsedCount);
            Assert.Equal(100, result.ApprovedCount);
            Assert.Equal(0, result.QuarantinedCount);
            Assert.Single(result.Batches);
            Assert.Equal(100, result.Batches[0].RecordCount);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2715)))
            {
                Assert.True(gate.ApproveBatch(result.Batches[0].BatchId, "human", "s27-14-ship").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM platform WHERE platform_id LIKE 'test-ship-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
            Assert.Equal(100, count);
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
    public void Reference_ship_markdown_parses_4844_records_for_off_ci_nightly_scale()
    {
        var path = CmoMarkdownImporter.ResolveReferenceShipMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path);
        Assert.Equal(4844, platforms.Count);

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(platforms, chunkSize: 500);
        Assert.Equal(10, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Equal(500, chunks[8].Length);
        Assert.Equal(344, chunks[9].Length);
    }

    [Fact]
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

        Assert.Equal(2, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Single(chunks[1]);
        Assert.Equal("test-ship-chunk-0000", chunks[0][0].PlatformId);
    }

    [Fact]
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

            Assert.Equal(1, result.ParsedCount);
            Assert.Equal(1, result.QuarantinedCount);
            Assert.Single(result.FittingQuarantineReport);
            Assert.Equal("orphan_weapon_id", result.FittingQuarantineReport[0].Reason);

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

            Assert.True(File.Exists(reportPath));
            var json = File.ReadAllText(reportPath);
            Assert.Contains("orphan_weapon_id", json);
            Assert.Contains("nonexistent-orphan-missile", json);
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

    [Fact]
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

            Assert.Equal(3, result.ParsedCount);
            Assert.Equal(3, result.Batches.Count);
            Assert.Equal(3, result.Batches[0].RecordCount);
            Assert.Equal(4, result.Batches[1].RecordCount);
            Assert.Equal(3, result.Batches[2].RecordCount);
            Assert.Equal(4, result.QuarantinedCount);
            Assert.Equal(4, result.FittingQuarantineReport.Count);

            foreach (var batch in result.Batches)
            {
                using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2604));
                Assert.True(gate.ApproveBatch(batch.BatchId, "human", "s26-platform").Committed);
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
                Assert.Equal(3, Convert.ToInt32(platformCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
            }

            using (var mountCmd = connection.CreateCommand())
            {
                mountCmd.CommandText = "SELECT COUNT(*) FROM platform_mount";
                // Seed (gun-76, vls-fwd) + 4 CMO Baltic fixture mounts.
                Assert.Equal(6, Convert.ToInt32(mountCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
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