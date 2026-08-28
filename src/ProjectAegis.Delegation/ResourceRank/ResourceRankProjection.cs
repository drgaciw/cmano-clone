namespace ProjectAegis.Delegation.ResourceRank;

using System.Globalization;
using System.Text;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-217: headless resource ranking under scarcity for eligible shooters/weapons.
/// Consumes engage preview and policy facts only — never issues fire or enqueues orders.
/// </summary>
public static class ResourceRankProjection
{
    private const double EffectWeight = 0.35;
    private const double TimeWeight = 0.20;
    private const double AvailabilityWeight = 0.20;
    private const double CommitmentWeight = 0.15;
    private const double ConservationWeight = 0.10;

    private static readonly PolicyEvaluator PolicyEvaluator = new();

    /// <summary>Projects an advisory ranked list from one or more shooter/weapon candidates.</summary>
    public static ResourceRankSnapshot Project(IReadOnlyList<ResourceRankCandidateInput> candidates)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return ResourceRankSnapshot.Empty;
        }

        var contactId = candidates[0].ContactId;
        var targetId = candidates[0].TargetId;
        if (string.IsNullOrWhiteSpace(contactId) || string.IsNullOrWhiteSpace(targetId))
        {
            return ResourceRankSnapshot.Empty;
        }

        var evaluated = new List<EvaluatedCandidate>(candidates.Count);
        foreach (var candidate in candidates)
        {
            evaluated.Add(EvaluateCandidate(candidate));
        }

        var eligible = evaluated
            .Where(static e => e.Disposition != ResourceRankDisposition.Excluded)
            .OrderByDescending(static e => e.Scores.Total)
            .ThenBy(static e => e.Input.ShooterUnitId, StringComparer.Ordinal)
            .ThenBy(static e => e.Input.WeaponId, StringComparer.Ordinal)
            .ToList();

        var excluded = evaluated
            .Where(static e => e.Disposition == ResourceRankDisposition.Excluded)
            .OrderBy(static e => e.Input.ShooterUnitId, StringComparer.Ordinal)
            .ThenBy(static e => e.Input.WeaponId, StringComparer.Ordinal)
            .ToList();

        var ranked = new List<ResourceRankRankedCandidate>(evaluated.Count);
        ResourceRankScores? bestScores = eligible.Count > 0 ? eligible[0].Scores : null;

        for (var i = 0; i < eligible.Count; i++)
        {
            var item = eligible[i];
            var rank = i + 1;
            var disposition = rank == 1
                ? ResourceRankDisposition.Preferred
                : ResourceRankDisposition.Alternative;

            string? reasonCode = null;
            var reasonPlain = rank == 1
                ? "Preferred shooter/weapon under current scarcity constraints."
                : ExplainNotPreferred(item.Scores, bestScores!, out reasonCode);

            ranked.Add(BuildRankedCandidate(
                item.Input,
                disposition,
                rank,
                item.Scores,
                reasonCode,
                reasonPlain));
        }

        foreach (var item in excluded)
        {
            ranked.Add(BuildRankedCandidate(
                item.Input,
                ResourceRankDisposition.Excluded,
                rank: 0,
                item.Scores,
                item.ReasonCode,
                item.ReasonPlain));
        }

        var statusLine = BuildSnapshotStatusLine(ranked);
        return new ResourceRankSnapshot(
            contactId,
            targetId,
            ResourceRankKind.AdvisoryRanking,
            IsWeaponsReleaseAuthorization: false,
            IsFireOrder: false,
            IsAutomaticEngagement: false,
            ranked,
            statusLine);
    }

    /// <summary>Replay-stable canonical form: same inputs yield the same string. Invariant culture; no wall clock.</summary>
    public static string ComputeFingerprint(ResourceRankSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.RankedCandidates.Count == 0)
        {
            return "rr:empty";
        }

        var builder = new StringBuilder();
        builder.Append("rr:");
        builder.Append(snapshot.ContactId);
        builder.Append('|');
        builder.Append(snapshot.TargetId);
        builder.Append('|');
        builder.Append((int)snapshot.Kind);
        builder.Append('|');
        builder.Append(snapshot.IsWeaponsReleaseAuthorization ? '1' : '0');
        builder.Append(snapshot.IsFireOrder ? '1' : '0');
        builder.Append(snapshot.IsAutomaticEngagement ? '1' : '0');
        builder.Append('|');
        builder.Append(snapshot.StatusLine);
        builder.Append('|');

        foreach (var candidate in snapshot.RankedCandidates)
        {
            builder.Append(candidate.ShooterUnitId);
            builder.Append(':');
            builder.Append(candidate.WeaponId);
            builder.Append(':');
            builder.Append((int)candidate.Disposition);
            builder.Append(':');
            builder.Append(candidate.Rank);
            builder.Append(':');
            builder.Append(FormatDouble(candidate.Scores.Total));
            builder.Append(':');
            builder.Append(candidate.ReasonCode ?? string.Empty);
            builder.Append(';');
        }

        return builder.ToString();
    }

    private static EvaluatedCandidate EvaluateCandidate(ResourceRankCandidateInput input)
    {
        var ctx = input.EngageContext;
        var availability = input.Availability ?? ResourceRankAvailabilityFacts.None;
        var salvo = Math.Max(1, ctx.SalvoSize);
        var roundsAvailable = Math.Max(0, ctx.RoundsRemaining - availability.RoundsCommittedElsewhere);

        if (!availability.MountAvailable)
        {
            return Excluded(
                input,
                ResourceRankReasonCode.ExcludedByAvailability,
                "Mount is not available for this engagement window.",
                ResourceRankScores.Zero);
        }

        if (availability.RoundsCommittedElsewhere > 0 &&
            availability.RoundsCommittedElsewhere + salvo > ctx.RoundsRemaining)
        {
            return Excluded(
                input,
                ResourceRankReasonCode.ExcludedByCommitment,
                $"Rounds committed elsewhere ({availability.RoundsCommittedElsewhere}) leave insufficient magazine for salvo {salvo}.",
                ResourceRankScores.Zero);
        }

        if (roundsAvailable < salvo)
        {
            return Excluded(
                input,
                ResourceRankReasonCode.ExcludedByAvailability,
                $"Insufficient rounds available ({roundsAvailable}) for salvo {salvo}.",
                ResourceRankScores.Zero);
        }

        var preview = EngagePreviewProjection.Project(in ctx, ctx.DlzPersonality);
        var policyConstraints = EvaluatePolicyConstraints(in input, in ctx);
        if (!policyConstraints.PolicyAllowsFire)
        {
            return Excluded(
                input,
                ResourceRankReasonCode.ExcludedByPolicy,
                $"Policy withholds fire ({policyConstraints.PolicyAbortCode ?? "ROE"}).",
                ResourceRankScores.Zero);
        }

        if (!preview.CanFire)
        {
            return Excluded(
                input,
                ResourceRankReasonCode.ExcludedByEngage,
                $"Engage gate blocked ({preview.AbortPreviewCode ?? "BLOCKED"}).",
                ResourceRankScores.Zero);
        }

        var scores = ComputeScores(in input, in ctx, availability, roundsAvailable, salvo);
        return new EvaluatedCandidate(input, ResourceRankDisposition.Preferred, scores, null, string.Empty);
    }

    private static ResourceRankScores ComputeScores(
        in ResourceRankCandidateInput input,
        in EngageContext ctx,
        ResourceRankAvailabilityFacts availability,
        int roundsAvailable,
        int salvo)
    {
        var dlzState = DlzEngageGate.EvaluateState(ctx.RangeMeters, ctx.Envelope);
        var inEnvelope = ctx.Envelope.Contains(ctx.RangeMeters);

        var effect = Math.Clamp(ctx.PkBase * (inEnvelope ? 1.0 : 0.55), 0, 1);
        var time = ScoreTime(dlzState, availability.TimeToEffectSeconds);
        var availabilityScore = Math.Clamp((double)roundsAvailable / Math.Max(1, salvo * 2), 0, 1);
        var commitment = availability.RoundsCommittedElsewhere == 0
            ? 1.0
            : Math.Clamp(1.0 - ((double)availability.RoundsCommittedElsewhere / Math.Max(1, ctx.RoundsRemaining)), 0, 1);
        var conservation = ScoreConservation(in ctx);

        var total = (effect * EffectWeight)
                    + (time * TimeWeight)
                    + (availabilityScore * AvailabilityWeight)
                    + (commitment * CommitmentWeight)
                    + (conservation * ConservationWeight);

        return new ResourceRankScores(effect, time, availabilityScore, commitment, conservation, total);
    }

    private static double ScoreTime(DlzState dlzState, double timeToEffectSeconds)
    {
        var dlzScore = dlzState switch
        {
            DlzState.InZone => 1.0,
            DlzState.Approaching => 0.6,
            DlzState.OutOfZone => 0.2,
            _ => 0.1,
        };

        if (timeToEffectSeconds <= 0)
        {
            return dlzScore;
        }

        var timeScore = 1.0 / (1.0 + (timeToEffectSeconds / 120.0));
        return Math.Clamp((dlzScore * 0.7) + (timeScore * 0.3), 0, 1);
    }

    private static double ScoreConservation(in EngageContext ctx)
    {
        if (ctx.RoundsRemaining <= 0)
        {
            return 0;
        }

        var threshold = Math.Max(1, ctx.ShotgunRoundsThreshold);
        if (ctx.RoundsRemaining > threshold)
        {
            return 1.0;
        }

        var scarcity = 1.0 - ((double)ctx.RoundsRemaining / threshold);
        var techPenalty = Math.Clamp(ctx.WeaponTechnologyLevel * 0.08, 0, 0.5);
        return Math.Clamp(1.0 - (scarcity * 0.5) - techPenalty, 0, 1);
    }

    private static string ExplainNotPreferred(
        ResourceRankScores candidate,
        ResourceRankScores best,
        out string? reasonCode)
    {
        if (candidate.ExpectedEffect + 0.05 < best.ExpectedEffect)
        {
            reasonCode = ResourceRankReasonCode.NotPreferredLowerEffect;
            return "Lower expected effect than the preferred alternative.";
        }

        if (candidate.Time + 0.05 < best.Time)
        {
            reasonCode = ResourceRankReasonCode.NotPreferredTime;
            return "Slower time-to-effect than the preferred alternative.";
        }

        if (candidate.Conservation + 0.05 < best.Conservation)
        {
            reasonCode = ResourceRankReasonCode.NotPreferredConservation;
            return "Less favorable magazine conservation than the preferred alternative.";
        }

        reasonCode = ResourceRankReasonCode.NotPreferredLowerEffect;
        return "Lower composite scarcity score than the preferred alternative.";
    }

    private static ResourceRankRankedCandidate BuildRankedCandidate(
        ResourceRankCandidateInput input,
        ResourceRankDisposition disposition,
        int rank,
        ResourceRankScores scores,
        string? reasonCode,
        string reasonPlain)
    {
        var statusLine = disposition switch
        {
            ResourceRankDisposition.Preferred =>
                $"RANK #{rank}: PREFER {input.WeaponLabel} on {input.ShooterUnitId} (advisory — not weapons release)",
            ResourceRankDisposition.Alternative =>
                $"RANK #{rank}: ALT {input.WeaponLabel} on {input.ShooterUnitId} — {reasonCode} (advisory)",
            _ =>
                $"RANK: EXCLUDED {input.WeaponLabel} on {input.ShooterUnitId} — {reasonCode}",
        };

        return new ResourceRankRankedCandidate(
            input.ContactId,
            input.TargetId,
            input.ShooterUnitId,
            input.WeaponId,
            input.WeaponLabel,
            input.Posture,
            disposition,
            rank,
            scores,
            reasonCode,
            reasonPlain,
            statusLine);
    }

    private static string BuildSnapshotStatusLine(IReadOnlyList<ResourceRankRankedCandidate> ranked)
    {
        var preferred = ranked.FirstOrDefault(c => c.Disposition == ResourceRankDisposition.Preferred);
        if (preferred is null)
        {
            return "RANK: NO ELIGIBLE SHOOTERS (advisory — not weapons release)";
        }

        var alternativeCount = ranked.Count(c => c.Disposition == ResourceRankDisposition.Alternative);
        return alternativeCount == 0
            ? $"RANK: PREFER {preferred.WeaponLabel} (advisory — not weapons release)"
            : $"RANK: PREFER {preferred.WeaponLabel} +{alternativeCount} ALT (advisory — not weapons release)";
    }

    private static EvaluatedCandidate Excluded(
        ResourceRankCandidateInput input,
        string reasonCode,
        string reasonPlain,
        ResourceRankScores scores) =>
        new(input, ResourceRankDisposition.Excluded, scores, reasonCode, reasonPlain);

    private static ThreatPolicyConstraints EvaluatePolicyConstraints(
        in ResourceRankCandidateInput input,
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

    private static string FormatDouble(double value) =>
        (value == 0 ? 0.0 : value).ToString("0.######", CultureInfo.InvariantCulture);

    private sealed record ThreatPolicyConstraints(
        RoeLevel RoeLevel,
        int MaxSalvo,
        bool AutoEngageAuthorized,
        bool ExpendAuthorized,
        bool PolicyAllowsFire,
        string? PolicyAbortCode);

    private sealed record EvaluatedCandidate(
        ResourceRankCandidateInput Input,
        ResourceRankDisposition Disposition,
        ResourceRankScores Scores,
        string? ReasonCode,
        string ReasonPlain);
}
