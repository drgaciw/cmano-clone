namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>One guided step in a DRG-177 / DRG-235 combat tutorial or acceptance walkthrough.</summary>
public sealed record CombatTutorialStep(
    string StageKey,
    string Title,
    string RequirementId,
    string PlayerPrompt,
    string ExpectedOutcome);

/// <summary>Definition of a scenario-based combat usability walkthrough.</summary>
public sealed record CombatTutorialWalkthrough(
    string ScenarioId,
    string Title,
    IReadOnlyList<CombatTutorialStep> Steps);

/// <summary>Recorded execution outcome for a single tutorial/usability step.</summary>
public sealed record CombatTutorialStepOutcome(
    string StageKey,
    bool Passed,
    double ElapsedSeconds,
    bool UsedKeyboardOrPointer,
    string EvidenceSource,
    string Notes);

/// <summary>Honest, fail-closed evaluation report for a combat tutorial / Slice G2 usability run.</summary>
public sealed record CombatTutorialUsabilityReport(
    string ScenarioId,
    bool Passed,
    int CompletedStepCount,
    int TotalStepCount,
    bool ClaimsLiveScenarioProof,
    IReadOnlyList<string> VerifiedViewports,
    IReadOnlyList<string> Failures);

/// <summary>
/// DRG-177 / DRG-235 scenario-based combat tutorial and Slice G2 degraded-comms walkthrough evaluator.
/// Strictly distinguishes pointer/keyboard runs from callback-only automation and fixture runs from live-scenario proof.
/// </summary>
public static class CombatTutorialUsabilityRunner
{
    private static readonly string[] RequiredViewports = { "1600x979", "1280x779" };

    /// <summary>Returns the canonical 7-stage Slice G2 degraded-comms acceptance &amp; tutorial walkthrough.</summary>
    public static CombatTutorialWalkthrough BuildDegradedCommsAcceptanceWalkthrough() =>
        new(
            ScenarioId: "baltic-patrol-degraded-comms-g2",
            Title: "Slice G2 Degraded-Comms Integrated C2 Acceptance Walkthrough",
            Steps: new[]
            {
                new CombatTutorialStep(
                    "plan-assign",
                    "Plan & Assign Mission",
                    "CMD-01",
                    "Select friendly unit via OOB or map, inspect detail panel, and confirm mission assignment before execution.",
                    "Selection syncs across OOB, map, and detail panel; persistent chrome zones render."),
                new CombatTutorialStep(
                    "accelerate-alert",
                    "Accelerate Time & Handle Alert",
                    "CMD-07",
                    "Begin execution, step time compression up, and observe priority alert / safe-stop feedback.",
                    "Time compression steps cleanly and priority alert surfaces with actionable cue."),
                new CombatTutorialStep(
                    "inspect-doctrine-refusal",
                    "Inspect Doctrine & Refusal",
                    "CMD-10",
                    "Inspect unit doctrine/ROE and attempt a restricted or approval-gated action.",
                    "Command refusal or approval-required explanation renders without bypassing command facades."),
                new CombatTutorialStep(
                    "intervene-return",
                    "Intervene & Return to Delegation",
                    "CMD-10",
                    "Take direct control or issue an override order, then return unit to delegated control.",
                    "Direct-control badge updates and returns cleanly with logged order."),
                new CombatTutorialStep(
                    "lose-link",
                    "Lose Comms Link",
                    "ACC-11",
                    "Transition network health to DEGRADED/DENIED and inspect COMMS triad and HUD legend tooltip.",
                    "COMMS degrade triad renders non-color text, dimmed/stale indicators, and legend tooltip copy."),
                new CombatTutorialStep(
                    "recover-link",
                    "Recover Comms Link",
                    "ACC-11",
                    "Restore network health to NOMINAL and confirm recovery feedback in top bar and log.",
                    "COMMS state returns to NOMINAL with no stale override lock."),
                new CombatTutorialStep(
                    "replay-verify",
                    "Verify History/Replay Separation",
                    "VER-04",
                    "Inspect historical order log / replay state and return to live view.",
                    "Historical inspection stays distinct from live authorization; replay hash remains deterministic."),
            });

    /// <summary>Evaluates recorded step outcomes, viewport coverage, and pointer/keyboard honesty.</summary>
    public static CombatTutorialUsabilityReport Evaluate(
        CombatTutorialWalkthrough walkthrough,
        IReadOnlyList<CombatTutorialStepOutcome>? outcomes,
        IReadOnlyList<string>? verifiedViewports,
        bool scaleExtremesVerified,
        bool containsFixtureOnlySteps)
    {
        if (walkthrough is null)
        {
            throw new ArgumentNullException(nameof(walkthrough));
        }

        var failures = new List<string>();
        var outcomeMap = (outcomes ?? Array.Empty<CombatTutorialStepOutcome>())
            .ToDictionary(o => o.StageKey, StringComparer.OrdinalIgnoreCase);

        var completed = 0;
        foreach (var step in walkthrough.Steps)
        {
            if (!outcomeMap.TryGetValue(step.StageKey, out var outcome))
            {
                failures.Add($"Stage '{step.StageKey}' ({step.RequirementId}): missing outcome.");
                continue;
            }

            if (!outcome.Passed)
            {
                failures.Add($"Stage '{step.StageKey}' ({step.RequirementId}): failed ({outcome.Notes}).");
            }
            else
            {
                completed++;
            }

            if (!outcome.UsedKeyboardOrPointer)
            {
                failures.Add(
                    $"Stage '{step.StageKey}' ({step.RequirementId}): must be exercised via keyboard/pointer input, not callback-only automation.");
            }
        }

        var viewportSet = new HashSet<string>(
            verifiedViewports ?? Array.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var required in RequiredViewports)
        {
            if (!viewportSet.Contains(required))
            {
                failures.Add($"Viewport '{required}' reachability was not verified.");
            }
        }

        if (!scaleExtremesVerified)
        {
            failures.Add("Supported UI scale extremes were not verified.");
        }

        return new CombatTutorialUsabilityReport(
            ScenarioId: walkthrough.ScenarioId,
            Passed: failures.Count == 0,
            CompletedStepCount: completed,
            TotalStepCount: walkthrough.Steps.Count,
            ClaimsLiveScenarioProof: !containsFixtureOnlySteps,
            VerifiedViewports: viewportSet.OrderBy(v => v, StringComparer.Ordinal).ToArray(),
            Failures: failures);
    }
}
