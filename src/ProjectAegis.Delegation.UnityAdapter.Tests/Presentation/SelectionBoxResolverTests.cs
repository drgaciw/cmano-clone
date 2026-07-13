using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 §Selection, TR-c2-005 (AC-7): drag-box multi-select rect→unit-ids math, pure and
/// headless — no UnityEngine types, no live Editor.</summary>
[TestFixture]
public sealed class SelectionBoxResolverTests
{
    [Test]
    public void Resolve_selects_only_friendly_symbols_inside_the_rect()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.2f, false),
            new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.8f, 0.8f, false), // outside rect
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.25f, 0.25f, false), // inside rect but hostile
        };

        var rect = NormalizedRect.FromCorners(0.1f, 0.1f, 0.3f, 0.3f);
        var result = SelectionBoxResolver.Resolve(rect, symbols);

        Assert.That(result, Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void Resolve_excludes_destroyed_friendly_symbols()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.2f, true), // destroyed
        };

        var rect = NormalizedRect.FromCorners(0f, 0f, 1f, 1f);
        var result = SelectionBoxResolver.Resolve(rect, symbols);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Resolve_returns_ids_in_symbol_input_order()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u3", "Friendly", "■", "u3", 0.3f, 0.3f, false),
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.1f, 0.1f, false),
            new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.2f, 0.2f, false),
        };

        var rect = NormalizedRect.FromCorners(0f, 0f, 1f, 1f);
        var result = SelectionBoxResolver.Resolve(rect, symbols);

        Assert.That(result, Is.EqualTo(new[] { "u3", "u1", "u2" }), "insertion/input order, not re-sorted");
    }

    [Test]
    public void Resolve_rect_bounds_are_inclusive()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.5f, 0.5f, false), // exactly on the boundary
        };

        var rect = NormalizedRect.FromCorners(0.1f, 0.1f, 0.5f, 0.5f);
        var result = SelectionBoxResolver.Resolve(rect, symbols);

        Assert.That(result, Is.EqualTo(new[] { "u1" }));
    }

    [Test]
    public void Resolve_returns_empty_for_no_symbols()
    {
        var rect = NormalizedRect.FromCorners(0f, 0f, 1f, 1f);
        Assert.That(SelectionBoxResolver.Resolve(rect, Array.Empty<MapSymbolEntry>()), Is.Empty);
    }

    [Test]
    public void FromCorners_normalizes_arbitrary_corner_order()
    {
        var rect = NormalizedRect.FromCorners(0.7f, 0.7f, 0.2f, 0.2f); // pointer-up above/left of pointer-down

        Assert.That(rect.MinX, Is.EqualTo(0.2f));
        Assert.That(rect.MinY, Is.EqualTo(0.2f));
        Assert.That(rect.MaxX, Is.EqualTo(0.7f));
        Assert.That(rect.MaxY, Is.EqualTo(0.7f));
    }

    [Test]
    public void IsDrag_is_false_for_a_near_zero_movement_rect_click_disambiguation()
    {
        var rect = NormalizedRect.FromCorners(0.5f, 0.5f, 0.5001f, 0.5001f);
        Assert.That(rect.IsDrag, Is.False, "sub-threshold movement is a click, not a marquee drag");
    }

    [Test]
    public void IsDrag_is_true_once_movement_exceeds_the_threshold()
    {
        var rect = NormalizedRect.FromCorners(0.2f, 0.2f, 0.4f, 0.2f);
        Assert.That(rect.IsDrag, Is.True);
    }
}
