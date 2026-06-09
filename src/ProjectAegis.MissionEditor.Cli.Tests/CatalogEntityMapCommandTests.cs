using System.Text.Json;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class CatalogEntityMapCommandTests
{
    [Fact]
    public void catalog_entity_map_emits_sorted_entity_table_metadata()
    {
        using var output = new StringWriter();
        Assert.Equal(0, CatalogEntityMapCommand.Run(output));
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
        var entities = doc.RootElement.GetProperty("entities");
        Assert.True(entities.GetArrayLength() >= 3);
        var names = entities.EnumerateArray()
            .Select(e => e.GetProperty("entity").GetString())
            .Where(n => n is not null)
            .Cast<string>()
            .ToList();
        Assert.Equal(names.OrderBy(n => n, StringComparer.Ordinal), names);
        Assert.Contains("CatalogSensorBinding", names);
    }
}