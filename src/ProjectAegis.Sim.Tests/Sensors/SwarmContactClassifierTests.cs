using ProjectAegis.Sim.Sensors;
using NUnit.Framework;

namespace ProjectAegis.Sim.Tests.Sensors;

/// <summary>
/// SWARM-26 / DRG-96 (SWARM-B5): pure contact classification for swarms.
/// </summary>
public sealed class SwarmContactClassifierTests
{
    [Test]
    public void Low_quality_yields_Unknown()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.10);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.Unknown));
        Assert.That(r.Confidence, Is.InRange(0, 1));
        Assert.That(r.Confidence < 0.25, Is.True);
        Assert.That(r.ReasonCode, Is.EqualTo("low_quality_unknown"));
    }

    [Test]
    public void Low_quality_even_with_swarm_truth_stays_Unknown_not_UasSwarmCloud()
    {
        // Misclassification path: truth is swarm but quality too low to identify as cloud.
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.15,
            estimatedCountHint: 12);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.Unknown));
        Assert.That(r.Class, Is.Not.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(r.Confidence, Is.InRange(0, 1));
    }

    [Test]
    public void High_quality_plus_isSwarm_yields_UasSwarmCloud()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.85);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(r.Confidence >= 0.55, Is.True);
        Assert.That(r.Confidence, Is.InRange(0, 1));
        Assert.That(r.ReasonCode, Is.EqualTo("high_quality_truth_swarm_cloud"));
    }

    [Test]
    public void High_quality_single_yields_SingleAirframe()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.90,
            estimatedCountHint: 1);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.SingleAirframe));
        Assert.That(r.Confidence >= 0.55, Is.True);
        Assert.That(r.Confidence, Is.InRange(0, 1));
        Assert.That(r.ReasonCode, Is.EqualTo("high_quality_single_airframe"));
    }

    [Test]
    public void Mid_quality_plus_count_hint_yields_PossibleSwarm()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.40,
            estimatedCountHint: 6);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.PossibleSwarm));
        Assert.That(r.Confidence, Is.InRange(0.25, 0.75));
        Assert.That(r.ReasonCode, Is.EqualTo("mid_quality_count_hint_possible_swarm"));
    }

    [Test]
    public void Mid_quality_swarm_truth_is_PossibleSwarm_not_full_cloud()
    {
        // Misclassification / under-ID path: mid quality cannot fully resolve UasSwarmCloud.
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: true,
            sensorQuality: 0.35);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.PossibleSwarm));
        Assert.That(r.Class, Is.Not.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(r.ReasonCode, Is.EqualTo("mid_quality_truth_swarm_ambiguous"));
    }

    [Test]
    public void High_quality_count_band_3_to_7_yields_PossibleSwarm()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.70,
            estimatedCountHint: 4);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.PossibleSwarm));
        Assert.That(r.ReasonCode, Is.EqualTo("high_quality_count_band_possible_swarm"));
    }

    [Test]
    public void High_quality_count_hint_ge_8_yields_UasSwarmCloud()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.75,
            estimatedCountHint: 10);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(r.ReasonCode, Is.EqualTo("high_quality_count_hint_swarm_cloud"));
    }

    [Test]
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

        Assert.That(normal.Class, Is.EqualTo(SwarmContactClass.PossibleSwarm));
        Assert.That(hiRes.Class, Is.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(hiRes.Confidence >= normal.Confidence, Is.True);
    }

    [Test]
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

        Assert.That(normal.Class, Is.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(hiRes.Class, Is.EqualTo(SwarmContactClass.UasSwarmCloud));
        Assert.That(hiRes.Confidence > normal.Confidence, Is.True);
        Assert.That(
            Math.Round(hiRes.Confidence - normal.Confidence, 6, MidpointRounding.AwayFromZero),
            Is.EqualTo(Math.Round(SwarmContactClassifier.HighResolutionConfidenceBoost, 6, MidpointRounding.AwayFromZero)));
    }

    [Test]
    public void Classify_is_deterministic_for_same_inputs()
    {
        const bool isSwarm = true;
        const double q = 0.62;
        int? hint = 9;
        const bool hiRes = false;

        var a = SwarmContactClassifier.Classify(isSwarm, q, hint, hiRes);
        var b = SwarmContactClassifier.Classify(isSwarm, q, hint, hiRes);

        Assert.That(b, Is.EqualTo(a));
        Assert.That(b.Class, Is.EqualTo(a.Class));
        Assert.That(b.Confidence, Is.EqualTo(a.Confidence));
        Assert.That(b.ReasonCode, Is.EqualTo(a.ReasonCode));
    }

    [TestCase(-1.0)]
    [TestCase(0.0)]
    [TestCase(0.5)]
    [TestCase(1.0)]
    [TestCase(2.5)]
    public void Confidence_is_clamped_0_to_1(double quality)
    {
        var r1 = SwarmContactClassifier.Classify(false, quality);
        var r2 = SwarmContactClassifier.Classify(true, quality, estimatedCountHint: 20, highResolutionMode: true);

        Assert.That(r1.Confidence, Is.InRange(0, 1));
        Assert.That(r2.Confidence, Is.InRange(0, 1));
        Assert.That(r1.Confidence, Is.EqualTo(SwarmContactClassifier.ClampConfidence(r1.Confidence)));
    }

    [Test]
    public void Mid_quality_without_swarm_hint_is_weak_SingleAirframe()
    {
        var r = SwarmContactClassifier.Classify(
            targetIsSwarmPlatform: false,
            sensorQuality: 0.40,
            estimatedCountHint: 2);

        Assert.That(r.Class, Is.EqualTo(SwarmContactClass.SingleAirframe));
        Assert.That(r.ReasonCode, Is.EqualTo("mid_quality_single_airframe_weak"));
        Assert.That(r.Confidence < 0.55, Is.True);
    }

    [Test]
    public void SwarmContactLabel_formats_class_and_confidence()
    {
        var cloud = new SwarmContactClassificationResult(
            SwarmContactClass.UasSwarmCloud,
            0.82,
            "high_quality_truth_swarm_cloud");
        Assert.That(SwarmContactLabel.Format(cloud), Is.EqualTo("UAS swarm cloud (0.82)"));

        var possible = new SwarmContactClassificationResult(
            SwarmContactClass.PossibleSwarm,
            0.41,
            "mid_quality_count_hint_possible_swarm");
        Assert.That(SwarmContactLabel.Format(possible), Is.EqualTo("Possible swarm (0.41)"));

        var single = new SwarmContactClassificationResult(
            SwarmContactClass.SingleAirframe,
            0.70,
            "high_quality_single_airframe");
        Assert.That(SwarmContactLabel.Format(single), Is.EqualTo("Single airframe (0.70)"));

        var unknown = new SwarmContactClassificationResult(
            SwarmContactClass.Unknown,
            0.12,
            "low_quality_unknown");
        Assert.That(SwarmContactLabel.Format(unknown), Is.EqualTo("Unknown (0.12)"));
    }
}
