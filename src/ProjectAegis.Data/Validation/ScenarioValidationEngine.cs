namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation.Rules;

/// <summary>Deterministic v1 validation pipeline (ADR-008). S84 additive: includes EventGraphComplexityRule (ADR-016) for AC-7/11/16 tracks. Cites: scenario-editor-scope-boundary-2026-07-04.md, sprint-84-event-debugger.md, qa-plan-scenario-editor-2026-07-01.md.</summary>
public sealed class ScenarioValidationEngine : IScenarioValidationEngine
{
    public ValidationReport Validate(
        ScenarioDocumentDto scenario,
        ICatalogReader catalog,
        ValidationConfig config)
    {
        var findings = new List<ValidationFinding>();
        ValidationRules.TlBranchRule(scenario, catalog, findings);
        ValidationRules.TlReleaseTrainRule(scenario, catalog, findings);
        ValidationRules.DbRefRule(scenario, catalog, findings);
        ValidationRules.MissionNoUnitsRule(scenario, findings);
        ValidationRules.PatrolZoneRule(scenario, findings);
        ValidationRules.StrikeNoTargetsRule(scenario, findings);
        ValidationRules.FerryDestinationRule(scenario, findings);
        ValidationRules.AirReadyLaunchRule(scenario, findings);
        ValidationRules.FerryReachabilityRule(scenario, catalog, config, findings);
        ValidationRules.StrikeReachabilityRule(scenario, catalog, config, findings);
        ValidationRules.IncompatibleHostRule(scenario, findings); // model integrity "incompatible host relationships"
        ValidationRules.BrokenRefRule(scenario, findings); // "broken references"
        ValidationRules.DoctrineInheritanceRule(scenario, findings);
        ValidationRules.EventGraphComplexityRule(scenario, config, findings); // ADR-016 S84: soft warnings + hard 32-cond cap (never blocks on soft)
        return ValidationReport.FromFindings(findings);
    }
}

public interface IScenarioValidationEngine
{
    ValidationReport Validate(ScenarioDocumentDto scenario, ICatalogReader catalog, ValidationConfig config);
}