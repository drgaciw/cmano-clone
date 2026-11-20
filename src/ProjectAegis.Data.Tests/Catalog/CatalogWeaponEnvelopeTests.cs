namespace ProjectAegis.Data.Tests.Catalog;

using ProjectAegis.Data.Catalog;
using NUnit.Framework;

public sealed class CatalogWeaponEnvelopeTests
{
    [Test]
    public void InMemory_reader_resolves_mvp_default_envelope()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.That(catalog.TryGetWeaponEnvelope(CatalogWeaponIds.MvpDefault, out var envelope), Is.True);
        Assert.That(envelope.MinRangeMeters, Is.EqualTo(1_000));
        Assert.That(envelope.MaxRangeMeters, Is.EqualTo(100_000));
    }

    [Test]
    public void Null_reader_does_not_resolve_weapon_envelope()
    {
        Assert.That(NullCatalogReader.Instance.TryGetWeaponEnvelope("mvp-default", out _), Is.False);
    }
}