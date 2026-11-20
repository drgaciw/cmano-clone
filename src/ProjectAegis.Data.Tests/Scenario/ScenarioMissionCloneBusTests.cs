using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioMissionCloneBusTests
{
    [Test]
    public void Bus_CloneMission_bumps_edit_version_and_validates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-clone-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(1, "p1", ["u1"],
            [
                new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
            ], save: true);

            var r = session.Bus.CloneMission(session.EditVersion, "p1", "p2", save: true);
            Assert.That(r.Ok, Is.True);
            Assert.That(r.EditVersion, Is.EqualTo(3)); // create=1, attach=2, clone=3
            Assert.That(session.Editor.Missions.Count, Is.EqualTo(2));
            Assert.That(r.Report, Is.Not.Null);
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
    public void Bus_CloneMission_stale_edit_version_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-clone-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.AttachPatrolFromSelection(1, "p1", ["u1"],
            [
                new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20 },
            ], save: true);

            var r = session.Bus.CloneMission(99, "p1", "p2", save: true);
            Assert.That(r.Ok, Is.False);
            Assert.That(r.ErrorCode, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
            Assert.That(session.Editor.Missions, Has.Exactly(1).Items);
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
    public void Bus_AddFromTemplate_and_DeleteMission()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var add = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-tpl", save: true);
            Assert.That(add.Ok, Is.True);
            Assert.That(session.Editor.Missions, Has.Exactly(1).Items);

            var del = session.Bus.DeleteMission(session.EditVersion, "p-tpl", save: true);
            Assert.That(del.Ok, Is.True);
            Assert.That(session.Editor.Missions, Is.Empty);
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
    public void Bus_AddFromTemplate_live_findings_include_mission_no_units_for_empty_patrol()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-v-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var r = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-empty", save: true);
            Assert.That(r.Ok, Is.True);
            Assert.That(r.Report!.Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);
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
