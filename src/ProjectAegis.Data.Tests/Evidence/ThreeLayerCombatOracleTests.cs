using NUnit.Framework;
using ProjectAegis.Data.Evidence;

namespace ProjectAegis.Data.Tests.Evidence;

[TestFixture]
public sealed class ThreeLayerCombatOracleTests
{
    [Test]
    public void Evaluate_passes_when_all_three_layers_agree_and_provenance_is_honest()
    {
        var sim = new SimTruthOracleFact(
            ScenarioId: "baltic-patrol-v2",
            Seed: 42,
            SimTick: 120,
            WorldHash: "17144800277401907079",
            OrderLogFingerprint: "sha256:abc123",
            WeaponFamiliesObserved: ["missile", "gun"],
            EngagementEventsCount: 3,
            ReplayStableAcrossRuns: true);

        var projection = new PresentationProjectionOracleFact(
            EvidenceClass: OracleEvidenceClass.EditorPlayMode,
            WeaponFamiliesRendered: ["missile", "gun"],
            AllIndicatorsNonColorCoded: true,
            ZoomBand: "Tactical",
            ZoomBandDeclutterWithinBudget: true,
            EventToRenderMatchedCount: 3,
            StaleUnknownCommsStatesVerified: true);

        var rendered = new RenderedAccessibilityOracleFact(
            EvidenceClass: OracleEvidenceClass.EditorPlayMode,
            ViewportsVerified: ["1600x979", "1280x779"],
            ScaleExtremesVerified: true,
            VisibleMessageLogRowsAt100Pct: 14,
            CommsTriadAndLegendTooltipVerified: true,
            PlatformEditorTextButtonsAndDisabledReadabilityVerified: true,
            KeyboardAndPointerVerifiedSeparatelyFromCallbacks: true,
            OwnerWalkCompleted: false);

        var verdict = ThreeLayerCombatOracleEvaluator.Evaluate(sim, projection, rendered);

        Assert.That(verdict.Passed, Is.True, string.Join("; ", verdict.Failures));
        Assert.That(verdict.Layer1SimTruthPassed, Is.True);
        Assert.That(verdict.Layer2ProjectionPassed, Is.True);
        Assert.That(verdict.Layer3RenderedAccessibilityPassed, Is.True);
        Assert.That(verdict.OwnerAcceptanceClaimed, Is.False);
        Assert.That(verdict.Failures, Is.Empty);
    }

    [Test]
    public void Evaluate_fails_closed_when_weapon_family_or_event_correspondence_diverges_across_layers()
    {
        var sim = new SimTruthOracleFact(
            ScenarioId: "baltic-patrol-v2",
            Seed: 42,
            SimTick: 120,
            WorldHash: "17144800277401907079",
            OrderLogFingerprint: "sha256:abc123",
            WeaponFamiliesObserved: ["missile", "gun"],
            EngagementEventsCount: 4,
            ReplayStableAcrossRuns: true);

        var projection = new PresentationProjectionOracleFact(
            EvidenceClass: OracleEvidenceClass.HeadlessProjection,
            WeaponFamiliesRendered: ["missile"],
            AllIndicatorsNonColorCoded: false,
            ZoomBand: "Tactical",
            ZoomBandDeclutterWithinBudget: false,
            EventToRenderMatchedCount: 2,
            StaleUnknownCommsStatesVerified: false);

        var rendered = new RenderedAccessibilityOracleFact(
            EvidenceClass: OracleEvidenceClass.HeadlessProjection,
            ViewportsVerified: ["1600x979"],
            ScaleExtremesVerified: false,
            VisibleMessageLogRowsAt100Pct: 8,
            CommsTriadAndLegendTooltipVerified: false,
            PlatformEditorTextButtonsAndDisabledReadabilityVerified: false,
            KeyboardAndPointerVerifiedSeparatelyFromCallbacks: false,
            OwnerWalkCompleted: false);

        var verdict = ThreeLayerCombatOracleEvaluator.Evaluate(sim, projection, rendered);

        Assert.That(verdict.Passed, Is.False);
        Assert.That(verdict.Layer2ProjectionPassed, Is.False);
        Assert.That(verdict.Layer3RenderedAccessibilityPassed, Is.False);
        Assert.That(verdict.Failures, Has.Some.Contains("weapon family 'gun'"));
        Assert.That(verdict.Failures, Has.Some.Contains("non-color"));
        Assert.That(verdict.Failures, Has.Some.Contains("event-to-render"));
        Assert.That(verdict.Failures, Has.Some.Contains("1280x779"));
        Assert.That(verdict.Failures, Has.Some.Contains("ACC-11"));
    }

    [Test]
    public void Evaluate_rejects_fixture_or_headless_evidence_falsely_claiming_live_owner_acceptance()
    {
        var sim = new SimTruthOracleFact(
            ScenarioId: "baltic-patrol-v2",
            Seed: 7,
            SimTick: 60,
            WorldHash: "17144800277401907079",
            OrderLogFingerprint: "sha256:def456",
            WeaponFamiliesObserved: ["missile"],
            EngagementEventsCount: 1,
            ReplayStableAcrossRuns: true);

        var projection = new PresentationProjectionOracleFact(
            EvidenceClass: OracleEvidenceClass.FixtureOnly,
            WeaponFamiliesRendered: ["missile"],
            AllIndicatorsNonColorCoded: true,
            ZoomBand: "Operational",
            ZoomBandDeclutterWithinBudget: true,
            EventToRenderMatchedCount: 1,
            StaleUnknownCommsStatesVerified: true);

        var rendered = new RenderedAccessibilityOracleFact(
            EvidenceClass: OracleEvidenceClass.FixtureOnly,
            ViewportsVerified: ["1600x979", "1280x779"],
            ScaleExtremesVerified: true,
            VisibleMessageLogRowsAt100Pct: 12,
            CommsTriadAndLegendTooltipVerified: true,
            PlatformEditorTextButtonsAndDisabledReadabilityVerified: true,
            KeyboardAndPointerVerifiedSeparatelyFromCallbacks: true,
            OwnerWalkCompleted: true);

        var verdict = ThreeLayerCombatOracleEvaluator.Evaluate(sim, projection, rendered);

        Assert.That(verdict.Passed, Is.False);
        Assert.That(verdict.Failures, Has.Some.Contains("OwnerWalkCompleted"));
    }

    [Test]
    public void Evaluate_produces_deterministic_digest_and_correlates_requirement_links()
    {
        var sim = new SimTruthOracleFact(
            "baltic-patrol-v2", 11, 90, "17144800277401907079", "fp-11", ["missile"], 2, true);
        var proj = new PresentationProjectionOracleFact(
            OracleEvidenceClass.EditorPlayMode, ["missile"], true, "Tactical", true, 2, true);
        var rend = new RenderedAccessibilityOracleFact(
            OracleEvidenceClass.EditorPlayMode, ["1600x979", "1280x779"], true, 12, true, true, true, false);

        var first = ThreeLayerCombatOracleEvaluator.Evaluate(sim, proj, rend);
        var second = ThreeLayerCombatOracleEvaluator.Evaluate(sim, proj, rend);

        Assert.That(first.VerdictDigestSha256, Is.EqualTo(second.VerdictDigestSha256));
        Assert.That(first.VerdictDigestSha256.Length, Is.EqualTo(64));
        Assert.That(first.LinkedRequirementIds, Does.Contain("VER-04"));
        Assert.That(first.LinkedRequirementIds, Does.Contain("ACC-11"));
    }
}
