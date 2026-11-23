using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class C2SelectionResolverTests
{
    [Test]
    public void ResolveDefaultFriendlyUnit_prefers_alive_sorted_by_id()
    {
        var id = C2SelectionResolver.ResolveDefaultFriendlyUnit(
        [
            new OobTreeEntry("u2", false),
            new OobTreeEntry("u1", true),
        ]);

        Assert.That(id, Is.EqualTo("u1"));
    }

    [Test]
    public void TryResolveFriendlyUnitFromSymbol_matches_friendly_only()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false),
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        };

        Assert.That(
            C2SelectionResolver.TryResolveFriendlyUnitFromSymbol("u1", symbols, out var unitId),
            Is.True);
        Assert.That(unitId, Is.EqualTo("u1"));
        Assert.That(
            C2SelectionResolver.TryResolveFriendlyUnitFromSymbol("c1", symbols, out _),
            Is.False);
    }

    [Test]
    public void TryResolveHostileContactFromSymbol_matches_hostile_only()
    {
        var symbols = new[]
        {
            new MapSymbolEntry("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        };

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("c1", symbols, out var contactId),
            Is.True);
        Assert.That(contactId, Is.EqualTo("c1"));
    }

    private static readonly MapSymbolEntry[] D36Symbols =
    {
        new("u1", "Friendly", "■", "u1", 0.2f, 0.3f, false),
        new("c1", "Hostile", "◆", "c1", 0.5f, 0.5f, false),
        new("hostile-1", "Hostile", "◆", "hostile-1", 0.6f, 0.6f, false),
    };

    [Test]
    public void TryResolveHostileContactFromSymbol_unit_own_marker_maps_to_tracking_contact()
    {
        var contacts = new[] { new ContactPictureEntry("c1", "hostile-1", "u1", "CLASSIFIED", 5, 5.0) };

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("hostile-1", D36Symbols, contacts, out var contactId),
            Is.True);
        Assert.That(contactId, Is.EqualTo("c1"));
    }

    [Test]
    public void TryResolveHostileContactFromSymbol_contact_marker_resolves_to_itself()
    {
        var contacts = new[] { new ContactPictureEntry("c1", "hostile-1", "u1", "CLASSIFIED", 5, 5.0) };

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("c1", D36Symbols, contacts, out var contactId),
            Is.True);
        Assert.That(contactId, Is.EqualTo("c1"));
    }

    [Test]
    public void TryResolveHostileContactFromSymbol_unit_without_tracking_contact_is_not_selectable()
    {
        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol(
                "hostile-1", D36Symbols, Array.Empty<ContactPictureEntry>(), out var contactId),
            Is.False);
        Assert.That(contactId, Is.Empty);
    }

    [Test]
    public void TryResolveHostileContactFromSymbol_friendly_symbol_is_not_a_contact()
    {
        var contacts = new[] { new ContactPictureEntry("c1", "u1", "hostile-1", "CLASSIFIED", 5, 5.0) };

        Assert.That(
            C2SelectionResolver.TryResolveHostileContactFromSymbol("u1", D36Symbols, contacts, out _),
            Is.False);
    }
}
