namespace ProjectAegis.Data.Tests.Scenario;

using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

/// <summary>AC-7: structured event debugger JSON for unmet UnitEntersZone conditions.</summary>
public sealed class EventDebuggerTests
{
    [Fact]
    public void UnitEntersZone_never_holds_emits_fired_false_with_unmet_conditions()
    {
        var document = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { Seed = 42 },
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-no-fire",
                    TriggerType = "Time",
                    Conditions =
                    [
                        new ScenarioEventConditionDto
                        {
                            Type = "UnitEntersZone",
                            UnitId = "u1",
                            ZoneId = "zone-alpha",
                        },
                    ],
                    Actions =
                    [
                        new ScenarioEventActionDto { Type = "Message" },
                    ],
                },
            ],
        };

        var json = EventDebuggerTrace.ToJson(document, "evt-no-fire");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("evt-no-fire", root.GetProperty("event_id").GetString());
        Assert.False(root.GetProperty("fired").GetBoolean());
        Assert.Equal(EventDebuggerTrace.DefaultEvaluationHorizonTicks, root.GetProperty("last_evaluated_tick").GetInt32());

        var unmet = root.GetProperty("unmet_conditions");
        Assert.Equal(JsonValueKind.Array, unmet.ValueKind);
        var condition = Assert.Single(unmet.EnumerateArray().ToArray());

        Assert.Equal("UnitEntersZone", condition.GetProperty("type").GetString());
        Assert.False(condition.GetProperty("result").GetBoolean());
        Assert.Equal("u1", condition.GetProperty("unit_id").GetString());
        Assert.Equal("zone-alpha", condition.GetProperty("zone_id").GetString());
    }

    [Fact]
    public void ExplainEventTrace_delegates_to_structured_json()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.Events.Add(new ScenarioEventDto
        {
            Id = "evt-42",
            TriggerType = "Time",
            Conditions =
            [
                new ScenarioEventConditionDto
                {
                    Type = "UnitEntersZone",
                    UnitId = "patrol-boat-1",
                    ZoneId = "chokepoint",
                },
            ],
            Actions = [new ScenarioEventActionDto { Type = "ActivateMission" }],
        });

        var json = editor.ExplainEventTrace("evt-42");

        Assert.StartsWith("{", json);
        Assert.Contains("\"event_id\":\"evt-42\"", json, StringComparison.Ordinal);
        Assert.Contains("\"fired\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"unmet_conditions\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplainEventTrace_blank_eventId_falls_back_to_evt()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var json = editor.ExplainEventTrace("   ");

        using var doc = JsonDocument.Parse(json);
        Assert.Equal("evt", doc.RootElement.GetProperty("event_id").GetString());
    }
}