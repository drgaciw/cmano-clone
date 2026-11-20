namespace ProjectAegis.Data.Tests.Scenario;

using System;
using System.IO;
using System.Text.Json;
using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

// ScenarioDocumentJsonLoader is in ProjectAegis.Data.Scenario.Authoring (already using'd above)

/// <summary>
/// AC-7: structured event debugger JSON for unmet UnitEntersZone conditions (S84-01).
/// Fixture: event-no-fire.scenario.json . Cites: sprint-84-event-debugger.md + kickoff +
/// roadmap-execute-plan-07042026.md §4 S84 + qa-plan-scenario-editor-2026-07-01.md (unit#7) +
/// scenario-editor-scope-boundary-2026-07-04.md + 11-Agentic-Mission-Editor.md AC-7.
/// </summary>
public sealed class EventDebuggerTests
{
    [Test]
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

        Assert.That(root.GetProperty("event_id").GetString(), Is.EqualTo("evt-no-fire"));
        Assert.That(root.GetProperty("fired").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("last_evaluated_tick").GetInt32(), Is.EqualTo(EventDebuggerTrace.DefaultEvaluationHorizonTicks));

        // AC-7 full projection fields (ME-W2 Track W2-a)
        Assert.That(root.GetProperty("sim_tick").GetInt32(), Is.EqualTo(EventDebuggerTrace.DefaultEvaluationHorizonTicks));
        Assert.That(root.GetProperty("sequence_id").GetInt32(), Is.EqualTo(0)); // single event → ordinal index 0

        var actionResults = root.GetProperty("action_results");
        Assert.That(actionResults.ValueKind, Is.EqualTo(JsonValueKind.Array));
        var action = ProjectAegis.Data.Tests.AssertEx.Single(actionResults.EnumerateArray().ToArray());
        Assert.That(action.GetProperty("type").GetString(), Is.EqualTo("Message"));
        Assert.That(action.GetProperty("applied").GetBoolean(), Is.False);
        Assert.That(action.GetProperty("note").GetString(), Is.EqualTo("not-fired"));

        var unmet = root.GetProperty("unmet_conditions");
        Assert.That(unmet.ValueKind, Is.EqualTo(JsonValueKind.Array));
        var condition = ProjectAegis.Data.Tests.AssertEx.Single(unmet.EnumerateArray().ToArray());

        Assert.That(condition.GetProperty("type").GetString(), Is.EqualTo("UnitEntersZone"));
        Assert.That(condition.GetProperty("result").GetBoolean(), Is.False);
        Assert.That(condition.GetProperty("unit_id").GetString(), Is.EqualTo("u1"));
        Assert.That(condition.GetProperty("zone_id").GetString(), Is.EqualTo("zone-alpha"));
    }

    [Test]
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

        Assert.That(json, Does.StartWith("{"));
        Assert.That(json, Does.Contain("\"event_id\":\"evt-42\""));
        Assert.That(json, Does.Contain("\"fired\":false"));
        Assert.That(json, Does.Contain("\"unmet_conditions\""));
    }

    [Test]
    public void ExplainEventTrace_blank_eventId_falls_back_to_evt()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var json = editor.ExplainEventTrace("   ");

        using var doc = JsonDocument.Parse(json);
        Assert.That(doc.RootElement.GetProperty("event_id").GetString(), Is.EqualTo("evt"));
    }

    /// <summary>AC-7 additive: Time trigger with explicit Result=true fires (no unmet).</summary>
    [Test]
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

        Assert.That(root.GetProperty("event_id").GetString(), Is.EqualTo("evt-fire"));
        Assert.That(root.GetProperty("fired").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("last_evaluated_tick").GetInt32(), Is.EqualTo(0));
        Assert.That(root.GetProperty("unmet_conditions").EnumerateArray(), Is.Empty);
    }

    [Test]
    public void UnitEntersZone_never_holds_from_event_no_fire_fixture_emits_structured_unmet_json()
    {
        // TDD extension for AC-7: use dedicated fixture per sprint spec + qa unit #7.
        var path = ResolveDataPath("examples", "event-no-fire.scenario.json");
        var document = ScenarioDocumentJsonLoader.LoadFromFile(path);

        var json = EventDebuggerTrace.ToJson(document, "evt-no-fire");
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.That(root.GetProperty("event_id").GetString(), Is.EqualTo("evt-no-fire"));
        Assert.That(root.GetProperty("fired").GetBoolean(), Is.False);
        Assert.That(root.GetProperty("last_evaluated_tick").GetInt32(), Is.EqualTo(EventDebuggerTrace.DefaultEvaluationHorizonTicks));

        // AC-7 full projection fields
        Assert.That(root.GetProperty("sim_tick").GetInt32(), Is.EqualTo(EventDebuggerTrace.DefaultEvaluationHorizonTicks));
        Assert.That(root.GetProperty("sequence_id").ValueKind, Is.EqualTo(JsonValueKind.Number));
        Assert.That(root.GetProperty("action_results").ValueKind, Is.EqualTo(JsonValueKind.Array));

        var unmet = root.GetProperty("unmet_conditions");
        Assert.That(unmet.ValueKind, Is.EqualTo(JsonValueKind.Array));
        var condition = ProjectAegis.Data.Tests.AssertEx.Single(unmet.EnumerateArray().ToArray());

        Assert.That(condition.GetProperty("type").GetString(), Is.EqualTo("UnitEntersZone"));
        Assert.That(condition.GetProperty("result").GetBoolean(), Is.False);
        Assert.That(condition.GetProperty("unit_id").GetString(), Is.EqualTo("u1"));
        Assert.That(condition.GetProperty("zone_id").GetString(), Is.EqualTo("zone-alpha"));
        // richer detail from extension
        if (condition.TryGetProperty("note", out var note))
        {
            Assert.That(note.GetString(), Is.EqualTo("no-sim-state"));
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