namespace ProjectAegis.Data.Tests.Scenario;

using System.IO;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

/// <summary>AC-11: TeleportUnit actions are stripped at export with logged manifest entries.</summary>
public sealed class TeleportUnitExportTests
{
    [Test]
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

        Assert.That(result.ManifestEntries, Has.Exactly(1).Items);
        var entry = result.ManifestEntries[0];
        Assert.That(entry.EventId, Is.EqualTo("evt-teleport-test"));
        Assert.That(entry.ActionIndex, Is.EqualTo(0));
        Assert.That(entry.ActionType, Is.EqualTo("TeleportUnit"));
        Assert.That(entry.Message, Does.Contain("Removed TeleportUnit"));

        var exportedEvent = ProjectAegis.Data.Tests.AssertEx.Single(result.ExportedDocument.Events!);
        var exportedAction = ProjectAegis.Data.Tests.AssertEx.Single(exportedEvent.Actions);
        Assert.That(exportedAction.Type, Is.EqualTo("ActivateMission"));
        Assert.That(            exportedEvent.Actions.Any(            a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase)), Is.False);
    }

    [Test]
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

        Assert.That(exportPackage.Allowed, Is.True);
        Assert.That(            ScenarioDocumentJsonWriter.Serialize(exportPackage.ExportDocument), Is.EqualTo(            ScenarioDocumentJsonWriter.Serialize(directTransform.ExportedDocument)));
        Assert.That(exportPackage.TransformManifest.Count, Is.EqualTo(directTransform.ManifestEntries.Count));

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
            Assert.That(simJson, Is.EqualTo(exportJson));  // identical post-transform event sets
            Assert.That(simPackage.TransformManifest.Count, Is.EqualTo(directTransform.ManifestEntries.Count));

            // Explicit: no TeleportUnit survives in either path's output events
            foreach (var e in exportPackage.ExportDocument.Events ?? Array.Empty<ScenarioEventDto>())
{
Assert.That(e.Actions.Any(a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase)), Is.False);
}
            foreach (var e in simPackage.ExportDocument.Events ?? Array.Empty<ScenarioEventDto>())
{
Assert.That(e.Actions.Any(a => string.Equals(a.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase)), Is.False);
}
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Test]
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

        Assert.That(manifest.ExportTransformLog, Has.Exactly(1).Items);
        Assert.That(manifest.ExportTransformLog[0].Message, Does.Contain("edit-test only"));

        var serialized = ManifestBuilder.Serialize(manifest);
        Assert.That(serialized, Does.Contain("exportTransformLog"));
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
    [Test]
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
        Assert.That(result.ManifestEntries.Count, Is.EqualTo(2));
        foreach (var e in result.ManifestEntries)
{
Assert.That(e.ActionType, Is.EqualTo("TeleportUnit"));
}
        var kept = ProjectAegis.Data.Tests.AssertEx.Single(ProjectAegis.Data.Tests.AssertEx.Single(result.ExportedDocument.Events!).Actions);
        Assert.That(kept.Type, Is.EqualTo("ActivateMission"));
    }
}