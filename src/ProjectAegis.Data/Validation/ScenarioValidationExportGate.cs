namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>
/// Headless export gate (sole gate for export/play/simulate per ADR-008 / GDD).
///
/// Save path (<see cref="ScenarioDocumentEditor.Save"/>) deliberately bypasses validation
/// so WIP states with blocking errors can be persisted (AME-6.5 / AC-12).
///
/// All export paths (scenario_export_brief, ScenarioExportCommand.Prepare used by publish +
/// scenario_simulate_sample, play) call this and are blocked when any finding meets or exceeds
/// <see cref="ValidationConfig.ExportBlockSeverityFloor"/> (default: Error).
///
/// QA: qa-plan-scenario-editor-2026-07-01.md unit #12; scenario-editor-scope-boundary-2026-07-04.md;
/// roadmap-execute-plan-07042026.md; production/sprints/sprint-82-validation-tracks-ac.md (S82-03).
/// </summary>
public static class ScenarioValidationExportGate
{
    /// <summary>
    /// Evaluate whether the given scenario document is allowed for export under the provided
    /// catalog and config. Returns the decision + full report (always computed fresh for determinism).
    /// </summary>
    public static (bool Allowed, ValidationReport Report) EvaluateExport(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig? config = null)
    {
        config ??= new ValidationConfig();
        var engine = new ScenarioValidationEngine();
        var report = engine.Validate(scenario, catalog, config);
        return (report.CanExport(config), report);
    }
}