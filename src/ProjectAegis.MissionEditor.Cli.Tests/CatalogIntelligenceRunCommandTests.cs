using System.Text.Json;
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
}