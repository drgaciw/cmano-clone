using ProjectAegis.Data.Agents;
using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Agents;

public sealed class DatabaseIntelligenceOrchestratorTests
{
    [Fact]
    public void Pipeline_reports_follow_documented_agent_order()
    {
        var result = new DatabaseIntelligenceOrchestrator().Run(InMemoryCatalogReader.BalticPatrolFixture());
        Assert.Equal(
            DatabaseIntelligenceOrchestrator.PipelineAgentOrder,
            result.Reports.Select(r => r.AgentId).ToArray());
    }

    [Fact]
    public void Baltic_fixture_pipeline_passes_rules_agent()
    {
        var result = new DatabaseIntelligenceOrchestrator().Run(InMemoryCatalogReader.BalticPatrolFixture());
        var rules = result.Reports.First(r => r.AgentId == "rules_validation");
        Assert.True(rules.Passed);
        Assert.Empty(rules.Findings);
    }

    [Fact]
    public void Entity_resolution_flags_whitespace_platform_ids()
    {
        var bad = new InMemoryCatalogReader(
        [
            new CatalogSensorBinding("bad id", "radar", 0.5, ReviewState: CatalogReviewStates.Approved),
        ]);
        var entity = new CatalogEntityResolutionAgent().Run(new DatabaseAgentContext(bad));
        Assert.Contains(entity.Findings, f => f.Code == "ENTITY_ID_ALIAS_REQUIRED");
    }
}