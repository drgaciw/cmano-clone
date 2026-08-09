namespace ProjectAegis.Sim.Sensors;

/// <summary>
/// SWARM-26 / DRG-96 (SWARM-B5): pure, deterministic classification of hostile contacts
/// as single airframe vs UAS swarm cloud when sensors allow, with confidence.
/// Misclassification is possible at low sensor quality (truth flag alone is not enough).
/// No Unity, no lifecycle side effects — call sites may consume later.
/// </summary>
public static class SwarmContactClassifier
{
    /// <summary>Below this quality the observer cannot form a useful class.</summary>
    public const double LowQualityCeiling = 0.25;

    /// <summary>Below this (and at/above <see cref="LowQualityCeiling"/>) only weak/ambiguous classes.</summary>
    public const double MidQualityCeiling = 0.5;

    /// <summary>Normal-mode multi-return count that forces PossibleSwarm in mid quality.</summary>
    public const int MidCountHintForPossibleSwarm = 5;

    /// <summary>Normal-mode multi-return count that forces UasSwarmCloud in high quality.</summary>
    public const int HighCountHintForSwarmCloud = 8;

    /// <summary>High-resolution mode lowers the multi-return bar for UasSwarmCloud.</summary>
    public const int HighResCountHintForSwarmCloud = 6;

    /// <summary>Inclusive low end of high-quality ambiguous count band (PossibleSwarm).</summary>
    public const int HighQualityAmbiguousCountMin = 3;

    /// <summary>Inclusive high end of high-quality ambiguous count band (PossibleSwarm).</summary>
    public const int HighQualityAmbiguousCountMax = 7;

    /// <summary>Additive confidence boost when <paramref name="highResolutionMode"/> is true.</summary>
    public const double HighResolutionConfidenceBoost = 0.08;

    /// <summary>
    /// Classify a contact from observer sensor quality and optional multi-return hints.
    /// <paramref name="targetIsSwarmPlatform"/> is ground truth (catalog/isSwarm); the observer
    /// only fully resolves it when quality is high enough — low quality may misclassify.
    /// </summary>
    public static SwarmContactClassificationResult Classify(
        bool targetIsSwarmPlatform,
        double sensorQuality,
        int? estimatedCountHint = null,
        bool highResolutionMode = false)
    {
        var q = Math.Clamp(sensorQuality, 0, 1);
        var count = estimatedCountHint;
        var hiRes = highResolutionMode;
        var cloudThreshold = hiRes ? HighResCountHintForSwarmCloud : HighCountHintForSwarmCloud;

        if (q < LowQualityCeiling)
        {
            // Low quality: cannot distinguish swarm cloud from single airframe.
            // Even if truth is swarm, class stays Unknown (misclassification path).
            var conf = ClampConfidence(0.10 + q * 0.4 + (hiRes ? HighResolutionConfidenceBoost * 0.5 : 0));
            return new SwarmContactClassificationResult(
                SwarmContactClass.Unknown,
                conf,
                "low_quality_unknown");
        }

        if (q < MidQualityCeiling)
        {
            var midCountBar = hiRes
                ? Math.Max(3, MidCountHintForPossibleSwarm - 1)
                : MidCountHintForPossibleSwarm;
            var swarmHint = targetIsSwarmPlatform || (count is int c && c >= midCountBar);
            if (swarmHint)
            {
                // Mid quality + swarm signal → PossibleSwarm only (not full UasSwarmCloud).
                var conf = ClampConfidence(0.30 + (q - LowQualityCeiling) * 0.6 + Boost(hiRes));
                return new SwarmContactClassificationResult(
                    SwarmContactClass.PossibleSwarm,
                    conf,
                    targetIsSwarmPlatform
                        ? "mid_quality_truth_swarm_ambiguous"
                        : "mid_quality_count_hint_possible_swarm");
            }

            var weakConf = ClampConfidence(0.25 + (q - LowQualityCeiling) * 0.5 + Boost(hiRes));
            return new SwarmContactClassificationResult(
                SwarmContactClass.SingleAirframe,
                weakConf,
                "mid_quality_single_airframe_weak");
        }

        // High quality (q >= 0.5)
        var isCloud =
            targetIsSwarmPlatform
            || (count is int n && n >= cloudThreshold);

        if (isCloud)
        {
            // Confidence scales with quality; hi-res boosts slightly.
            var conf = ClampConfidence(0.55 + (q - MidQualityCeiling) * 0.8 + Boost(hiRes));
            return new SwarmContactClassificationResult(
                SwarmContactClass.UasSwarmCloud,
                conf,
                targetIsSwarmPlatform
                    ? "high_quality_truth_swarm_cloud"
                    : "high_quality_count_hint_swarm_cloud");
        }

        if (count is int amb
            && amb >= HighQualityAmbiguousCountMin
            && amb <= HighQualityAmbiguousCountMax)
        {
            // 3..7 multi-returns without truth flag → ambiguous PossibleSwarm
            // (when hi-res lowers cloud threshold to 6, counts 6..7 already hit UasSwarmCloud above).
            var conf = ClampConfidence(0.45 + (q - MidQualityCeiling) * 0.5 + Boost(hiRes));
            return new SwarmContactClassificationResult(
                SwarmContactClass.PossibleSwarm,
                conf,
                "high_quality_count_band_possible_swarm");
        }

        var singleConf = ClampConfidence(0.55 + (q - MidQualityCeiling) * 0.7 + Boost(hiRes));
        return new SwarmContactClassificationResult(
            SwarmContactClass.SingleAirframe,
            singleConf,
            "high_quality_single_airframe");
    }

    private static double Boost(bool highResolutionMode) =>
        highResolutionMode ? HighResolutionConfidenceBoost : 0;

    /// <summary>Clamp confidence to the closed unit interval.</summary>
    public static double ClampConfidence(double confidence) => Math.Clamp(confidence, 0, 1);
}
