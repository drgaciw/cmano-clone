using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class MissionAddFerryCommandTests
{
    [Test]
    public void mission_add_ferry_with_destination_validates_clean_no_ferry_no_destination_finding()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using (var writer = new StringWriter())
            {
                Assert.That(MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"ok\":true"));
                Assert.That(writer.ToString(), Does.Contain("\"type\":\"Ferry\""));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.Type, Is.EqualTo("Ferry"));
            Assert.That(mission.FerryDestinationBaseId, Is.EqualTo("base-1"));

            // Validation engine should not raise FERRY_NO_DESTINATION since a destination was supplied.
            using (var writer = new StringWriter())
            {
                ScenarioValidateCommand.Run(path, quiet: false, writer);
                Assert.That(writer.ToString(), Does.Not.Contain("FERRY_NO_DESTINATION"));
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

    [Test]
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
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("INVALID_DESTINATION"));

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Is.Empty);
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
            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("FERRY_NO_DESTINATION"));
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
    public void mission_update_ferry_round_trip_changes_destination()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", new StringWriter());

            using (var writer = new StringWriter())
            {
                Assert.That(MissionUpdateFerryCommand.Run(path, 2, "ferry-1", ["u1", "u2"], "base-2", writer), Is.EqualTo(0));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.FerryDestinationBaseId, Is.EqualTo("base-2"));
            Assert.That(mission.AssignedUnitIds, Is.EqualTo(new[] { "u1", "u2" }));
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
    public void mission_update_ferry_stale_edit_version_returns_conflict_exit_code()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-ferry-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            MissionAddFerryCommand.Run(path, 1, "ferry-1", ["u1"], "base-1", new StringWriter());

            using var writer = new StringWriter();
            Assert.That(MissionUpdateFerryCommand.Run(path, 99, "ferry-1", ["u1"], "base-2", writer), Is.EqualTo(3));
            Assert.That(writer.ToString(), Does.Contain("CONFLICT"));
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
