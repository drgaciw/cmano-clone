using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario.Authoring;

/// <summary>
/// Explicit JSON null and null-forgiving initialization must not store null nested collections.
/// </summary>
public sealed class ScenarioNullCollectionContractTests
{
    [Test]
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
            Assert.That(strict.Events![0].Actions[0].Type, Is.EqualTo("Message"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
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
            Assert.That(document.Sides[0].Postures, Is.EqualTo(["defensive"]));
            Assert.That(document.Events![0].Conditions[0].Type, Is.EqualTo("UnitEntersZone"));
            Assert.That(document.Events[0].Conditions[0].UnitId, Is.EqualTo("u1"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
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

        Assert.That(editor.Events[0].Conditions, Is.Empty);
        Assert.That(editor.Events[0].Actions[0].Type, Is.EqualTo("Message"));
        var document = editor.ToDto();
        Assert.That(document.Sides[0].Postures, Is.Empty);
        var json = ScenarioStableJsonWriter.Serialize(document);
        Assert.That(json, Does.Contain("\"id\": \"e1\""));
        Assert.That(json, Does.Contain("\"id\": \"blue\""));
    }

    private static void AssertEmptyConditionsAndPostures(ScenarioDocumentDto document)
    {
        var side = AssertEx.Single(document.Sides);
        Assert.That(side.Postures, Is.Not.Null);
        Assert.That(side.Postures, Is.Empty);
        var evt = AssertEx.Single(document.Events!);
        Assert.That(evt.Conditions, Is.Not.Null);
        Assert.That(evt.Conditions, Is.Empty);
    }
}
