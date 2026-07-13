namespace ProjectAegis.Data.Tests.Scenario;

using System.IO;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

/// <summary>AC-11: TeleportUnit actions are stripped at export with logged manifest entries.</summary>
public sealed class TeleportUnitExportTests
{
    [Fact]
    public void Export_removes_TeleportUnit_actions_and_logs_manifest_entries()
    {
        var source = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = CatalogTlTier.Default,
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["u1"],
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-teleport-test",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions =
                    [
                        new ScenarioEventActionDto { Type = "TeleportUnit", UnitId = "u1", Lat = 58.0, Lon = 21.0 },
                        new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "patrol-1" },
                    ],
                },
            ],
        };

        var result = ScenarioExportCommand.ApplyTeleportUnitExportTransform(source);

        Assert.Single(result.ManifestEntries);
        var entry = result.ManifestEntries[0];
        Assert.Equal("evt-teleport-test", entry.EventId);
        Assert.Equal(0, entry.ActionIndex);
        Assert.Equal("TeleportUnit", entry.ActionType);
        Assert.Contains("Removed TeleportUnit", entry.Message, StringComparison.Ordinal);

        var exportedEvent = Assert.Single(result.ExportedDocument.Events!);
        var exportedAction = Assert.Single(exportedEvent.Actions);
        Assert.Equal("ActivateMission", exportedAction.Type);
        Assert.DoesNotContain(
            exportedEvent.Actions,
            a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ScenarioExportCommand_and_simulate_sample_share_identical_post_transform_event_set()
    {
        // S84-02 (AC-11): Teleport export transform track.
        // Cites: production/sprints/sprint-84-event-debugger.md, production/agentic/sprint-84-parallel-kickoff-2026-07-04.md,
        // production/scenario-editor-scope-boundary-2026-07-04.md, docs/reports/roadmap-execute-plan-07042026.md §4 (S84),
        // production/qa/qa-plan-scenario-editor-2026-07-01.md (#11), Game-Requirements/requirements/11-Agentic-Mission-Editor.md (AME-6.8, AC-11),
        // implementation-tracker-2026-07-04.md, AGENTS.md, CLAUDE.md, design/gdd/agentic-mission-editor.md.
        // GitNexus pre completed on ScenarioDocumentEditor (CRITICAL 20), ScenarioValidationEngine (HIGH 17), ValidationRules, ScenarioSimulateSampleCommand.
        // Focus: export side + manifest (TeleportUnit action may be partial). Never silent strip: manifest always records removals.
        var source = ScenarioWithTeleportUnit();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var config = new ValidationConfig();

        // Export path (Prepare used by ScenarioPublishCommand / direct export)
        var exportPackage = ScenarioExportCommand.Prepare(source, catalog, config);
        var directTransform = ScenarioExportCommand.ApplyTeleportUnitExportTransform(source);

        Assert.True(exportPackage.Allowed);
        Assert.Equal(
            ScenarioDocumentJsonWriter.Serialize(directTransform.ExportedDocument),
            ScenarioDocumentJsonWriter.Serialize(exportPackage.ExportDocument));
        Assert.Equal(directTransform.ManifestEntries.Count, exportPackage.TransformManifest.Count);

        // Simulate sample path (mimics ScenarioSimulateSampleCommand.Run: file load then Prepare)
        // Ensures post-transform event sets (serialized doc + manifest count + no TeleportUnit) identical.
        var tmpPath = Path.Combine(Path.GetTempPath(), $"teleport-ac11-sim-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentJsonWriter.WriteToFile(source, tmpPath);
            var loaded = ScenarioDocumentJsonLoader.LoadFromFile(tmpPath);
            var simPackage = ScenarioExportCommand.Prepare(loaded, catalog, config);

            var exportJson = ScenarioDocumentJsonWriter.Serialize(exportPackage.ExportDocument);
            var simJson = ScenarioDocumentJsonWriter.Serialize(simPackage.ExportDocument);
            Assert.Equal(exportJson, simJson);  // identical post-transform event sets
            Assert.Equal(directTransform.ManifestEntries.Count, simPackage.TransformManifest.Count);

            // Explicit: no TeleportUnit survives in either path's output events
            Assert.All(exportPackage.ExportDocument.Events ?? Array.Empty<ScenarioEventDto>(), e =>
                Assert.DoesNotContain(e.Actions, a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase)));
            Assert.All(simPackage.ExportDocument.Events ?? Array.Empty<ScenarioEventDto>(), e =>
                Assert.DoesNotContain(e.Actions, a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase)));
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Fact]
    public void ManifestBuilder_includes_export_transform_log_entries()
    {
        var source = ScenarioWithTeleportUnit();
        var transform = ScenarioExportCommand.ApplyTeleportUnitExportTransform(source);
        var report = ValidationReport.FromFindings([]);

        var manifest = ManifestBuilder.Build(
            "teleport-test",
            transform.ExportedDocument,
            report,
            exportTransformLog: transform.ManifestEntries);

        Assert.Single(manifest.ExportTransformLog);
        Assert.Contains("edit-test only", manifest.ExportTransformLog[0].Message, StringComparison.Ordinal);

        var serialized = ManifestBuilder.Serialize(manifest);
        Assert.Contains("exportTransformLog", serialized, StringComparison.Ordinal);
    }

    private static ScenarioDocumentDto ScenarioWithTeleportUnit() =>
        new()
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = CatalogTlTier.Default,
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["u1"],
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-teleport-test",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions =
                    [
                        new ScenarioEventActionDto { Type = "TeleportUnit", UnitId = "u1", Lat = 58.0, Lon = 21.0 },
                    ],
                },
            ],
        };

    /// <summary>AC-11 additive: multiple TeleportUnits in one event all stripped, multiple manifest entries.</summary>
    [Fact]
    public void Export_removes_multiple_TeleportUnit_actions_and_logs_all()
    {
        var source = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { DbRef = "baltic_patrol", TlBranch = CatalogTlTier.Default },
            Events =
            [
                new ScenarioEventDto
                {
                    Id = "evt-multi-tp",
                    TriggerType = "Time",
                    Conditions = [new ScenarioEventConditionDto { Type = "Time", Result = true }],
                    Actions =
                    [
                        new ScenarioEventActionDto { Type = "TeleportUnit", UnitId = "u1", Lat = 1, Lon = 2 },
                        new ScenarioEventActionDto { Type = "TeleportUnit", UnitId = "u2", Lat = 3, Lon = 4 },
                        new ScenarioEventActionDto { Type = "ActivateMission", UnitId = "m1" },
                    ],
                },
            ],
        };

        var result = ScenarioExportCommand.ApplyTeleportUnitExportTransform(source);
        Assert.Equal(2, result.ManifestEntries.Count);
        Assert.All(result.ManifestEntries, e => Assert.Equal("TeleportUnit", e.ActionType));
        var kept = Assert.Single(Assert.Single(result.ExportedDocument.Events!).Actions);
        Assert.Equal("ActivateMission", kept.Type);
    }
}