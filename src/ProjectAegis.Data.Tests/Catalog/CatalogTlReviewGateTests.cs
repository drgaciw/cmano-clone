using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogTlReviewGateTests
{
    [Fact]
    public void ApplyTlReviewGate_rejects_provisional_and_low_trl()
    {
        var bindings = new[]
        {
            new CatalogSensorBinding("u1", "a", 0.9, TrlLevel: 9, ReviewState: CatalogReviewStates.Approved),
            new CatalogSensorBinding("u1", "b", 0.9, TrlLevel: 3, ReviewState: CatalogReviewStates.Approved),
            new CatalogSensorBinding("u1", "c", 0.9, TrlLevel: 9, ReviewState: CatalogReviewStates.Provisional),
        };

        var gated = CatalogImportGate.ApplyTlReviewGate(bindings);

        Assert.Single(gated);
        Assert.Equal("a", gated[0].SensorId);
    }
}