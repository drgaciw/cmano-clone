using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for event_add / event_delete (ME-W2 event graph CRUD).
/// </summary>
[TestFixture]
public sealed class EventCrudCliTests
{
    [Test]
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

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"eventId\":\"evt-cli-1\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.Events!, Has.Exactly(1).Items);
            var evt = dto.Events!.Single();
            Assert.That(evt.Id, Is.EqualTo("evt-cli-1"));
            Assert.That(evt.TriggerType, Is.EqualTo("Time"));
            Assert.That(evt.Conditions[0].Type, Is.EqualTo("UnitEntersZone"));
            Assert.That(evt.Conditions[0].UnitId, Is.EqualTo("u1"));
            Assert.That(evt.Conditions[0].ZoneId, Is.EqualTo("zone-a"));
            Assert.That(evt.Actions[0].Type, Is.EqualTo("ActivateMission"));
            Assert.That(evt.Actions[0].UnitId, Is.EqualTo("m-patrol"));
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
    public void Event_delete_ok_removes_event()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.That(EventAddCommand.Run(
                        path,
                        editVersion: 1,
                        eventId: "to-delete",
                        triggerType: "Time",
                        Array.Empty<ScenarioEventConditionDto>(),
                        Array.Empty<ScenarioEventActionDto>(),
                        seed), Is.EqualTo(0));
            }

            using var writer = new StringWriter();
            var code = EventDeleteCommand.Run(path, editVersion: 2, eventId: "to-delete", writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"deletedEventId\":\"to-delete\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(3));
            Assert.That(dto.Events is null || dto.Events.Count == 0, Is.True);
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

            Assert.That(code, Is.EqualTo(3));
            Assert.That(writer.ToString(), Does.Contain("CONFLICT"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(before));
            Assert.That(ScenarioDocumentJsonLoader.LoadFromFile(path).Metadata.EditVersion, Is.EqualTo(1));
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
    public void ParseCondition_and_ParseAction_accept_colon_and_comma()
    {
        var c1 = EventAddCommand.ParseCondition("Time");
        Assert.That(c1.Type, Is.EqualTo("Time"));
        Assert.That(c1.UnitId, Is.Null);

        var c2 = EventAddCommand.ParseCondition("UnitEntersZone,u9,z9");
        Assert.That(c2.Type, Is.EqualTo("UnitEntersZone"));
        Assert.That(c2.UnitId, Is.EqualTo("u9"));
        Assert.That(c2.ZoneId, Is.EqualTo("z9"));

        var a1 = EventAddCommand.ParseAction("Message");
        Assert.That(a1.Type, Is.EqualTo("Message"));
        Assert.That(a1.UnitId, Is.Null);

        var a2 = EventAddCommand.ParseAction("TeleportUnit:u1");
        Assert.That(a2.Type, Is.EqualTo("TeleportUnit"));
        Assert.That(a2.UnitId, Is.EqualTo("u1"));
    }
}
