using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogKillChainReportCommandTests
{
    [Fact]
    public void KillChain_catalog_kill_chain_report_empty_golden_on_clean_baltic()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-kc-report-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var writer = new StringWriter();
            Assert.Equal(0, CatalogKillChainReportCommand.Run(dbPath, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("catalog_kill_chain_report", root.GetProperty("verb").GetString());
            Assert.True(root.GetProperty("isEmpty").GetBoolean());
            Assert.Equal(0, root.GetProperty("findingCount").GetInt32());
            Assert.Equal(
                KillChainGoldenHashes.BalticPatrolClean,
                root.GetProperty("findingsHash").GetString());

            var lines = root.GetProperty("canonicalLines").EnumerateArray().ToArray();
            Assert.Empty(lines);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void KillChain_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogKillChainReportCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.Contains("catalog_kill_chain_report", help);
        Assert.Contains("Read-only", help);
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