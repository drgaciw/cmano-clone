using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownWeaponImportTests
{
    [Fact]
    public void ProposeWeaponsFromMarkdown_stages_slice50_and_approve_commits()
    {
        var markdown = CmoMarkdownImporter.ResolveWeaponSlice50FixturePath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-s26-weapon-{Guid.NewGuid():N}.db");

        try
        {
            var result = CmoMarkdownImportProposer.ProposeWeaponsFromMarkdown(
                dbPath,
                markdown,
                maxRecords: null,
                chunkSize: 500,
                clock: new FixedCatalogClock(2602));

            Assert.Equal(50, result.ParsedCount);
            Assert.Equal(50, result.ApprovedCount);
            Assert.Single(result.Batches);
            Assert.Equal(50, result.Batches[0].RecordCount);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2603)))
            {
                Assert.True(gate.ApproveBatch(result.Batches[0].BatchId, "human", "s26-weapon").Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM weapon_catalog WHERE weapon_id LIKE 'cmo-weapon-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.Equal(50, count);
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
    public void Reference_weapon_markdown_parses_4403_records_for_off_ci_nightly_scale()
    {
        var path = CmoMarkdownImporter.ResolveReferenceWeaponMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        Assert.Equal(4403, weapons.Count);

        var chunks = CmoMarkdownImportProposer.ChunkWeapons(weapons, chunkSize: 500);
        Assert.Equal(9, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Equal(500, chunks[7].Length);
        Assert.Equal(403, chunks[8].Length);
    }

    [Fact]
    public void ChunkWeapons_with_501_rows_produces_two_batches_at_chunk_size_500()
    {
        var rows = Enumerable.Range(0, 501)
            .Select(i => new CatalogWeaponRecord(
                $"cmo-weapon-{i:D4}",
                DisplayName: $"Weapon {i}",
                MinRangeMeters: 0,
                MaxRangeMeters: 10_000,
                WeaponType: "Guided Weapon",
                ReviewState: CatalogReviewStates.Provisional,
                SourceFactId: $"fact-{i}",
                ImportBatchId: "chunk-test",
                SourceFile: "chunk-test.md"))
            .ToArray();

        var chunks = CmoMarkdownImportProposer.ChunkWeapons(rows, chunkSize: 500);

        Assert.Equal(2, chunks.Length);
        Assert.Equal(500, chunks[0].Length);
        Assert.Single(chunks[1]);
        Assert.Equal("cmo-weapon-0000", chunks[0][0].WeaponId);
    }
}