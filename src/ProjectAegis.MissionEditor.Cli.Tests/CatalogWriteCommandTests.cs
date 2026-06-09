using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogWriteCommandTests
{
    [Fact]
    public void catalog_write_propose_then_approve_commits_sensor_binding()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-write-{Guid.NewGuid():N}.db");
        try
        {
            using (var proposeOut = new StringWriter())
            {
                Assert.Equal(0, CatalogWriteProposeCommand.Run(
                    dbPath, "u-cli-test", "radar-cli", 0.55, proposeOut));
                var proposeJson = proposeOut.ToString();
                Assert.Contains("\"ok\": true", proposeJson);
                using var doc = JsonDocument.Parse(proposeJson);
                var batchId = doc.RootElement.GetProperty("batchId").GetString();
                Assert.False(string.IsNullOrWhiteSpace(batchId));

                using var approveOut = new StringWriter();
                Assert.Equal(0, CatalogWriteApproveCommand.Run(dbPath, batchId!, approveOut));
                var approveJson = approveOut.ToString();
                Assert.Contains("\"ok\": true", approveJson);
                Assert.Contains("\"snapshotId\":", approveJson);
            }

            using var reader = new SqliteCatalogReader(dbPath, "cli-test");
            Assert.True(reader.TryGetBasePd("u-cli-test", "radar-cli", out var basePd));
            Assert.Equal(0.55, basePd, 3);
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Fact]
    public void catalog_write_approve_missing_db_returns_error()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-missing-{Guid.NewGuid():N}.db");
        using var output = new StringWriter();
        Assert.Equal(1, CatalogWriteApproveCommand.Run(dbPath, "batch-nope", output));
        Assert.Contains("database_not_found", output.ToString());
    }
}