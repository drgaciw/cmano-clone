namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ValidationPipelineTests
{
    [Fact]
    public void Run_baltic_fixture_passes_agent_chain()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var result = new ValidationPipeline().Run(catalog);
        Assert.True(result.Passed);
        Assert.True(result.Reports.Count > 0);
    }
}