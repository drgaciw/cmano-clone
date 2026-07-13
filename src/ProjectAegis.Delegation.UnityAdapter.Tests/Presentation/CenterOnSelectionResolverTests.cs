using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 §Selection, TR-c2-005 (AC-7): center-on-selection centroid/anchor math.</summary>
[TestFixture]
public sealed class CenterOnSelectionResolverTests
{
    [Test]
    public void Resolve_returns_null_for_empty_selection()
    {
        var selection = new SelectionSet();
        var symbols = new[] { new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.5f, 0.5f, false) };

        Assert.That(CenterOnSelectionResolver.Resolve(selection, symbols), Is.Null);
    }

    [Test]
    public void Resolve_centers_on_the_single_selected_unit()
    {
        var selection = new SelectionSet();
        selection.Add("u1");
        var symbols = new[] { new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.25f, 0.75f, false) };

        var target = CenterOnSelectionResolver.Resolve(selection, symbols);

        Assert.That(target, Is.Not.Null);
        Assert.That(target!.Value.NormalizedX, Is.EqualTo(0.25f));
        Assert.That(target.Value.NormalizedY, Is.EqualTo(0.75f));
    }

    [Test]
    public void Resolve_averages_the_positions_of_a_multi_select()
    {
        var selection = new SelectionSet();
        selection.Add("u1");
        selection.Add("u2");
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.0f, 0.0f, false),
            new MapSymbolEntry("u2", "Friendly", "■", "u2", 1.0f, 1.0f, false),
        };

        var target = CenterOnSelectionResolver.Resolve(selection, symbols);

        Assert.That(target!.Value.NormalizedX, Is.EqualTo(0.5f));
        Assert.That(target.Value.NormalizedY, Is.EqualTo(0.5f));
    }

    [Test]
    public void Resolve_excludes_destroyed_symbols_from_the_average()
    {
        var selection = new SelectionSet();
        selection.Add("u1");
        selection.Add("u2");
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.0f, 0.0f, true), // destroyed
            new MapSymbolEntry("u2", "Friendly", "■", "u2", 0.4f, 0.6f, false),
        };

        var target = CenterOnSelectionResolver.Resolve(selection, symbols);

        Assert.That(target!.Value.NormalizedX, Is.EqualTo(0.4f));
        Assert.That(target.Value.NormalizedY, Is.EqualTo(0.6f));
    }

    [Test]
    public void Resolve_returns_null_when_none_of_the_selection_has_a_live_symbol()
    {
        var selection = new SelectionSet();
        selection.Add("u1");
        var symbols = new[] { new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.5f, 0.5f, true) };

        Assert.That(CenterOnSelectionResolver.Resolve(selection, symbols), Is.Null);
    }

    [Test]
    public void Resolve_ignores_hostile_symbols_sharing_a_friendly_units_id()
    {
        var selection = new SelectionSet();
        selection.Add("u1");
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Hostile", "◆", "u1", 0.9f, 0.9f, false),
        };

        Assert.That(CenterOnSelectionResolver.Resolve(selection, symbols), Is.Null);
    }
}
