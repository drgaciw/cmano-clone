using ProjectAegis.Delegation.UnityAdapter.Baltic;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// ACC-11 HUD tooltip formatter for the C2 top-bar COMMS legend.
/// Pure presentation formatting over already-bound <see cref="C2NetworkHealthPanelLabels"/>
/// (ADR-010 §2–3, ADR-007, ADR-001).
/// </summary>
public static class C2CommsLegendTooltipBinder
{
    /// <summary>Static ACC-11 HUD legend tooltip copy explaining COMMS degrade states.</summary>
    public const string DefaultLegendTooltip =
        "COMMS STATES — NOMINAL: full sensor & command picture; " +
        "DEGRADED: stale contacts & reduced symbol opacity; " +
        "DENIED: link lost, no new remote engagements, map dimmed.";

    /// <summary>Builds the ACC-11 HUD tooltip combining state legend copy with current network health summary.</summary>
    public static string BuildTooltip(C2NetworkHealthPanelLabels? labels)
    {
        if (labels is null || string.IsNullOrWhiteSpace(labels.NetworkHealthLine))
        {
            return $"{DefaultLegendTooltip} Network: UNKNOWN — no snapshot bound.";
        }

        var next = string.IsNullOrWhiteSpace(labels.NextActionLine)
            ? string.Empty
            : $" {labels.NextActionLine}";
        return $"{DefaultLegendTooltip} {labels.NetworkHealthLine}.{next}";
    }
}

/// <summary>Result of a VER-04 multi-run deterministic seed-invariance check.</summary>
public sealed record Ver04SeedInvarianceResult(
    bool Passed,
    int RunsExecuted,
    string ScenarioPolicyId,
    int Seed,
    int Ticks,
    ulong FinalWorldHash,
    string LogFingerprint,
    IReadOnlyList<string> Failures);

/// <summary>Input for ACC-11 C2 &amp; Platform Editor accessibility release gate evaluation.</summary>
public sealed record Acc11AccessibilityInput(
    bool TierDeclaredStandard,
    int VisibleMessageLogRowsAt100Pct,
    bool CommsDegradeTriadVisible,
    string CommsLegendTooltipText,
    IReadOnlyList<string> PlatformEditorButtonLabels,
    bool ApproveDisabledReadableAt45Pct,
    bool KeyboardOobAndDrawerNavigable,
    bool AffiliationShapeNotColorOnly);

/// <summary>Input for VER-06 CI gate and test-floor release check.</summary>
public sealed record Ver06CiGateInput(
    int ReportedPassedTests,
    int RequiredTestFloor,
    int ReplayGoldenPassed,
    int ReplayGoldenRequired,
    bool GoldenHashPreserved,
    int DelegationBridgeHotpathDiffLines,
    bool GauntletOracleCiFailClosedVerified);

/// <summary>Generic deterministic gate check result for DRG-205 release gates.</summary>
public sealed record CombatReleaseGateResult(
    string GateId,
    bool Passed,
    IReadOnlyList<string> Failures);

/// <summary>DRG-205 release gate evaluator covering VER-04, VER-06, and ACC-11.</summary>
public static class CombatReleaseGateEvaluator
{
    private static readonly string[] RequiredPlatformButtons = { "Export", "Diff", "Propose", "Approve" };

    /// <summary>VER-04.1: runs a scenario N times with identical seed and verifies identical hashes &amp; fingerprints.</summary>
    public static Ver04SeedInvarianceResult VerifyVer04SeedInvariance(
        string scenarioPolicyId,
        int seed,
        int ticks,
        int runs = 3)
    {
        var failures = new List<string>();
        if (runs < 2)
        {
            failures.Add("VER-04.1 requires at least 2 runs to verify seed invariance.");
            return new Ver04SeedInvarianceResult(false, 0, scenarioPolicyId, seed, ticks, 0UL, string.Empty, failures);
        }

        ulong baselineHash = 0UL;
        var baselineFingerprint = string.Empty;

        for (var i = 0; i < runs; i++)
        {
            var run = BalticReplayHarness.Run(seed, scenarioPolicyId, ticks: ticks, mvpEngagement: false);
            if (string.IsNullOrWhiteSpace(run.Fingerprint))
            {
                failures.Add($"Run {i + 1}: empty order-log fingerprint.");
            }

            if (i == 0)
            {
                baselineHash = run.WorldHash;
                baselineFingerprint = run.Fingerprint;
            }
            else
            {
                if (run.WorldHash != baselineHash)
                {
                    failures.Add($"Run {i + 1}: world hash {run.WorldHash} != baseline {baselineHash}.");
                }

                if (!string.Equals(run.Fingerprint, baselineFingerprint, StringComparison.Ordinal))
                {
                    failures.Add($"Run {i + 1}: log fingerprint '{run.Fingerprint}' != baseline '{baselineFingerprint}'.");
                }
            }
        }

        return new Ver04SeedInvarianceResult(
            Passed: failures.Count == 0,
            RunsExecuted: runs,
            ScenarioPolicyId: scenarioPolicyId,
            Seed: seed,
            Ticks: ticks,
            FinalWorldHash: baselineHash,
            LogFingerprint: baselineFingerprint,
            Failures: failures);
    }

    /// <summary>ACC-11: validates C2 Command Post &amp; Platform Editor accessibility checklist conditions.</summary>
    public static CombatReleaseGateResult EvaluateAcc11(Acc11AccessibilityInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var failures = new List<string>();
        if (!input.TierDeclaredStandard)
        {
            failures.Add("ACC-11: accessibility Standard tier must be declared.");
        }

        if (input.VisibleMessageLogRowsAt100Pct < 12)
        {
            failures.Add($"ACC-11: C2 message log requires >= 12 visible rows at 100% scale (got {input.VisibleMessageLogRowsAt100Pct}).");
        }

        if (!input.CommsDegradeTriadVisible)
        {
            failures.Add("ACC-11: COMMS degrade triad (opacity + ghost + top bar text) must be visible.");
        }

        if (string.IsNullOrWhiteSpace(input.CommsLegendTooltipText)
            || !input.CommsLegendTooltipText.Contains("NOMINAL", StringComparison.Ordinal)
            || !input.CommsLegendTooltipText.Contains("DEGRADED", StringComparison.Ordinal)
            || !input.CommsLegendTooltipText.Contains("DENIED", StringComparison.Ordinal))
        {
            failures.Add("ACC-11: COMMS legend tooltip copy must define NOMINAL, DEGRADED, and DENIED states.");
        }

        var buttons = new HashSet<string>(
            input.PlatformEditorButtonLabels,
            StringComparer.OrdinalIgnoreCase);

        foreach (var required in RequiredPlatformButtons)
        {
            if (!buttons.Contains(required))
            {
                failures.Add($"ACC-11: Platform Editor missing required text button '{required}'.");
            }
        }

        if (!input.ApproveDisabledReadableAt45Pct)
        {
            failures.Add("ACC-11: Platform Editor Approve button disabled state at 45% opacity must remain readable.");
        }

        if (!input.KeyboardOobAndDrawerNavigable)
        {
            failures.Add("ACC-11: OOB selection and drawer tabs must be keyboard navigable without pointer.");
        }

        if (!input.AffiliationShapeNotColorOnly)
        {
            failures.Add("ACC-11: Map and OOB affiliation must be distinguishable by shape, not color alone.");
        }

        return new CombatReleaseGateResult("ACC-11", failures.Count == 0, failures);
    }

    /// <summary>VER-06: validates CI test floor, ReplayGolden, golden hash, DelegationBridge zero-touch, and oracle fail-closed.</summary>
    public static CombatReleaseGateResult EvaluateVer06CiGate(Ver06CiGateInput input)
    {
        if (input is null)
        {
            throw new ArgumentNullException(nameof(input));
        }

        var failures = new List<string>();
        if (input.ReportedPassedTests < input.RequiredTestFloor)
        {
            failures.Add($"VER-06.1: passed tests {input.ReportedPassedTests} < AGENTS.md floor {input.RequiredTestFloor}.");
        }

        if (input.ReplayGoldenPassed < input.ReplayGoldenRequired)
        {
            failures.Add($"VER-04.2: ReplayGolden passed {input.ReplayGoldenPassed} < required {input.ReplayGoldenRequired}.");
        }

        if (!input.GoldenHashPreserved)
        {
            failures.Add("VER-04.3: Baltic v2 golden hash 17144800277401907079 was not preserved.");
        }

        if (input.DelegationBridgeHotpathDiffLines != 0)
        {
            failures.Add($"VER-06.3: DelegationBridge.cs hotpath has {input.DelegationBridgeHotpathDiffLines} unauthorized diff lines.");
        }

        if (!input.GauntletOracleCiFailClosedVerified)
        {
            failures.Add("VER-06.2: gauntlet_oracle_eval --profile ci fail-closed rejection was not verified.");
        }

        return new CombatReleaseGateResult("VER-06", failures.Count == 0, failures);
    }
}
