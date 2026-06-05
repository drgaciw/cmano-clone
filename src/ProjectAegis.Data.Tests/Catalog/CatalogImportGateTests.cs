using ProjectAegis.Data.Catalog;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogImportGateTests
{
    [Fact]
    public void ApplyMinimumConfidence_filters_low_confidence_rows()
    {
        var bindings = new[]
        {
            new CatalogSensorBinding("u1", "radar-1", 0.9, Confidence: 0.9),
            new CatalogSensorBinding("u1", "radar-2", 0.5, Confidence: 0.4),
        };

        var gated = CatalogImportGate.ApplyMinimumConfidence(bindings, minimumConfidence: 0.5);

        Assert.Single(gated);
        Assert.Equal("radar-1", gated[0].SensorId);
    }
}