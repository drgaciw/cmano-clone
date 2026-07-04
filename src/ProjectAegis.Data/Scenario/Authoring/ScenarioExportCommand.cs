namespace ProjectAegis.Data.Scenario.Authoring;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;

/// <summary>
/// Pre-export pipeline: apply logged transforms, then run the validation export gate.
/// Used by <c>scenario_publish</c> and <c>scenario_simulate_sample</c>.
/// 
/// S83-01 (track D): Export command polish per sprint-83-export-undo-ferry.md + roadmap-execute-plan-07042026.md §4 + scenario-editor-scope-boundary-2026-07-04.md.
/// Cites: qa-plan-scenario-editor-2026-07-01.md (units), implementation-tracker-2026-07-04.md (track D), 11-Agentic-Mission-Editor.md (AC-12 etc), S81/S82 closeouts, AGENTS.md.
/// GitNexus pre: ScenarioDocumentEditor CRITICAL upstream 20; editor subset filters required.
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

    /// <summary>
    /// S83-01 polish helper: produce CLI/MCP friendly summary for scenario_export surface.
    /// Includes manifest size, allowed flag, report hash for provenance.
    /// </summary>
    public static string FormatExportSummary(ExportPackage pkg)
    {
        var manifestCount = pkg.TransformManifest?.Count ?? 0;
        var reportHash = pkg.ValidationReport?.ReportHash ?? "n/a";
        return $"export_allowed={pkg.Allowed} transforms={manifestCount} reportHash={reportHash} missions={pkg.ExportDocument?.Missions?.Count ?? 0}";
    }

    /// <summary>AC-11 export transform: strip edit-test-only TeleportUnit actions with manifest logging.</summary>
    /// <remarks>
    /// S84-02 track. Explicit logged (never silent). Post-transform event sets must match between
    /// export (Prepare/publish) and simulate sample paths. Cites: sprint-84-event-debugger.md,
    /// sprint-84-parallel-kickoff-2026-07-04.md, scenario-editor-scope-boundary-2026-07-04.md,
    /// roadmap-execute-plan-07042026.md, qa-plan-scenario-editor-2026-07-01.md #11,
    /// 11-Agentic-Mission-Editor.md (AME-6.8 AC-11), AGENTS.md.
    /// GitNexus pre: Editor CRITICAL, Validation HIGH.
    /// </remarks>
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
            var actions = evt.Actions ?? Array.Empty<ScenarioEventActionDto>();
            var keptActions = new List<ScenarioEventActionDto>();
            for (var i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
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