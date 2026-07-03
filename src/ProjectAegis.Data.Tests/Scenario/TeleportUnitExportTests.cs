namespace ProjectAegis.Data.Tests.Scenario;

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
        var source = ScenarioWithTeleportUnit();
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var config = new ValidationConfig();

        var exportPackage = ScenarioExportCommand.Prepare(source, catalog, config);
        var directTransform = ScenarioExportCommand.ApplyTeleportUnitExportTransform(source);

        Assert.True(exportPackage.Allowed);
        Assert.Equal(
            ScenarioDocumentJsonWriter.Serialize(directTransform.ExportedDocument),
            ScenarioDocumentJsonWriter.Serialize(exportPackage.ExportDocument));
        Assert.Equal(directTransform.ManifestEntries.Count, exportPackage.TransformManifest.Count);
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
}