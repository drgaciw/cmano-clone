using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for event_add / event_delete (ME-W2 event graph CRUD).
/// </summary>
public sealed class EventCrudCliTests
{
    [Fact]
    public void Event_add_ok_bumps_edit_version_and_persists_conditions_actions()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            var conditions = new[]
            {
                EventAddCommand.ParseCondition("UnitEntersZone:u1:zone-a"),
            };
            var actions = new[]
            {
                EventAddCommand.ParseAction("ActivateMission:m-patrol"),
            };

            using var writer = new StringWriter();
            var code = EventAddCommand.Run(
                path,
                editVersion: 1,
                eventId: "evt-cli-1",
                triggerType: "Time",
                conditions,
                actions,
                writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"eventId\":\"evt-cli-1\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var evt = Assert.Single(dto.Events!);
            Assert.Equal("evt-cli-1", evt.Id);
            Assert.Equal("Time", evt.TriggerType);
            Assert.Equal("UnitEntersZone", evt.Conditions[0].Type);
            Assert.Equal("u1", evt.Conditions[0].UnitId);
            Assert.Equal("zone-a", evt.Conditions[0].ZoneId);
            Assert.Equal("ActivateMission", evt.Actions[0].Type);
            Assert.Equal("m-patrol", evt.Actions[0].UnitId);
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
    public void Event_delete_ok_removes_event()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.Equal(
                    0,
                    EventAddCommand.Run(
                        path,
                        editVersion: 1,
                        eventId: "to-delete",
                        triggerType: "Time",
                        Array.Empty<ScenarioEventConditionDto>(),
                        Array.Empty<ScenarioEventActionDto>(),
                        seed));
            }

            using var writer = new StringWriter();
            var code = EventDeleteCommand.Run(path, editVersion: 2, eventId: "to-delete", writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"deletedEventId\":\"to-delete\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(3, dto.Metadata.EditVersion);
            Assert.True(dto.Events is null || dto.Events.Count == 0);
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
    public void Event_add_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-conflict-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllText(path);

            using var writer = new StringWriter();
            var code = EventAddCommand.Run(
                path,
                editVersion: 99,
                eventId: "stale",
                triggerType: "Time",
                Array.Empty<ScenarioEventConditionDto>(),
                Array.Empty<ScenarioEventActionDto>(),
                writer);

            Assert.Equal(3, code);
            Assert.Contains("CONFLICT", writer.ToString());
            Assert.Equal(before, File.ReadAllText(path));
            Assert.Equal(1, ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion);
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
    public void ParseCondition_and_ParseAction_accept_colon_and_comma()
    {
        var c1 = EventAddCommand.ParseCondition("Time");
        Assert.Equal("Time", c1.Type);
        Assert.Null(c1.UnitId);

        var c2 = EventAddCommand.ParseCondition("UnitEntersZone,u9,z9");
        Assert.Equal("UnitEntersZone", c2.Type);
        Assert.Equal("u9", c2.UnitId);
        Assert.Equal("z9", c2.ZoneId);

        var a1 = EventAddCommand.ParseAction("Message");
        Assert.Equal("Message", a1.Type);
        Assert.Null(a1.UnitId);

        var a2 = EventAddCommand.ParseAction("TeleportUnit:u1");
        Assert.Equal("TeleportUnit", a2.Type);
        Assert.Equal("u1", a2.UnitId);
    }
}
