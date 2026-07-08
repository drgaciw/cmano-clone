using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Map;

/// <summary>
/// Headless coverage for the APP-6(D) domain (battle-dimension) modifier subset (req 20 rev 2
/// §Map and Symbology — "affiliation frames + domain modifiers used by docs 13–17"). Domain modifier
/// is a pure function of <see cref="CombatDomain"/>; it never encodes affiliation (see
/// App6GrayscaleAffiliationShapeTests for the shape-primary contract).
/// </summary>
public sealed class App6DomainModifierTests
{
    private static readonly CombatDomain[] AllDomains =
    [
        CombatDomain.Air,
        CombatDomain.Surface,
        CombatDomain.Subsurface,
        CombatDomain.Land,
        CombatDomain.Mine,
        CombatDomain.Facility,
    ];

    [Test]
    public void Resolve_covers_every_docs13to17_combat_domain_with_distinct_modifier_classes()
    {
        var classes = AllDomains.Select(d => App6DomainModifier.Resolve(d).ModifierClass).ToArray();

        Assert.That(classes, Has.Length.EqualTo(6));
        Assert.That(classes, Is.Unique);
        Assert.That(App6DomainModifier.KnownDomainModifierClasses, Has.Count.EqualTo(6));
        Assert.That(App6DomainModifier.KnownDomainModifierClasses, Is.Unique);
    }

    [Test]
    public void Resolve_assigns_distinct_battle_dimension_characters_per_domain()
    {
        var dimensions = AllDomains.Select(d => App6DomainModifier.Resolve(d).BattleDimension).ToArray();

        Assert.That(dimensions, Is.Unique);
        Assert.That(App6DomainModifier.Resolve(CombatDomain.Air).BattleDimension, Is.EqualTo('A'));
        Assert.That(App6DomainModifier.Resolve(CombatDomain.Surface).BattleDimension, Is.EqualTo('S'));
        Assert.That(App6DomainModifier.Resolve(CombatDomain.Subsurface).BattleDimension, Is.EqualTo('U'));
        Assert.That(App6DomainModifier.Resolve(CombatDomain.Land).BattleDimension, Is.EqualTo('G'));
    }

    [Test]
    public void Resolve_assigns_distinct_unicode_fallback_icons_per_domain()
    {
        var icons = AllDomains.Select(d => App6DomainModifier.Resolve(d).UnicodeIcon).ToArray();

        Assert.That(icons, Is.Unique);
        Assert.That(icons, Has.All.Not.Empty);
    }

    [TestCase(CombatDomain.Air)]
    [TestCase(CombatDomain.Surface)]
    [TestCase(CombatDomain.Subsurface)]
    [TestCase(CombatDomain.Land)]
    [TestCase(CombatDomain.Mine)]
    [TestCase(CombatDomain.Facility)]
    public void BuildSidc_splices_battle_dimension_without_disturbing_affiliation_identity(CombatDomain domain)
    {
        var friendlySidc = App6Sidc.FriendlySurfaceUnitSidc; // "SFGPU----------"

        var spliced = App6DomainModifier.BuildSidc(friendlySidc, domain);

        Assert.That(spliced, Has.Length.EqualTo(App6Sidc.SidcLength));
        Assert.That(spliced[0], Is.EqualTo('S'), "coding scheme unchanged");
        Assert.That(spliced[1], Is.EqualTo('F'), "affiliation identity unchanged");
        Assert.That(spliced[2], Is.EqualTo(App6DomainModifier.Resolve(domain).BattleDimension));
        Assert.That(spliced.Substring(3), Is.EqualTo(friendlySidc.Substring(3)), "remaining positions unchanged");
    }

    [Test]
    public void BuildSidc_hostile_air_and_hostile_surface_produce_distinct_sidc_strings()
    {
        var hostileSidc = App6Sidc.HostileContactSidc;

        var air = App6DomainModifier.BuildSidc(hostileSidc, CombatDomain.Air);
        var surface = App6DomainModifier.BuildSidc(hostileSidc, CombatDomain.Surface);
        var subsurface = App6DomainModifier.BuildSidc(hostileSidc, CombatDomain.Subsurface);

        Assert.That(air, Is.Not.EqualTo(surface));
        Assert.That(surface, Is.Not.EqualTo(subsurface));
        Assert.That(air, Is.Not.EqualTo(subsurface));
        // Affiliation identity char (position 1) is identical across domains for the same affiliation.
        Assert.That(air[1], Is.EqualTo(surface[1]));
        Assert.That(surface[1], Is.EqualTo(subsurface[1]));
    }

    [Test]
    public void BuildSidc_returns_input_unchanged_for_invalid_sidc()
    {
        Assert.That(App6DomainModifier.BuildSidc("SHORT", CombatDomain.Air), Is.EqualTo("SHORT"));
        Assert.That(App6DomainModifier.BuildSidc(null, CombatDomain.Air), Is.EqualTo(App6Sidc.FallbackSidc));
    }

    [Test]
    public void Resolve_unknown_domain_value_degrades_to_land_without_throwing()
    {
        var outOfRange = (CombatDomain)999;

        var resolution = App6DomainModifier.Resolve(outOfRange);

        Assert.That(resolution, Is.EqualTo(App6DomainModifier.Resolve(CombatDomain.Land)));
    }
}
