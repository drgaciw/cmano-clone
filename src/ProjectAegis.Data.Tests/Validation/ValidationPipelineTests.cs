namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

public sealed class ValidationPipelineTests
{
    [Test]
    public void Run_baltic_fixture_passes_agent_chain()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var result = new ValidationPipeline().Run(catalog);
        Assert.That(result.Passed, Is.True);
        Assert.That(result.Reports.Count > 0, Is.True);
    }
}