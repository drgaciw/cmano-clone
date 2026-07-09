using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

/// <summary>
/// CLI/MCP parity tests for timeline_list / timeline_upsert / timeline_delete (ME-W3 AME-3.5).
/// </summary>
public sealed class TimelineCliTests
{
    [Fact]
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

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"missionId\":\"patrol-1\"", writer.ToString());
            Assert.Contains("\"activateAtTick\":30", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(2, dto.Metadata.EditVersion);
            var entry = Assert.Single(dto.OperationsTimeline);
            Assert.Equal("patrol-1", entry.MissionId);
            Assert.Equal(30, entry.ActivateAtTick);
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
    public void Timeline_list_returns_entries_sorted_by_tick()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-list-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.Equal(0, TimelineUpsertCommand.Run(path, 1, "late", 100, seed));
                Assert.Equal(0, TimelineUpsertCommand.Run(path, 2, "early", 5, seed));
            }

            using var writer = new StringWriter();
            var code = TimelineListCommand.Run(path, writer);

            Assert.Equal(0, code);
            var json = writer.ToString();
            Assert.Contains("\"ok\":true", json);
            Assert.Contains("\"count\":2", json);
            Assert.Contains("\"missionId\":\"early\"", json);
            Assert.Contains("\"missionId\":\"late\"", json);
            // early (tick 5) should appear before late (tick 100) in stable sort
            Assert.True(
                json.IndexOf("\"missionId\":\"early\"", StringComparison.Ordinal) <
                json.IndexOf("\"missionId\":\"late\"", StringComparison.Ordinal));
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
    public void Timeline_delete_ok_removes_entry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-del-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using (var seed = new StringWriter())
            {
                Assert.Equal(0, TimelineUpsertCommand.Run(path, 1, "to-delete", 10, seed));
            }

            using var writer = new StringWriter();
            var code = TimelineDeleteCommand.Run(path, editVersion: 2, missionId: "to-delete", writer);

            Assert.Equal(0, code);
            Assert.Contains("\"ok\":true", writer.ToString());
            Assert.Contains("\"deletedMissionId\":\"to-delete\"", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(3, dto.Metadata.EditVersion);
            Assert.Empty(dto.OperationsTimeline);
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
    public void Timeline_upsert_stale_edit_version_returns_conflict()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-conflict-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var before = File.ReadAllText(path);

            using var writer = new StringWriter();
            var code = TimelineUpsertCommand.Run(path, editVersion: 99, missionId: "stale", activateAtTick: 1, writer);

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
    public void Timeline_delete_missing_returns_not_found()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-tl-miss-cli-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var code = TimelineDeleteCommand.Run(path, editVersion: 1, missionId: "missing", writer);

            Assert.Equal(1, code);
            Assert.Contains("TIMELINE_NOT_FOUND", writer.ToString());
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
