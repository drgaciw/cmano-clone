namespace ProjectAegis.Data.Tests.Scenario;

using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

/// <summary>
/// ME-W3 Track W3-c / AME-7.3 Partial+: pure <see cref="ScenarioSemanticDiff.Summarize"/> bullets.
/// </summary>
public sealed class ScenarioSemanticDiffTests
{
    [Fact]
    public void Summarize_identical_documents_returns_no_semantic_changes()
    {
        var doc = BaseDoc();
        Assert.Equal(ScenarioSemanticDiff.NoChanges, ScenarioSemanticDiff.Summarize(doc, doc));
    }

    [Fact]
    public void Summarize_null_arguments_throw()
    {
        var doc = BaseDoc();
        Assert.Throws<ArgumentNullException>(() => ScenarioSemanticDiff.Summarize(null!, doc));
        Assert.Throws<ArgumentNullException>(() => ScenarioSemanticDiff.Summarize(doc, null!));
    }

    [Fact]
    public void Summarize_mission_add_remove_and_type_change()
    {
        var before = BaseDoc(missions:
        [
            Mission("m-keep", "Patrol"),
            Mission("m-gone", "Strike"),
            Mission("m-flip", "Patrol"),
        ]);
        var after = BaseDoc(missions:
        [
            Mission("m-keep", "Patrol"),
            Mission("m-new", "Ferry"),
            Mission("m-flip", "Strike"),
        ]);

        var summary = ScenarioSemanticDiff.Summarize(before, after);

        Assert.Contains("mission +m-new (Ferry)", summary, StringComparison.Ordinal);
        Assert.Contains("mission -m-gone", summary, StringComparison.Ordinal);
        Assert.Contains("mission ~m-flip type Patrol→Strike", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("mission +m-keep", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("mission -m-keep", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("mission ~m-keep", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Summarize_unit_side_event_add_remove()
    {
        var before = BaseDoc(
            sides: [Side("blue"), Side("red")],
            units: [Unit("u1"), Unit("u2")],
            events: [Event("e1"), Event("e2")]);
        var after = BaseDoc(
            sides: [Side("blue"), Side("green")],
            units: [Unit("u1"), Unit("u3")],
            events: [Event("e1"), Event("e3")]);

        var summary = ScenarioSemanticDiff.Summarize(before, after);

        Assert.Contains("side +green", summary, StringComparison.Ordinal);
        Assert.Contains("side -red", summary, StringComparison.Ordinal);
        Assert.Contains("unit +u3", summary, StringComparison.Ordinal);
        Assert.Contains("unit -u2", summary, StringComparison.Ordinal);
        Assert.Contains("event +e3", summary, StringComparison.Ordinal);
        Assert.Contains("event -e2", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Summarize_timeline_add_remove_and_tick_change()
    {
        var before = BaseDoc(timeline:
        [
            Timeline("m-a", 10),
            Timeline("m-b", 20),
            Timeline("m-c", 30),
        ]);
        var after = BaseDoc(timeline:
        [
            Timeline("m-a", 10),
            Timeline("m-b", 25),
            Timeline("m-d", 40),
        ]);

        var summary = ScenarioSemanticDiff.Summarize(before, after);

        Assert.Contains("timeline +m-d", summary, StringComparison.Ordinal);
        Assert.Contains("timeline -m-c", summary, StringComparison.Ordinal);
        Assert.Contains("timeline ~m-b tick 20→25", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("timeline ~m-a", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Summarize_bullets_are_ordinal_sorted_and_joined_with_semicolon()
    {
        var before = BaseDoc();
        var after = BaseDoc(
            sides: [Side("alpha")],
            units: [Unit("z-unit")],
            missions: [Mission("m1", "Patrol")],
            events: [Event("e-early")]);

        var summary = ScenarioSemanticDiff.Summarize(before, after);
        var expected = string.Join(
            ScenarioSemanticDiff.Separator,
            new[]
            {
                "event +e-early",
                "mission +m1 (Patrol)",
                "side +alpha",
                "unit +z-unit",
            }.OrderBy(s => s, StringComparer.Ordinal));

        Assert.Equal(expected, summary);
        Assert.DoesNotContain("\n", summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Summarize_null_events_and_orbat_treated_as_empty()
    {
        var before = new ScenarioDocumentDto
        {
            Missions = Array.Empty<ScenarioMissionDto>(),
            Sides = Array.Empty<ScenarioSideDto>(),
            OperationsTimeline = Array.Empty<ScenarioOperationTimelineEntryDto>(),
            Events = null,
            Orbat = null,
        };
        var after = BaseDoc(units: [Unit("u-new")], events: [Event("e-new")]);

        var summary = ScenarioSemanticDiff.Summarize(before, after);
        Assert.Contains("unit +u-new", summary, StringComparison.Ordinal);
        Assert.Contains("event +e-new", summary, StringComparison.Ordinal);
    }

    private static ScenarioDocumentDto BaseDoc(
        IReadOnlyList<ScenarioMissionDto>? missions = null,
        IReadOnlyList<ScenarioSideDto>? sides = null,
        IReadOnlyList<ScenarioOrbatUnitDto>? units = null,
        IReadOnlyList<ScenarioOperationTimelineEntryDto>? timeline = null,
        IReadOnlyList<ScenarioEventDto>? events = null) =>
        new()
        {
            Missions = missions ?? Array.Empty<ScenarioMissionDto>(),
            Sides = sides ?? Array.Empty<ScenarioSideDto>(),
            Orbat = units == null
                ? new ScenarioOrbatDto()
                : new ScenarioOrbatDto { Units = units },
            OperationsTimeline = timeline ?? Array.Empty<ScenarioOperationTimelineEntryDto>(),
            Events = events ?? Array.Empty<ScenarioEventDto>(),
        };

    private static ScenarioMissionDto Mission(string id, string type) =>
        new() { Id = id, Type = type };

    private static ScenarioSideDto Side(string id) =>
        new() { Id = id, Name = id };

    private static ScenarioOrbatUnitDto Unit(string id) =>
        new() { Id = id, SideId = "blue", PlatformId = "p1" };

    private static ScenarioEventDto Event(string id) =>
        new() { Id = id, TriggerType = "Time" };

    private static ScenarioOperationTimelineEntryDto Timeline(string missionId, int tick) =>
        new() { MissionId = missionId, ActivateAtTick = tick };
}
