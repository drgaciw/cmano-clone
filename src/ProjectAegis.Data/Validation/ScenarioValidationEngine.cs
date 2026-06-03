namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation.Rules;

/// <summary>Deterministic v1 validation pipeline (ADR-008).</summary>
public sealed class ScenarioValidationEngine : IScenarioValidationEngine
{
    public ValidationReport Validate(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig config)
    {
        var findings = new List<ValidationFinding>();
        ValidationRules.DbRefRule(scenario, catalog, findings);
        ValidationRules.MissionNoUnitsRule(scenario, findings);
        ValidationRules.PatrolZoneRule(scenario, findings);
        ValidationRules.StrikeNoTargetsRule(scenario, findings);
        ValidationRules.FerryDestinationRule(scenario, findings);
        ValidationRules.StrikeReachabilityRule(scenario, catalog, config, findings);
        return ValidationReport.FromFindings(findings);
    }
}

public interface IScenarioValidationEngine
{
    ValidationReport Validate(ScenarioDocumentDto scenario, ICatalogReader catalog, ValidationConfig config);
}