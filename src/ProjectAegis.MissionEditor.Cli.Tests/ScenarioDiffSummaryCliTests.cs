namespace ProjectAegis.MissionEditor.Cli.Tests;

using System.Text.Json;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

/// <summary>
/// ME-W3 Track W3-c: CLI contract for <c>scenario_diff_summary</c> → <c>{ ok, summary }</c>.
/// </summary>
public sealed class ScenarioDiffSummaryCliTests
{
    [Fact]
    public void scenario_diff_summary_identical_files_returns_no_semantic_changes()
    {
        var path = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 1 },
              "missions": [ { "id": "patrol-1", "type": "Patrol", "assignedUnitIds": ["u1"] } ],
              "sides": [ { "id": "blue", "name": "Blue" } ],
              "orbat": { "units": [ { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 57.0, "lon": 20.0 } ] }
            }
            """);
        try
        {
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(path, path, writer);
            Assert.Equal(0, exit);

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
            Assert.Equal("no semantic changes", doc.RootElement.GetProperty("summary").GetString());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void scenario_diff_summary_reports_mission_unit_and_event_deltas()
    {
        var before = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 1 },
              "missions": [ { "id": "m1", "type": "Patrol" } ],
              "sides": [ { "id": "blue", "name": "Blue" } ],
              "orbat": { "units": [ { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 1, "lon": 2 } ] },
              "operationsTimeline": [ { "missionId": "m1", "activateAtTick": 10 } ],
              "events": [ { "id": "e1", "triggerType": "Time" } ]
            }
            """);
        var after = WriteTempScenario("""
            {
              "metadata": { "tlBranch": "TL-0", "dbRef": "baltic_patrol", "seed": 1, "editVersion": 2 },
              "missions": [ { "id": "m1", "type": "Strike" }, { "id": "m2", "type": "Ferry" } ],
              "sides": [ { "id": "blue", "name": "Blue" }, { "id": "red", "name": "Red" } ],
              "orbat": { "units": [
                { "id": "u1", "sideId": "blue", "platformId": "p1", "lat": 1, "lon": 2 },
                { "id": "u2", "sideId": "red", "platformId": "p2", "lat": 3, "lon": 4 }
              ] },
              "operationsTimeline": [ { "missionId": "m1", "activateAtTick": 20 } ],
              "events": [ { "id": "e1", "triggerType": "Time" }, { "id": "e2", "triggerType": "UnitDestroyed" } ]
            }
            """);
        try
        {
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(before, after, writer);
            Assert.Equal(0, exit);

            using var doc = JsonDocument.Parse(writer.ToString());
            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
            var summary = doc.RootElement.GetProperty("summary").GetString() ?? "";

            Assert.Contains("mission +m2 (Ferry)", summary, StringComparison.Ordinal);
            Assert.Contains("mission ~m1 type Patrol→Strike", summary, StringComparison.Ordinal);
            Assert.Contains("side +red", summary, StringComparison.Ordinal);
            Assert.Contains("unit +u2", summary, StringComparison.Ordinal);
            Assert.Contains("event +e2", summary, StringComparison.Ordinal);
            Assert.Contains("timeline ~m1 tick 10→20", summary, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(before);
            File.Delete(after);
        }
    }

    [Fact]
    public void scenario_diff_summary_missing_before_returns_error()
    {
        var after = WriteTempScenario("""{ "missions": [] }""");
        try
        {
            var missing = Path.Combine(Path.GetTempPath(), $"aegis-missing-{Guid.NewGuid():N}.json");
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(missing, after, writer);
            Assert.Equal(1, exit);
            Assert.Contains("NOT_FOUND", writer.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.Contains("\"ok\":false", writer.ToString().Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(after);
        }
    }

    [Fact]
    public void scenario_diff_summary_missing_after_returns_error()
    {
        var before = WriteTempScenario("""{ "missions": [] }""");
        try
        {
            var missing = Path.Combine(Path.GetTempPath(), $"aegis-missing-{Guid.NewGuid():N}.json");
            using var writer = new StringWriter();
            var exit = ScenarioDiffSummaryCommand.Run(before, missing, writer);
            Assert.Equal(1, exit);
            Assert.Contains("NOT_FOUND", writer.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(before);
        }
    }

    private static string WriteTempScenario(string json)
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-diff-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
