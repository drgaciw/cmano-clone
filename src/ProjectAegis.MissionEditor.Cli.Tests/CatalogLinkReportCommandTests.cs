using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogLinkReportCommandTests
{
    [Test]
    public void LinkReport_catalog_link_report_baltic_golden_on_clean_seed()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-link-report-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var writer = new StringWriter();
            Assert.That(CatalogLinkReportCommand.Run(dbPath, writer), Is.EqualTo(0));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.That(root.GetProperty("ok").GetBoolean(), Is.True);
            Assert.That(root.GetProperty("verb").GetString(), Is.EqualTo("catalog_link_report"));
            Assert.That(root.GetProperty("linkCount").GetInt32(), Is.EqualTo(2));
            Assert.That(root.GetProperty("linksHash").GetString(), Is.EqualTo(LinkCatalogGoldenHashes.BalticPatrol));

            var lines = root.GetProperty("canonicalLines").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.That(lines, Is.EqualTo(lines.OrderBy(l => l, StringComparer.Ordinal).ToArray()));
            Assert.That(lines, Is.EqualTo([
                    "NATO_TADIL_J|NATO Link 16|tactical|50",
                    "SATCOM_B|SATCOM Wideband|satcom|250",
                ]));

            var linkIds = root.GetProperty("links").EnumerateArray()
                .Select(link => link.GetProperty("linkId").GetString() ?? string.Empty)
                .ToArray();
            Assert.That(linkIds, Is.EqualTo(["NATO_TADIL_J", "SATCOM_B"]));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void LinkReport_reported_database_path_reflects_actual_source_when_input_path_missing()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-link-missing-{Guid.NewGuid():N}.db");
        Assert.That(File.Exists(missingPath), Is.False);

        using var writer = new StringWriter();
        Assert.That(CatalogLinkReportCommand.Run(missingPath, writer), Is.EqualTo(0));

        using var doc = JsonDocument.Parse(writer.ToString());
        var reportedPath = doc.RootElement.GetProperty("databasePath").GetString();

        Assert.That(reportedPath, Is.Not.EqualTo(missingPath));
        Assert.That(reportedPath, Is.EqualTo(CatalogReaderFactory.ResolveBalticPatrolDatabasePath()));
    }

    [Test]
    public void LinkReport_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogLinkReportCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.That(help, Does.Contain("catalog_link_report"));
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