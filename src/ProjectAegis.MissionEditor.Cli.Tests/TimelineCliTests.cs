using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for timeline_list / timeline_upsert / timeline_delete (ME-W3 AME-3.5).
/// </summary>
[TestFixture]
public sealed class TimelineCliTests
{
    [Test]
    public void Timeline_upsert_ok_bumps_edit_version_and_persists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = TimelineUpsertCommand.Run(
                path,
                editVersion: 1,
                missionId: "patrol-1",
                activateAtTick: 30,
                writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"missionId\":\"patrol-1\""));
            Assert.That(writer.ToString(), Does.Contain("\"activateAtTick\":30"));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
            Assert.That(dto.OperationsTimeline, Has.Exactly(1).Items);
            var entry = dto.OperationsTimeline.Single();
            Assert.That(entry.MissionId, Is.EqualTo("patrol-1"));
            Assert.That(entry.ActivateAtTick, Is.EqualTo(30));
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
    public void Timeline_list_returns_entries_sorted_by_tick()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-list-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.That(TimelineUpsertCommand.Run(path, 1, "late", 100, seed), Is.EqualTo(0));
                Assert.That(TimelineUpsertCommand.Run(path, 2, "early", 5, seed), Is.EqualTo(0));
            }

            using var writer = new StringWriter();
            var code = TimelineListCommand.Run(path, writer);

            Assert.That(code, Is.EqualTo(0));
            var json = writer.ToString();
            Assert.That(json, Does.Contain("\"ok\":true"));
            Assert.That(json, Does.Contain("\"count\":2"));
            Assert.That(json, Does.Contain("\"missionId\":\"early\""));
            Assert.That(json, Does.Contain("\"missionId\":\"late\""));
            // early (tick 5) should appear before late (tick 100) in stable sort
            Assert.That(json.IndexOf("\"missionId\":\"early\"", StringComparison.Ordinal) <
                json.IndexOf("\"missionId\":\"late\"", StringComparison.Ordinal), Is.True);
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
    public void Timeline_delete_ok_removes_entry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.That(TimelineUpsertCommand.Run(path, 1, "to-delete", 10, seed), Is.EqualTo(0));
            }

            using var writer = new StringWriter();
            var code = TimelineDeleteCommand.Run(path, editVersion: 2, missionId: "to-delete", writer);

            Assert.That(code, Is.EqualTo(0));
            Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
            Assert.That(writer.ToString(), Does.Contain("\"deletedMissionId\":\"to-delete\""));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(3));
            Assert.That(dto.OperationsTimeline, Is.Empty);
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
    public void Timeline_upsert_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-conflict-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllText(path);

            using var writer = new StringWriter();
            var code = TimelineUpsertCommand.Run(path, editVersion: 99, missionId: "stale", activateAtTick: 1, writer);

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
    public void Timeline_delete_missing_returns_not_found()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-miss-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = TimelineDeleteCommand.Run(path, editVersion: 1, missionId: "missing", writer);

            Assert.That(code, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("TIMELINE_NOT_FOUND"));
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
