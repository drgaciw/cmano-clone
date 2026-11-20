using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Event CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W2 / AME-5.x).
/// </summary>
public sealed class ScenarioEventCrudEditorTests
{
    [Test]
    public void UpsertEvent_inserts_deep_copies_and_syncs_EventIds()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        var conditions = new List<ScenarioEventConditionDto>
        {
            new() { Type = "UnitEntersZone", UnitId = "u1", ZoneId = "z1" },
        };
        var actions = new List<ScenarioEventActionDto>
        {
            new() { Type = "Message" },
        };

        editor.UpsertEvent(new ScenarioEventDto
        {
            Id = "evt-1",
            TriggerType = "Time",
            Conditions = conditions,
            Actions = actions,
        });

        Assert.That(editor.Events, Has.Exactly(1).Items);
        Assert.That(editor.EventIds, Does.Contain("evt-1"));
        Assert.That(editor.Events[0].TriggerType, Is.EqualTo("Time"));
        Assert.That(editor.Events[0].Conditions[0].Type, Is.EqualTo("UnitEntersZone"));

        // Mutating the source lists must not affect the stored event (deep copy).
        conditions[0] = new ScenarioEventConditionDto { Type = "Time", Result = true };
        actions.Clear();
        Assert.That(editor.Events[0].Conditions[0].Type, Is.EqualTo("UnitEntersZone"));
        Assert.That(editor.Events[0].Actions, Has.Exactly(1).Items);
    }

    [Test]
    public void UpsertEvent_replaces_by_case_insensitive_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertEvent(new ScenarioEventDto
        {
            Id = "Evt-Alpha",
            TriggerType = "Time",
            Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
            Actions = [new ScenarioEventActionDto { Type = "Message" }],
        });

        editor.UpsertEvent(new ScenarioEventDto
        {
            Id = "evt-alpha",
            TriggerType = "UnitEntersZone",
            Conditions =
            [
                new ScenarioEventConditionDto
                {
                    Type = "UnitEntersZone",
                    UnitId = "u2",
                    ZoneId = "z2",
                },
            ],
            Actions = [new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m1" }],
        });

        Assert.That(editor.Events, Has.Exactly(1).Items);
        Assert.That(editor.Events[0].Id, Is.EqualTo("evt-alpha"));
        Assert.That(editor.Events[0].TriggerType, Is.EqualTo("UnitEntersZone"));
        Assert.That(editor.Events[0].Conditions[0].UnitId, Is.EqualTo("u2"));
        Assert.That(editor.EventIds, Has.Exactly(1).Items);
        Assert.That(editor.EventIds[0], Is.EqualTo("evt-alpha"));
    }

    [Test]
    public void UpsertEvent_blank_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertEvent(new ScenarioEventDto { Id = "  ", TriggerType = "Time" }));
    }

    [Test]
    public void TryRemoveEvent_removes_event_and_EventIds_entry()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertEvent(new ScenarioEventDto { Id = "e1", TriggerType = "Time" });
        editor.UpsertEvent(new ScenarioEventDto { Id = "e2", TriggerType = "Time" });

        Assert.That(editor.TryRemoveEvent("E1"), Is.True);
        Assert.That(editor.TryRemoveEvent("missing"), Is.False);
        Assert.That(editor.Events, Has.Exactly(1).Items);
        Assert.That(editor.Events[0].Id, Is.EqualTo("e2"));
        Assert.That(editor.EventIds.Any(id =>
            string.Equals(id, "e1", StringComparison.OrdinalIgnoreCase)), Is.False);
        Assert.That(editor.EventIds, Does.Contain("e2"));
    }

    [Test]
    public void UpsertEvent_round_trips_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            var undo = loaded.CaptureUndoSnapshot();
            loaded.UpsertEvent(new ScenarioEventDto
            {
                Id = "red_launch",
                TriggerType = "Time",
                Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                Actions = [new ScenarioEventActionDto { Type = "Message" }],
            });
            loaded.PersistUndoSnapshot(path, undo);
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            var evt = ProjectAegis.Data.Tests.AssertEx.Single(dto.Events!);
            Assert.That(evt.Id, Is.EqualTo("red_launch"));
            Assert.That(evt.TriggerType, Is.EqualTo("Time"));
            Assert.That(evt.Conditions, Has.Exactly(1).Items);
            Assert.That(evt.Actions, Has.Exactly(1).Items);
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
    public void Bus_UpsertEvent_and_DeleteEvent_bump_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-bus-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var upsert = session.Bus.UpsertEvent(
                expectedEditVersion: 1,
                new ScenarioEventDto
                {
                    Id = "evt-bus",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions = [new ScenarioEventActionDto { Type = "Message" }],
                },
                save: true);

            Assert.That(upsert.Ok, Is.True);
            Assert.That(upsert.EditVersion, Is.EqualTo(2));
            Assert.That(session.Editor.Events, Has.Exactly(1).Items);

            var del = session.Bus.DeleteEvent(session.EditVersion, "evt-bus", save: true);
            Assert.That(del.Ok, Is.True);
            Assert.That(del.EditVersion, Is.EqualTo(3));
            Assert.That(session.Editor.Events, Is.Empty);
            Assert.That(session.Editor.EventIds, Is.Empty);
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
    public void Bus_UpsertEvent_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-evt-bus-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.UpsertEvent(
                expectedEditVersion: 99,
                new ScenarioEventDto { Id = "e", TriggerType = "Time" },
                save: true);

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
            Assert.That(session.Editor.Events, Is.Empty);
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
}
