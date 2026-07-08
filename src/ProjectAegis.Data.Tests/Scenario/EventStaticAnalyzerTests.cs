namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

/// <summary>
/// ME-W2 Track W2-b: pure EventStaticAnalyzer codes
/// (EVENT_DEAD_TRIGGER / EVENT_UNREACHABLE_ACTION / EVENT_CONTRADICTORY / EVENT_CIRCULAR).
/// </summary>
public sealed class EventStaticAnalyzerTests
{
    private static ScenarioDocumentDto Doc(
        IReadOnlyList<ScenarioEventDto> events,
        IReadOnlyList<ScenarioMissionDto>? missions = null) =>
        new()
        {
            Metadata = new ScenarioMetadataDto
            {
                TlBranch = CatalogTlTier.Default,
                DbRef = "baltic_patrol",
                Seed = 42,
                EditVersion = 1,
            },
            Missions = missions ?? Array.Empty<ScenarioMissionDto>(),
            Events = events,
        };

    private static ScenarioMissionDto Mission(string id) =>
        new()
        {
            Id = id,
            Type = "Patrol",
            AssignedUnitIds = ["u1"],
            PatrolZone =
            [
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
            ],
        };

    [Fact]
    public void Empty_events_emits_no_findings()
    {
        var findings = EventStaticAnalyzer.Analyze(Doc(Array.Empty<ScenarioEventDto>()));
        Assert.Empty(findings);
    }

    [Fact]
    public void Dead_trigger_when_zero_conditions_and_trigger_not_Time()
    {
        var doc = Doc([
            new ScenarioEventDto
            {
                Id = "evt-dead",
                TriggerType = "UnitDestroyed",
                Conditions = [],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            },
        ]);

        var findings = EventStaticAnalyzer.Analyze(doc);

        var f = Assert.Single(findings, x => x.Code == EventStaticAnalyzer.DeadTriggerCode);
        Assert.Equal(ValidationSeverity.Warning, f.Severity);
        Assert.Contains("evt-dead", f.Message, StringComparison.Ordinal);
        Assert.Equal("evt-dead", EventStaticAnalyzer.EventIdOf(f));
    }

    [Fact]
    public void Time_trigger_with_zero_conditions_is_not_dead()
    {
        var doc = Doc([
            new ScenarioEventDto
            {
                Id = "evt-time",
                TriggerType = "Time",
                Conditions = [],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            },
        ]);

        var findings = EventStaticAnalyzer.Analyze(doc);
        Assert.DoesNotContain(findings, f => f.Code == EventStaticAnalyzer.DeadTriggerCode);
    }

    [Fact]
    public void Unreachable_ActivateMission_missing_mission_id()
    {
        var doc = Doc([
            new ScenarioEventDto
            {
                Id = "evt-miss",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "ActivateMission" }],
            },
        ]);

        var findings = EventStaticAnalyzer.Analyze(doc);

        var f = Assert.Single(findings, x => x.Code == EventStaticAnalyzer.UnreachableActionCode);
        Assert.Contains("missing mission id", f.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("evt-miss", EventStaticAnalyzer.EventIdOf(f));
    }

    [Fact]
    public void Unreachable_ActivateMission_unknown_mission_id()
    {
        var doc = Doc(
            [
                new ScenarioEventDto
                {
                    Id = "evt-bad",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "no-such-mission" }],
                },
            ],
            [Mission("patrol-1")]);

        var findings = EventStaticAnalyzer.Analyze(doc);

        var f = Assert.Single(findings, x => x.Code == EventStaticAnalyzer.UnreachableActionCode);
        Assert.Equal("no-such-mission", f.MissionId);
        Assert.Contains("no-such-mission", f.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ActivateMission_known_mission_is_reachable()
    {
        var doc = Doc(
            [
                new ScenarioEventDto
                {
                    Id = "evt-ok",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "patrol-1" }],
                },
            ],
            [Mission("patrol-1")]);

        var findings = EventStaticAnalyzer.Analyze(doc);
        Assert.DoesNotContain(findings, f => f.Code == EventStaticAnalyzer.UnreachableActionCode);
    }

    [Fact]
    public void Contradictory_when_Result_true_and_false_on_same_event()
    {
        var doc = Doc([
            new ScenarioEventDto
            {
                Id = "evt-contra",
                TriggerType = "Time",
                Conditions =
                [
                    new ScenarioEventConditionDto { Type = "UnitDestroyed", UnitId = "u1", Result = true },
                    new ScenarioEventConditionDto { Type = "UnitDestroyed", UnitId = "u2", Result = false },
                ],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            },
        ]);

        var findings = EventStaticAnalyzer.Analyze(doc);

        var f = Assert.Single(findings, x => x.Code == EventStaticAnalyzer.ContradictoryCode);
        Assert.Contains("evt-contra", f.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Circular_two_event_ActivateMission_MissionComplete_cycle()
    {
        // A activates m1; B completes m1 and activates m2; A completes m2 → A↔B cycle.
        var doc = Doc(
            [
                new ScenarioEventDto
                {
                    Id = "evt-a",
                    TriggerType = "Time",
                    Conditions =
                    [
                        new ScenarioEventConditionDto { Type = "MissionComplete", UnitId = "m2", Result = true },
                    ],
                    Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m1" }],
                },
                new ScenarioEventDto
                {
                    Id = "evt-b",
                    TriggerType = "Time",
                    Conditions =
                    [
                        new ScenarioEventConditionDto { Type = "MissionComplete", UnitId = "m1", Result = true },
                    ],
                    Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m2" }],
                },
            ],
            [Mission("m1"), Mission("m2")]);

        var findings = EventStaticAnalyzer.Analyze(doc);
        var circular = findings.Where(f => f.Code == EventStaticAnalyzer.CircularCode).ToList();

        Assert.Equal(2, circular.Count);
        Assert.Contains(circular, f => EventStaticAnalyzer.EventIdOf(f) == "evt-a");
        Assert.Contains(circular, f => EventStaticAnalyzer.EventIdOf(f) == "evt-b");
    }

    [Fact]
    public void One_way_ActivateMission_MissionComplete_is_not_circular()
    {
        var doc = Doc(
            [
                new ScenarioEventDto
                {
                    Id = "evt-start",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m1" }],
                },
                new ScenarioEventDto
                {
                    Id = "evt-after",
                    TriggerType = "Time",
                    Conditions =
                    [
                        new ScenarioEventConditionDto { Type = "MissionComplete", UnitId = "m1", Result = true },
                    ],
                    Actions = [new ScenarioEventActionDto { Type = "Message" }],
                },
            ],
            [Mission("m1")]);

        var findings = EventStaticAnalyzer.Analyze(doc);
        Assert.DoesNotContain(findings, f => f.Code == EventStaticAnalyzer.CircularCode);
    }

    [Fact]
    public void AnalyzeTcaGraph_formats_counts_nodes_and_findings()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.Events.Add(new ScenarioEventDto
        {
            Id = "evt-dead",
            TriggerType = "UnitDestroyed",
            Conditions = [],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });
        editor.AddEvent("evt-dead");

        var result = editor.AnalyzeTcaGraph();

        Assert.Equal(
            "TCA static analysis: dead=1 unreachable=0 contradictory=0 circular=0; " +
            "graph nodes=[evt-dead]; findings=[EVENT_DEAD_TRIGGER:evt-dead]",
            result);
    }

    [Fact]
    public void Engine_wires_static_findings_as_warnings_export_still_allowed()
    {
        var doc = Doc([
            new ScenarioEventDto
            {
                Id = "evt-dead",
                TriggerType = "UnitDestroyed",
                Conditions = [],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            },
        ]);

        var report = new ScenarioValidationEngine().Validate(
            doc,
            InMemoryCatalogReader.BalticPatrolFixture(),
            new ValidationConfig());

        Assert.Contains(report.Findings, f =>
            f.Code == EventStaticAnalyzer.DeadTriggerCode
            && f.Severity == ValidationSeverity.Warning);
        Assert.True(report.CanExport(new ValidationConfig()));
    }
}
