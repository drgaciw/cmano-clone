namespace ProjectAegis.Delegation.ThreatAssessment;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-212: headless threat assessment + weapon recommendation projector.
/// Consumes engage preview and policy facts only — never issues fire or enqueues orders.
/// </summary>
public static class ThreatAssessmentProjection
{
    private static readonly PolicyEvaluator PolicyEvaluator = new();

    /// <summary>Projects an advisory weapon recommendation from engage and policy facts.</summary>
    public static WeaponRecommendation Project(in ThreatAssessmentInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ContactId) ||
            string.IsNullOrWhiteSpace(input.TargetId) ||
            string.IsNullOrWhiteSpace(input.WeaponId))
        {
            return WeaponRecommendation.Empty;
        }

        var ctx = input.EngageContext;
        var preview = EngagePreviewProjection.Project(in ctx, ctx.DlzPersonality);
        var range = BuildRangeAssessment(in ctx, preview);
        var policyConstraints = EvaluatePolicyConstraints(in input, in ctx);
        var tuning = input.ResolvedTuning;
        var (outcome, engageAbortCode) = ResolveOutcome(in ctx, preview, policyConstraints);
        var assumptions = BuildAssumptions(in input, in ctx, preview, policyConstraints, outcome, engageAbortCode);
        var withheldCode = ResolveWithheldReasonCode(outcome, engageAbortCode, policyConstraints);
        var confidence = ComputeConfidence(outcome, in ctx, engageAbortCode, policyConstraints, tuning);
        var statusLine = BuildStatusLine(outcome, input.WeaponLabel, withheldCode, preview);

        return new WeaponRecommendation(
            input.ContactId,
            input.TargetId,
            input.ShooterUnitId,
            input.WeaponId,
            input.WeaponLabel,
            input.Posture,
            outcome,
            ThreatRecommendationKind.AdvisoryRecommendation,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            IsAutomaticEngagement: false,
            confidence,
            assumptions,
            range,
            policyConstraints,
            withheldCode,
            statusLine);
    }

    /// <summary>Replay-stable canonical form: same input yields the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(WeaponRecommendation? recommendation)
    {
        if (recommendation is null || recommendation.Outcome == WeaponRecommendationOutcome.NoRecommendation)
        {
            return "ta:empty";
        }

        var builder = new StringBuilder();
        builder.Append("ta:");
        builder.Append(recommendation.ContactId);
        builder.Append('|');
        builder.Append(recommendation.TargetId);
        builder.Append('|');
        builder.Append(recommendation.ShooterUnitId);
        builder.Append('|');
        builder.Append(recommendation.WeaponId);
        builder.Append('|');
        builder.Append((int)recommendation.Posture);
        builder.Append('|');
        builder.Append((int)recommendation.Outcome);
        builder.Append('|');
        builder.Append((int)recommendation.RecommendationKind);
        builder.Append('|');
        builder.Append(recommendation.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(recommendation.IsFireOrder ? '1' : '0');
        builder.Append(recommendation.IsAutomaticEngagement ? '1' : '0');
        builder.Append('|');
        builder.Append(FormatDouble(recommendation.Confidence));
        builder.Append('|');
        builder.Append(FormatDouble(recommendation.Range.RangeMeters));
        builder.Append('|');
        builder.Append(FormatDouble(recommendation.Range.EnvelopeMinMeters));
        builder.Append('|');
        builder.Append(FormatDouble(recommendation.Range.EnvelopeMaxMeters));
        builder.Append('|');
        builder.Append((int)recommendation.Range.DlzState);
        builder.Append('|');
        builder.Append(recommendation.Range.InEnvelope ? '1' : '0');
        builder.Append('|');
        builder.Append((int)recommendation.PolicyConstraints.RoeLevel);
        builder.Append('|');
        builder.Append(recommendation.PolicyConstraints.MaxSalvo);
        builder.Append('|');
        builder.Append(recommendation.PolicyConstraints.AutoEngageAuthorized ? '1' : '0');
        builder.Append('|');
        builder.Append(recommendation.PolicyConstraints.ExpendAuthorized ? '1' : '0');
        builder.Append('|');
        builder.Append(recommendation.PolicyConstraints.PolicyAllowsFire ? '1' : '0');
        builder.Append('|');
        builder.Append(recommendation.PolicyConstraints.PolicyAbortCode ?? string.Empty);
        builder.Append('|');
        builder.Append(recommendation.Range.DlzLabel);
        builder.Append('|');
        builder.Append(recommendation.WithheldReasonCode ?? string.Empty);
        builder.Append('|');
        builder.Append(recommendation.StatusLine);
        builder.Append('|');
        AppendJoined(builder, recommendation.Assumptions);
        return builder.ToString();
    }

    private static ThreatRangeAssessment BuildRangeAssessment(in EngageContext ctx, EngagePreview preview)
    {
        var dlzState = DlzEngageGate.EvaluateState(ctx.RangeMeters, ctx.Envelope);
        return new ThreatRangeAssessment(
            ctx.RangeMeters,
            ctx.Envelope.MinRangeMeters,
            ctx.Envelope.MaxRangeMeters,
            dlzState,
            preview.DlzLabel,
            ctx.Envelope.Contains(ctx.RangeMeters));
    }

    private static ThreatPolicyConstraints EvaluatePolicyConstraints(
        in ThreatAssessmentInput input,
        in EngageContext ctx)
    {
        var policyContext = new PolicyContext(
            UnitId: ParseUnitId(input.ShooterUnitId),
            PolicySnapshotId: 1,
            SimTick: 0,
            Effective: input.Policy,
            SalvoSize: Math.Max(1, ctx.SalvoSize));

        var request = new ActionRequest(
            ActionKind.FireGuided,
            TargetId: ParseUnitId(input.TargetId),
            MountId: 1);
        var verdict = PolicyEvaluator.Evaluate(in policyContext, in request);
        var abortCode = verdict.Allowed ? null : MapPolicyAbortCode(verdict.Reason);

        return new ThreatPolicyConstraints(
            input.Policy.Roe,
            input.Policy.MaxSalvo,
            input.Policy.AutoEngageAuthorized,
            input.Policy.ExpendAuthorized,
            verdict.Allowed,
            abortCode);
    }

    private static (WeaponRecommendationOutcome Outcome, string? EngageAbortCode) ResolveOutcome(
        in EngageContext ctx,
        EngagePreview preview,
        ThreatPolicyConstraints policyConstraints)
    {
        if (!policyConstraints.PolicyAllowsFire)
        {
            return (WeaponRecommendationOutcome.WithheldByPolicy, null);
        }

        if (ctx.RoundsRemaining <= 0)
        {
            return (WeaponRecommendationOutcome.WithheldByEngage, AbortReasonCatalog.Engage.WINCHESTER_ORDNANCE);
        }

        var salvoSize = Math.Max(1, ctx.SalvoSize);
        if (ctx.RoundsRemaining < salvoSize)
        {
            return (WeaponRecommendationOutcome.WithheldByEngage, AbortReasonCatalog.Engage.NO_AMMO);
        }

        if (!preview.CanFire)
        {
            return (WeaponRecommendationOutcome.WithheldByEngage, preview.AbortPreviewCode);
        }

        return (WeaponRecommendationOutcome.Feasible, null);
    }

    private static string? ResolveWithheldReasonCode(
        WeaponRecommendationOutcome outcome,
        string? engageAbortCode,
        ThreatPolicyConstraints policyConstraints) =>
        outcome switch
        {
            WeaponRecommendationOutcome.WithheldByPolicy => policyConstraints.PolicyAbortCode,
            WeaponRecommendationOutcome.WithheldByEngage => engageAbortCode,
            _ => null,
        };

    private static double ComputeConfidence(
        WeaponRecommendationOutcome outcome,
        in EngageContext ctx,
        string? engageAbortCode,
        ThreatPolicyConstraints policyConstraints,
        ThreatAssessmentTuning tuning)
    {
        if (outcome == WeaponRecommendationOutcome.WithheldByPolicy)
        {
            return tuning.WithheldByPolicyConfidence;
        }

        if (outcome == WeaponRecommendationOutcome.WithheldByEngage)
        {
            if (string.Equals(engageAbortCode, AbortReasonCatalog.Engage.DLZ_OUT, StringComparison.Ordinal))
            {
                return tuning.WithheldByEngageDlzOutConfidence;
            }

            if (string.Equals(engageAbortCode, AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK, StringComparison.Ordinal))
            {
                return tuning.WithheldByEngageNoFireControlConfidence;
            }

            return tuning.WithheldByEngageDefaultConfidence;
        }

        var dlzState = DlzEngageGate.EvaluateState(ctx.RangeMeters, ctx.Envelope);
        var baseConfidence = dlzState switch
        {
            DlzState.InZone => tuning.DlzInZoneConfidence,
            DlzState.Approaching => tuning.DlzApproachingConfidence,
            _ => tuning.DlzOutOfZoneConfidence,
        };

        if (ctx.RoundsRemaining <= Math.Max(1, ctx.ShotgunRoundsThreshold))
        {
            baseConfidence *= tuning.LowMagazineMultiplier;
        }

        if (!policyConstraints.AutoEngageAuthorized)
        {
            baseConfidence *= tuning.AutoEngageUnauthorizedMultiplier;
        }

        return Math.Clamp(baseConfidence, 0, 1);
    }

    private static IReadOnlyList<string> BuildAssumptions(
        in ThreatAssessmentInput input,
        in EngageContext ctx,
        EngagePreview preview,
        ThreatPolicyConstraints policyConstraints,
        WeaponRecommendationOutcome outcome,
        string? engageAbortCode)
    {
        var assumptions = new List<string>
        {
            "Advisory recommendation only — not weapons release authorization.",
            "Does not enqueue fire orders or trigger automatic engagement.",
            $"Assumes {input.Posture.ToString().ToLowerInvariant()} posture against contact {input.ContactId}.",
            $"Assumes weapon {input.WeaponLabel} on mount for shooter {input.ShooterUnitId}.",
        };

        if (ctx.HasFireControlTrack)
        {
            assumptions.Add("Assumes current fire-control track is held.");
        }
        else
        {
            assumptions.Add("Assumes fire-control track is not yet established.");
        }

        if (ctx.MountOnline)
        {
            assumptions.Add("Assumes weapon mount is online.");
        }

        if (ctx.RadarEmconActive)
        {
            assumptions.Add("Assumes radar EMCON permits fire-control emissions.");
        }

        if (preview.CanFire && policyConstraints.PolicyAllowsFire && outcome == WeaponRecommendationOutcome.Feasible)
        {
            assumptions.Add("Engage preview and ROE both permit launch if authorized separately.");
        }
        else if (!policyConstraints.PolicyAllowsFire)
        {
            assumptions.Add($"ROE {policyConstraints.RoeLevel} withholds weapons release.");
        }
        else if (outcome == WeaponRecommendationOutcome.WithheldByEngage)
        {
            assumptions.Add($"Engage gate blocked ({engageAbortCode ?? preview.AbortPreviewCode ?? "unknown"}).");
        }
        else
        {
            assumptions.Add($"Engage gate blocked ({preview.AbortPreviewCode ?? "unknown"}).");
        }

        return assumptions;
    }

    private static string BuildStatusLine(
        WeaponRecommendationOutcome outcome,
        string weaponLabel,
        string? withheldCode,
        EngagePreview preview)
    {
        return outcome switch
        {
            WeaponRecommendationOutcome.Feasible =>
                $"THREAT: RECOMMEND {weaponLabel} (advisory — not weapons release)",
            WeaponRecommendationOutcome.WithheldByPolicy =>
                $"THREAT: WITHHELD BY POLICY — {withheldCode ?? "ROE"} (recommend {weaponLabel} if ROE permits)",
            WeaponRecommendationOutcome.WithheldByEngage =>
                $"THREAT: WITHHELD BY ENGAGE — {withheldCode ?? preview.AbortPreviewCode ?? "BLOCKED"} (recommend {weaponLabel} when feasible)",
            _ => "THREAT: —",
        };
    }

    private static string? MapPolicyAbortCode(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.RoeHoldFire => AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE,
            FireAbortReason.WeaponsTight => AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT,
            FireAbortReason.WraRange => AbortReasonCatalog.Doctrine.WRA_RANGE,
            FireAbortReason.WraSalvo => AbortReasonCatalog.Doctrine.WRA_SALVO,
            FireAbortReason.EmconOff => AbortReasonCatalog.Doctrine.EMCON_OFF,
            FireAbortReason.NoFireControlTrack => AbortReasonCatalog.Doctrine.NO_FIRE_CONTROL_TRACK,
            FireAbortReason.CommsDenied => AbortReasonCatalog.Doctrine.COMMS_DENIED,
            FireAbortReason.AutoEngageDenied => AbortReasonCatalog.Doctrine.AUTO_ENGAGE_DENIED,
            FireAbortReason.ExpendUnauthorized => AbortReasonCatalog.Doctrine.EXPEND_UNAUTHORIZED,
            _ => reason.ToString(),
        };

    private static ulong ParseUnitId(string value) =>
        ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;

    private static void AppendJoined(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(';');
            }

            builder.Append(values[i]);
        }
    }

    private static string FormatDouble(double value) =>
        (value == 0 ? 0.0 : value).ToString("0.######", CultureInfo.InvariantCulture);
}
