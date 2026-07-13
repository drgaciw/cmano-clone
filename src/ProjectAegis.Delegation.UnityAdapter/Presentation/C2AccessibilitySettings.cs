using System;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Player-facing text scale tiers for C2 log / OOB / staging-diff hosts
/// (design/accessibility-requirements.md §3 "Text Scaling"). Default is <see cref="Scale100"/>
/// until a settings UI ships (P1 stub).
/// </summary>
public enum C2TextScalePercent
{
    Scale100 = 100,
    Scale125 = 125,
    Scale150 = 150,
}

/// <summary>Surfaces with a documented baseline font size in accessibility-requirements.md §3.</summary>
public enum C2TextSurface
{
    /// <summary>Message log — 10px monospace baseline.</summary>
    MessageLog,

    /// <summary>OOB / catalog lists — 12px sans baseline.</summary>
    OobList,

    /// <summary>Platform Import staging diff — 10px monospace baseline (same curve as message log).</summary>
    StagingDiff,
}

/// <summary>
/// Resolves <see cref="C2TextScalePercent"/> → rendered font-size (px) for C2 presentation
/// hosts. Pure — no UnityEngine dependency, headlessly testable.
/// </summary>
/// <remarks>
/// accessibility-requirements.md §3: "Hard floor: Never render evidence-bearing text below
/// <b>10px</b> at 100% scale." The floor is applied to the pre-scale baseline so every tier
/// (100% / 125% / 150%) stays at or above the floor — scaling only ever multiplies upward from a
/// floored baseline, it never allows a sub-floor baseline to leak into a higher scale tier.
/// </remarks>
public static class C2AccessibilitySettings
{
    /// <summary>Hard floor (px) for evidence-bearing text at 100% scale (art-bible.md §4).</summary>
    public const float MinFontSizePx = 10f;

    /// <summary>Default scale until the settings UI ships (accessibility-requirements.md §3).</summary>
    public const C2TextScalePercent DefaultScalePercent = C2TextScalePercent.Scale100;

    /// <summary>Documented §3 baseline font size (px) at 100% scale for a given surface.</summary>
    public static float BaseFontSizePx(C2TextSurface surface) => surface switch
    {
        C2TextSurface.MessageLog => 10f,
        C2TextSurface.OobList => 12f,
        C2TextSurface.StagingDiff => 10f,
        _ => throw new ArgumentOutOfRangeException(nameof(surface), surface, null),
    };

    /// <summary>Resolves the rendered font size (px) for <paramref name="surface"/> at
    /// <paramref name="scalePercent"/>, applying the 10px floor.</summary>
    public static float ResolveFontSizePx(C2TextSurface surface, C2TextScalePercent scalePercent) =>
        ResolveFontSizePx(BaseFontSizePx(surface), scalePercent);

    /// <summary>Resolves the rendered font size (px) for an arbitrary baseline at
    /// <paramref name="scalePercent"/>, applying the 10px floor to the baseline before scaling.</summary>
    public static float ResolveFontSizePx(float baseFontSizePx, C2TextScalePercent scalePercent)
    {
        var flooredBase = Math.Max(baseFontSizePx, MinFontSizePx);
        var scaled = flooredBase * ((int)scalePercent / 100f);

        // Redundant-but-cheap safety net: guarantees the floor holds even if a future scale
        // tier below 100% is ever added to the enum.
        return Math.Max(scaled, MinFontSizePx);
    }

    /// <summary>USS class applied to a panel root to select the given scale tier's custom
    /// property values (see unity/ProjectAegis/Assets/UI/AegisTokens.uss). Returns
    /// <see langword="null"/> for <see cref="C2TextScalePercent.Scale100"/> — the base
    /// (un-scaled) tier is expressed by the :root token defaults, no class needed.</summary>
    public static string? ScaleUssClass(C2TextScalePercent scalePercent) => scalePercent switch
    {
        C2TextScalePercent.Scale100 => null,
        C2TextScalePercent.Scale125 => "aegis-scale-125",
        C2TextScalePercent.Scale150 => "aegis-scale-150",
        _ => throw new ArgumentOutOfRangeException(nameof(scalePercent), scalePercent, null),
    };
}
