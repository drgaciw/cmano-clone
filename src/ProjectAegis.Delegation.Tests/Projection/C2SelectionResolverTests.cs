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
}