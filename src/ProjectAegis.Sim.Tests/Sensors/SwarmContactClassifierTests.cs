using ProjectAegis.Sim.Sensors;
using Xunit;

namespace ProjectAegis.Sim.Tests.Sensors;

/// <summary>
/// SWARM-26 / DRG-96 (SWARM-B5): pure contact classification for swarms.
/// </summary>
public sealed class SwarmContactClassifierTests
{
    [Fact]
    public void Low_quality_yields_Unknown()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.10);

        Assert.Equal(SwarmContactClass.Unknown, r.Class);
        Assert.InRange(r.Confidence, 0, 1);
        Assert.True(r.Confidence < 0.25);
        Assert.Equal("low_quality_unknown", r.ReasonCode);
    }

    [Fact]
    public void Low_quality_even_with_swarm_truth_stays_Unknown_not_UasSwarmCloud()
    {
        // Misclassification path: truth is swarm but quality too low to identify as cloud.
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.15,
            estimatedCountHint: 12);

        Assert.Equal(SwarmContactClass.Unknown, r.Class);
        Assert.NotEqual(SwarmContactClass.UasSwarmCloud, r.Class);
        Assert.InRange(r.Confidence, 0, 1);
    }

    [Fact]
    public void High_quality_plus_isSwarm_yields_UasSwarmCloud()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.85);

        Assert.Equal(SwarmContactClass.UasSwarmCloud, r.Class);
        Assert.True(r.Confidence >= 0.55);
        Assert.InRange(r.Confidence, 0, 1);
        Assert.Equal("high_quality_truth_swarm_cloud", r.ReasonCode);
    }

    [Fact]
    public void High_quality_single_yields_SingleAirframe()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.90,
            estimatedCountHint: 1);

        Assert.Equal(SwarmContactClass.SingleAirframe, r.Class);
        Assert.True(r.Confidence >= 0.55);
        Assert.InRange(r.Confidence, 0, 1);
        Assert.Equal("high_quality_single_airframe", r.ReasonCode);
    }

    [Fact]
    public void Mid_quality_plus_count_hint_yields_PossibleSwarm()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.40,
            estimatedCountHint: 6);

        Assert.Equal(SwarmContactClass.PossibleSwarm, r.Class);
        Assert.InRange(r.Confidence, 0.25, 0.75);
        Assert.Equal("mid_quality_count_hint_possible_swarm", r.ReasonCode);
    }

    [Fact]
    public void Mid_quality_swarm_truth_is_PossibleSwarm_not_full_cloud()
    {
        // Misclassification / under-ID path: mid quality cannot fully resolve UasSwarmCloud.
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.35);

        Assert.Equal(SwarmContactClass.PossibleSwarm, r.Class);
        Assert.NotEqual(SwarmContactClass.UasSwarmCloud, r.Class);
        Assert.Equal("mid_quality_truth_swarm_ambiguous", r.ReasonCode);
    }

    [Fact]
    public void High_quality_count_band_3_to_7_yields_PossibleSwarm()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.70,
            estimatedCountHint: 4);

        Assert.Equal(SwarmContactClass.PossibleSwarm, r.Class);
        Assert.Equal("high_quality_count_band_possible_swarm", r.ReasonCode);
    }

    [Fact]
    public void High_quality_count_hint_ge_8_yields_UasSwarmCloud()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.75,
            estimatedCountHint: 10);

        Assert.Equal(SwarmContactClass.UasSwarmCloud, r.Class);
        Assert.Equal("high_quality_count_hint_swarm_cloud", r.ReasonCode);
    }

    [Fact]
    public void High_resolution_lowers_swarm_cloud_count_threshold()
    {
        var normal = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.80,
            estimatedCountHint: 6,
            highResolutionMode: false);
        var hiRes = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.80,
            estimatedCountHint: 6,
            highResolutionMode: true);

        Assert.Equal(SwarmContactClass.PossibleSwarm, normal.Class);
        Assert.Equal(SwarmContactClass.UasSwarmCloud, hiRes.Class);
        Assert.True(hiRes.Confidence >= normal.Confidence);
    }

    [Fact]
    public void High_resolution_boosts_confidence_slightly()
    {
        var normal = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.80,
            highResolutionMode: false);
        var hiRes = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.80,
            highResolutionMode: true);

        Assert.Equal(SwarmContactClass.UasSwarmCloud, normal.Class);
        Assert.Equal(SwarmContactClass.UasSwarmCloud, hiRes.Class);
        Assert.True(hiRes.Confidence > normal.Confidence);
        Assert.Equal(
            SwarmContactClassifier.HighResolutionConfidenceBoost,
            hiRes.Confidence - normal.Confidence,
            6);
    }

    [Fact]
    public void Classify_is_deterministic_for_same_inputs()
    {
        const bool isSwarm = true;
        const double q = 0.62;
        int? hint = 9;
        const bool hiRes = false;

        var a = SwarmContactClassifier.Classify(isSwarm, q, hint, hiRes);
        var b = SwarmContactClassifier.Classify(isSwarm, q, hint, hiRes);

        Assert.Equal(a, b);
        Assert.Equal(a.Class, b.Class);
        Assert.Equal(a.Confidence, b.Confidence);
        Assert.Equal(a.ReasonCode, b.ReasonCode);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    [InlineData(2.5)]
    public void Confidence_is_clamped_0_to_1(double quality)
    {
        var r1 = SwarmContactClassifier.Classify(false, quality);
        var r2 = SwarmContactClassifier.Classify(true, quality, estimatedCountHint: 20, highResolutionMode: true);

        Assert.InRange(r1.Confidence, 0, 1);
        Assert.InRange(r2.Confidence, 0, 1);
        Assert.Equal(SwarmContactClassifier.ClampConfidence(r1.Confidence), r1.Confidence);
    }

    [Fact]
    public void Mid_quality_without_swarm_hint_is_weak_SingleAirframe()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.40,
            estimatedCountHint: 2);

        Assert.Equal(SwarmContactClass.SingleAirframe, r.Class);
        Assert.Equal("mid_quality_single_airframe_weak", r.ReasonCode);
        Assert.True(r.Confidence < 0.55);
    }

    [Fact]
    public void SwarmContactLabel_formats_class_and_confidence()
    {
        var cloud = new SwarmContactClassificationResult(
            SwarmContactClass.UasSwarmCloud,
            0.82,
            "high_quality_truth_swarm_cloud");
        Assert.Equal("UAS swarm cloud (0.82)", SwarmContactLabel.Format(cloud));

        var possible = new SwarmContactClassificationResult(
            SwarmContactClass.PossibleSwarm,
            0.41,
            "mid_quality_count_hint_possible_swarm");
        Assert.Equal("Possible swarm (0.41)", SwarmContactLabel.Format(possible));

        var single = new SwarmContactClassificationResult(
            SwarmContactClass.SingleAirframe,
            0.70,
            "high_quality_single_airframe");
        Assert.Equal("Single airframe (0.70)", SwarmContactLabel.Format(single));

        var unknown = new SwarmContactClassificationResult(
            SwarmContactClass.Unknown,
            0.12,
            "low_quality_unknown");
        Assert.Equal("Unknown (0.12)", SwarmContactLabel.Format(unknown));
    }
}
