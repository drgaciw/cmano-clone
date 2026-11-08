namespace ProjectAegis.Data.Validation;

using Catalog;
using Scenario.Authoring;

public interface IScenarioValidationRule
{
    string RuleId { get; }

    void Evaluate(ScenarioDocumentDto scenario, ICatalogReader catalog, ValidationConfig config, List<ValidationFinding> sink);
}