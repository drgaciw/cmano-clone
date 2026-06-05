using ProjectAegis.Data.Catalog;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class CatalogEngageEnvelopeTests
{
    [Test]
    public void Apply_replaces_envelope_from_catalog()
    {
        var context = new EngageContext(
            50_000,
            new WeaponEnvelope(0, 0),
            2,
            true);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var merged = CatalogEngageEnvelope.Apply(context, catalog);
        Assert.That(merged.Envelope.MinRangeMeters, Is.EqualTo(1_000));
        Assert.That(merged.Envelope.MaxRangeMeters, Is.EqualTo(100_000));
        Assert.That(merged.RangeMeters, Is.EqualTo(50_000));
    }
}