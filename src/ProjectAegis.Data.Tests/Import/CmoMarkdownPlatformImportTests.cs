using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Import;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Import;

[Collection("CatalogSqlite")]
public sealed class CmoMarkdownPlatformImportTests
{
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
            Assert.Equal(2, result.Batches.Count);
            Assert.Equal(3, result.Batches[0].RecordCount);
            Assert.Equal(4, result.Batches[1].RecordCount);

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
                Assert.Equal(4, Convert.ToInt32(mountCmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
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
}