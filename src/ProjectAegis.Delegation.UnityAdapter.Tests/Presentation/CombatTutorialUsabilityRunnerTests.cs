using NUnit.Framework;
using ProjectAegis.Delegation.UnityAdapter.Presentation;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class CombatTutorialUsabilityRunnerTests
{
    [Test]
    public void BuildDegradedCommsAcceptanceWalkthrough_covers_all_seven_slice_g2_stages()
    {
        var walkthrough = CombatTutorialUsabilityRunner.BuildDegradedCommsAcceptanceWalkthrough();

        Assert.That(walkthrough.ScenarioId, Is.EqualTo("baltic-patrol-degraded-comms-g2"));
        Assert.That(walkthrough.Steps.Count, Is.EqualTo(7));
        Assert.That(walkthrough.Steps.Select(s => s.StageKey).ToArray(), Is.EqualTo(new[]
        {
            "plan-assign",
            "accelerate-alert",
            "inspect-doctrine-refusal",
            "intervene-return",
            "lose-link",
            "recover-link",
            "replay-verify",
        }));
        Assert.That(walkthrough.Steps.All(s => !string.IsNullOrWhiteSpace(s.ExpectedOutcome)), Is.True);
        Assert.That(walkthrough.Steps.All(s => !string.IsNullOrWhiteSpace(s.RequirementId)), Is.True);
    }

    [Test]
    public void EvaluateUsabilityRun_passes_when_all_steps_pass_and_input_modes_and_viewports_are_distinct()
    {
        var walkthrough = CombatTutorialUsabilityRunner.BuildDegradedCommsAcceptanceWalkthrough();
        var outcomes = walkthrough.Steps
            .Select(s => new CombatTutorialStepOutcome(
                s.StageKey,
                Passed: true,
                ElapsedSeconds: 4.5,
                UsedKeyboardOrPointer: true,
                EvidenceSource: "EditorPlayModeFixture",
                Notes: "Verified"))
            .ToArray();

        var report = CombatTutorialUsabilityRunner.Evaluate(
            walkthrough,
            outcomes,
            verifiedViewports: ["1600x979", "1280x779"],
            scaleExtremesVerified: true,
            containsFixtureOnlySteps: true);

        Assert.That(report.Passed, Is.True, string.Join("; ", report.Failures));
        Assert.That(report.CompletedStepCount, Is.EqualTo(7));
        Assert.That(report.ClaimsLiveScenarioProof, Is.False);
    }

    [Test]
    public void EvaluateUsabilityRun_fails_closed_when_step_fails_or_callback_only_automation_masquerades_as_pointer_keyboard()
    {
        var walkthrough = CombatTutorialUsabilityRunner.BuildDegradedCommsAcceptanceWalkthrough();
        var outcomes = walkthrough.Steps
            .Select((s, idx) => new CombatTutorialStepOutcome(
                s.StageKey,
                Passed: idx != 4,
                ElapsedSeconds: 2.0,
                UsedKeyboardOrPointer: idx != 1,
                EvidenceSource: "CallbackOnly",
                Notes: idx == 4 ? "COMMS loss badge missing" : "ok"))
            .ToArray();

        var report = CombatTutorialUsabilityRunner.Evaluate(
            walkthrough,
            outcomes,
            verifiedViewports: ["1600x979"],
            scaleExtremesVerified: false,
            containsFixtureOnlySteps: true);

        Assert.That(report.Passed, Is.False);
        Assert.That(report.Failures, Has.Some.Contains("lose-link"));
        Assert.That(report.Failures, Has.Some.Contains("keyboard/pointer"));
        Assert.That(report.Failures, Has.Some.Contains("1280x779"));
    }
}
