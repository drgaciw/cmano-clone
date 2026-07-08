namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

/// <summary>
/// Pins scenario-editor surface contracts after ME-W2:
/// full AC-7 event debugger projection (sim_tick, sequence_id, action_results),
/// real event static analysis (AnalyzeTcaGraph → EventStaticAnalyzer),
/// plus remaining stubs (AI/NL scaffold, IncompatibleHost/BrokenRef heuristics).
/// See production/qa/qa-plan-scenario-editor-2026-07-01.md unit #17 and doc 11.
/// </summary>
public sealed class StubScopePinTests
{
    // ---------------------------------------------------------------
    // Event debugger — ExplainEventTrace / scenario_event_trace
    // Full AC-7 projection (ME-W2 Track W2-a): sim_tick, sequence_id, action_results
    // plus event_id / fired / last_evaluated_tick / unmet_conditions.
    // See: Game-Requirements/requirements/11-Agentic-Mission-Editor.md AC-7 / AME-5.5
    // ---------------------------------------------------------------

    [Fact]
    public void ExplainEventTrace_returns_full_ac7_projection_fields_with_correct_types()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.Events.Add(new ScenarioEventDto
        {
            Id = "evt-stub-demo",
            TriggerType = "Time",
            Conditions = [],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.AddEvent("evt-stub-demo");

        var json = editor.ExplainEventTrace("evt-stub-demo");

        // Pins full AC-7 projection shape (compact JSON).
        Assert.StartsWith("{", json);
        Assert.Contains("\"event_id\":\"evt-stub-demo\"", json);
        Assert.Contains("\"fired\":", json);
        Assert.Contains("\"last_evaluated_tick\":", json);
        Assert.Contains("\"unmet_conditions\"", json);

        // Full AC-7 fields MUST appear with correct JSON types
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(System.Text.Json.JsonValueKind.Number, root.GetProperty("sim_tick").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Number, root.GetProperty("sequence_id").ValueKind);
        Assert.Equal(System.Text.Json.JsonValueKind.Array, root.GetProperty("action_results").ValueKind);
        Assert.True(root.GetProperty("sim_tick").TryGetInt32(out _));
        Assert.True(root.GetProperty("sequence_id").TryGetInt32(out _));
    }

    [Fact]
    public void ExplainEventTrace_for_missing_event_and_unitenterszone_pin_full_projection()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        // no events populated -> missing-event path
        var jsonMissing = editor.ExplainEventTrace("nonexistent-evt");

        Assert.Contains("\"event_id\":\"nonexistent-evt\"", jsonMissing);
        Assert.Contains("\"fired\":false", jsonMissing);
        Assert.Contains("\"last_evaluated_tick\":0", jsonMissing);
        Assert.Contains("\"unmet_conditions\":[]", jsonMissing);
        Assert.Contains("\"sim_tick\":0", jsonMissing);
        Assert.Contains("\"sequence_id\":0", jsonMissing);
        Assert.Contains("\"action_results\":[]", jsonMissing);

        // UnitEntersZone case without live editorState -> never holds
        var doc = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { Seed = 42 },
            Events = [
                new ScenarioEventDto
                {
                    Id = "evt-zone",
                    TriggerType = "Time",
                    Conditions = [
                        new ScenarioEventConditionDto { Type = "UnitEntersZone", UnitId = "u1", ZoneId = "z1" }
                    ],
                }
            ],
        };
        var jsonZone = EventDebuggerTrace.ToJson(doc, "evt-zone");

        Assert.Contains("\"fired\":false", jsonZone);
        Assert.Contains("\"last_evaluated_tick\":32", jsonZone); // pinned horizon default
        Assert.Contains("unmet_conditions", jsonZone);
        Assert.Contains("\"sim_tick\":32", jsonZone);
        Assert.Contains("\"sequence_id\":", jsonZone);
        Assert.Contains("\"action_results\"", jsonZone);
    }

    // ---------------------------------------------------------------
    // TCA static analysis — ScenarioDocumentEditor.AnalyzeTcaGraph
    // (ME-W2 real EventStaticAnalyzer; no longer MISSION_NO_UNITS relabel)
    // ---------------------------------------------------------------

    [Fact]
    public void AnalyzeTcaGraph_dead_count_is_EVENT_DEAD_TRIGGER_not_MISSION_NO_UNITS()
    {
        var editor = ScenarioDocumentEditor.CreateNew();

        // Missions alone must not inflate dead/unreachable counts (old stub
        // relabeled MISSION_NO_UNITS as dead=). Real analysis only looks at events.
        editor.AddPatrolMission("m1", Array.Empty<string>(), new[]
        {
            new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
        });
        editor.AddStrikeMission("m2", new[] { "u1" }, new[] { "t1" });
        editor.AddFerryMission("m3", new[] { "u1" }, "base-x");

        editor.Events.Add(new ScenarioEventDto
        {
            Id = "evt-dead",
            TriggerType = "UnitDestroyed",
            Conditions = [],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.AddEvent("evt-dead");

        var liveReport = editor.LiveValidate();
        Assert.Contains(liveReport.Findings, f => f.Code == "MISSION_NO_UNITS" && f.MissionId == "m1");
        Assert.Contains(liveReport.Findings, f => f.Code == "EVENT_DEAD_TRIGGER");

        var result = editor.AnalyzeTcaGraph();

        Assert.Equal(
            "TCA static analysis: dead=1 unreachable=0 contradictory=0 circular=0; " +
            "graph nodes=[evt-dead]; findings=[EVENT_DEAD_TRIGGER:evt-dead]",
            result);
    }

    [Fact]
    public void AnalyzeTcaGraph_nodes_are_event_ids_not_mission_chain()
    {
        var editor = ScenarioDocumentEditor.CreateNew();

        // Missions present but nodes must come from events, not insertion-order mission chain.
        editor.AddStrikeMission("alpha", new[] { "u1" }, new[] { "t1" });
        editor.AddFerryMission("bravo", new[] { "u2" }, "base-x");
        editor.AddStrikeMission("charlie", new[] { "u3" }, new[] { "t2" });

        editor.Events.Add(new ScenarioEventDto
        {
            Id = "e1",
            TriggerType = "Time",
            Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.Events.Add(new ScenarioEventDto
        {
            Id = "e2",
            TriggerType = "Time",
            Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.AddEvent("e1");
        editor.AddEvent("e2");

        var result = editor.AnalyzeTcaGraph();

        Assert.Contains("graph nodes=[e1,e2]", result);
        Assert.DoesNotContain("edges=", result);
        Assert.DoesNotContain("alpha", result);
        Assert.Contains("dead=0", result);
        Assert.Contains("findings=[]", result);
    }

    // ---------------------------------------------------------------
    // Stub 2: AI scaffold / NL stub — AiAuthoringServices.NlScaffold
    // ---------------------------------------------------------------

    [Fact]
    public void NlScaffold_patrol_keyword_deterministically_maps_to_patrol_mission()
    {
        var result = AiAuthoringServices.NlScaffold("Blue forces patrol the Baltic strait chokepoint");

        Assert.Contains("patrol-blue-1", result.Missions);
        // Pin the "keyword parse" self-description — proves this is
        // deterministic keyword matching, not an LLM call.
        Assert.StartsWith("Scaffolded from NL brief using keyword parse.", result.Explanation);
    }

    [Fact]
    public void NlScaffold_brief_with_no_recognized_keywords_falls_back_to_hardcoded_patrol_default()
    {
        // No occurrence of any recognized keyword (patrol, defend, baltic,
        // strike, attack, offense, support, tanker, red, opfor).
        var result = AiAuthoringServices.NlScaffold("zzz qqq foobar nonsense text");

        // Guards doc 11 §9's promise that v1 has "no LLM in any blocking or
        // non-blocking authoring path": a real NL-understanding system would
        // not need a hardcoded fallback mission for unrecognized input.
        Assert.Single(result.Missions);
        Assert.Contains("patrol-default", result.Missions);
        Assert.DoesNotContain("patrol-blue-1", result.Missions);
    }

    // ---------------------------------------------------------------
    // Stub 3: Model-integrity demo rules — IncompatibleHostRule / BrokenRefRule
    // (IncompatibleHostRule/BrokenRefRule are `internal`; exercised here
    // through the public ScenarioValidationEngine, matching the idiom in
    // ScenarioValidationEngineTests.cs)
    // ---------------------------------------------------------------

    [Fact]
    public void IncompatibleHostRule_fires_on_air_substring_unit_when_no_carrier_unit_anywhere()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["air-unit-1"],
                    TargetIds = ["t1"],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Substring match on "air", not real platform-compatibility modeling.
        Assert.Contains(report.Findings, f => f.Code == "INCOMPATIBLE_HOST" && f.UnitId == "air-unit-1");
    }

    [Fact]
    public void IncompatibleHostRule_does_not_fire_when_a_literal_carrier_unit_is_present()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["airbase-1"], // still contains the substring "air"
                    TargetIds = ["t1"],
                },
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["carrier"], // literal id match required
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Presence of a mission with the literal unit id "carrier" anywhere
        // in the scenario suppresses the finding, even though "airbase-1"
        // still contains "air" — proving this is naive substring/id matching.
        Assert.DoesNotContain(report.Findings, f => f.Code == "INCOMPATIBLE_HOST");
    }

    [Fact]
    public void BrokenRefRule_fires_only_on_literal_ref_colon_prefix_naming_convention()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["ref:missing-unit"],
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        Assert.Contains(report.Findings, f => f.Code == "BROKEN_REF" && f.MissionId == "strike-1");
    }

    [Fact]
    public void BrokenRefRule_does_not_fire_on_a_dangling_reference_lacking_the_ref_prefix()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["some-other-id"], // also dangling, but lacks the "ref:" prefix
                },
            ],
        };

        var report = new ScenarioValidationEngine().Validate(
            scenario, InMemoryCatalogReader.BalticPatrolFixture(), new ValidationConfig());

        // Not a general referential-integrity check — only the "ref:" naming
        // convention is caught, so this equally-dangling target id is missed.
        Assert.DoesNotContain(report.Findings, f => f.Code == "BROKEN_REF");
    }

    // ---------------------------------------------------------------
    // Additional pin for event debugger full AC-7 projection (missing-event path).
    // Cites: ME-W2 Track W2-a, 11-Agentic-Mission-Editor.md AC-7 / AME-5.5.
    // ---------------------------------------------------------------

    [Fact]
    public void ExplainEventTrace_returns_full_shape_with_fired_false_for_missing_event()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        // No events in document => missing path returns non-firing full projection.
        var json = editor.ExplainEventTrace("evt-no-such");

        Assert.Contains("\"event_id\":\"evt-no-such\"", json, StringComparison.Ordinal);
        Assert.Contains("\"fired\":false", json, StringComparison.Ordinal);
        Assert.Contains("\"last_evaluated_tick\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"unmet_conditions\":[]", json, StringComparison.Ordinal);
        Assert.Contains("\"sim_tick\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"sequence_id\":0", json, StringComparison.Ordinal);
        Assert.Contains("\"action_results\":[]", json, StringComparison.Ordinal);
    }

    [Fact]
    public void AnalyzeTcaGraph_shape_is_real_event_static_analysis()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddPatrolMission("p1", new[] { "u1" }, new[] { new ScenarioWaypointDto { Lat = 1, Lon = 2 } });

        var result = editor.AnalyzeTcaGraph();

        // No events → zero findings; nodes empty (missions are not graph nodes).
        Assert.Contains("dead=0", result);
        Assert.Contains("unreachable=0", result);
        Assert.Contains("graph nodes=[]", result);
        Assert.Contains("findings=[]", result);
        Assert.Contains("TCA static analysis:", result);
        Assert.DoesNotContain("edges=", result);
    }
}
