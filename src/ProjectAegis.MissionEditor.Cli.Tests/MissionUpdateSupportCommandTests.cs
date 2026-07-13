using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class MissionUpdateSupportCommandTests
{
    [Fact]
    public void mission_update_support_changes_role_and_bumps_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-support-upd-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = new[]
            {
                new ScenarioWaypointDto { Lat = 1, Lon = 1 },
                new ScenarioWaypointDto { Lat = 1, Lon = 2 },
                new ScenarioWaypointDto { Lat = 2, Lon = 2 },
            };

            using (var writer = new StringWriter())
            {
                Assert.Equal(
                    0,
                    MissionAddSupportCommand.Run(path, 1, "sup-1", ["u1"], "Tanker", zone, writer));
            }

            using (var writer = new StringWriter())
            {
                Assert.Equal(
                    0,
                    MissionUpdateSupportCommand.Run(
                        path,
                        2,
                        "sup-1",
                        unitIds: null,
                        supportRole: "AEW",
                        stationZone: null,
                        writer));
                Assert.Contains("\"ok\":true", writer.ToString());
                Assert.Contains("\"type\":\"Support\"", writer.ToString());
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("Support", mission.Type);
            Assert.Equal("AEW", mission.SupportRole);
            Assert.Equal(3, dto.Metadata.EditVersion);
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
    public void mission_update_support_missing_mission_returns_not_found()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-support-miss-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var writer = new StringWriter();
            var code = MissionUpdateSupportCommand.Run(path, 1, "nope", null, "Tanker", null, writer);
            Assert.Equal(1, code);
            Assert.Contains("MISSION_NOT_FOUND", writer.ToString());
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
