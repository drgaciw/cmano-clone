using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S30-11 — curated aircraft/submarine/facility nightly slices + off-CI scale metadata.</summary>
[Collection("CatalogSqlite")]
public sealed class CmoMarkdownEntitySliceImportTests
{
    [Theory]
    [InlineData("aircraft")]
    [InlineData("submarine")]
    [InlineData("facility")]
    public void Propose_entity_slice100_and_approve_commits_via_WriteGate(string domain)
    {
        var (markdown, slugPrefix) = ResolveSlice(domain);
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s30-{domain}-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdown,
                mapBalticPlatformIds: false,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(30110 + domain.Length));

            Assert.Equal(100, result.ParsedCount);
            Assert.Equal(100, result.ApprovedCount);
            Assert.Equal(0, result.QuarantinedCount);
            Assert.Single(result.Batches);
            Assert.Equal(100, result.Batches[0].RecordCount);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(30120 + domain.Length)))
            {
                Assert.True(gate.ApproveBatch(result.Batches[0].BatchId, "human", $"s30-11-{domain}").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM platform WHERE platform_id LIKE '{slugPrefix}-%'";
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

    [Theory]
    [InlineData("aircraft", 7387, 15, 387)]
    [InlineData("submarine", 732, 2, 232)]
    [InlineData("facility", 4511, 10, 11)]
    public void Reference_entity_markdown_parses_expected_records_for_off_ci_nightly_scale(
        string domain,
        int expectedCount,
        int expectedBatches,
        int expectedLastChunk)
    {
        var path = ResolveReference(domain);
        if (!File.Exists(path))
        {
            return;
        }

        var platforms = CmoMarkdownImporter.ReadPlatformBindings(path);
        Assert.Equal(expectedCount, platforms.Count);

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(platforms, chunkSize: 500);
        Assert.Equal(expectedBatches, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Equal(expectedLastChunk, chunks[^1].Length);
    }

    [Theory]
    [InlineData("aircraft")]
    [InlineData("submarine")]
    [InlineData("facility")]
    public void Nightly_entity_slice_propose_approve_records_pinned_snapshot_hash(string domain)
    {
        var markdown = ResolveSlice(domain).markdown;
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s30-nightly-{domain}-{Guid.NewGuid():N}.db");

        try
        {
            var proposed = CmoMarkdownImportProposer.ProposePlatformsFromMarkdown(
                dbPath,
                markdown,
                mapBalticPlatformIds: false,
                maxRecords: 12,
                chunkSize: 500,
                clock: new FixedCatalogClock(30130 + domain.Length));

            Assert.Equal(12, proposed.ParsedCount);
            Assert.NotEmpty(proposed.Batches);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(30140 + domain.Length)))
            {
                foreach (var batch in proposed.Batches)
                {
                    Assert.True(gate.ApproveBatch(batch.BatchId, "human", "nightly-curator").Committed);
                }
            }

            var platformBatchId = proposed.Batches
                .First(batch => batch.BatchId.StartsWith("batch-platform-", StringComparison.Ordinal))
                .BatchId;

            var bind = CatalogSnapshotBinder.BindAfterApprove(
                dbPath,
                platformBatchId,
                new FixedCatalogClock(30150 + domain.Length),
                releaseVersion: $"nightly-{domain}-s30-11");

            Assert.Matches("^[a-f0-9]{64}$", bind.ContentHashSha256);
            Assert.Equal($"nightly-{domain}-s30-11", bind.ReleaseVersion);

            using var store = new DbSnapshotStore(dbPath);
            Assert.True(store.TryGetContentHash(bind.SnapshotId, out var storedHash));
            Assert.Equal(bind.ContentHashSha256, storedHash);
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

    [Theory]
    [InlineData("aircraft", CatalogSortKeyGoldenHashes.AircraftSlice100PlatformV2)]
    [InlineData("submarine", CatalogSortKeyGoldenHashes.SubmarineSlice100PlatformV2)]
    [InlineData("facility", CatalogSortKeyGoldenHashes.FacilitySlice100PlatformV2)]
    public void Entity_slice100_reimport_preserves_catalog_ordering_hash(string domain, string expectedHash)
    {
        var markdown = ResolveSlice(domain).markdown;
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s30-golden-{domain}-{Guid.NewGuid():N}.db");

        try
        {
            var hashBefore = ImportApproveAndHashPlatforms(dbPath, markdown, clockSeed: 30160 + domain.Length);
            var hashAfter = ImportApproveAndHashPlatforms(dbPath, markdown, clockSeed: 30170 + domain.Length);
            Assert.Equal(hashBefore, hashAfter);
            Assert.Equal(expectedHash, hashBefore);
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

    private static (string markdown, string slugPrefix) ResolveSlice(string domain) =>
        domain switch
        {
            "aircraft" => (CmoMarkdownImporter.ResolveAircraftSlice100FixturePath(), "test-aircraft"),
            "submarine" => (CmoMarkdownImporter.ResolveSubmarineSlice100FixturePath(), "test-submarine"),
            "facility" => (CmoMarkdownImporter.ResolveFacilitySlice100FixturePath(), "test-facility"),
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown entity domain"),
        };

    private static string ResolveReference(string domain) =>
        domain switch
        {
            "aircraft" => CmoMarkdownImporter.ResolveReferenceAircraftMarkdownPath(),
            "submarine" => CmoMarkdownImporter.ResolveReferenceSubmarineMarkdownPath(),
            "facility" => CmoMarkdownImporter.ResolveReferenceFacilityMarkdownPath(),
            _ => throw new ArgumentOutOfRangeException(nameof(domain), domain, "Unknown entity domain"),
        };

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
                Assert.True(gate.ApproveBatch(batch.BatchId, "human", "s30-golden").Committed);
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