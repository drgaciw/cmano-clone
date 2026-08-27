using ProjectAegis.Delegation.ThreatAssessment;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.ThreatAssessment;

public sealed class ThreatAssessmentProjectionTests
{
    private static EngageContext FeasibleContext => new(
        RangeMeters: 50_000,
        Envelope: new WeaponEnvelope(1_000, 100_000),
        RoundsRemaining: 4,
        HasFireControlTrack: true,
        RadarEmconActive: true,
        MountOnline: true,
        SalvoSize: 2,
        DlzPersonality: DlzPersonality.Normal);

    private static ThreatAssessmentInput BaseInput(
        EffectivePolicy policy,
        in EngageContext ctx) =>
        new(
            ContactId: "contact-1",
            TargetId: "1001",
            ShooterUnitId: "2001",
            WeaponId: "sm-2",
            WeaponLabel: "SM-2 Block IIIA",
            EngageContext: ctx,
            Policy: policy,
            Posture: ThreatAssessmentPosture.Defensive);

    [Test]
    public void Feasible_recommendation_includes_confidence_range_and_policy_constraints()
    {
        var input = BaseInput(EffectivePolicy.DefaultFree, FeasibleContext);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.Feasible));
        Assert.That(recommendation.RecommendationKind, Is.EqualTo(ThreatRecommendationKind.AdvisoryRecommendation));
        Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(recommendation.IsFireOrder, Is.False);
        Assert.That(recommendation.IsAutomaticEngagement, Is.False);
        Assert.That(recommendation.Confidence, Is.GreaterThan(0.5));
        Assert.That(recommendation.Assumptions, Is.Not.Empty);
        Assert.That(recommendation.Assumptions, Does.Contain("Advisory recommendation only — not weapons release authorization."));
        Assert.That(recommendation.Range.RangeMeters, Is.EqualTo(50_000));
        Assert.That(recommendation.Range.InEnvelope, Is.True);
        Assert.That(recommendation.Range.DlzState, Is.EqualTo(DlzState.InZone));
        Assert.That(recommendation.PolicyConstraints.RoeLevel, Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(recommendation.PolicyConstraints.PolicyAllowsFire, Is.True);
        Assert.That(recommendation.WithheldReasonCode, Is.Null);
        Assert.That(recommendation.StatusLine, Does.Contain("RECOMMEND").IgnoreCase);
        Assert.That(recommendation.StatusLine, Does.Contain("not weapons release").IgnoreCase);
    }

    [Test]
    public void Weapons_tight_withholds_recommendation_by_policy()
    {
        var policy = new EffectivePolicy(RoeLevel.WeaponsTight, MaxSalvo: 2);
        var input = BaseInput(policy, FeasibleContext);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByPolicy));
        Assert.That(recommendation.Confidence, Is.EqualTo(0));
        Assert.That(recommendation.PolicyConstraints.PolicyAllowsFire, Is.False);
        Assert.That(recommendation.PolicyConstraints.RoeLevel, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(recommendation.PolicyConstraints.PolicyAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT));
        Assert.That(recommendation.WithheldReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT));
        Assert.That(recommendation.Assumptions, Does.Contain("ROE WeaponsTight withholds weapons release."));
        Assert.That(recommendation.StatusLine, Does.Contain("WITHHELD BY POLICY"));
        Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(recommendation.IsFireOrder, Is.False);
        Assert.That(recommendation.IsAutomaticEngagement, Is.False);
    }

    [Test]
    public void Hold_fire_withholds_recommendation_by_named_policy_abort()
    {
        var policy = new EffectivePolicy(RoeLevel.HoldFire, MaxSalvo: 1);
        var input = BaseInput(policy, FeasibleContext);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByPolicy));
        Assert.That(recommendation.WithheldReasonCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE));
        Assert.That(recommendation.PolicyConstraints.PolicyAbortCode, Is.EqualTo(AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE));
    }

    [Test]
    public void Engage_gate_blocks_when_dlz_out_even_under_weapons_free()
    {
        var ctx = FeasibleContext with { RangeMeters = 90_000 };
        var input = BaseInput(EffectivePolicy.DefaultFree, ctx);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByEngage));
        Assert.That(recommendation.WithheldReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.DLZ_OUT));
        Assert.That(recommendation.PolicyConstraints.PolicyAllowsFire, Is.True);
        Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
    }

    [Test]
    public void Winchester_empty_mag_withholds_even_when_other_gates_pass()
    {
        var ctx = FeasibleContext with { RoundsRemaining = 0 };
        var input = BaseInput(EffectivePolicy.DefaultFree, ctx);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByEngage));
        Assert.That(recommendation.WithheldReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE));
        Assert.That(recommendation.PolicyConstraints.PolicyAllowsFire, Is.True);
        Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
        Assert.That(recommendation.IsFireOrder, Is.False);
        Assert.That(recommendation.IsAutomaticEngagement, Is.False);
    }

    [Test]
    public void Below_salvo_mag_withholds_with_no_ammo_even_when_other_gates_pass()
    {
        var ctx = FeasibleContext with { RoundsRemaining = 1, SalvoSize = 2 };
        var input = BaseInput(EffectivePolicy.DefaultFree, ctx);
        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.WithheldByEngage));
        Assert.That(recommendation.WithheldReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.NO_AMMO));
        Assert.That(recommendation.PolicyConstraints.PolicyAllowsFire, Is.True);
        Assert.That(recommendation.IsWeaponsReleaseAuthorization, Is.False);
    }

    [Test]
    public void Different_tuning_changes_confidence_for_feasible_recommendation()
    {
        var baselineInput = BaseInput(EffectivePolicy.DefaultFree, FeasibleContext);
        var highConfidenceTuning = ThreatAssessmentTuning.Default with { DlzInZoneConfidence = 0.99 };
        var highInput = baselineInput with { Tuning = highConfidenceTuning };

        var baseline = ThreatAssessmentProjection.Project(baselineInput);
        var high = ThreatAssessmentProjection.Project(highInput);

        Assert.That(baseline.Outcome, Is.EqualTo(WeaponRecommendationOutcome.Feasible));
        Assert.That(high.Outcome, Is.EqualTo(WeaponRecommendationOutcome.Feasible));
        Assert.That(high.Confidence, Is.GreaterThan(baseline.Confidence));
    }

    [Test]
    public void Fingerprint_changes_when_auto_engage_expend_or_dlz_label_differ()
    {
        var baseline = ThreatAssessmentProjection.Project(BaseInput(EffectivePolicy.DefaultFree, FeasibleContext));
        var baselineFingerprint = ThreatAssessmentProjection.ComputeFingerprint(baseline);

        var autoEngageVariant = baseline with
        {
            PolicyConstraints = baseline.PolicyConstraints with { AutoEngageAuthorized = false },
        };
        var expendVariant = baseline with
        {
            PolicyConstraints = baseline.PolicyConstraints with { ExpendAuthorized = true },
        };
        var dlzLabelVariant = baseline with
        {
            Range = baseline.Range with { DlzLabel = "DLZ: Approaching (Normal)" },
        };

        Assert.That(
            ThreatAssessmentProjection.ComputeFingerprint(autoEngageVariant),
            Is.Not.EqualTo(baselineFingerprint));
        Assert.That(
            ThreatAssessmentProjection.ComputeFingerprint(expendVariant),
            Is.Not.EqualTo(baselineFingerprint));
        Assert.That(
            ThreatAssessmentProjection.ComputeFingerprint(dlzLabelVariant),
            Is.Not.EqualTo(baselineFingerprint));
    }

    [Test]
    public void Same_inputs_yield_same_fingerprint()
    {
        var input = BaseInput(EffectivePolicy.DefaultFree, FeasibleContext);
        var a = ThreatAssessmentProjection.Project(input);
        var b = ThreatAssessmentProjection.Project(input);

        Assert.That(
            ThreatAssessmentProjection.ComputeFingerprint(a),
            Is.EqualTo(ThreatAssessmentProjection.ComputeFingerprint(b)));
    }

    [Test]
    public void Empty_input_returns_empty_recommendation()
    {
        var input = new ThreatAssessmentInput(
            ContactId: string.Empty,
            TargetId: "1001",
            ShooterUnitId: "2001",
            WeaponId: "sm-2",
            WeaponLabel: "SM-2",
            EngageContext: FeasibleContext,
            Policy: EffectivePolicy.DefaultFree);

        var recommendation = ThreatAssessmentProjection.Project(input);

        Assert.That(recommendation.Outcome, Is.EqualTo(WeaponRecommendationOutcome.NoRecommendation));
        Assert.That(ThreatAssessmentProjection.ComputeFingerprint(recommendation), Is.EqualTo("ta:empty"));
    }
}
