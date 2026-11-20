using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Import;

[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class CmoMarkdownWeaponImportTests
{
    [Test]
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

            Assert.That(result.ParsedCount, Is.EqualTo(50));
            Assert.That(result.ApprovedCount, Is.EqualTo(50));
            Assert.That(result.Batches, Has.Exactly(1).Items);
            Assert.That(result.Batches[0].RecordCount, Is.EqualTo(50));

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(2603)))
            {
                Assert.That(gate.ApproveBatch(result.Batches[0].BatchId, "human", "s26-weapon").Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM weapon_catalog WHERE weapon_id LIKE 'cmo-weapon-%'";
            var count = Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(count, Is.EqualTo(50));
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
    public void Reference_weapon_markdown_parses_4403_records_for_off_ci_nightly_scale()
    {
        var path = CmoMarkdownImporter.ResolveReferenceWeaponMarkdownPath();
        if (!File.Exists(path))
        {
            return;
        }

        var weapons = CmoMarkdownImporter.ReadWeaponBindings(path);
        Assert.That(weapons.Count, Is.EqualTo(4403));

        var chunks = CmoMarkdownImportProposer.ChunkWeapons(weapons, chunkSize: 500);
        Assert.That(chunks.Length, Is.EqualTo(9));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[7].Length, Is.EqualTo(500));
        Assert.That(chunks[8].Length, Is.EqualTo(403));
    }

    [Test]
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

        Assert.That(chunks.Length, Is.EqualTo(2));
        Assert.That(chunks[0].Length, Is.EqualTo(500));
        Assert.That(chunks[1], Has.Exactly(1).Items);
        Assert.That(chunks[0][0].WeaponId, Is.EqualTo("cmo-weapon-0000"));
    }
}