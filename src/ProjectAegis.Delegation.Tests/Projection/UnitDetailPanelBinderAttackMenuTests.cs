using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

[TestFixture]
public sealed class UnitDetailPanelBinderAttackMenuTests
{
    [Test]
    public void Bind_passes_attack_menu_options_to_state()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(4);
        var preview = EngagePreviewProjection.Project(in ctx, defaults.DlzPersonality);
        var menu = EngageAttackOptions.Build(in ctx, preview);
        var entry = new UnitDetailEntry(
            "u1",
            true,
            "OPERATIONAL",
            "MAGAZINE: —",
            "EMCON: —",
            "DOCTRINE: —",
            "FUEL: —",
            "ENGAGE: —",
            "ATTACK: —",
            menu);

        var state = UnitDetailPanelBinder.Bind(entry);

        Assert.That(state.AttackMenu, Has.Count.EqualTo(3));
        Assert.That(state.AttackMenu[0].Id, Is.EqualTo("fire-single"));
    }
}