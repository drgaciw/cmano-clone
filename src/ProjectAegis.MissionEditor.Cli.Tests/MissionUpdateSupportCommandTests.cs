using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class MissionUpdateSupportCommandTests
{
    [Test]
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
                Assert.That(MissionAddSupportCommand.Run(path, 1, "sup-1", ["u1"], "Tanker", zone, writer), Is.EqualTo(0));
            }

            using (var writer = new StringWriter())
            {
                Assert.That(MissionUpdateSupportCommand.Run(
                        path,
                        2,
                        "sup-1",
                        unitIds: null,
                        supportRole: "AEW",
                        stationZone: null,
                        writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
                Assert.That(writer.ToString(), Does.Contain("\"type\":\"Support\""));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.Type, Is.EqualTo("Support"));
            Assert.That(mission.SupportRole, Is.EqualTo("AEW"));
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(3));
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
    public void mission_update_support_missing_mission_returns_not_found()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-support-miss-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var writer = new StringWriter();
            var code = MissionUpdateSupportCommand.Run(path, 1, "nope", null, "Tanker", null, writer);
            Assert.That(code, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("MISSION_NOT_FOUND"));
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
