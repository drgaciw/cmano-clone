using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Import;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogWriteCommandTests
{
    [Test]
    public void catalog_write_propose_then_approve_commits_sensor_binding()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-write-{Guid.NewGuid():N}.db");
        try
        {
            using (var proposeOut = new StringWriter())
            {
                Assert.That(CatalogWriteProposeCommand.Run(
                    dbPath, "u-cli-test", "radar-cli", 0.55, proposeOut), Is.EqualTo(0));
                var proposeJson = proposeOut.ToString();
                Assert.That(proposeJson, Does.Contain("\"ok\": true"));
                using var doc = JsonDocument.Parse(proposeJson);
                var batchId = doc.RootElement.GetProperty("batchId").GetString();
                Assert.That(string.IsNullOrWhiteSpace(batchId), Is.False);
                var approvedBatchId = batchId!;

                using var approveOut = new StringWriter();
                Assert.That(CatalogWriteApproveCommand.Run(dbPath, approvedBatchId, approveOut), Is.EqualTo(0));
                var approveJson = approveOut.ToString();
                Assert.That(approveJson, Does.Contain("\"ok\": true"));
                Assert.That(approveJson, Does.Contain("\"snapshotId\":"));
            }

            using var reader = new SqliteCatalogReader(dbPath, "cli-test");
            Assert.That(reader.TryGetBasePd("u-cli-test", "radar-cli", out var basePd), Is.True);
            Assert.That(basePd, Is.EqualTo(0.55).Within(0.0005));
        }
        finally
        {
            if (File.Exists(dbPath)) File.Delete(dbPath);
        }
    }

    [Test]
    public void catalog_import_markdown_then_write_approve_records_snapshot_hash_for_platform_slice()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-nightly-approve-{Guid.NewGuid():N}.db");
        var markdown = CmoMarkdownImporter.ResolveShipSlice100FixturePath();

        try
        {
            using (var importOut = new StringWriter())
            {
                Assert.That(CatalogImportMarkdownCommand.Run(
                        dbPath,
                        markdown,
                        maxRecords: 12,
                        chunkSize: 500,
                        importOut,
                        entity: CmoMarkdownImportEntity.Platform), Is.EqualTo(0));

                using var doc = JsonDocument.Parse(importOut.ToString());
                var batches = doc.RootElement.GetProperty("batches");
                Assert.That(batches.GetArrayLength(), Is.EqualTo(1));
                var batchId = batches[0].GetProperty("batchId").GetString();
                Assert.That(string.IsNullOrWhiteSpace(batchId), Is.False);
                var approvedBatchId = batchId!;

                using var approveOut = new StringWriter();
                Assert.That(CatalogWriteApproveCommand.Run(
                    dbPath,
                    approvedBatchId,
                    approveOut,
                    releaseVersion: "cli-nightly-platform-s29-03"), Is.EqualTo(0));

                using var approveDoc = JsonDocument.Parse(approveOut.ToString());
                var root = approveDoc.RootElement;
                Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
                var hash = root.GetProperty("contentHashSha256").GetString();
                Assert.That(hash, Does.Match("^[a-f0-9]{64}$"));
                Assert.That(root.GetProperty("releaseVersion").GetString(), Is.EqualTo("cli-nightly-platform-s29-03"));
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

    [Test]
    public void catalog_write_approve_default_omits_balance_drift_advisory()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-balance-default-{Guid.NewGuid():N}.db");
        try
        {
            using var proposeOut = new StringWriter();
            Assert.That(CatalogWriteProposeCommand.Run(
                dbPath, "u-cli-balance", "radar-balance", 0.55, proposeOut), Is.EqualTo(0));
            using var proposeDoc = JsonDocument.Parse(proposeOut.ToString());
            var batchId = proposeDoc.RootElement.GetProperty("batchId").GetString();

            using var approveOut = new StringWriter();
            Assert.That(CatalogWriteApproveCommand.Run(dbPath, batchId!, approveOut), Is.EqualTo(0));
            using var approveDoc = JsonDocument.Parse(approveOut.ToString());
            Assert.That(approveDoc.RootElement.TryGetProperty("balanceDriftAdvisory", out _), Is.False);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Test]
    public void catalog_write_approve_enable_balance_drift_includes_advisory_payload()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-balance-enabled-{Guid.NewGuid():N}.db");
        try
        {
            using var proposeOut = new StringWriter();
            Assert.That(CatalogWriteProposeCommand.Run(
                dbPath, "u-cli-balance-on", "radar-balance-on", 0.55, proposeOut), Is.EqualTo(0));
            using var proposeDoc = JsonDocument.Parse(proposeOut.ToString());
            var batchId = proposeDoc.RootElement.GetProperty("batchId").GetString();

            using var approveOut = new StringWriter();
            Assert.That(CatalogWriteApproveCommand.Run(
                dbPath,
                batchId!,
                approveOut,
                enableBalanceDrift: true), Is.EqualTo(0));

            using var approveDoc = JsonDocument.Parse(approveOut.ToString());
            var advisory = approveDoc.RootElement.GetProperty("balanceDriftAdvisory");
            Assert.That(advisory.GetProperty("driftDetectionEnabled").GetBoolean(), Is.True);
            Assert.That(advisory.GetProperty("findingCount").GetInt32(), Is.EqualTo(0));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Test]
    public void catalog_write_approve_missing_db_returns_error()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-missing-{Guid.NewGuid():N}.db");
        using var output = new StringWriter();
        Assert.That(CatalogWriteApproveCommand.Run(dbPath, "batch-nope", output), Is.EqualTo(1));
        Assert.That(output.ToString(), Does.Contain("database_not_found"));
    }
}