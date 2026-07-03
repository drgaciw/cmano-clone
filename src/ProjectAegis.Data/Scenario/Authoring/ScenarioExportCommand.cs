namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;

/// <summary>
/// Pre-export pipeline: apply logged transforms, then run the validation export gate.
/// Used by <c>scenario_publish</c> and <c>scenario_simulate_sample</c>.
/// </summary>
public static class ScenarioExportCommand
{
    public sealed record ExportPackage(
        ScenarioDocumentDto SourceDocument,
        ScenarioDocumentDto ExportDocument,
        IReadOnlyList<ExportTransformRemovalEntry> TransformManifest,
        ValidationReport ValidationReport,
        bool Allowed);

    public static ExportPackage Prepare(
        ScenarioDocumentDto source,
        ICatalogReader catalog,
        ValidationConfig? config = null)
    {
        config ??= new ValidationConfig();
        var transform = ApplyTeleportUnitExportTransform(source);
        var (allowed, report) = ScenarioValidationExportGate.EvaluateExport(
            transform.ExportedDocument,
            catalog,
            config);

        return new ExportPackage(
            source,
            transform.ExportedDocument,
            transform.ManifestEntries,
            report,
            allowed);
    }

    /// <summary>AC-11 export transform: strip edit-test-only TeleportUnit actions with manifest logging.</summary>
    public static ExportTransformResult ApplyTeleportUnitExportTransform(ScenarioDocumentDto document)
    {
        if (document.Events == null || document.Events.Count == 0)
        {
            return new ExportTransformResult(document, Array.Empty<ExportTransformRemovalEntry>());
        }

        var manifest = new List<ExportTransformRemovalEntry>();
        var exportedEvents = new List<ScenarioEventDto>(document.Events.Count);

        foreach (var evt in document.Events)
        {
            var keptActions = new List<ScenarioEventActionDto>();
            for (var i = 0; i < evt.Actions.Count; i++)
            {
                var action = evt.Actions[i];
                if (IsTeleportUnit(action))
                {
                    manifest.Add(new ExportTransformRemovalEntry(
                        evt.Id,
                        i,
                        action.Type,
                        $"Removed TeleportUnit action from event '{evt.Id}' at action index {i} (edit-test only)"));
                    continue;
                }

                keptActions.Add(action);
            }

            exportedEvents.Add(new ScenarioEventDto
            {
                Id = evt.Id,
                TriggerType = evt.TriggerType,
                Conditions = evt.Conditions,
                Actions = keptActions,
            });
        }

        var exported = new ScenarioDocumentDto
        {
            Metadata = document.Metadata,
            Missions = document.Missions,
            Events = exportedEvents,
            EditorState = document.EditorState,
        };

        return new ExportTransformResult(exported, manifest);
    }

    private static bool IsTeleportUnit(ScenarioEventActionDto action) =>
        string.Equals(action.Type, "TeleportUnit", StringComparison.OrdinalIgnoreCase);
}