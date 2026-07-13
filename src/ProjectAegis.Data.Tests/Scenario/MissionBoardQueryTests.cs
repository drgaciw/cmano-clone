using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class MissionBoardQueryTests
{
    private static ScenarioDocumentDto DocWithMissions()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u1", SideId = "blue", PlatformId = "ffg", Lat = 57, Lon = 20,
        });
        editor.UpsertOrbatUnit(new ScenarioOrbatUnitDto
        {
            Id = "u2", SideId = "red", PlatformId = "ddg", Lat = 58, Lon = 21,
        });
        editor.AddPatrolMission("patrol-1", ["u1"],
        [
            new ScenarioWaypointDto { Lat = 57, Lon = 20 },
            new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
            new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
        ]);
        editor.AddStrikeMission("strike-1", ["u2"], ["hostile-1"]);
        editor.AddFerryMission("ferry-empty", [], "base-1");
        return editor.ToDto();
    }

    [Fact]
    public void List_returns_all_missions_sorted_by_id_ordinal()
    {
        var rows = MissionBoardQuery.List(DocWithMissions());
        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "ferry-empty", "patrol-1", "strike-1" }, rows.Select(r => r.Id).ToArray());
    }

    [Fact]
    public void List_filter_by_type_patrol()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), typeFilter: "Patrol");
        Assert.Single(rows);
        Assert.Equal("patrol-1", rows[0].Id);
        Assert.Equal("blue", rows[0].SideId);
        Assert.Equal("Assigned", rows[0].Status);
        Assert.Equal(1, rows[0].UnitCount);
    }

    [Fact]
    public void List_filter_by_side_red()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), sideFilter: "red");
        Assert.Single(rows);
        Assert.Equal("strike-1", rows[0].Id);
    }

    [Fact]
    public void List_filter_unassigned_status()
    {
        var rows = MissionBoardQuery.List(DocWithMissions(), statusFilter: "Unassigned");
        Assert.Single(rows);
        Assert.Equal("ferry-empty", rows[0].Id);
        Assert.Null(rows[0].SideId);
    }

    [Fact]
    public void Summary_line_includes_type_and_id()
    {
        var row = MissionBoardQuery.List(DocWithMissions(), typeFilter: "Strike").Single();
        Assert.Contains("strike-1", row.SummaryLine, StringComparison.Ordinal);
        Assert.Contains("Strike", row.SummaryLine, StringComparison.OrdinalIgnoreCase);
    }
}
