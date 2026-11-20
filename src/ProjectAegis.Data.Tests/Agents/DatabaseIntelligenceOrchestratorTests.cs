using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Agents;

public sealed class DatabaseIntelligenceOrchestratorTests
{
    [Test]
    public void Pipeline_reports_follow_documented_agent_order()
    {
        var result = new DatabaseIntelligenceOrchestrator().Run(InMemoryCatalogReader.BalticPatrolFixture());
        Assert.That(            result.Reports.Select(r => r.AgentId).ToArray(), Is.EqualTo(            DatabaseIntelligenceOrchestrator.PipelineAgentOrder));
    }

    [Test]
    public void Baltic_fixture_pipeline_passes_rules_agent()
    {
        var result = new DatabaseIntelligenceOrchestrator().Run(InMemoryCatalogReader.BalticPatrolFixture());
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");
        Assert.That(rules.Passed, Is.True);
        Assert.That(rules.Findings, Is.Empty);
    }

    [Test]
    public void Entity_resolution_flags_whitespace_platform_ids()
    {
        var bad = new InMemoryCatalogReader(
        [
            new CatalogSensorBinding("bad id", "radar", 0.5, ReviewState: CatalogReviewStates.Approved),
        ]);
        var entity = new CatalogEntityResolutionAgent().Run(new DatabaseAgentContext(bad));
        Assert.That(entity.Findings.Any(f => f.Code == "ENTITY_ID_ALIAS_REQUIRED"), Is.True);
    }
}