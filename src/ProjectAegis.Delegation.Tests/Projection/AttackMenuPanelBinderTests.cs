using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class AttackMenuPanelBinderTests
{
    [Test]
    public void FindOption_returns_matching_entry()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(4);
        var preview = EngagePreviewProjection.Project(in ctx, defaults.DlzPersonality);
        var menu = EngageAttackOptions.Build(in ctx, preview);

        var option = AttackMenuPanelBinder.FindOption(menu, "fire-single");

        Assert.That(option, Is.Not.Null);
        Assert.That(option!.Enabled, Is.True);
    }

    [Test]
    public void ResolveButtonName_maps_known_ids()
    {
        Assert.That(AttackMenuPanelBinder.ResolveButtonName("fire-salvo"), Is.EqualTo("attack-fire-salvo"));
    }
}