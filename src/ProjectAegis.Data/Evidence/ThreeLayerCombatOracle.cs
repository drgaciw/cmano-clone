using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ProjectAegis.Data.Evidence;

/// <summary>Honest provenance classification for three-layer combat oracle evidence (DRG-202 / H6).</summary>
public enum OracleEvidenceClass
{
    FixtureOnly = 1,
    HeadlessProjection = 2,
    ProxySmoke = 3,
    EditorPlayMode = 4,
    OwnerAcceptedWalk = 5,
}

/// <summary>Layer 1 (Sim Truth) oracle input for DRG-202 three-layer combat verification.</summary>
public sealed record SimTruthOracleFact(
    string ScenarioId,
    int Seed,
    ulong SimTick,
    string WorldHash,
    string OrderLogFingerprint,
    IReadOnlyList<string> WeaponFamiliesObserved,
    int EngagementEventsCount,
    bool ReplayStableAcrossRuns);

/// <summary>Layer 2 (Presentation Projection) oracle input for DRG-202 three-layer combat verification.</summary>
public sealed record PresentationProjectionOracleFact(
    OracleEvidenceClass EvidenceClass,
    IReadOnlyList<string> WeaponFamiliesRendered,
    bool AllIndicatorsNonColorCoded,
    string ZoomBand,
    bool ZoomBandDeclutterWithinBudget,
    int EventToRenderMatchedCount,
    bool StaleUnknownCommsStatesVerified);

/// <summary>Layer 3 (Rendered &amp; Accessibility Invariants) oracle input for DRG-202 / DRG-205.</summary>
public sealed record RenderedAccessibilityOracleFact(
    OracleEvidenceClass EvidenceClass,
    IReadOnlyList<string> ViewportsVerified,
    bool ScaleExtremesVerified,
    int VisibleMessageLogRowsAt100Pct,
    bool CommsTriadAndLegendTooltipVerified,
    bool PlatformEditorTextButtonsAndDisabledReadabilityVerified,
    bool KeyboardAndPointerVerifiedSeparatelyFromCallbacks,
    bool OwnerWalkCompleted);

/// <summary>Deterministic fail-closed verdict across all three combat oracle layers.</summary>
public sealed record ThreeLayerCombatOracleVerdict(
    bool Passed,
    bool Layer1SimTruthPassed,
    bool Layer2ProjectionPassed,
    bool Layer3RenderedAccessibilityPassed,
    bool OwnerAcceptanceClaimed,
    IReadOnlyList<string> LinkedRequirementIds,
    IReadOnlyList<string> Failures,
    string VerdictDigestSha256);

/// <summary>
/// DRG-202 fail-closed evaluator correlating Layer 1 (Sim Truth), Layer 2 (Presentation Projection),
/// and Layer 3 (Rendered &amp; Accessibility Invariants) without mutating simulation state.
/// </summary>
public static class ThreeLayerCombatOracleEvaluator
{
    /// <summary>Minimum visible message log rows at 100% scale per ACC-11.</summary>
    public const int MinVisibleMessageLogRowsAt100Pct = 12;

    private static readonly string[] RequiredBaselineViewports = { "1600x979", "1280x779" };
    private static readonly string[] DefaultLinkedRequirements =
    {
        "VER-04",
        "VER-06",
        "ACC-11",
        "CMD-01",
        "CMD-07",
        "CMD-10",
    };

    /// <summary>Evaluates all three combat oracle layers and returns a deterministic SHA-256 digested verdict.</summary>
    public static ThreeLayerCombatOracleVerdict Evaluate(
        SimTruthOracleFact? simTruth,
        PresentationProjectionOracleFact? projection,
        RenderedAccessibilityOracleFact? rendered)
    {
        var failures = new List<string>();
        var layer1Pass = true;
        var layer2Pass = true;
        var layer3Pass = true;

        if (simTruth is null)
        {
            layer1Pass = false;
            failures.Add("Layer 1 (SimTruth): missing sim truth fact.");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(simTruth.ScenarioId))
            {
                layer1Pass = false;
                failures.Add("Layer 1 (SimTruth): scenario id is required.");
            }

            if (string.IsNullOrWhiteSpace(simTruth.WorldHash))
            {
                layer1Pass = false;
                failures.Add("Layer 1 (SimTruth): world hash is required.");
            }

            if (string.IsNullOrWhiteSpace(simTruth.OrderLogFingerprint))
            {
                layer1Pass = false;
                failures.Add("Layer 1 (SimTruth): order log fingerprint is required.");
            }

            if (!simTruth.ReplayStableAcrossRuns)
            {
                layer1Pass = false;
                failures.Add("Layer 1 (SimTruth / VER-04): replay stability across repeated runs failed.");
            }
        }

        if (projection is null)
        {
            layer2Pass = false;
            failures.Add("Layer 2 (Projection): missing presentation projection fact.");
        }
        else
        {
            var renderedFamilies = new HashSet<string>(
                projection.WeaponFamiliesRendered ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (simTruth?.WeaponFamiliesObserved is not null)
            {
                foreach (var observed in simTruth.WeaponFamiliesObserved)
                {
                    if (!string.IsNullOrWhiteSpace(observed) && !renderedFamilies.Contains(observed))
                    {
                        layer2Pass = false;
                        failures.Add($"Layer 2 (Projection): weapon family '{observed}' observed in sim truth is missing from presentation projection.");
                    }
                }
            }

            if (!projection.AllIndicatorsNonColorCoded)
            {
                layer2Pass = false;
                failures.Add("Layer 2 (Projection / ACC): non-color indicator encoding requirement failed.");
            }

            if (!projection.ZoomBandDeclutterWithinBudget)
            {
                layer2Pass = false;
                failures.Add($"Layer 2 (Projection / DRG-176): zoom-band '{projection.ZoomBand}' declutter exceeded budget.");
            }

            if (simTruth is not null && projection.EventToRenderMatchedCount < simTruth.EngagementEventsCount)
            {
                layer2Pass = false;
                failures.Add(
                    $"Layer 2 (Projection): event-to-render correspondence mismatch ({projection.EventToRenderMatchedCount}/{simTruth.EngagementEventsCount}).");
            }

            if (!projection.StaleUnknownCommsStatesVerified)
            {
                layer2Pass = false;
                failures.Add("Layer 2 (Projection): stale/unknown/comms-loss states were not verified.");
            }
        }

        if (rendered is null)
        {
            layer3Pass = false;
            failures.Add("Layer 3 (Rendered/ACC): missing rendered accessibility fact.");
        }
        else
        {
            var viewports = new HashSet<string>(
                rendered.ViewportsVerified ?? Array.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            foreach (var required in RequiredBaselineViewports)
            {
                if (!viewports.Contains(required))
                {
                    layer3Pass = false;
                    failures.Add($"Layer 3 (Rendered): required baseline viewport '{required}' was not verified.");
                }
            }

            if (!rendered.ScaleExtremesVerified)
            {
                layer3Pass = false;
                failures.Add("Layer 3 (Rendered): supported UI scale extremes were not verified.");
            }

            if (rendered.VisibleMessageLogRowsAt100Pct < MinVisibleMessageLogRowsAt100Pct)
            {
                layer3Pass = false;
                failures.Add(
                    $"Layer 3 (ACC-11): visible message log rows at 100% scale ({rendered.VisibleMessageLogRowsAt100Pct}) < minimum {MinVisibleMessageLogRowsAt100Pct}.");
            }

            if (!rendered.CommsTriadAndLegendTooltipVerified)
            {
                layer3Pass = false;
                failures.Add("Layer 3 (ACC-11): COMMS degrade triad and HUD legend tooltip copy were not verified.");
            }

            if (!rendered.PlatformEditorTextButtonsAndDisabledReadabilityVerified)
            {
                layer3Pass = false;
                failures.Add("Layer 3 (ACC-11): Platform Editor text-only buttons and readable disabled state were not verified.");
            }

            if (!rendered.KeyboardAndPointerVerifiedSeparatelyFromCallbacks)
            {
                layer3Pass = false;
                failures.Add("Layer 3 (Rendered / DRG-235): keyboard and pointer interactions must be verified separately from callback automation.");
            }

            if (rendered.OwnerWalkCompleted && rendered.EvidenceClass != OracleEvidenceClass.OwnerAcceptedWalk)
            {
                layer3Pass = false;
                failures.Add(
                    $"Layer 3 (H6 / Honesty): OwnerWalkCompleted cannot be true when evidence class is '{rendered.EvidenceClass}'.");
            }
        }

        var passed = layer1Pass && layer2Pass && layer3Pass && failures.Count == 0;
        var ownerClaimed = rendered is { OwnerWalkCompleted: true, EvidenceClass: OracleEvidenceClass.OwnerAcceptedWalk };
        var digest = ComputeDigest(simTruth, projection, rendered, passed, failures);

        return new ThreeLayerCombatOracleVerdict(
            Passed: passed,
            Layer1SimTruthPassed: layer1Pass,
            Layer2ProjectionPassed: layer2Pass,
            Layer3RenderedAccessibilityPassed: layer3Pass,
            OwnerAcceptanceClaimed: ownerClaimed,
            LinkedRequirementIds: DefaultLinkedRequirements,
            Failures: failures,
            VerdictDigestSha256: digest);
    }

    private static string ComputeDigest(
        SimTruthOracleFact? simTruth,
        PresentationProjectionOracleFact? projection,
        RenderedAccessibilityOracleFact? rendered,
        bool passed,
        IReadOnlyList<string> failures)
    {
        var sb = new StringBuilder();
        sb.Append("v1|").Append(passed ? "PASS" : "FAIL").Append('|');
        if (simTruth is not null)
        {
            sb.Append(simTruth.ScenarioId).Append('|')
                .Append(simTruth.Seed.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(simTruth.SimTick.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(simTruth.WorldHash).Append('|')
                .Append(simTruth.OrderLogFingerprint).Append('|');
        }

        if (projection is not null)
        {
            sb.Append((int)projection.EvidenceClass).Append('|')
                .Append(projection.ZoomBand).Append('|')
                .Append(projection.EventToRenderMatchedCount.ToString(CultureInfo.InvariantCulture)).Append('|');
        }

        if (rendered is not null)
        {
            sb.Append((int)rendered.EvidenceClass).Append('|')
                .Append(rendered.VisibleMessageLogRowsAt100Pct.ToString(CultureInfo.InvariantCulture)).Append('|')
                .Append(rendered.OwnerWalkCompleted ? "1" : "0").Append('|');
        }

        for (var i = 0; i < failures.Count; i++)
        {
            sb.Append(failures[i]).Append(';');
        }

        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        var hex = new StringBuilder(bytes.Length * 2);
        for (var i = 0; i < bytes.Length; i++)
        {
            hex.Append(bytes[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return hex.ToString();
    }
}
