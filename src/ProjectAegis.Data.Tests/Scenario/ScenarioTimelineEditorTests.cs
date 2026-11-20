using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Operations timeline CRUD on <see cref="ScenarioDocumentEditor"/> and
/// <see cref="ScenarioEditCommandBus"/> (ME-W3 / AME-3.5 Partial+ headless).
/// </summary>
public sealed class ScenarioTimelineEditorTests
{
    [Test]
    public void UpsertTimelineEntry_inserts_and_replaces_by_case_insensitive_mission_id()
    {
        var editor = ScenarioDocumentEditor.CreateNew();
        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "Patrol-1",
            ActivateAtTick = 10,
        });

        Assert.That(editor.ToDto().OperationsTimeline, Has.Exactly(1).Items);
        Assert.That(editor.ToDto().OperationsTimeline[0].MissionId, Is.EqualTo("Patrol-1"));
        Assert.That(editor.ToDto().OperationsTimeline[0].ActivateAtTick, Is.EqualTo(10));

        editor.UpsertTimelineEntry(new ScenarioOperationTimelineEntryDto
        {
            MissionId = "patrol-1",
            ActivateAtTick = 42,
        });

        var entries = editor.ToDto().OperationsTimeline;
        Assert.That(entries, Has.Exactly(1).Items);
        Assert.That(entries[0].MissionId, Is.EqualTo("patrol-1"));
        Assert.That(entries[0].ActivateAtTick, Is.EqualTo(42));
    }

    [Test]
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

    [Test]
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

    [Test]
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

        Assert.That(editor.TryRemoveTimelineEntry("M1"), Is.True);
        Assert.That(editor.TryRemoveTimelineEntry("missing"), Is.False);
        var remaining = ProjectAegis.Data.Tests.AssertEx.Single(editor.ToDto().OperationsTimeline);
        Assert.That(remaining.MissionId, Is.EqualTo("m2"));
    }

    [Test]
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
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            var entry = ProjectAegis.Data.Tests.AssertEx.Single(dto.OperationsTimeline);
            Assert.That(entry.MissionId, Is.EqualTo("strike-alpha"));
            Assert.That(entry.ActivateAtTick, Is.EqualTo(120));
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

        Assert.That(snap.OperationsTimeline, Has.Exactly(1).Items);
        Assert.That(snap.OperationsTimeline[0].ActivateAtTick, Is.EqualTo(1));
        Assert.That(editor.ToDto().OperationsTimeline.Count, Is.EqualTo(2));
    }

    [Test]
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

            Assert.That(upsert.Ok, Is.True);
            Assert.That(upsert.EditVersion, Is.EqualTo(2));
            Assert.That(session.Editor.ToDto().OperationsTimeline, Has.Exactly(1).Items);

            var del = session.Bus.DeleteTimelineEntry(session.EditVersion, "m-bus", save: true);
            Assert.That(del.Ok, Is.True);
            Assert.That(del.EditVersion, Is.EqualTo(3));
            Assert.That(session.Editor.ToDto().OperationsTimeline, Is.Empty);
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

            Assert.That(result.Ok, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo(ScenarioEditVersionGuard.ConflictCode));
            Assert.That(session.Editor.ToDto().OperationsTimeline, Is.Empty);
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
