using ProjectAegis.Delegation.Projection;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class CombatDomainsHotTickPanelBinderTests
{
    [Test]
    public void BindIdle_emits_default_domains_as_idle()
    {
        var state = CombatDomainsHotTickPanelBinder.BindIdle();

        Assert.That(state.Rows.Count, Is.EqualTo(CombatDomainsHotTickPanelBinder.DefaultDomainKeys.Length));
        Assert.That(state.Rows.All(r => r.StateLabel == "IDLE"), Is.True);
        Assert.That(state.Rows.Select(r => r.DomainKey), Is.EquivalentTo(CombatDomainsHotTickPanelBinder.DefaultDomainKeys));
    }

    [Test]
    public void BindFromDomainActivity_marks_engaged_and_degraded()
    {
        var map = new Dictionary<string, CombatDomainHudEngagement>
        {
            ["Air"] = CombatDomainHudEngagement.Engaged,
            ["Surface"] = CombatDomainHudEngagement.Degraded,
        };

        var state = CombatDomainsHotTickPanelBinder.BindFromDomainActivity(map);

        var air = state.Rows.Single(r => r.DomainKey == "Air");
        var surface = state.Rows.Single(r => r.DomainKey == "Surface");
        var sub = state.Rows.Single(r => r.DomainKey == "Subsurface");

        Assert.That(air.StateLabel, Is.EqualTo("ENGAGED"));
        Assert.That(air.StateCssClass, Does.Contain("engaged"));
        Assert.That(surface.StateLabel, Is.EqualTo("DEGRADED"));
        Assert.That(surface.StateCssClass, Does.Contain("degraded"));
        Assert.That(sub.StateLabel, Is.EqualTo("IDLE"));
    }

    [Test]
    public void BindFromActiveDomainTags_promotes_tags_to_engaged()
    {
        var state = CombatDomainsHotTickPanelBinder.BindFromActiveDomainTags(new[] { "Surface", "surface", "Mine" });

        Assert.That(state.Rows.Single(r => r.DomainKey == "Surface").StateLabel, Is.EqualTo("ENGAGED"));
        Assert.That(state.Rows.Single(r => r.DomainKey == "Mine").StateLabel, Is.EqualTo("ENGAGED"));
        Assert.That(state.Rows.Single(r => r.DomainKey == "Air").StateLabel, Is.EqualTo("IDLE"));
    }
}
