using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

/// <summary>
/// Explicit JSON null and null-forgiving initialization must not store null nested collections.
/// </summary>
public sealed class ScenarioNullCollectionContractTests
{
    [Fact]
    public void Load_stores_explicit_null_conditions_and_postures_as_empty()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {
                    "sides": [{ "id": "blue", "name": "Blue", "postures": null }],
                    "events": [{ "id": "e1", "triggerType": "Time", "conditions": null, "actions": [{ "type": "Message" }] }]
                }
                """);

            var tolerant = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var strict = ScenarioDocumentJsonLoader.LoadFromFile(path, strict: true);

            AssertEmptyConditionsAndPostures(tolerant);
            AssertEmptyConditionsAndPostures(strict);
            Assert.Equal("Message", strict.Events![0].Actions[0].Type);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_keeps_populated_conditions_and_postures()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {
                    "sides": [{ "id": "blue", "name": "Blue", "postures": ["defensive"] }],
                    "events": [{
                        "id": "e1",
                        "triggerType": "Time",
                        "conditions": [{ "type": "UnitEntersZone", "unitId": "u1" }]
                    }]
                }
                """);

            var document = ScenarioDocumentJsonLoader.LoadFromFile(path, strict: true);
            Assert.Equal(["defensive"], document.Sides[0].Postures);
            Assert.Equal("UnitEntersZone", document.Events![0].Conditions[0].Type);
            Assert.Equal("u1", document.Events[0].Conditions[0].UnitId);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Editor_deep_copy_stores_null_conditions_and_postures_as_empty()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertEvent(new ScenarioEventDto
        {
            Id = "e1",
            TriggerType = "Time",
            Conditions = null!,
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.UpsertSide(new ScenarioSideDto
        {
            Id = "blue",
            Name = "Blue",
            Postures = null!,
        });

        Assert.Empty(editor.Events[0].Conditions);
        Assert.Equal("Message", editor.Events[0].Actions[0].Type);
        var document = editor.ToDto();
        Assert.Empty(document.Sides[0].Postures);
        var json = ScenarioStableJsonWriter.Serialize(document);
        Assert.Contains("\"id\": \"e1\"", json, StringComparison.Ordinal);
        Assert.Contains("\"id\": \"blue\"", json, StringComparison.Ordinal);
    }

    private static void AssertEmptyConditionsAndPostures(ScenarioDocumentDto document)
    {
        var side = Assert.Single(document.Sides);
        Assert.NotNull(side.Postures);
        Assert.Empty(side.Postures);
        var evt = Assert.Single(document.Events!);
        Assert.NotNull(evt.Conditions);
        Assert.Empty(evt.Conditions);
    }
}
