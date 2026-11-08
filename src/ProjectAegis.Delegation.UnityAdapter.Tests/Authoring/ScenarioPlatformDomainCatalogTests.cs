using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

[TestFixture]
public sealed class ScenarioPlatformDomainCatalogTests
{
    [Test]
    public void Domains_include_aircraft_ships_subs_ground()
    {
        Assert.That(ScenarioPlatformDomainCatalog.Domains, Is.EqualTo(new[]
        {
            ScenarioPlatformDomainCatalog.DomainAircraft,
            ScenarioPlatformDomainCatalog.DomainShips,
            ScenarioPlatformDomainCatalog.DomainSubs,
            ScenarioPlatformDomainCatalog.DomainGround,
        }));
    }

    [Test]
    public void Each_domain_exposes_at_least_one_platform_preset()
    {
        foreach (var domain in ScenarioPlatformDomainCatalog.Domains)
        {
            var presets = ScenarioPlatformDomainCatalog.GetPresets(domain);
            Assert.That(presets, Is.Not.Empty, domain);
            Assert.That(presets[0].PlatformId, Is.Not.Empty);
            Assert.That(presets[0].DisplayName, Is.Not.Empty);
            Assert.That(presets[0].DomainId, Is.EqualTo(domain));
            Assert.That(presets[0].ListLine, Does.Contain(presets[0].PlatformId));
        }
    }

    [Test]
    public void Domain_aliases_resolve_to_catalog()
    {
        Assert.That(ScenarioPlatformDomainCatalog.GetPresets("air"), Is.Not.Empty);
        Assert.That(ScenarioPlatformDomainCatalog.GetPresets("surface"), Is.Not.Empty);
        Assert.That(ScenarioPlatformDomainCatalog.GetPresets("submarine"), Is.Not.Empty);
        Assert.That(ScenarioPlatformDomainCatalog.GetPresets("land"), Is.Not.Empty);
        Assert.That(ScenarioPlatformDomainCatalog.GetPresets("unknown-domain"), Is.Empty);
    }

    [Test]
    public void FindByPlatformId_and_SuggestUnitId_work()
    {
        var ships = ScenarioPlatformDomainCatalog.GetPresets(ScenarioPlatformDomainCatalog.DomainShips);
        var first = ships[0];
        var found = ScenarioPlatformDomainCatalog.FindByPlatformId(first.PlatformId);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.PlatformId, Is.EqualTo(first.PlatformId));

        var id = ScenarioPlatformDomainCatalog.SuggestUnitId(first.PlatformId, 2);
        Assert.That(id, Does.StartWith("u-"));
        Assert.That(id, Does.EndWith("-2"));
    }
}
