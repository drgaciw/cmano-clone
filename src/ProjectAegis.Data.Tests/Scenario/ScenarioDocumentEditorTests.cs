using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioDocumentEditorTests
{
    [Fact]
    public void Add_patrol_bumps_edit_version_and_round_trips_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-scenario-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            loaded.AddPatrolMission(
                "patrol-1",
                ["u1"],
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ]);
            loaded.CommitMutation();
            loaded.Save(path);

            var roundTrip = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, roundTrip.Metadata.EditVersion);
            Assert.Single(roundTrip.Missions);
            Assert.Equal("Patrol", roundTrip.Missions[0].Type);
            Assert.Equal(3, roundTrip.Missions[0].PatrolZone.Count);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Stale_edit_version_throws_conflict()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<ScenarioEditConflictException>(() => editor.RequireEditVersion(99));
        Assert.Equal(ScenarioEditVersionGuard.ConflictCode, ex.Code);
    }
}