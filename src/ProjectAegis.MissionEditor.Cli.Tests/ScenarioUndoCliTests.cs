using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.MissionEditor.Cli.Tests;

[TestFixture]
public sealed class ScenarioUndoCliTests
{
    [Test]
    public void scenario_undo_round_trip_restores_prior_document_state()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));
            Assert.That(MissionAddStrikeCommand.Run(path, 2, "strike-1", ["u1"], ["hostile-1"], new StringWriter()), Is.EqualTo(0));

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioUndoCommand.Run(path, 3, writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"undone\":true"));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.Type, Is.EqualTo("Patrol"));
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(2));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Test]
    public void scenario_undo_without_snapshot_returns_no_undo_snapshot_error()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);

            using var writer = new StringWriter();
            Assert.That(ScenarioUndoCommand.Run(path, 1, writer), Is.EqualTo(1));
            Assert.That(writer.ToString(), Does.Contain("NO_UNDO_SNAPSHOT"));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Test]
    public void scenario_undo_does_not_push_snapshot_on_conflict_rejected_mutation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            using (var writer = new StringWriter())
            {
                Assert.That(MissionAddPatrolCommand.Run(path, 99, "patrol-1", ["u1"], zone, writer), Is.EqualTo(3));
            }

            Assert.That(ScenarioUndoStackStore.Count(path), Is.EqualTo(0));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Test]
    public void scenario_undo_persistence_is_disk_backed_cross_invocation_roundtrip()
    {
        // S83-02 AME-8.5: explicitly exercises disk sidecar persistence (not in-process only).
        // After mutation, sidecar file exists + count; undo succeeds using store (cross CLI process).
        // Cites qa-plan #14, sprint-83-export-undo-ferry.md, roadmap-execute-plan-07042026.md, boundary-2026-07-04.md, execute-plan, AGENTS.
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-disk-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-disk", ["u1"], zone, new StringWriter()), Is.EqualTo(0));

            var stackFile = path + ".undo-stack.json";
            Assert.That(File.Exists(stackFile), Is.True, "disk sidecar for AME-8.5 persistence");
            Assert.That(ScenarioUndoStackStore.Count(path) >= 1, Is.True);

            using (var writer = new StringWriter())
            {
                // current after add is 2
                Assert.That(ScenarioUndoCommand.Run(path, 2, writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"undone\":true"));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(dto.Metadata.EditVersion, Is.EqualTo(1));
            Assert.That(ScenarioUndoStackStore.Count(path), Is.EqualTo(0));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Test]
    public void scenario_undo_does_not_push_snapshot_on_mission_not_found_rejected_delete()
    {
        // Regression for the phantom-undo-snapshot bug: mutation commands call
        // editor.PushUndoSnapshot(scenarioPath) unconditionally right after RequireEditVersion
        // succeeds, but *before* attempting the actual mutation. When the mutation itself is then
        // rejected (e.g. MISSION_NOT_FOUND on delete, or DUPLICATE_MISSION on add) the on-disk
        // scenario file is correctly left untouched -- but the disk-backed undo-stack sidecar has
        // already gained a spurious snapshot for a mutation that never happened. That phantom entry
        // silently consumes an undo level and desyncs remainingUndoDepth from real edit history.
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);

            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));
            Assert.That(ScenarioUndoStackStore.Count(path), Is.EqualTo(1));

            using (var writer = new StringWriter())
            {
                var exitCode = MissionDeleteCommand.Run(path, 2, "does-not-exist", writer);
                Assert.That(exitCode, Is.EqualTo(1));
                Assert.That(writer.ToString(), Does.Contain("MISSION_NOT_FOUND"));
            }

            // A rejected delete (mission not found) must not leave a phantom undo snapshot behind:
            // the scenario file was never actually mutated for this attempt.
            Assert.That(ScenarioUndoStackStore.Count(path), Is.EqualTo(1));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Test]
    public void scenario_undo_preserves_sides_and_support_mission_fields_not_touched_by_the_mutation()
    {
        // ScenarioUndoStackStore.CloneDocument (used by both Push and TryPop) builds each undo
        // snapshot by hand-copying only a subset of ScenarioDocumentDto/ScenarioMissionDto fields
        // (Metadata + a partial Missions projection). Canonical sections such as Sides, Features,
        // Orbat, ReferencePoints, OperationsTimeline, Events, Variables, and EditorState -- plus the
        // per-mission StationGeometry/EmconOverride fields -- are silently omitted from the clone, so
        // a successful `scenario undo` doesn't just revert the mutated mission: it also resets every
        // one of these untouched sections/fields back to their empty defaults, destroying data that
        // was never part of the undone edit.
        var path = Path.Combine(Path.GetTempPath(), $"aegis-undo-{Guid.NewGuid():N}.json");
        try
        {
            var metadata = ScenarioDocumentEditor.CreateNew().ToDto().Metadata;
            var seeded = new ScenarioDocumentDto
            {
                Metadata = metadata,
                Sides = new[]
                {
                    new ScenarioSideDto { Id = "blue", Name = "Blue Force", DefaultRoe = "WeaponsFree" },
                },
                Missions = new[]
                {
                    new ScenarioMissionDto
                    {
                        Id = "tanker-1",
                        Type = "Support",
                        AssignedUnitIds = new[] { "u2" },
                        SupportRole = "Tanker",
                        PatrolZone = new[] { new ScenarioWaypointDto { Lat = 58.0, Lon = 21.0 } },
                        StationGeometry = new[] { new ScenarioWaypointDto { Lat = 58.0, Lon = 21.0 } },
                        EmconOverride = "Silent",
                    },
                },
            };
            ScenarioDocumentJsonWriter.WriteToFile(seeded, path);

            var zone = CliArgParser.ParseWaypoints(["57,20", "57.1,20.1", "57.2,20.2"]);
            Assert.That(MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()), Is.EqualTo(0));

            using (var writer = new StringWriter())
            {
                Assert.That(ScenarioUndoCommand.Run(path, 2, writer), Is.EqualTo(0));
                Assert.That(writer.ToString(), Does.Contain("\"undone\":true"));
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);

            // The undone add should be gone, leaving only the pre-existing support mission.
            Assert.That(dto.Missions, Has.Exactly(1).Items);
            var mission = dto.Missions.Single();
            Assert.That(mission.Id, Is.EqualTo("tanker-1"));

            // Fields untouched by the undone mutation must survive the undo round trip.
            Assert.That(dto.Sides, Has.Exactly(1).Items);
            var side = dto.Sides.Single();
            Assert.That(side.Id, Is.EqualTo("blue"));
            Assert.That(mission.StationGeometry ?? Array.Empty<ScenarioWaypointDto>(), Is.Not.Empty);
            Assert.That(mission.EmconOverride, Is.EqualTo("Silent"));
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