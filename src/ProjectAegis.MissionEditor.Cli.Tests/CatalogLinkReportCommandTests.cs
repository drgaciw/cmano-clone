using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogLinkReportCommandTests
{
    [Fact]
    public void LinkReport_catalog_link_report_baltic_golden_on_clean_seed()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-link-report-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var writer = new StringWriter();
            Assert.Equal(0, CatalogLinkReportCommand.Run(dbPath, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("catalog_link_report", root.GetProperty("verb").GetString());
            Assert.Equal(2, root.GetProperty("linkCount").GetInt32());
            Assert.Equal(
                LinkCatalogGoldenHashes.BalticPatrol,
                root.GetProperty("linksHash").GetString());

            var lines = root.GetProperty("canonicalLines").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.Equal(lines.OrderBy(l => l, StringComparer.Ordinal).ToArray(), lines);
            Assert.Equal(
                [
                    "NATO_TADIL_J|NATO Link 16|tactical|50",
                    "SATCOM_B|SATCOM Wideband|satcom|250",
                ],
                lines);

            var linkIds = root.GetProperty("links").EnumerateArray()
                .Select(link => link.GetProperty("linkId").GetString() ?? string.Empty)
                .ToArray();
            Assert.Equal(["NATO_TADIL_J", "SATCOM_B"], linkIds);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void LinkReport_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogLinkReportCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.Contains("catalog_link_report", help);
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