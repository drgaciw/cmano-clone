using ProjectAegis.Delegation.Projection;

namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

/// <summary>Map-scale tiers governing mission-package coverage visibility (DRG-192).</summary>
public enum CoverageMapScaleTier
{
    /// <summary>No calibrated MapScaleState bound; authored sectors remain visible without suppression.</summary>
    Unknown = 0,

    /// <summary>Zoomed-in tactical frame (&lt;= 50 NM): full sector polygons, labels, and posture summary.</summary>
    Tactical = 1,

    /// <summary>Regional operational frame (&gt; 50 NM and &lt;= 250 NM): sector polygons and posture summary; per-polygon text hidden.</summary>
    Operational = 2,

    /// <summary>Theater/strategic frame (&gt; 250 NM): micro-polygons suppressed in favor of aggregated coverage &amp; gap posture.</summary>
    Strategic = 3,
}

/// <summary>Pure scale-appropriateness decision for coverage polygons, sector labels, and posture banners.</summary>
public sealed record CoverageMapScaleDecision(
    CoverageMapScaleTier Tier,
    bool IsScaleExplicit,
    bool ShowPolygons,
    bool ShowSectorLabels,
    bool ShowSummaryBanner,
    double ScaleNauticalMiles,
    double CameraAltitudeMeters,
    string TierLabel,
    string SummaryLabel,
    string Rationale);

/// <summary>
/// Evaluates whether mission-package coverage geometry and labels are appropriate at the current
/// <see cref="MapScaleState"/>. Pure headless calculation; never mutates simulation or camera state.
/// </summary>
public static class CoverageMapScaleEvaluator
{
    /// <summary>Maximum nautical-mile scale bar for full tactical polygon + label display.</summary>
    public const double TacticalMaxScaleNm = 50.0;

    /// <summary>Maximum nautical-mile scale bar for operational contour display before strategic rollup.</summary>
    public const double OperationalMaxScaleNm = 250.0;

    /// <summary>Fallback camera altitude ceiling (meters) for tactical tier when NM scale is zero.</summary>
    public const double TacticalMaxAltitudeMeters = 150_000.0;

    /// <summary>Fallback camera altitude ceiling (meters) for operational tier when NM scale is zero.</summary>
    public const double OperationalMaxAltitudeMeters = 750_000.0;

    /// <summary>Evaluates coverage visibility and clutter tiers for the supplied <paramref name="scale"/>.</summary>
    public static CoverageMapScaleDecision Evaluate(
        MapScaleState? scale,
        bool isInspectingHistory = false)
    {
        var scaleNm = scale is not null && double.IsFinite(scale.ScaleNauticalMiles) && scale.ScaleNauticalMiles > 0
            ? scale.ScaleNauticalMiles
            : 0.0;
        var altMeters = scale is not null && double.IsFinite(scale.CameraAltitudeMeters) && scale.CameraAltitudeMeters > 0
            ? scale.CameraAltitudeMeters
            : 0.0;

        if (isInspectingHistory)
        {
            return new CoverageMapScaleDecision(
                Tier: ResolveTier(scaleNm, altMeters),
                IsScaleExplicit: scaleNm > 0 || altMeters > 0,
                ShowPolygons: false,
                ShowSectorLabels: false,
                ShowSummaryBanner: false,
                ScaleNauticalMiles: scaleNm,
                CameraAltitudeMeters: altMeters,
                TierLabel: "HISTORY",
                SummaryLabel: "Coverage hidden during history inspection",
                Rationale: "Live coverage and posture overlays are suppressed while inspecting historical timeline frames.");
        }

        var tier = ResolveTier(scaleNm, altMeters);
        return tier switch
        {
            CoverageMapScaleTier.Tactical => new CoverageMapScaleDecision(
                tier,
                IsScaleExplicit: true,
                ShowPolygons: true,
                ShowSectorLabels: true,
                ShowSummaryBanner: true,
                scaleNm,
                altMeters,
                TierLabel: "TACTICAL",
                SummaryLabel: $"TACTICAL ({FormatScaleToken(scale, scaleNm, altMeters)}) — full sector polygons & labels",
                Rationale: "Tactical scale permits full unit-sector boundaries, dashed gap contours, and sector labels."),

            CoverageMapScaleTier.Operational => new CoverageMapScaleDecision(
                tier,
                IsScaleExplicit: true,
                ShowPolygons: true,
                ShowSectorLabels: false,
                ShowSummaryBanner: true,
                scaleNm,
                altMeters,
                TierLabel: "OPERATIONAL",
                SummaryLabel: $"OPERATIONAL ({FormatScaleToken(scale, scaleNm, altMeters)}) — sector contours & gap rollup",
                Rationale: "Operational scale retains defense contours while suppressing per-polygon labels to prevent clutter."),

            CoverageMapScaleTier.Strategic => new CoverageMapScaleDecision(
                tier,
                IsScaleExplicit: true,
                ShowPolygons: false,
                ShowSectorLabels: false,
                ShowSummaryBanner: true,
                scaleNm,
                altMeters,
                TierLabel: "STRATEGIC",
                SummaryLabel: $"STRATEGIC ({FormatScaleToken(scale, scaleNm, altMeters)}) — theater gap summary only",
                Rationale: "Strategic theater scale hides unit-level polygons and surfaces aggregated coverage and gap posture."),

            _ => new CoverageMapScaleDecision(
                CoverageMapScaleTier.Unknown,
                IsScaleExplicit: false,
                ShowPolygons: true,
                ShowSectorLabels: true,
                ShowSummaryBanner: true,
                scaleNm,
                altMeters,
                TierLabel: "UNCALIBRATED",
                SummaryLabel: "SCALE UNCALIBRATED — showing authored sectors",
                Rationale: "No live MapScaleState bound; displaying authored coverage polygons without scale suppression."),
        };
    }

    private static CoverageMapScaleTier ResolveTier(double scaleNm, double altMeters)
    {
        if (scaleNm > 0)
        {
            if (scaleNm <= TacticalMaxScaleNm)
            {
                return CoverageMapScaleTier.Tactical;
            }

            if (scaleNm <= OperationalMaxScaleNm)
            {
                return CoverageMapScaleTier.Operational;
            }

            return CoverageMapScaleTier.Strategic;
        }

        if (altMeters > 0)
        {
            if (altMeters <= TacticalMaxAltitudeMeters)
            {
                return CoverageMapScaleTier.Tactical;
            }

            if (altMeters <= OperationalMaxAltitudeMeters)
            {
                return CoverageMapScaleTier.Operational;
            }

            return CoverageMapScaleTier.Strategic;
        }

        return CoverageMapScaleTier.Unknown;
    }

    private static string FormatScaleToken(MapScaleState? scale, double scaleNm, double altMeters)
    {
        if (scale is not null && !string.IsNullOrWhiteSpace(scale.ScaleBarLabel) && scaleNm > 0)
        {
            return scale.ScaleBarLabel;
        }

        if (scaleNm > 0)
        {
            return $"{scaleNm:0.#} NM";
        }

        return $"{altMeters / 1000.0:0.#} km";
    }
}
