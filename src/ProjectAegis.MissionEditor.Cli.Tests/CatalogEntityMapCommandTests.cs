using System.Text.Json;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class CatalogEntityMapCommandTests
{
    [Test]
    public void catalog_entity_map_emits_sorted_entity_table_metadata()
    {
        using var output = new StringWriter();
        Assert.That(CatalogEntityMapCommand.Run(output), Is.EqualTo(0));
        var json = output.ToString();
        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("ok").GetBoolean(), Is.True);
        var entities = doc.RootElement.GetProperty("entities");
        Assert.That(entities.GetArrayLength() >= 3, Is.True);
        var names = entities.EnumerateArray()
            .Select(e => e.GetProperty("entity").GetString())
            .Where(n => n is not null)
            .Cast<string>()
            .ToList();
        Assert.That(names, Is.EqualTo(names.OrderBy(n => n, StringComparer.Ordinal)));
        Assert.That(names, Does.Contain("CatalogSensorBinding"));
    }
}