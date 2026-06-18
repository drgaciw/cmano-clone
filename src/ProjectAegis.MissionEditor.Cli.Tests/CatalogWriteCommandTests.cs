using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
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
    public void catalog_import_markdown_then_write_approve_records_snapshot_hash_for_platform_slice()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-nightly-approve-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveShipSlice100FixturePath();

        try
        {
            using (var importOut = new StringWriter())
            {
                Assert.Equal(
                    0,
                    CatalogImportMarkdownCommand.Run(
                        dbPath,
                        markdown,
                        maxRecords: 12,
                        chunkSize: 500,
                        importOut,
                        entity: CmoMarkdownImportEntity.Platform));

                using var doc = JsonDocument.Parse(importOut.ToString());
                var batches = doc.RootElement.GetProperty("batches");
                Assert.Equal(1, batches.GetArrayLength());
                var batchId = batches[0].GetProperty("batchId").GetString();
                Assert.False(string.IsNullOrWhiteSpace(batchId));

                using var approveOut = new StringWriter();
                Assert.Equal(0, CatalogWriteApproveCommand.Run(
                    dbPath,
                    batchId!,
                    approveOut,
                    releaseVersion: "cli-nightly-platform-s29-03"));

                using var approveDoc = JsonDocument.Parse(approveOut.ToString());
                var root = approveDoc.RootElement;
                Assert.True(root.GetProperty("ok").GetBoolean());
                var hash = root.GetProperty("contentHashSha256").GetString();
                Assert.Matches("^[a-f0-9]{64}$", hash);
                Assert.Equal("cli-nightly-platform-s29-03", root.GetProperty("releaseVersion").GetString());
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
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