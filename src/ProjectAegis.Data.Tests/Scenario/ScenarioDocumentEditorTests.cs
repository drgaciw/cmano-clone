using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioDocumentEditorTests
{
    [Test]
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
            Assert.That(roundTrip.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(roundTrip.Missions, Has.Exactly(1).Items);
            Assert.That(roundTrip.Missions[0].Type, Is.EqualTo("Patrol"));
            Assert.That(roundTrip.Missions[0].PatrolZone.Count, Is.EqualTo(3));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Stale_edit_version_throws_conflict()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var ex = Assert.Throws<ScenarioEditConflictException>(() => editor.RequireEditVersion(99));
        Assert.That(ex!.Code, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
    }

    [Test]
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

        Assert.That(result, Does.Contain($"restored {snapshotId}"));
        Assert.That(editor.ComputeFileHash(), Is.EqualTo(expectedHash));
        Assert.That(editor.Missions, Has.Exactly(1).Items);
        Assert.That(editor.Missions[0].Id, Is.EqualTo("patrol-before"));
    }

    [Test]
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

        Assert.That(result, Does.Contain("not found").IgnoreCase);
        Assert.That(editor.ComputeFileHash(), Is.EqualTo(beforeHash));
        Assert.That(editor.Missions, Has.Exactly(1).Items);
    }

    [Test]
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
            Assert.That(afterUpdate.Missions[0].AssignedUnitIds.Count, Is.EqualTo(2));
            Assert.That(afterUpdate.TryRemoveMission("patrol-1"), Is.True);
            afterUpdate.CommitMutation();
            afterUpdate.Save(path);

            var finalDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(finalDto.Missions, Is.Empty);
            Assert.That(finalDto.Metadata.EditVersion, Is.EqualTo(4));
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
