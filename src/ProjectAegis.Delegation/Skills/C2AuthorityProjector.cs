namespace ProjectAegis.Delegation.Skills;

using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-209 headless projector: maps skill lane, required approval, and ROE snapshot
/// into explicit authority and targeting dispositions for DRG-182.
/// Pure — does not touch DelegationBridge, SimulationSession, or the order log.
/// </summary>
public static class C2AuthorityProjector
{
    public const string ReasonWeaponsTight = nameof(FireAbortReason.WeaponsTight);
    public const string ReasonRoeHoldFire = nameof(FireAbortReason.RoeHoldFire);
    public const string ReasonNoFireControl = "NO_FIRE_CONTROL";
    public const string ReasonSharedTrackNoRelease = SkillEnvelopeValidator.ReasonSharedTrackNoRelease;
    public const string ReasonWeaponsReleaseRequired = SkillEnvelopeValidator.ReasonWeaponsReleaseRequired;
    public const string ReasonApprovalRequired = SkillEnvelopeValidator.ReasonApprovalRequired;
    public const string ReasonNotHumanControlled = "NOT_HUMAN_CONTROL";
    public const string ReasonLaneSubmit = "LANE_SUBMIT_NO_RECOMMEND";

    /// <summary>Project authority for the supplied context.</summary>
    public static C2AuthorityProjection Project(in C2AuthorityProjectionContext ctx)
    {
        var roe = ProjectRoe(in ctx);
        var targeting = ProjectTargeting(in ctx, roe);
        var actions = new[]
        {
            ProjectObserve(in ctx),
            ProjectRecommend(in ctx),
            ProjectApprove(in ctx, targeting),
            ProjectEngage(in ctx, roe, targeting),
            ProjectAbort(in ctx),
            ProjectRetask(in ctx, targeting),
        };

        return new C2AuthorityProjection(roe, targeting, actions);
    }

    /// <summary>Parse doctrine labels from skill envelopes or policy snapshots.</summary>
    public static RoeLevel ParseRoeLabel(string? roeLabel)
    {
        if (string.IsNullOrWhiteSpace(roeLabel))
        {
            return RoeLevel.WeaponsFree;
        }

        var normalized = roeLabel.Trim().Replace("_", " ", StringComparison.Ordinal);
        if (Enum.TryParse<RoeLevel>(normalized, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        if (normalized.Contains("HOLD", StringComparison.OrdinalIgnoreCase))
        {
            return RoeLevel.HoldFire;
        }

        if (normalized.Contains("TIGHT", StringComparison.OrdinalIgnoreCase))
        {
            return RoeLevel.WeaponsTight;
        }

        if (normalized.Contains("FREE", StringComparison.OrdinalIgnoreCase))
        {
            return RoeLevel.WeaponsFree;
        }

        return RoeLevel.WeaponsFree;
    }

    private static RoeProjection ProjectRoe(in C2AuthorityProjectionContext ctx)
    {
        var engageAllowed = ctx.Roe == RoeLevel.WeaponsFree;
        var disposition = engageAllowed
            ? C2AuthorityDisposition.Permitted
            : C2AuthorityDisposition.Withheld;
        var reason = ctx.Roe switch
        {
            RoeLevel.HoldFire => ReasonRoeHoldFire,
            RoeLevel.WeaponsTight => ReasonWeaponsTight,
            _ => null,
        };

        return new RoeProjection(
            ctx.Roe,
            RoeProjection.FormatRoeLabel(ctx.Roe),
            disposition,
            reason,
            engageAllowed);
    }

    private static C2TargetingAuthority ProjectTargeting(
        in C2AuthorityProjectionContext ctx,
        RoeProjection roe)
    {
        if (IsSharedSa(ctx.TrackSource))
        {
            return new C2TargetingAuthority(
                C2AuthorityDisposition.Withheld,
                ReasonSharedTrackNoRelease,
                PendingApproval: null);
        }

        if (!ctx.FireControlSatisfied && IsEngageContext(ctx))
        {
            return new C2TargetingAuthority(
                C2AuthorityDisposition.Withheld,
                ReasonNoFireControl,
                PendingApproval: null);
        }

        if (!roe.EngageAllowedByRoe)
        {
            return new C2TargetingAuthority(
                C2AuthorityDisposition.Withheld,
                roe.TargetingReasonCode,
                PendingApproval: null);
        }

        var pendingApproval = ResolvePendingApproval(in ctx);
        if (pendingApproval is not null)
        {
            var reason = pendingApproval == RequiredApproval.WeaponsRelease
                ? ReasonWeaponsReleaseRequired
                : ReasonApprovalRequired;
            return new C2TargetingAuthority(
                C2AuthorityDisposition.ApprovalRequired,
                reason,
                pendingApproval);
        }

        return new C2TargetingAuthority(
            C2AuthorityDisposition.Permitted,
            ReasonCode: null,
            PendingApproval: null);
    }

    private static C2AuthorityActionState ProjectObserve(in C2AuthorityProjectionContext ctx) =>
        new(C2AuthorityActionKind.Observe, C2AuthorityDisposition.Permitted, ReasonCode: null);

    private static C2AuthorityActionState ProjectRecommend(in C2AuthorityProjectionContext ctx) =>
        ctx.Lane switch
        {
            SkillLane.Submit => new(
                C2AuthorityActionKind.Recommend,
                C2AuthorityDisposition.Withheld,
                ReasonLaneSubmit),
            _ => new(C2AuthorityActionKind.Recommend, C2AuthorityDisposition.Permitted, ReasonCode: null),
        };

    private static C2AuthorityActionState ProjectApprove(
        in C2AuthorityProjectionContext ctx,
        C2TargetingAuthority targeting)
    {
        if (string.IsNullOrEmpty(ctx.CommandId))
        {
            return new(C2AuthorityActionKind.Approve, C2AuthorityDisposition.Withheld, ReasonApprovalRequired);
        }

        if (targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
        {
            return new(
                C2AuthorityActionKind.Approve,
                C2AuthorityDisposition.ApprovalRequired,
                targeting.ReasonCode);
        }

        if (targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return new(
                C2AuthorityActionKind.Approve,
                C2AuthorityDisposition.Withheld,
                targeting.ReasonCode);
        }

        return new(C2AuthorityActionKind.Approve, C2AuthorityDisposition.Permitted, ReasonCode: null);
    }

    private static C2AuthorityActionState ProjectEngage(
        in C2AuthorityProjectionContext ctx,
        RoeProjection roe,
        C2TargetingAuthority targeting)
    {
        if (targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return new(C2AuthorityActionKind.Engage, C2AuthorityDisposition.Withheld, targeting.ReasonCode);
        }

        if (targeting.Disposition == C2AuthorityDisposition.ApprovalRequired)
        {
            return new(
                C2AuthorityActionKind.Engage,
                C2AuthorityDisposition.ApprovalRequired,
                targeting.ReasonCode);
        }

        if (roe.EngageAllowedByRoe
            && ctx.TrackSource == TrackSource.Organic
            && ctx.FireControlSatisfied)
        {
            return new(C2AuthorityActionKind.Engage, C2AuthorityDisposition.Permitted, ReasonCode: null);
        }

        return new(C2AuthorityActionKind.Engage, C2AuthorityDisposition.Withheld, ReasonNoFireControl);
    }

    private static C2AuthorityActionState ProjectAbort(in C2AuthorityProjectionContext ctx) =>
        ctx.HumanControlled
            ? new(C2AuthorityActionKind.Abort, C2AuthorityDisposition.Permitted, ReasonCode: null)
            : new(C2AuthorityActionKind.Abort, C2AuthorityDisposition.Withheld, ReasonNotHumanControlled);

    private static C2AuthorityActionState ProjectRetask(
        in C2AuthorityProjectionContext ctx,
        C2TargetingAuthority targeting)
    {
        if (!ctx.HumanControlled)
        {
            return new(C2AuthorityActionKind.Retask, C2AuthorityDisposition.Withheld, ReasonNotHumanControlled);
        }

        if (string.IsNullOrEmpty(ctx.CommandId) || !IsEngage(ctx.CommandId))
        {
            var pending = ResolvePendingApproval(in ctx);
            if (pending == RequiredApproval.Operator)
            {
                return new(
                    C2AuthorityActionKind.Retask,
                    C2AuthorityDisposition.ApprovalRequired,
                    ReasonApprovalRequired);
            }

            return new(C2AuthorityActionKind.Retask, C2AuthorityDisposition.Permitted, ReasonCode: null);
        }

        if (targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return new(C2AuthorityActionKind.Retask, C2AuthorityDisposition.Withheld, targeting.ReasonCode);
        }

        return new(C2AuthorityActionKind.Retask, C2AuthorityDisposition.Permitted, ReasonCode: null);
    }

    private static RequiredApproval? ResolvePendingApproval(in C2AuthorityProjectionContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.CommandId))
        {
            return null;
        }

        if (IsEngage(ctx.CommandId))
        {
            return RequiredApproval.WeaponsRelease;
        }

        if (ctx.RequiredApproval is RequiredApproval.Operator or RequiredApproval.WeaponsRelease)
        {
            return ctx.RequiredApproval;
        }

        if (ctx.Lane is SkillLane.Propose or SkillLane.Submit)
        {
            return RequiredApproval.Operator;
        }

        return null;
    }

    private static bool IsEngageContext(in C2AuthorityProjectionContext ctx) =>
        IsEngage(ctx.CommandId) || ctx.Lane is SkillLane.Propose or SkillLane.Submit;

    private static bool IsEngage(string? commandId) =>
        string.Equals(commandId?.Trim(), "engage", StringComparison.OrdinalIgnoreCase);

    private static bool IsSharedSa(TrackSource source) =>
        source is TrackSource.DatalinkShared or TrackSource.FusedWithoutOrganicFc;
}
