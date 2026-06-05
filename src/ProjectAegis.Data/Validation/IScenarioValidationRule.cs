namespace ProjectAegis.Data.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;

public interface IScenarioValidationRule
{
    string RuleId { get; }

    void Evaluate(ScenarioDocumentDto scenario, ICatalogReader catalog, ValidationConfig config, List<ValidationFinding> sink);
}