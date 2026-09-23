using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class WraSalvoRemainingBinderTests
{
    [Test]
    public void Nominal_salvo_within_wra_cap_shows_remaining_over_max()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(roundsRemaining: 8) with { SalvoSize = 2 };
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8);

        var bound = WraSalvoRemainingBinder.Bind("u1", in ctx, policy);

        Assert.That(bound.StateLine, Is.EqualTo("WRA SALVO: 2/8"));
        Assert.That(bound.RemainingSalvo, Is.EqualTo(2));
        Assert.That(bound.MaxSalvo, Is.EqualTo(8));
        Assert.That(bound.IsExhausted, Is.False);
        Assert.That(bound.AbortReasonCode, Is.Null);
        Assert.That(bound.CueClass, Is.EqualTo(WraSalvoCueClasses.Nominal));
        Assert.That(bound.DeclutterToken, Is.EqualTo(WraSalvoDeclutterTokens.Ready));
        Assert.That(bound.IsFireOrder, Is.False);
    }

    [Test]
    public void Salvo_exceeding_wra_cap_is_exhausted_with_wra_salvo_abort()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(roundsRemaining: 8) with { SalvoSize = 4 };
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2);

        var bound = WraSalvoRemainingBinder.Bind("u1", in ctx, policy);

        Assert.That(bound.RemainingSalvo, Is.EqualTo(0));
        Assert.That(bound.MaxSalvo, Is.EqualTo(2));
        Assert.That(bound.IsExhausted, Is.True);
        Assert.That(bound.AbortReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(bound.StateLine, Does.Contain(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(bound.CueClass, Is.EqualTo(WraSalvoCueClasses.Exhausted));
        Assert.That(bound.IsFireOrder, Is.False);
    }

    [Test]
    public void Winchester_magazine_withhold_is_exhausted_with_ordnance_abort()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(roundsRemaining: 0) with { SalvoSize = 2 };
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8);

        var bound = WraSalvoRemainingBinder.Bind("u1", in ctx, policy);

        Assert.That(bound.RemainingSalvo, Is.EqualTo(0));
        Assert.That(bound.IsExhausted, Is.True);
        Assert.That(bound.AbortReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        Assert.That(bound.CueClass, Is.EqualTo(WraSalvoCueClasses.Exhausted));
    }

    [Test]
    public void Below_salvo_rounds_use_employment_no_ammo_abort()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(roundsRemaining: 1) with { SalvoSize = 2 };
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 8);

        var bound = WraSalvoRemainingBinder.Bind("u1", in ctx, policy);

        Assert.That(bound.RemainingSalvo, Is.EqualTo(0));
        Assert.That(bound.AbortReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(bound.IsFireOrder, Is.False);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Missing_shooter_selection_fails_closed(string? shooterId)
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(4);
        var policy = EffectivePolicy.DefaultFree;

        Assert.That(
            WraSalvoRemainingBinder.Bind(shooterId, in ctx, policy),
            Is.EqualTo(WraSalvoRemainingState.Empty));
    }

    [Test]
    public void Zero_max_salvo_is_exhausted_with_abort()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(4) with { SalvoSize = 1 };
        var policy = new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 0);

        var bound = WraSalvoRemainingBinder.Bind("u1", in ctx, policy);

        Assert.That(bound.RemainingSalvo, Is.EqualTo(0));
        Assert.That(bound.AbortReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
    }

    [Test]
    public void Panel_binder_maps_wra_salvo_line_element()
    {
        var state = new WraSalvoRemainingState(
            "WRA SALVO: 1/2",
            1,
            2,
            null,
            false,
            WraSalvoDeclutterTokens.Ready,
            WraSalvoCueClasses.Nominal,
            IsFireOrder: false);

        var rows = WraSalvoRemainingPanelBinder.BindRows(state);

        Assert.That(rows, Has.Count.EqualTo(1));
        Assert.That(rows[0].ElementName, Is.EqualTo("wra-salvo-line"));
        Assert.That(rows[0].Text, Is.EqualTo("WRA SALVO: 1/2"));
        Assert.That(rows[0].CueClass, Is.EqualTo(WraSalvoCueClasses.Nominal));
    }
}
