namespace ProjectAegis.Data.Tests.Scenario;

using System;
using System.IO;
using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

// ScenarioDocumentJsonLoader is in ProjectAegis.Data.Scenario.Authoring (already using'd above)

/// <summary>
/// AC-7: structured event debugger JSON for unmet UnitEntersZone conditions (S84-01).
/// Fixture: event-no-fire.scenario.json . Cites: sprint-84-event-debugger.md + kickoff +
/// roadmap-execute-plan-07042026.md §4 S84 + qa-plan-scenario-editor-2026-07-01.md (unit#7) +
/// scenario-editor-scope-boundary-2026-07-04.md + 11-Agentic-Mission-Editor.md AC-7.
/// </summary>
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

    /// <summary>AC-7 additive: Time trigger with explicit Result=true fires (no unmet).</summary>
    [Fact]
    public void Time_trigger_with_result_true_emits_fired_true_no_unmet()
    {
        var document = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { Seed = 1 },
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-fire",
                    TriggerType = "Time",
                    Conditions = [ new ScenarioEventConditionDto { Type = "Time", Result = true } ],
                    Actions = [ new ScenarioEventActionDto { Type = "Message" } ],
                },
            ],
        };

        var json = EventDebuggerTrace.ToJson(document, "evt-fire");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("evt-fire", root.GetProperty("event_id").GetString());
        Assert.True(root.GetProperty("fired").GetBoolean());
        Assert.Equal(0, root.GetProperty("last_evaluated_tick").GetInt32());
        Assert.Empty(root.GetProperty("unmet_conditions").EnumerateArray());
    }

    [Fact]
    public void UnitEntersZone_never_holds_from_event_no_fire_fixture_emits_structured_unmet_json()
    {
        // TDD extension for AC-7: use dedicated fixture per sprint spec + qa unit #7.
        var path = ResolveDataPath("examples", "event-no-fire.scenario.json");
        var document = ScenarioDocumentJsonLoader.LoadFromFile(path);

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
        // richer detail from extension
        if (condition.TryGetProperty("note", out var note))
        {
            Assert.Equal("no-sim-state", note.GetString());
        }
    }

    private static string ResolveDataPath(string? subDirectory, string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
        {
            if (dir == null) break;
            var candidate = subDirectory == null
                ? Path.Combine(dir.FullName, "data", "scenarios", fileName)
                : Path.Combine(dir.FullName, "data", "scenarios", subDirectory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"Could not resolve '{fileName}' under data/scenarios.");
    }
}