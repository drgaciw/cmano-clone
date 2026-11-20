namespace ProjectAegis.Data.Tests.Catalog;

using ProjectAegis.Data.Catalog;
using NUnit.Framework;

public sealed class BalticV3UcavCatalogTests
{
    [Test]
    public void BalticV3Fixture_includes_ucav_recon_loadouts_on_both_sides()
    {
        var catalog = InMemoryCatalogReader.BalticV3Fixture();

        Assert.That(catalog.TryGetBasePd("ucav-blue", "internal-ir", out var bluePd), Is.True);
        Assert.That(catalog.TryGetBasePd("ucav-red", "internal-ir", out var redPd), Is.True);
        Assert.That(catalog.TryGetBasePd("ucav-blue", "recon-radar", out var blueRadarPd), Is.True);
        Assert.That(catalog.TryGetBasePd("ucav-red", "recon-radar", out var redRadarPd), Is.True);
        Assert.That(bluePd, Is.EqualTo(0.85).Within(0.000000005));
        Assert.That(redPd, Is.EqualTo(0.75).Within(0.000000005));
        Assert.That(blueRadarPd, Is.EqualTo(0.70).Within(0.000000005));
        Assert.That(redRadarPd, Is.EqualTo(0.65).Within(0.000000005));

        var loadouts = catalog.GetSortedLoadouts();
        Assert.That(loadouts.Any(l =>
            l.PlatformId == "ucav-blue" &&
            l.LoadoutId == "recon-internal-ir" &&
            l.LoadoutName == "Recon [Internal IR]"), Is.True);
        Assert.That(loadouts.Any(l =>
            l.PlatformId == "ucav-red" &&
            l.LoadoutId == "recon-internal-ir" &&
            l.LoadoutName == "Recon [Internal IR]"), Is.True);

        Assert.That(catalog.TryGetPlatformPosition("ucav-blue", out _, out _), Is.True);
        Assert.That(catalog.TryGetPlatformPosition("ucav-red", out _, out _), Is.True);
    }

    [Test]
    public void ResolveForScenario_uses_v3_fixture_for_baltic_v3_prefix_only()
    {
        var v3 = CatalogReaderFactory.ResolveForScenario("baltic-v3-patrol");
        var v2 = CatalogReaderFactory.ResolveForScenario("baltic-patrol");

        Assert.That(v3.LayerVersion, Is.EqualTo("p0-baltic-v3-fixture"));
        Assert.That(v3.TryGetBasePd("ucav-blue", "internal-ir", out _), Is.True);
        Assert.That(v2.TryGetBasePd("ucav-blue", "internal-ir", out _), Is.False);
    }
}
