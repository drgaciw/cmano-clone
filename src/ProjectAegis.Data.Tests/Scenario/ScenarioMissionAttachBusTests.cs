using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// ScenarioEditCommandBus attach-from-selection paths (Phase 2.1 Task 6):
/// Patrol / Strike / Support missions use selected unit ids and type-specific fields.
/// </summary>
public sealed class ScenarioMissionAttachBusTests
{
    [Fact]
    public void Attach_patrol_uses_selected_unit_ids()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-attach-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.PlaceUnit(
                1,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            var zone = new[]
            {
                new ScenarioWaypointDto { Lat = 57, Lon = 20 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
            };
            var r = session.Bus.AttachPatrolFromSelection(
                session.EditVersion,
                "patrol-1",
                new[] { "u1" },
                zone,
                save: true);

            Assert.True(r.Ok);
            Assert.Equal("Patrol", session.Editor.Missions.Single().Type);
            Assert.Equal(new[] { "u1" }, session.Editor.Missions.Single().AssignedUnitIds);
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
    public void Attach_strike_from_selection_persists_targets()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-attach-s-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.PlaceUnit(
                1,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);
            session.Bus.PlaceUnit(
                session.EditVersion,
                new ScenarioOrbatUnitDto
                {
                    Id = "hostile-1",
                    SideId = "red",
                    PlatformId = "ssk",
                    Lat = 58,
                    Lon = 21,
                },
                save: true);

            var r = session.Bus.AttachStrikeFromSelection(
                session.EditVersion,
                "strike-1",
                unitIds: new[] { "u1" },
                targetIds: new[] { "hostile-1" },
                save: true);

            Assert.True(r.Ok);
            var mission = session.Editor.Missions.Single();
            Assert.Equal("Strike", mission.Type);
            Assert.Equal(new[] { "u1" }, mission.AssignedUnitIds);
            Assert.Equal(new[] { "hostile-1" }, mission.TargetIds);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(new[] { "hostile-1" }, reloaded.Missions.Single().TargetIds);
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
    public void Attach_support_from_selection_sets_role()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-attach-sup-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);
            session.Bus.PlaceUnit(
                1,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "tanker",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);

            var station = new[]
            {
                new ScenarioWaypointDto { Lat = 56.9, Lon = 19.9 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.2 },
            };
            var r = session.Bus.AttachSupportFromSelection(
                session.EditVersion,
                "support-1",
                unitIds: new[] { "u1" },
                supportRole: "Tanker",
                stationZone: station,
                save: true);

            Assert.True(r.Ok);
            var mission = session.Editor.Missions.Single();
            Assert.Equal("Support", mission.Type);
            Assert.Equal("Tanker", mission.SupportRole);
            Assert.Equal(new[] { "u1" }, mission.AssignedUnitIds);
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
