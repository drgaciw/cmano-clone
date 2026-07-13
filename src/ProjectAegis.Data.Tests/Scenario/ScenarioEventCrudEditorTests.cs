using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Event CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W2 / AME-5.x).
/// </summary>
public sealed class ScenarioEventCrudEditorTests
{
    [Fact]
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

        Assert.Single(editor.Events);
        Assert.Contains("evt-1", editor.EventIds);
        Assert.Equal("Time", editor.Events[0].TriggerType);
        Assert.Equal("UnitEntersZone", editor.Events[0].Conditions[0].Type);

        // Mutating the source lists must not affect the stored event (deep copy).
        conditions[0] = new ScenarioEventConditionDto { Type = "Time", Result = true };
        actions.Clear();
        Assert.Equal("UnitEntersZone", editor.Events[0].Conditions[0].Type);
        Assert.Single(editor.Events[0].Actions);
    }

    [Fact]
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

        Assert.Single(editor.Events);
        Assert.Equal("evt-alpha", editor.Events[0].Id);
        Assert.Equal("UnitEntersZone", editor.Events[0].TriggerType);
        Assert.Equal("u2", editor.Events[0].Conditions[0].UnitId);
        Assert.Single(editor.EventIds);
        Assert.Equal("evt-alpha", editor.EventIds[0]);
    }

    [Fact]
    public void UpsertEvent_blank_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertEvent(new ScenarioEventDto { Id = "  ", TriggerType = "Time" }));
    }

    [Fact]
    public void TryRemoveEvent_removes_event_and_EventIds_entry()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertEvent(new ScenarioEventDto { Id = "e1", TriggerType = "Time" });
        editor.UpsertEvent(new ScenarioEventDto { Id = "e2", TriggerType = "Time" });

        Assert.True(editor.TryRemoveEvent("E1"));
        Assert.False(editor.TryRemoveEvent("missing"));
        Assert.Single(editor.Events);
        Assert.Equal("e2", editor.Events[0].Id);
        Assert.DoesNotContain(editor.EventIds, id =>
            string.Equals(id, "e1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("e2", editor.EventIds);
    }

    [Fact]
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
            Assert.Equal(2, dto.Metadata.EditVersion);
            var evt = Assert.Single(dto.Events!);
            Assert.Equal("red_launch", evt.Id);
            Assert.Equal("Time", evt.TriggerType);
            Assert.Single(evt.Conditions);
            Assert.Single(evt.Actions);
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

            Assert.True(upsert.Ok);
            Assert.Equal(2, upsert.EditVersion);
            Assert.Single(session.Editor.Events);

            var del = session.Bus.DeleteEvent(session.EditVersion, "evt-bus", save: true);
            Assert.True(del.Ok);
            Assert.Equal(3, del.EditVersion);
            Assert.Empty(session.Editor.Events);
            Assert.Empty(session.Editor.EventIds);
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

            Assert.False(result.Ok);
            Assert.Equal(ScenarioEditVersionGuard.ConflictCode, result.ErrorCode);
            Assert.Empty(session.Editor.Events);
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
}
