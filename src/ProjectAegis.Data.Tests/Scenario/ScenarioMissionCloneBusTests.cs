using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

public sealed class ScenarioMissionCloneBusTests
{
    [Fact]
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
            Assert.True(r.Ok);
            Assert.Equal(3, r.EditVersion); // create=1, attach=2, clone=3
            Assert.Equal(2, session.Editor.Missions.Count);
            Assert.NotNull(r.Report);
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
            Assert.False(r.Ok);
            Assert.Equal(ScenarioEditVersionGuard.ConflictCode, r.ErrorCode);
            Assert.Single(session.Editor.Missions);
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
    public void Bus_AddFromTemplate_and_DeleteMission()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var add = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-tpl", save: true);
            Assert.True(add.Ok);
            Assert.Single(session.Editor.Missions);

            var del = session.Bus.DeleteMission(session.EditVersion, "p-tpl", save: true);
            Assert.True(del.Ok);
            Assert.Empty(session.Editor.Missions);
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
    public void Bus_AddFromTemplate_live_findings_include_mission_no_units_for_empty_patrol()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tpl-v-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            var r = session.Bus.AddFromTemplate(1, "tpl-patrol-empty", "p-empty", save: true);
            Assert.True(r.Ok);
            Assert.Contains(r.Report!.Findings, f => f.Code == "MISSION_NO_UNITS");
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
