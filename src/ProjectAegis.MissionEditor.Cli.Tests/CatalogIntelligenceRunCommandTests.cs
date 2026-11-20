using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogIntelligenceRunCommandTests
{
    [Test]
    public void catalog_intelligence_run_passes_on_baltic_fixture_without_db_flag()
    {
        using var output = new StringWriter();
        var exit = CatalogIntelligenceRunCommand.Run(databasePath: null, output);
        Assert.That(exit, Is.EqualTo(0));
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
        var agents = doc.RootElement.GetProperty("agents");
        Assert.That(agents.GetArrayLength() >= 1, Is.True);
        var tools = doc.RootElement.GetProperty("mcpTools").EnumerateArray()
            .Select(t => t.GetString()).ToList();
        Assert.That(tools, Does.Contain("catalog_write_propose"));
        Assert.That(tools, Does.Contain("catalog_import_markdown"));
    }
}