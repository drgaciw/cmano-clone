namespace ProjectAegis.Delegation.Skills;

using ProjectAegis.Delegation.Input;

/// <summary>
/// DRG-196 contract gate. Pure validation — does not call
/// <c>C2PlayerCommandBridge.TryIssue</c>, does not append to the order log.
/// Submit still uses <see cref="C2CommandIssuance.TryResolve"/> for command ids.
/// </summary>
public static class SkillEnvelopeValidator
{
    public const string ReasonReadMustNotCommand = "READ_MUST_NOT_COMMAND";
    public const string ReasonAuthorizationImplied = "ENGAGEMENT_AUTHORIZATION_IMPLIED";
    public const string ReasonSharedTrackNoRelease = "SHARED_TRACK_NO_RELEASE";
    public const string ReasonProposalNotApproved = "PROPOSAL_NOT_APPROVED";
    public const string ReasonLaneNotAllowed = "LANE_NOT_ALLOWED";
    public const string ReasonUnknownSkill = "UNKNOWN_SKILL";
    public const string ReasonMissingAuthority = "MISSING_AUTHORITY";
    public const string ReasonMissingOverride = "MISSING_OVERRIDE";
    public const string ReasonMissingProposal = "MISSING_PROPOSAL";
    public const string ReasonMissingTtl = "MISSING_TTL";
    public const string ReasonNoFireControl = "NO_FIRE_CONTROL";
    public const string ReasonWeaponsReleaseRequired = "WEAPONS_RELEASE_REQUIRED";
    public const string ReasonCommandNotAllowed = "COMMAND_NOT_ALLOWED";
    public const string ReasonApprovalRequired = "APPROVAL_REQUIRED";

    /// <summary>
    /// Validate <paramref name="envelope"/>. Pass <paramref name="proposalApproved"/> only for
    /// <see cref="SkillLane.Submit"/>. Never mutates sim or log state.
    /// </summary>
    public static SkillEnvelopeValidation Validate(SkillEnvelope envelope, bool proposalApproved = false)
    {
        if (envelope is null)
        {
            throw new ArgumentNullException(nameof(envelope));
        }

        if (string.Equals(envelope.SkillId, SkillIds.Submit, StringComparison.Ordinal))
        {
            return ValidateSubmit(envelope, proposalApproved);
        }

        if (!SkillCatalog.TryGet(envelope.SkillId, out var descriptor))
        {
            return Fail(ReasonUnknownSkill);
        }

        if (!ContainsLane(descriptor, envelope.Lane))
        {
            return Fail(ReasonLaneNotAllowed);
        }

        return envelope.Lane switch
        {
            SkillLane.Read => ValidateRead(envelope),
            SkillLane.Propose => ValidatePropose(envelope, descriptor),
            SkillLane.Submit => Fail(ReasonLaneNotAllowed),
            _ => Fail(ReasonLaneNotAllowed),
        };
    }

    private static SkillEnvelopeValidation ValidateRead(SkillEnvelope envelope)
    {
        if (!string.IsNullOrEmpty(envelope.CommandId))
        {
            return Fail(ReasonReadMustNotCommand);
        }

        return Ok();
    }

    private static SkillEnvelopeValidation ValidatePropose(SkillEnvelope envelope, SkillDescriptor descriptor)
    {
        if (envelope.AuthorityBasis is null)
        {
            return Fail(ReasonMissingAuthority);
        }

        if (envelope.AuthorityBasis.EngagementAuthorizationImplied)
        {
            return Fail(ReasonAuthorizationImplied);
        }

        if (envelope.PlayerOverride is null)
        {
            return Fail(ReasonMissingOverride);
        }

        if (string.IsNullOrWhiteSpace(envelope.ProposalId))
        {
            return Fail(ReasonMissingProposal);
        }

        if (envelope.TtlTicks is null || envelope.CreatedSimTick is null)
        {
            return Fail(ReasonMissingTtl);
        }

        if (!string.IsNullOrEmpty(envelope.CommandId))
        {
            if (!C2CommandIssuance.TryResolve(envelope.CommandId, out _, out var reason))
            {
                return Fail(reason ?? C2CommandIssuance.ReasonUnknownCommand);
            }

            if (!IsCommandAllowed(descriptor, envelope.CommandId))
            {
                return Fail(ReasonCommandNotAllowed);
            }

            var engageFailure = ValidateEngageCommand(envelope);
            if (engageFailure is not null)
            {
                return engageFailure;
            }

            var approvalFailure = ValidateRequiredApproval(envelope);
            if (approvalFailure is not null)
            {
                return approvalFailure;
            }
        }

        return Ok();
    }

    private static SkillEnvelopeValidation ValidateSubmit(SkillEnvelope envelope, bool proposalApproved)
    {
        if (envelope.Lane != SkillLane.Submit)
        {
            return Fail(ReasonLaneNotAllowed);
        }

        if (string.IsNullOrWhiteSpace(envelope.ProposalId))
        {
            return Fail(ReasonMissingProposal);
        }

        if (!proposalApproved)
        {
            return Fail(ReasonProposalNotApproved);
        }

        if (envelope.AuthorityBasis is null)
        {
            return Fail(ReasonMissingAuthority);
        }

        if (envelope.PlayerOverride is null)
        {
            return Fail(ReasonMissingOverride);
        }

        if (!C2CommandIssuance.TryResolve(envelope.CommandId, out _, out var reason))
        {
            return Fail(reason ?? C2CommandIssuance.ReasonUnknownCommand);
        }

        var engageFailure = ValidateEngageCommand(envelope);
        if (engageFailure is not null)
        {
            return engageFailure;
        }

        var approvalFailure = ValidateRequiredApproval(envelope);
        if (approvalFailure is not null)
        {
            return approvalFailure;
        }

        return Ok(envelope.CommandId);
    }

    private static bool ContainsLane(SkillDescriptor descriptor, SkillLane lane)
    {
        foreach (var item in descriptor.Lanes)
        {
            if (item == lane)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCommandAllowed(SkillDescriptor descriptor, string commandId)
    {
        foreach (var allowed in descriptor.CommandIds)
        {
            if (string.Equals(allowed, commandId.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static SkillEnvelopeValidation? ValidateEngageCommand(SkillEnvelope envelope)
    {
        if (!IsEngage(envelope.CommandId))
        {
            return null;
        }

        var authority = envelope.AuthorityBasis!;
        if (IsSharedSa(authority.TrackSource))
        {
            return Fail(ReasonSharedTrackNoRelease);
        }

        if (authority.TrackSource != TrackSource.Organic || !authority.FireControlSatisfied)
        {
            return Fail(ReasonNoFireControl);
        }

        return null;
    }

    private static SkillEnvelopeValidation? ValidateRequiredApproval(SkillEnvelope envelope)
    {
        if (string.IsNullOrEmpty(envelope.CommandId))
        {
            return null;
        }

        var authority = envelope.AuthorityBasis!;
        if (IsEngage(envelope.CommandId))
        {
            if (envelope.RequiredApproval != RequiredApproval.WeaponsRelease)
            {
                return Fail(ReasonWeaponsReleaseRequired);
            }

            return null;
        }

        if (envelope.RequiredApproval == RequiredApproval.None)
        {
            return Fail(ReasonApprovalRequired);
        }

        if (IsSharedSa(authority.TrackSource)
            && envelope.RequiredApproval == RequiredApproval.WeaponsRelease)
        {
            return Fail(ReasonSharedTrackNoRelease);
        }

        return null;
    }

    private static bool IsEngage(string? commandId) =>
        string.Equals(commandId?.Trim(), "engage", StringComparison.OrdinalIgnoreCase);

    private static bool IsSharedSa(TrackSource source) =>
        source is TrackSource.DatalinkShared or TrackSource.FusedWithoutOrganicFc;

    private static SkillEnvelopeValidation Ok(string? resolved = null) =>
        new(true, null, resolved);

    private static SkillEnvelopeValidation Fail(string reason) =>
        new(false, reason, null);
}
