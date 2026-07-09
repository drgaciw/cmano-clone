using System;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>req 20 AC-1 (F5 scaling): ScalePercent {100,125,150} → font-size resolution,
/// including the 10px evidence-text floor (design/accessibility-requirements.md §3).</summary>
public sealed class C2AccessibilitySettingsTests
{
    [Test]
    public void ResolveFontSizePx_at_100_percent_returns_documented_baseline()
    {
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.MessageLog, C2TextScalePercent.Scale100),
            Is.EqualTo(10f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.OobList, C2TextScalePercent.Scale100),
            Is.EqualTo(12f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.StagingDiff, C2TextScalePercent.Scale100),
            Is.EqualTo(10f));
    }

    [Test]
    public void ResolveFontSizePx_scales_up_proportionally_at_125_and_150_percent()
    {
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.MessageLog, C2TextScalePercent.Scale125),
            Is.EqualTo(12.5f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.MessageLog, C2TextScalePercent.Scale150),
            Is.EqualTo(15f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.OobList, C2TextScalePercent.Scale125),
            Is.EqualTo(15f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(C2TextSurface.OobList, C2TextScalePercent.Scale150),
            Is.EqualTo(18f));
    }

    [Test]
    public void ResolveFontSizePx_10px_base_at_100_percent_stays_at_or_above_floor()
    {
        // Exact-floor baseline (10px) must survive unchanged at 100%.
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(10f, C2TextScalePercent.Scale100),
            Is.EqualTo(10f));
    }

    [Test]
    public void ResolveFontSizePx_never_renders_below_10px_floor_at_100_percent_even_for_sub_floor_base()
    {
        // A hypothetical sub-floor baseline (e.g. a future 8px surface) must be floored to 10px
        // at 100% scale — the hard floor from accessibility-requirements.md §3 / art-bible.md §4.
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(8f, C2TextScalePercent.Scale100),
            Is.EqualTo(10f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(1f, C2TextScalePercent.Scale100),
            Is.EqualTo(10f));
    }

    [Test]
    public void ResolveFontSizePx_flooring_applies_before_scaling_so_higher_tiers_never_drop_below_floor()
    {
        // A sub-floor 8px baseline is floored to 10px *before* the 125%/150% multiplier is
        // applied, so scaling only ever multiplies upward from a floored value.
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(8f, C2TextScalePercent.Scale125),
            Is.EqualTo(12.5f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(8f, C2TextScalePercent.Scale150),
            Is.EqualTo(15f));
    }

    [Test]
    public void DefaultScalePercent_is_100()
    {
        Assert.That(C2AccessibilitySettings.DefaultScalePercent, Is.EqualTo(C2TextScalePercent.Scale100));
    }

    [Test]
    public void ScaleUssClass_returns_null_for_100_percent_and_named_class_for_125_150()
    {
        Assert.That(C2AccessibilitySettings.ScaleUssClass(C2TextScalePercent.Scale100), Is.Null);
        Assert.That(C2AccessibilitySettings.ScaleUssClass(C2TextScalePercent.Scale125), Is.EqualTo("aegis-scale-125"));
        Assert.That(C2AccessibilitySettings.ScaleUssClass(C2TextScalePercent.Scale150), Is.EqualTo("aegis-scale-150"));
    }

    [Test]
    public void ResolveFontSizePx_10px_base_never_drops_below_floor_at_125_or_150_percent()
    {
        // An exact-floor (10px) baseline only ever scales upward — 125%/150% must never
        // round-trip back down through the floor. Coverage: implementation was already correct,
        // this scenario (as opposed to the sub-floor 8px case) was untested.
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(10f, C2TextScalePercent.Scale125),
            Is.EqualTo(12.5f));
        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(10f, C2TextScalePercent.Scale150),
            Is.EqualTo(15f));
    }

    [Test]
    public void BaseFontSizePx_throws_for_an_undefined_surface_enum_value()
    {
        // Coverage: the switch expression's default arm already throws for any C2TextSurface
        // value outside the three documented surfaces (e.g. a future surface added to the enum
        // without a matching §3 baseline entry) — was untested.
        var undefinedSurface = (C2TextSurface)999;

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            C2AccessibilitySettings.BaseFontSizePx(undefinedSurface)));
    }

    [Test]
    public void ScaleUssClass_throws_for_an_undefined_scale_percent_enum_value()
    {
        // Coverage: ScaleUssClass validates its enum argument via a switch default arm (unlike
        // the numeric ResolveFontSizePx overload, which tolerates an out-of-range scale via the
        // redundant floor safety net rather than throwing) — was untested.
        var undefinedScale = (C2TextScalePercent)999;

        Assert.Throws<ArgumentOutOfRangeException>((Action)(() =>
            C2AccessibilitySettings.ScaleUssClass(undefinedScale)));
    }

    [Test]
    public void ResolveFontSizePx_floor_safety_net_still_holds_for_an_undefined_scale_percent_enum_value()
    {
        // Coverage: unlike ScaleUssClass/BaseFontSizePx, the numeric ResolveFontSizePx overload
        // does not validate scalePercent via a switch — an out-of-range value (e.g. a
        // hypothetical 0% tier) is tolerated and falls through to the redundant
        // Math.Max(scaled, MinFontSizePx) safety net documented in the XML remarks. Confirms
        // that documented fallback actually behaves as claimed.
        var undefinedScale = (C2TextScalePercent)0;

        Assert.That(
            C2AccessibilitySettings.ResolveFontSizePx(10f, undefinedScale),
            Is.EqualTo(10f));
    }
}
