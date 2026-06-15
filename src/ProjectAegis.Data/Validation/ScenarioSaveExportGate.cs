namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Save always allowed; export/play/sample blocked on validation errors (GDD AC-12).</summary>
public static class ScenarioSaveExportGate
{
    public static bool CanSave(ScenarioDocumentDto scenario) => scenario != null;

    public static (bool Allowed, ValidationReport Report) CanExportOrPlay(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig? config = null)
    {
        config ??= ValidationConfigLoader.LoadFromRepo();
        var engine = new ScenarioValidationEngine();
        var report = engine.Validate(scenario, catalog, config);
        var complexity = EventComplexityAnalyzer.Analyze(scenario, config);
        if (complexity.Count > 0)
        {
            report = ValidationReport.FromFindings(report.Findings.Concat(complexity).ToList());
        }

        return (report.CanExport(config), report);
    }
}
