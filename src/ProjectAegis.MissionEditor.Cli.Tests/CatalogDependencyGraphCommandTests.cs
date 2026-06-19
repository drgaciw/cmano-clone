using System.Text.Json;
using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogDependencyGraphCommandTests
{
    [Fact]
    public void DependencyGraph_catalog_dependency_graph_emits_sorted_edges()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-cli-dep-graph-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);

            using var writer = new StringWriter();
            Assert.Equal(0, CatalogDependencyGraphCommand.Run(dbPath, writer));

            using var doc = JsonDocument.Parse(writer.ToString());
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("catalog_dependency_graph", root.GetProperty("verb").GetString());
            Assert.True(root.GetProperty("edgeCount").GetInt32() >= 0);

            var lines = root.GetProperty("canonicalLines").EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
            Assert.Equal(lines.OrderBy(l => l, StringComparer.Ordinal).ToArray(), lines);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void DependencyGraph_help_text_is_registered()
    {
        using var writer = new StringWriter();
        CatalogDependencyGraphCommand.PrintHelp(writer);
        var help = writer.ToString();
        Assert.Contains("catalog_dependency_graph", help);
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