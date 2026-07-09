using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

/// <summary>Req 20 P0 T3: "Why can't I fire?" ROE/WRA/EMCON explain.</summary>
[TestFixture]
public sealed class FireDenyExplainProjectionTests
{
    [Test]
    public void HoldFire_surfaces_ROE_HOLD_FIRE()
    {
        var preview = ReadyPreview();
        var explain = FireDenyExplainProjection.Project(
            preview, new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 4), requestedSalvo: 1);

        Assert.That(explain.CanFire, Is.False);
        Assert.That(explain.PrimaryAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE));
        Assert.That(explain.SummaryLine, Does.Contain("ROE_HOLD_FIRE"));
        Assert.That(explain.PositiveControlRequired, Is.False);
    }

    [Test]
    public void Wra_salvo_exceeded_surfaces_WRA_SALVO()
    {
        var preview = ReadyPreview();
        var explain = FireDenyExplainProjection.Project(
            preview, new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2), requestedSalvo: 4);

        Assert.That(explain.CanFire, Is.False);
        Assert.That(explain.PrimaryAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
        Assert.That(explain.SummaryLine, Does.Contain("WRA_SALVO"));
        Assert.That(explain.SummaryLine, Does.Contain("max salvo 2"));
    }

    [Test]
    public void WeaponsTight_ready_preview_requires_positive_control_but_can_fire()
    {
        var preview = ReadyPreview();
        var explain = FireDenyExplainProjection.Project(
            preview, new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 8), requestedSalvo: 1);

        Assert.That(explain.CanFire, Is.True);
        Assert.That(explain.PositiveControlRequired, Is.True);
        Assert.That(explain.SummaryLine, Does.Contain("positive control"));
        Assert.That(explain.PrimaryAbortCode, Is.Null);
    }

    [Test]
    public void Emcon_off_from_engage_preview_surfaces_in_reasons()
    {
        var preview = new EngagePreview("DLZ: In (Normal)", CanFire: false, AbortPreviewCode: AbortReasonCatalog.Doctrine.EMCON_OFF);
        var explain = FireDenyExplainProjection.Project(
            preview, new EffectivePolicy(RoeLevel.WeaponsFree), requestedSalvo: 1);

        Assert.That(explain.CanFire, Is.False);
        Assert.That(explain.PrimaryAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.EMCON_OFF));
        Assert.That(explain.ReasonCodes, Does.Contain(AbortReasonCatalog.Doctrine.EMCON_OFF));
    }

    [Test]
    public void FromFireAbortReason_maps_WraSalvo()
    {
        var explain = FireDenyExplainProjection.FromFireAbortReason(FireAbortReason.WraSalvo);
        Assert.That(explain.CanFire, Is.False);
        Assert.That(explain.PrimaryAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
    }

    [Test]
    public void EngagePreview_with_doctrine_HoldFire_sets_abort_code()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(defaults.DefaultMagazineRounds);
        var preview = EngagePreviewProjection.Project(
            in ctx, defaults.DlzPersonality, new EffectivePolicy(RoeLevel.HoldFire));

        Assert.That(preview.CanFire, Is.False);
        Assert.That(preview.AbortPreviewCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE));
    }

    [Test]
    public void EngagePreview_with_WRA_salvo_block_sets_abort_code()
    {
        var defaults = ScenarioEngageDefaults.MvpFallback;
        // SalvoSize defaults to 1; force WRA block by MaxSalvo 0-equivalent via MaxSalvo < salvo
        var ctx = defaults.ToEngageContext(defaults.DefaultMagazineRounds) with { SalvoSize = 4 };
        var preview = EngagePreviewProjection.Project(
            in ctx, defaults.DlzPersonality, new EffectivePolicy(RoeLevel.WeaponsFree, MaxSalvo: 2));

        Assert.That(preview.CanFire, Is.False);
        Assert.That(preview.AbortPreviewCode, Is.EqualTo(AbortReasonCatalog.Doctrine.WRA_SALVO));
    }

    private static EngagePreview ReadyPreview() =>
        new("DLZ: In (Normal)", CanFire: true, AbortPreviewCode: null);
}
