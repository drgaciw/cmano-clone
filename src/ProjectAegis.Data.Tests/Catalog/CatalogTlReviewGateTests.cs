using ProjectAegis.Data.Catalog;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Catalog;

public sealed class CatalogTlReviewGateTests
{
    [Test]
    public void ApplyTlReviewGate_rejects_provisional_and_low_trl()
    {
        var bindings = new[]
        {
            new CatalogSensorBinding("u1", "a", 0.9, TrlLevel: 9, ReviewState: CatalogReviewStates.Approved),
            new CatalogSensorBinding("u1", "b", 0.9, TrlLevel: 3, ReviewState: CatalogReviewStates.Approved),
            new CatalogSensorBinding("u1", "c", 0.9, TrlLevel: 9, ReviewState: CatalogReviewStates.Provisional),
        };

        var gated = CatalogImportGate.ApplyTlReviewGate(bindings);

        Assert.That(gated, Has.Exactly(1).Items);
        Assert.That(gated[0].SensorId, Is.EqualTo("a"));
    }
}