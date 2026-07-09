using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogIntelligenceRunCommandTests
{
    [Fact]
    public void catalog_intelligence_run_passes_on_baltic_fixture_without_db_flag()
    {
        using var output = new StringWriter();
        var exit = CatalogIntelligenceRunCommand.Run(databasePath: null, output);
        Assert.Equal(0, exit);
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var agents = doc.RootElement.GetProperty("agents");
        Assert.True(agents.GetArrayLength() >= 1);
        var tools = doc.RootElement.GetProperty("mcpTools").EnumerateArray()
            .Select(t => t.GetString()).ToList();
        Assert.Contains("catalog_write_propose", tools);
        Assert.Contains("catalog_import_markdown", tools);
    }

    [Fact]
    public void catalog_intelligence_run_passes_on_shipped_baltic_patrol_sqlite()
    {
        var dbPath = CatalogReaderFactory.ResolveBalticPatrolDatabasePath();
        Assert.False(string.IsNullOrWhiteSpace(dbPath));
        Assert.True(File.Exists(dbPath!), $"Expected Baltic seed at {dbPath}");

        using var output = new StringWriter();
        var exit = CatalogIntelligenceRunCommand.Run(dbPath, output);
        Assert.Equal(0, exit);

        using var doc = JsonDocument.Parse(output.ToString());
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());

        var agentIds = doc.RootElement.GetProperty("agents").EnumerateArray()
            .Select(a => a.GetProperty("agentId").GetString())
            .ToList();
        Assert.Contains("entity_resolution", agentIds);
        Assert.Contains("rules_validation", agentIds);
        Assert.Contains("consistency_normalization", agentIds);
        Assert.Contains("diff_proposal", agentIds);

        foreach (var agent in doc.RootElement.GetProperty("agents").EnumerateArray())
            Assert.True(agent.GetProperty("passed").GetBoolean(), agent.GetProperty("agentId").GetString());
    }
}