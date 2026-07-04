using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.MissionEditor.Cli;
using Xunit;

namespace ProjectAegis.MissionEditor.Cli.Tests;

public sealed class MissionAddFerryCommandTests
{
    [Fact]
    public void mission_add_ferry_with_destination_validates_clean_no_ferry_no_destination_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using (var writer = new StringWriter())
            {
                Assert.Equal(
                    0,
                    MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", writer));
                Assert.Contains("\"ok\":true", writer.ToString());
                Assert.Contains("\"type\":\"Ferry\"", writer.ToString());
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("Ferry", mission.Type);
            Assert.Equal("base-1", mission.FerryDestinationBaseId);

            // Validation engine should not raise FERRY_NO_DESTINATION since a destination was supplied.
            using (var writer = new StringWriter())
            {
                ScenarioValidateCommand.Run(path, quiet: false, writer);
                Assert.DoesNotContain("FERRY_NO_DESTINATION", writer.ToString());
            }
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
    public void mission_add_ferry_blank_destination_returns_invalid_destination_client_side()
    {
        // Defense-in-depth coverage note: the CLI itself blocks an empty --destination before
        // ever touching the editor/validation engine (see MissionAddFerryCommand.Run). The
        // ValidationRules.FerryDestinationRule / FERRY_NO_DESTINATION path (server-side) is
        // exercised separately below by constructing a Ferry mission directly via the editor,
        // bypassing this CLI-level guard, so both layers of defense are covered.
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            var exitCode = MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "   ", writer);
            Assert.Equal(1, exitCode);
            Assert.Contains("INVALID_DESTINATION", writer.ToString());

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Empty(dto.Missions);
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
    public void ferry_mission_with_empty_destination_fires_ferry_no_destination_via_validation_engine()
    {
        // Server-side / defense-in-depth: construct a Ferry mission with an empty destination
        // directly via the editor (bypassing MissionAddFerryCommand's client-side guard) and
        // confirm ScenarioValidateCommand (backed by ValidationRules.FerryDestinationRule)
        // still reports FERRY_NO_DESTINATION.
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            var editor = ScenarioDocumentEditor.CreateNew();
            editor.AddFerryMission("ferry-no-dest", ["u1"], string.Empty);
            editor.CommitMutation();
            editor.Save(path);

            using var writer = new StringWriter();
            var exitCode = ScenarioValidateCommand.Run(path, quiet: false, writer);
            Assert.Equal(1, exitCode);
            Assert.Contains("FERRY_NO_DESTINATION", writer.ToString());
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
    public void mission_update_ferry_round_trip_changes_destination()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", new StringWriter());

            using (var writer = new StringWriter())
            {
                Assert.Equal(
                    0,
                    MissionUpdateFerryCommand.Run(path, 2, "ferry-1", ["u1", "u2"], "base-2", writer));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("base-2", mission.FerryDestinationBaseId);
            Assert.Equal(new[] { "u1", "u2" }, mission.AssignedUnitIds);
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
    public void mission_update_ferry_stale_edit_version_returns_conflict_exit_code()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", new StringWriter());

            using var writer = new StringWriter();
            Assert.Equal(
                3,
                MissionUpdateFerryCommand.Run(path, 99, "ferry-1", ["u1"], "base-2", writer));
            Assert.Contains("CONFLICT", writer.ToString());
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
