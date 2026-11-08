namespace ProjectAegis.Data.Tests.Catalog;

using ProjectAegis.Data.Catalog;
using Xunit;

public sealed class BalticV3UcavCatalogTests
{
    [Fact]
    public void BalticV3Fixture_includes_ucav_recon_loadouts_on_both_sides()
    {
        var catalog = InMemoryCatalogReader.BalticV3Fixture();

        Assert.True(catalog.TryGetBasePd("ucav-blue", "internal-ir", out var bluePd));
        Assert.True(catalog.TryGetBasePd("ucav-red", "internal-ir", out var redPd));
        Assert.True(catalog.TryGetBasePd("ucav-blue", "recon-radar", out var blueRadarPd));
        Assert.True(catalog.TryGetBasePd("ucav-red", "recon-radar", out var redRadarPd));
        Assert.Equal(0.85, bluePd, precision: 9);
        Assert.Equal(0.75, redPd, precision: 9);
        Assert.Equal(0.70, blueRadarPd, precision: 9);
        Assert.Equal(0.65, redRadarPd, precision: 9);

        var loadouts = catalog.GetSortedLoadouts();
        Assert.Contains(loadouts, l =>
            l.PlatformId == "ucav-blue" &&
            l.LoadoutId == "recon-internal-ir" &&
            l.LoadoutName == "Recon [Internal IR]");
        Assert.Contains(loadouts, l =>
            l.PlatformId == "ucav-red" &&
            l.LoadoutId == "recon-internal-ir" &&
            l.LoadoutName == "Recon [Internal IR]");

        Assert.True(catalog.TryGetPlatformPosition("ucav-blue", out _, out _));
        Assert.True(catalog.TryGetPlatformPosition("ucav-red", out _, out _));
    }

    [Fact]
    public void ResolveForScenario_uses_v3_fixture_for_baltic_v3_prefix_only()
    {
        var v3 = CatalogReaderFactory.ResolveForScenario("baltic-v3-patrol");
        var v2 = CatalogReaderFactory.ResolveForScenario("baltic-patrol");

        Assert.Equal("p0-baltic-v3-fixture", v3.LayerVersion);
        Assert.True(v3.TryGetBasePd("ucav-blue", "internal-ir", out _));
        Assert.False(v2.TryGetBasePd("ucav-blue", "internal-ir", out _));
    }
}
