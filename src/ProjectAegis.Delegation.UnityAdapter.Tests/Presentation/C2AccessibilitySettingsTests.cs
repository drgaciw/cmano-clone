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
}
