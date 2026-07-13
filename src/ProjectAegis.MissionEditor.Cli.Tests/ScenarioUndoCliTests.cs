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

    [Fact]
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

            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-disk", ["u1"], zone, new StringWriter()));

            var stackFile = path + ".undo-stack.json";
            Assert.True(File.Exists(stackFile), "disk sidecar for AME-8.5 persistence");
            Assert.True(ScenarioUndoStackStore.Count(path) >= 1);

            using (var writer = new StringWriter())
            {
                // current after add is 2
                Assert.Equal(0, ScenarioUndoCommand.Run(path, 2, writer));
                Assert.Contains("\"undone\":true", writer.ToString());
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.Equal(1, dto.Metadata.EditVersion);
            Assert.Equal(0, ScenarioUndoStackStore.Count(path));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Fact]
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

            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));
            Assert.Equal(1, ScenarioUndoStackStore.Count(path));

            using (var writer = new StringWriter())
            {
                var exitCode = MissionDeleteCommand.Run(path, 2, "does-not-exist", writer);
                Assert.Equal(1, exitCode);
                Assert.Contains("MISSION_NOT_FOUND", writer.ToString());
            }

            // A rejected delete (mission not found) must not leave a phantom undo snapshot behind:
            // the scenario file was never actually mutated for this attempt.
            Assert.Equal(1, ScenarioUndoStackStore.Count(path));
        }
        finally
        {
            DeleteIfExists(path);
            DeleteIfExists(path + ".undo-stack.json");
        }
    }

    [Fact]
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
            Assert.Equal(0, MissionAddPatrolCommand.Run(path, 1, "patrol-1", ["u1"], zone, new StringWriter()));

            using (var writer = new StringWriter())
            {
                Assert.Equal(0, ScenarioUndoCommand.Run(path, 2, writer));
                Assert.Contains("\"undone\":true", writer.ToString());
            }

            var dto = ScenarioDocumentJsonLoader.LoadFromFile(path);

            // The undone add should be gone, leaving only the pre-existing support mission.
            var mission = Assert.Single(dto.Missions);
            Assert.Equal("tanker-1", mission.Id);

            // Fields untouched by the undone mutation must survive the undo round trip.
            var side = Assert.Single(dto.Sides);
            Assert.Equal("blue", side.Id);
            Assert.NotEmpty(mission.StationGeometry ?? Array.Empty<ScenarioWaypointDto>());
            Assert.Equal("Silent", mission.EmconOverride);
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