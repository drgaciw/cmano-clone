namespace ProjectAegis.Data.Tests.Catalog;

using ProjectAegis.Data.Catalog;
using Xunit;

public sealed class CatalogWeaponEnvelopeTests
{
    [Fact]
    public void InMemory_reader_resolves_mvp_default_envelope()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        Assert.True(catalog.TryGetWeaponEnvelope(CatalogWeaponIds.MvpDefault, out var envelope));
        Assert.Equal(1_000, envelope.MinRangeMeters);
        Assert.Equal(100_000, envelope.MaxRangeMeters);
    }

    [Fact]
    public void Null_reader_does_not_resolve_weapon_envelope()
    {
        Assert.False(NullCatalogReader.Instance.TryGetWeaponEnvelope("mvp-default", out _));
    }
}