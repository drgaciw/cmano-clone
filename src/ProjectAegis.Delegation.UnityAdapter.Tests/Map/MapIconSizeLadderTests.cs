using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Headless coverage for the icon size ladder (req 20 rev 2 §Map and Symbology — "Icon size ladder per
/// zoom band (theater / regional / tactical)" and "labels appear from regional zoom").
/// <see cref="MapIconSizeLadder"/> is a pure function of <see cref="MapZoomBand"/> only.
/// </summary>
public sealed class MapIconSizeLadderTests
{
    [Test]
    public void ResolveIconSizePx_is_monotonically_increasing_theater_to_tactical()
    {
        var theater = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Theater);
        var regional = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Regional);
        var tactical = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Tactical);

        Assert.That(theater, Is.LessThan(regional));
        Assert.That(regional, Is.LessThan(tactical));
    }

    [TestCase(MapZoomBand.Theater, 10f)]
    [TestCase(MapZoomBand.Regional, 14f)]
    [TestCase(MapZoomBand.Tactical, 20f)]
    public void ResolveIconSizePx_returns_documented_ladder_values(MapZoomBand band, float expectedPx)
    {
        Assert.That(MapIconSizeLadder.ResolveIconSizePx(band), Is.EqualTo(expectedPx));
    }

    [Test]
    public void ResolveIconSizePx_is_a_pure_function_of_zoom_band_only()
    {
        // Same input, called repeatedly and in any order, always yields the same output (no hidden state).
        var first = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Regional);
        _ = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Tactical);
        _ = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Theater);
        var second = MapIconSizeLadder.ResolveIconSizePx(MapZoomBand.Regional);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void AreLabelsVisible_hides_labels_only_at_theater_band()
    {
        Assert.That(MapIconSizeLadder.AreLabelsVisible(MapZoomBand.Theater), Is.False);
        Assert.That(MapIconSizeLadder.AreLabelsVisible(MapZoomBand.Regional), Is.True);
        Assert.That(MapIconSizeLadder.AreLabelsVisible(MapZoomBand.Tactical), Is.True);
    }

    [Test]
    public void ResolveIconSizePx_unknown_band_value_degrades_to_theater_without_throwing()
    {
        var outOfRange = (MapZoomBand)999;

        Assert.That(
            MapIconSizeLadder.ResolveIconSizePx(outOfRange),
            Is.EqualTo(MapIconSizeLadder.TheaterIconSizePx));
    }
}
