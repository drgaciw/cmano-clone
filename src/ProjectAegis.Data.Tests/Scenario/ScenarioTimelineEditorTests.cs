using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Operations timeline CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W3 / AME-3.5 Partial+ headless).
/// </summary>
public sealed class ScenarioTimelineEditorTests
{
    [Fact]
    public void UpsertTimelineEntry_inserts_and_replaces_by_case_insensitive_mission_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "Patrol-1",
            ActivateAtTick = 10,
        });

        Assert.Single(editor.ToDto().OperationsTimeline);
        Assert.Equal("Patrol-1", editor.ToDto().OperationsTimeline[0].MissionId);
        Assert.Equal(10, editor.ToDto().OperationsTimeline[0].ActivateAtTick);

        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "patrol-1",
            ActivateAtTick = 42,
        });

        var entries = editor.ToDto().OperationsTimeline;
        Assert.Single(entries);
        Assert.Equal("patrol-1", entries[0].MissionId);
        Assert.Equal(42, entries[0].ActivateAtTick);
    }

    [Fact]
    public void UpsertTimelineEntry_blank_mission_id_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
            {
                MissionId = "  ",
                ActivateAtTick = 0,
            }));
    }

    [Fact]
    public void UpsertTimelineEntry_negative_tick_throws()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        Assert.Throws<InvalidOperationException>(() =>
            editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
            {
                MissionId = "m1",
                ActivateAtTick = -1,
            }));
    }

    [Fact]
    public void TryRemoveTimelineEntry_removes_by_case_insensitive_mission_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "m1",
            ActivateAtTick = 5,
        });
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "m2",
            ActivateAtTick = 15,
        });

        Assert.True(editor.TryRemoveTimelineEntry("M1"));
        Assert.False(editor.TryRemoveTimelineEntry("missing"));
        var remaining = Assert.Single(editor.ToDto().OperationsTimeline);
        Assert.Equal("m2", remaining.MissionId);
    }

    [Fact]
    public void UpsertTimelineEntry_round_trips_json()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.Save(path);

            var loaded = ScenarioDocumentEditor.Load(path);
            loaded.RequireEditVersion(1);
            var undo = loaded.CaptureUndoSnapshot();
            loaded.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
            {
                MissionId = "strike-alpha",
                ActivateAtTick = 120,
            });
            loaded.PersistUndoSnapshot(path, undo);
            loaded.CommitMutation();
            loaded.Save(path);

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var entry = Assert.Single(dto.OperationsTimeline);
            Assert.Equal("strike-alpha", entry.MissionId);
            Assert.Equal(120, entry.ActivateAtTick);
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
    public void CaptureUndoSnapshot_isolates_timeline_from_subsequent_upsert()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "m1",
            ActivateAtTick = 1,
        });

        var snap = editor.CaptureUndoSnapshot();
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "m1",
            ActivateAtTick = 99,
        });
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "m2",
            ActivateAtTick = 2,
        });

        Assert.Single(snap.OperationsTimeline);
        Assert.Equal(1, snap.OperationsTimeline[0].ActivateAtTick);
        Assert.Equal(2, editor.ToDto().OperationsTimeline.Count);
    }

    [Fact]
    public void Bus_UpsertTimelineEntry_and_DeleteTimelineEntry_bump_edit_version()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-bus-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var upsert = session.Bus.UpsertTimelineEntry(
                expectedEditVersion: 1,
                new ScenarioOperationTimelineEntryDto
                {
                    MissionId = "m-bus",
                    ActivateAtTick = 7,
                },
                save: true);

            Assert.True(upsert.Ok);
            Assert.Equal(2, upsert.EditVersion);
            Assert.Single(session.Editor.ToDto().OperationsTimeline);

            var del = session.Bus.DeleteTimelineEntry(session.EditVersion, "m-bus", save: true);
            Assert.True(del.Ok);
            Assert.Equal(3, del.EditVersion);
            Assert.Empty(session.Editor.ToDto().OperationsTimeline);
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
    public void Bus_UpsertTimelineEntry_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-bus-c-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var result = session.Bus.UpsertTimelineEntry(
                expectedEditVersion: 99,
                new ScenarioOperationTimelineEntryDto
                {
                    MissionId = "m",
                    ActivateAtTick = 0,
                },
                save: true);

            Assert.False(result.Ok);
            Assert.Equal(ScenarioEditVersionGuard.ConflictCode, result.ErrorCode);
            Assert.Empty(session.Editor.ToDto().OperationsTimeline);
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
