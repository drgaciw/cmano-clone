using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for Mission Board verbs: list, clone, add_from_template (ME-W1 Task 5).
/// </summary>
[TestFixture]
public sealed class MissionBoardCliTests
{
    [Test]
    public void Mission_list_returns_rows_json_ok()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-list-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));

            using var writer = new StringWriter();
            var code = MissionListCommand.Run(path, typeFilter: null, sideFilter: null, statusFilter: null, writer);

            Assert.That(code, Is.EqualTo(0));
            var body = writer.ToString();
            Assert.That(body, Does.Contain("\"ok\":true"));
            Assert.That(body, Does.Contain("\"id\":\"patrol-1\""));
            Assert.That(body, Does.Contain("\"type\":\"Patrol\""));
            Assert.That(body, Does.Contain("\"status\":\"Assigned\""));
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
    public void Mission_clone_ok_bumps_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-clone-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));

            using var writer = new StringWriter();
            var code = MissionCloneCommand.Run(path, editVersion: 2, sourceMissionId: "patrol-1", newMissionId: "patrol-2", writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"missionId\":\"patrol-2\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(3));
            Assert.That(dto.Missions.Count, Is.EqualTo(2));
            Assert.That(dto.Missions.Any(m => m.Id == "patrol-1"), Is.True);
            Assert.That(dto.Missions.Any(m => m.Id == "patrol-2"), Is.True);
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
    public void Mission_clone_stale_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-clone-stale-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));

            var before = File.ReadAllText(path);
            var beforeDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(beforeDto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(beforeDto.Missions, Has.Exactly(1).Items);

            using var writer = new StringWriter();
            var code = MissionCloneCommand.Run(path, editVersion: 99, sourceMissionId: "patrol-1", newMissionId: "patrol-2", writer);

            Assert.That(code, Is.EqualTo(3));
            Assert.That(writer.ToString(), Does.Contain("CONFLICT"));

            var after = File.ReadAllText(path);
            Assert.That(after, Is.EqualTo(before));
            var afterDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(afterDto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(afterDto.Missions, Has.Exactly(1).Items);
            Assert.That(afterDto.Missions[0].Id, Is.EqualTo("patrol-1"));
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
    public void Mission_add_from_template_ok()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-tpl-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = MissionAddFromTemplateCommand.Run(
                path,
                editVersion: 1,
                templateId: "tpl-patrol-empty",
                newMissionId: "patrol-from-tpl",
                writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"missionId\":\"patrol-from-tpl\""));
            Assert.That(writer.ToString(), Does.Contain("\"templateId\":\"tpl-patrol-empty\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.Id, Is.EqualTo("patrol-from-tpl"));
            Assert.That(mission.Type, Is.EqualTo("Patrol"));
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
