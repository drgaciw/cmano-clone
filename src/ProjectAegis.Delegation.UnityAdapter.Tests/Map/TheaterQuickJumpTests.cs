using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>TR-c2-004 / ADR-018: pure theater quick-jump catalog (Baltic bbox + named theaters).</summary>
[TestFixture]
public sealed class TheaterQuickJumpTests
{
    [Test]
    public void Baltic_bounds_match_cesium_billboard_demo_span()
    {
        var b = TheaterQuickJump.BalticBounds;

        Assert.That(b.MinLatitude, Is.EqualTo(59.5).Within(1e-9));
        Assert.That(b.MaxLatitude, Is.EqualTo(60.5).Within(1e-9));
        Assert.That(b.MinLongitude, Is.EqualTo(24.0).Within(1e-9));
        Assert.That(b.MaxLongitude, Is.EqualTo(25.5).Within(1e-9));
        Assert.That(b.LatitudeSpan, Is.EqualTo(1.0).Within(1e-9));
        Assert.That(b.LongitudeSpan, Is.EqualTo(1.5).Within(1e-9));
        Assert.That(b.IsValid, Is.True);
        Assert.That(b.Contains(60.0, 25.0), Is.True);
        Assert.That(b.Contains(59.0, 25.0), Is.False);
    }

    [Test]
    public void All_includes_named_theaters_in_stable_order()
    {
        Assert.That(TheaterQuickJump.All.Select(t => t.Id), Is.EqualTo(new[]
        {
            "baltic",
            "giuk",
            "east-med",
            "persian-gulf",
        }));
    }

    [TestCase("baltic")]
    [TestCase("Baltic")]
    [TestCase("Baltic Sea")]
    [TestCase("baltic-patrol")]
    [TestCase("baltic-v3-asuw")]
    [TestCase("baltic-patrol-mission")]
    public void Resolve_maps_baltic_ids_and_scenario_aliases(string key)
    {
        var theater = TheaterQuickJump.Resolve(key);
        Assert.That(theater, Is.Not.Null);
        Assert.That(theater!.Id, Is.EqualTo(TheaterQuickJump.BalticId));
    }

    [Test]
    public void Resolve_returns_null_for_unknown_or_empty()
    {
        Assert.That(TheaterQuickJump.Resolve(null), Is.Null);
        Assert.That(TheaterQuickJump.Resolve(""), Is.Null);
        Assert.That(TheaterQuickJump.Resolve("   "), Is.Null);
        Assert.That(TheaterQuickJump.Resolve("not-a-theater"), Is.Null);
    }

    [Test]
    public void ResolveOrBaltic_falls_back_to_baltic()
    {
        Assert.That(TheaterQuickJump.ResolveOrBaltic("missing").Id, Is.EqualTo(TheaterQuickJump.BalticId));
    }

    [Test]
    public void Resolve_named_theaters_by_id()
    {
        Assert.That(TheaterQuickJump.Resolve("giuk")!.DisplayName, Is.EqualTo("GIUK Gap"));
        Assert.That(TheaterQuickJump.Resolve("east-med")!.Bounds.MinLatitude, Is.EqualTo(30.0));
        Assert.That(TheaterQuickJump.Resolve("persian-gulf")!.DefaultZoomBand, Is.EqualTo(MapZoomBand.Theater));
    }

    [Test]
    public void GeographicBounds_FromMinMax_normalizes_corner_order()
    {
        var bounds = GeographicBounds.FromMinMax(60.5, 25.5, 59.5, 24.0);
        Assert.That(bounds.MinLatitude, Is.EqualTo(59.5));
        Assert.That(bounds.MaxLongitude, Is.EqualTo(25.5));
    }
}
