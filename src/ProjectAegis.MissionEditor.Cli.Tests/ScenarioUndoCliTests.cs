using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class ScenarioUndoCliTests
{
    [Fact]
    public void scenario_undo_round_trip_restores_prior_document_state()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));
            Assert.Equal(0, MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], new StringWriter()));

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioUndoCommand.Run(path, 3, writer));
                Assert.Contains("\"undone\":true", writer.ToString());
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("Patrol", mission.Type);
            Assert.Equal(2, dto.Metadata.EditVersion);
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Fact]
    public void scenario_undo_without_snapshot_returns_no_undo_snapshot_error()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            Assert.Equal(1, ScenarioUndoCommand.Run(path, 1, writer));
            Assert.Contains("NO_UNDO_SNAPSHOT", writer.ToString());
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Fact]
    public void scenario_undo_does_not_push_snapshot_on_conflict_rejected_mutation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            using (var writer = new StringWriter())
            {
                Assert.Equal(3, MissionAddPatrolCommand.Run(path, 99, "patrol-1", ["u1"], zone, writer));
            }

            Assert.Equal(0, ScenarioUndoStackStore.Count(path));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}