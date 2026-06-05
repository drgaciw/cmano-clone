namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

/// <summary>Headless export gate for MCP scenario_validate (TR-editor-005).</summary>
public static class ScenarioValidationExportGate
{
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