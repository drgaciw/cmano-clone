using System.Globalization;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

/// <summary>S30-11 — curated aircraft/submarine/facility nightly slices + off-CI scale metadata.</summary>
[Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownEntitySliceImportTests
{
    [TestCase("aircraft")]
    [TestCase("submarine")]
    [TestCase("facility")]
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

            Assert.That(result.ParsedCount, Is.EqualTo(100));
            Assert.That(result.ApprovedCount, Is.EqualTo(100));
            Assert.That(result.QuarantinedCount, Is.EqualTo(0));
            Assert.That(result.Batches, Has.Exactly(1).Items);
            Assert.That(result.Batches[0].RecordCount, Is.EqualTo(100));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(30120 + domain.Length)))
            {
                Assert.That(gate.ApproveBatch(result.Batches[0].BatchId, "human", $"s30-11-{domain}").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT COUNT(*) FROM platform WHERE platform_id LIKE '{slugPrefix}-%'";
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
    [TestCase("aircraft", 7387, 15, 387)]
    [TestCase("submarine", 732, 2, 232)]
    [TestCase("facility", 4511, 10, 11)]
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
        Assert.That(platforms.Count, Is.EqualTo(expectedCount));

        var chunks = CmoMarkdownImportProposer.ChunkPlatforms(platforms, chunkSize: 500);
        Assert.That(chunks.Length, Is.EqualTo(expectedBatches));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[^1].Length, Is.EqualTo(expectedLastChunk));
    }
    [TestCase("aircraft")]
    [TestCase("submarine")]
    [TestCase("facility")]
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

            Assert.That(proposed.ParsedCount, Is.EqualTo(12));
            Assert.That(proposed.Batches, Is.Not.Empty);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(30140 + domain.Length)))
            {
                foreach (var batch in proposed.Batches)
                {
                    Assert.That(gate.ApproveBatch(batch.BatchId, "human", "nightly-curator").Committed, Is.True);
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

            Assert.That(bind.ContentHashSha256, Does.Match("^[a-f0-9]{64}$"));
            Assert.That(bind.ReleaseVersion, Is.EqualTo($"nightly-{domain}-s30-11"));

            using var store = new DbSnapshotStore(dbPath);
            Assert.That(store.TryGetContentHash(bind.SnapshotId, out var storedHash), Is.True);
            Assert.That(storedHash, Is.EqualTo(bind.ContentHashSha256));
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
    [TestCase("aircraft", CatalogSortKeyGoldenHashes.AircraftSlice100PlatformV2)]
    [TestCase("submarine", CatalogSortKeyGoldenHashes.SubmarineSlice100PlatformV2)]
    [TestCase("facility", CatalogSortKeyGoldenHashes.FacilitySlice100PlatformV2)]
    public void Entity_slice100_reimport_preserves_catalog_ordering_hash(string domain, string expectedHash)
    {
        var markdown = ResolveSlice(domain).markdown;
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s30-golden-{domain}-{Guid.NewGuid():N}.db");

        try
        {
            var hashBefore = ImportApproveAndHashPlatforms(dbPath, markdown, clockSeed: 30160 + domain.Length);
            var hashAfter = ImportApproveAndHashPlatforms(dbPath, markdown, clockSeed: 30170 + domain.Length);
            Assert.That(hashAfter, Is.EqualTo(hashBefore));
            Assert.That(hashBefore, Is.EqualTo(expectedHash));
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
                Assert.That(gate.ApproveBatch(batch.BatchId, "human", "s30-golden").Committed, Is.True);
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