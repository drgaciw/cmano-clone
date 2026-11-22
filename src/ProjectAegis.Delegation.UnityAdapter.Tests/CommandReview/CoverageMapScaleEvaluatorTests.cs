namespace ProjectAegis.Delegation.UnityAdapter.Tests.CommandReview;

using NUnit.Framework;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

[TestFixture]
public sealed class CoverageMapScaleEvaluatorTests
{
    [Test]
    public void Evaluate_null_or_zero_scale_returns_unknown_without_suppressing_polygons()
    {
        var nullResult = CoverageMapScaleEvaluator.Evaluate(null);
        Assert.That(nullResult.Tier, Is.EqualTo(CoverageMapScaleTier.Unknown));
        Assert.That(nullResult.IsScaleExplicit, Is.False);
        Assert.That(nullResult.ShowPolygons, Is.True);
        Assert.That(nullResult.ShowSectorLabels, Is.True);
        Assert.That(nullResult.ShowSummaryBanner, Is.True);

        var zeroResult = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("0 NM", "0 m", 0, 0));
        Assert.That(zeroResult.Tier, Is.EqualTo(CoverageMapScaleTier.Unknown));
        Assert.That(zeroResult.IsScaleExplicit, Is.False);
        Assert.That(zeroResult.ShowPolygons, Is.True);
        Assert.That(zeroResult.ShowSectorLabels, Is.True);
    }

    [Test]
    public void Evaluate_tactical_operational_strategic_bands_apply_expected_visibility_and_rationale()
    {
        var tactical = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("15 NM", "ALT 30 km", 15, 30_000));
        Assert.That(tactical.Tier, Is.EqualTo(CoverageMapScaleTier.Tactical));
        Assert.That(tactical.IsScaleExplicit, Is.True);
        Assert.That(tactical.ShowPolygons, Is.True);
        Assert.That(tactical.ShowSectorLabels, Is.True);
        Assert.That(tactical.ShowSummaryBanner, Is.True);
        Assert.That(tactical.TierLabel, Is.EqualTo("TACTICAL"));
        Assert.That(tactical.Rationale, Is.Not.Empty);

        var operational = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("120 NM", "ALT 250 km", 120, 250_000));
        Assert.That(operational.Tier, Is.EqualTo(CoverageMapScaleTier.Operational));
        Assert.That(operational.IsScaleExplicit, Is.True);
        Assert.That(operational.ShowPolygons, Is.True);
        Assert.That(operational.ShowSectorLabels, Is.False);
        Assert.That(operational.ShowSummaryBanner, Is.True);
        Assert.That(operational.TierLabel, Is.EqualTo("OPERATIONAL"));

        var strategic = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("400 NM", "ALT 1200 km", 400, 1_200_000));
        Assert.That(strategic.Tier, Is.EqualTo(CoverageMapScaleTier.Strategic));
        Assert.That(strategic.IsScaleExplicit, Is.True);
        Assert.That(strategic.ShowPolygons, Is.False);
        Assert.That(strategic.ShowSectorLabels, Is.False);
        Assert.That(strategic.ShowSummaryBanner, Is.True);
        Assert.That(strategic.TierLabel, Is.EqualTo("STRATEGIC"));
    }

    [Test]
    public void Evaluate_uses_camera_altitude_fallback_and_suppresses_when_inspecting_history()
    {
        var operationalByAlt = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("0 NM", "ALT 200 km", 0, 200_000));
        Assert.That(operationalByAlt.Tier, Is.EqualTo(CoverageMapScaleTier.Operational));
        Assert.That(operationalByAlt.IsScaleExplicit, Is.True);
        Assert.That(operationalByAlt.ShowPolygons, Is.True);
        Assert.That(operationalByAlt.ShowSectorLabels, Is.False);

        var strategicByAlt = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("0 NM", "ALT 900 km", 0, 900_000));
        Assert.That(strategicByAlt.Tier, Is.EqualTo(CoverageMapScaleTier.Strategic));
        Assert.That(strategicByAlt.ShowPolygons, Is.False);

        var history = CoverageMapScaleEvaluator.Evaluate(
            new MapScaleState("15 NM", "ALT 30 km", 15, 30_000),
            isInspectingHistory: true);
        Assert.That(history.ShowPolygons, Is.False);
        Assert.That(history.ShowSectorLabels, Is.False);
        Assert.That(history.ShowSummaryBanner, Is.False);
        Assert.That(history.TierLabel, Is.EqualTo("HISTORY"));
    }

    [Test]
    public void Evaluate_handles_non_finite_values_safely()
    {
        var nanScale = CoverageMapScaleEvaluator.Evaluate(new MapScaleState("NaN", "Inf", double.NaN, double.PositiveInfinity));
        Assert.That(nanScale.Tier, Is.EqualTo(CoverageMapScaleTier.Unknown));
        Assert.That(nanScale.IsScaleExplicit, Is.False);
        Assert.That(nanScale.ShowPolygons, Is.True);
    }
}
