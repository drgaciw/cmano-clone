using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogKillChainReportCommandTests
{
    [Test]
    public void KillChain_catalog_kill_chain_report_empty_golden_on_clean_baltic()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-kc-report-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var writer = new StringWriter();
            Assert.That(CatalogKillChainReportCommand.Run(dbPath, writer), Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("catalog_kill_chain_report"));
            Assert.That(root.GetProperty("isEmpty").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("findingCount").GetInt32(), Is.EqualTo(0));
            Assert.That(root.GetProperty("findingsHash").GetString(), Is.EqualTo(KillChainGoldenHashes.BalticPatrolClean));

            var lines = root.GetProperty("canonicalLines").EnumerateArray().ToArray();
            Assert.That(lines, Is.Empty);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void KillChain_reported_database_path_reflects_actual_source_when_input_path_missing()
    {
        // A caller who mistypes/loses their --db path should never receive a report
        // that silently claims the data came from a database that was never opened.
        var missingPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-kc-missing-{Guid.NewGuid():N}.db");
        Assert.That(File.Exists(missingPath), Is.False);

        using var writer = new StringWriter();
        Assert.That(CatalogKillChainReportCommand.Run(missingPath, writer), Is.EqualTo(0));

        using var doc = JsonDocument.Parse(writer.ToString());
        var reportedPath = doc.RootElement.GetProperty("databasePath").GetString();

        Assert.That(reportedPath, Is.Not.EqualTo(missingPath));
        Assert.That(reportedPath, Is.EqualTo(CatalogReaderFactory.ResolveBalticPatrolDatabasePath()));
    }

    [Test]
    public void KillChain_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogKillChainReportCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.That(help, Does.Contain("catalog_kill_chain_report"));
        Assert.That(help, Does.Contain("Read-only"));
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}