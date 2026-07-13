using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for Mission Board verbs: list, clone, add_from_template (ME-W1 Task 5).
/// </summary>
public sealed class MissionBoardCliTests
{
    [Fact]
    public void Mission_list_returns_rows_json_ok()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-list-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));

            using var writer = new StringWriter();
            var code = MissionListCommand.Run(path, typeFilter: null, sideFilter: null, statusFilter: null, writer);

            Assert.Equal(0, code);
            var body = writer.ToString();
            Assert.Contains("\"ok\":true", body);
            Assert.Contains("\"id\":\"patrol-1\"", body);
            Assert.Contains("\"type\":\"Patrol\"", body);
            Assert.Contains("\"status\":\"Assigned\"", body);
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
    public void Mission_clone_ok_bumps_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-clone-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));

            using var writer = new StringWriter();
            var code = MissionCloneCommand.Run(path, editVersion: 2, sourceMissionId: "patrol-1", newMissionId: "patrol-2", writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"missionId\":\"patrol-2\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(3, dto.Metadata.EditVersion);
            Assert.Equal(2, dto.Missions.Count);
            Assert.Contains(dto.Missions, m => m.Id == "patrol-1");
            Assert.Contains(dto.Missions, m => m.Id == "patrol-2");
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
    public void Mission_clone_stale_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-mission-clone-stale-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));

            var before = File.ReadAllText(path);
            var beforeDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, beforeDto.Metadata.EditVersion);
            Assert.Single(beforeDto.Missions);

            using var writer = new StringWriter();
            var code = MissionCloneCommand.Run(path, editVersion: 99, sourceMissionId: "patrol-1", newMissionId: "patrol-2", writer);

            Assert.Equal(3, code);
            Assert.Contains("CONFLICT", writer.ToString());

            var after = File.ReadAllText(path);
            Assert.Equal(before, after);
            var afterDto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, afterDto.Metadata.EditVersion);
            Assert.Single(afterDto.Missions);
            Assert.Equal("patrol-1", afterDto.Missions[0].Id);
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

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"missionId\":\"patrol-from-tpl\"", writer.ToString());
            Assert.Contains("\"templateId\":\"tpl-patrol-empty\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("patrol-from-tpl", mission.Id);
            Assert.Equal("Patrol", mission.Type);
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
