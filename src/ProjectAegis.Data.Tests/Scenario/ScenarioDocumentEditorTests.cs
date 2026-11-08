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

    [Fact]
    public void Rollback_known_snapshot_restores_captured_document()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddPatrolMission(
            "patrol-before",
            ["u1"],
            [new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 }]);
        editor.CommitMutation();
        var expectedHash = editor.ComputeFileHash();
        var (snapshotId, _) = editor.CreateSnapshotForRollback("test");

        editor.AddPatrolMission(
            "patrol-after",
            ["u2"],
            [new ScenarioWaypointDto { Lat = 58.0, Lon = 21.0 }]);
        editor.CommitMutation();

        var result = editor.RollbackToSnapshot(snapshotId);

        Assert.Contains($"restored {snapshotId}", result, StringComparison.Ordinal);
        Assert.Equal(expectedHash, editor.ComputeFileHash());
        Assert.Single(editor.Missions);
        Assert.Equal("patrol-before", editor.Missions[0].Id);
    }

    [Fact]
    public void Rollback_unknown_snapshot_reports_failure_without_mutating_document()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.AddPatrolMission(
            "patrol-1",
            ["u1"],
            [new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 }]);
        editor.CommitMutation();
        var beforeHash = editor.ComputeFileHash();

        var result = editor.RollbackToSnapshot("missing-snapshot");

        Assert.Contains("not found", result, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(beforeHash, editor.ComputeFileHash());
        Assert.Single(editor.Missions);
    }

    [Fact]
    public void Update_and_delete_mission_round_trip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-scenario-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.AddPatrolMission(
                "patrol-1",
                ["u1"],
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                ]);
            editor.CommitMutation();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(2);
            loaded.UpdatePatrolMission("patrol-1", ["u1", "u2"], null);
            loaded.CommitMutation();
            loaded.Save(path);

            var afterUpdate = ScenarioDocumentEditor.Load(path);
            afterUpdate.RequireEditVersion(3);
            Assert.Equal(2, afterUpdate.Missions[0].AssignedUnitIds.Count);
            Assert.True(afterUpdate.TryRemoveMission("patrol-1"));
            afterUpdate.CommitMutation();
            afterUpdate.Save(path);

            var finalDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Empty(finalDto.Missions);
            Assert.Equal(4, finalDto.Metadata.EditVersion);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
