namespace ProjectAegis.Delegation.Tests.Skills;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Skills;
using NUnit.Framework;

/// <summary>
/// DRG-196 / AGC-02..04: projection reads vs bounded proposals vs approved submit.
/// Validator is contract-only — it must not append to the order log.
/// </summary>
[TestFixture]
public sealed class SkillEnvelopeValidatorTests
{
    [Test]
    public void Read_track_assessment_is_valid_without_command()
    {
        var envelope = ReadTrack();
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.FailureReason, Is.Null);
        Assert.That(envelope.CommandId, Is.Null);
        Assert.That(envelope.ReplayProvenance.Submitted, Is.False);
    }

    [Test]
    public void Read_with_commandId_fails()
    {
        var envelope = ReadTrack() with { CommandId = "engage" };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonReadMustNotCommand));
    }

    [Test]
    public void Propose_pairing_requires_authority_override_and_provenance()
    {
        var envelope = ProposeOrganicEngage();
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.True);
        Assert.That(envelope.AuthorityBasis, Is.Not.Null);
        Assert.That(envelope.AuthorityBasis!.EngagementAuthorizationImplied, Is.False);
        Assert.That(envelope.PlayerOverride, Is.Not.Null);
        Assert.That(envelope.PlayerOverride!.Path, Is.EqualTo(PlayerOverride.SubmitPath));
        Assert.That(envelope.ReplayProvenance.SkillId, Is.EqualTo(SkillIds.PairingRecommend));
        Assert.That(envelope.ReplayProvenance.Submitted, Is.False);
    }

    [Test]
    public void Propose_does_not_imply_engagement_authorization()
    {
        var envelope = ProposeOrganicEngage() with
        {
            AuthorityBasis = OrganicAuthority() with
            {
                EngagementAuthorizationImplied = true,
            },
        };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonAuthorizationImplied));
    }

    [Test]
    public void Propose_engage_on_shared_track_fails()
    {
        var envelope = ProposeOrganicEngage() with
        {
            AuthorityBasis = OrganicAuthority() with
            {
                TrackSource = TrackSource.DatalinkShared,
                FireControlSatisfied = false,
            },
        };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonSharedTrackNoRelease));
    }

    [Test]
    public void Propose_organic_engage_without_fire_control_fails()
    {
        var envelope = ProposeOrganicEngage() with
        {
            AuthorityBasis = OrganicAuthority() with { FireControlSatisfied = false },
        };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonNoFireControl));
    }

    [Test]
    public void Propose_engage_requires_weapons_release_approval()
    {
        var envelope = ProposeOrganicEngage() with { RequiredApproval = RequiredApproval.Operator };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonWeaponsReleaseRequired));
    }

    [Test]
    public void Propose_track_assess_rejects_engage_not_in_skill_allowlist()
    {
        var envelope = ProposeOrganicEngage() with
        {
            SkillId = SkillIds.TrackAssess,
            RequiredApproval = RequiredApproval.Operator,
            CommandId = "engage",
            ReplayProvenance = ProposeOrganicEngage().ReplayProvenance with
            {
                SkillId = SkillIds.TrackAssess,
            },
        };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonCommandNotAllowed));
    }

    [Test]
    public void Submit_organic_engage_without_fire_control_fails()
    {
        var envelope = SubmitEngage() with
        {
            AuthorityBasis = OrganicAuthority() with { FireControlSatisfied = false },
        };
        var result = SkillEnvelopeValidator.Validate(envelope, proposalApproved: true);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonNoFireControl));
    }

    [Test]
    public void Submit_engage_requires_weapons_release_approval()
    {
        var envelope = SubmitEngage() with { RequiredApproval = RequiredApproval.None };
        var result = SkillEnvelopeValidator.Validate(envelope, proposalApproved: true);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonWeaponsReleaseRequired));
    }

    [Test]
    public void Propose_unknown_command_uses_C2CommandIssuance_reason()
    {
        var envelope = ProposeOrganicEngage() with { CommandId = "warp" };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(C2CommandIssuance.ReasonUnknownCommand));
    }

    [Test]
    public void Submit_without_approval_fails_and_does_not_append()
    {
        var log = new DecisionLog();
        var fingerprintBefore = log.ComputeFingerprint();
        var envelope = SubmitEngage();

        var result = SkillEnvelopeValidator.Validate(envelope, proposalApproved: false);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonProposalNotApproved));
        Assert.That(log.PlayerOrders, Is.Empty);
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprintBefore));
    }

    [Test]
    public void Submit_approved_resolves_command_without_enqueue()
    {
        var log = new DecisionLog();
        var fingerprintBefore = log.ComputeFingerprint();
        var envelope = SubmitEngage();

        var result = SkillEnvelopeValidator.Validate(envelope, proposalApproved: true);

        Assert.That(result.Ok, Is.True);
        Assert.That(result.ResolvedCommandId, Is.EqualTo("engage"));
        Assert.That(C2CommandIssuance.TryResolve(result.ResolvedCommandId, out var kind, out _), Is.True);
        Assert.That(kind, Is.EqualTo(OrderKind.Engage));
        Assert.That(log.PlayerOrders, Is.Empty);
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprintBefore));
    }

    [Test]
    public void Explain_is_read_only()
    {
        var envelope = ReadTrack() with
        {
            SkillId = SkillIds.Explain,
            Lane = SkillLane.Propose,
            CommandId = "hold",
            RequiredApproval = RequiredApproval.Operator,
            ProposalId = "prop-x",
            TtlTicks = 30,
            CreatedSimTick = 12,
            AuthorityBasis = OrganicAuthority(),
            PlayerOverride = HoldOverride(),
        };
        var result = SkillEnvelopeValidator.Validate(envelope);

        Assert.That(result.Ok, Is.False);
        Assert.That(result.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonLaneNotAllowed));
    }

    private static SkillEnvelope ReadTrack() =>
        new(
            Lane: SkillLane.Read,
            SkillId: SkillIds.TrackAssess,
            InvocationId: "inv-read-001",
            ProposalId: null,
            SimTick: 12,
            SimTime: 12.0,
            CommandId: null,
            TtlTicks: null,
            CreatedSimTick: null,
            RequiredApproval: RequiredApproval.None,
            InputsEvidence: new[]
            {
                new EvidencePointer(EvidenceKind.Contact, "c-12", SequenceId: 80, ProjectionType: "ContactPictureProjection", Field: "LifecycleState"),
            },
            Assumptions: new[] { "Observer ffg-1 holds the organic track" },
            Rationale: "Contact c-12 is tracking with fire-control true.",
            AuthorityBasis: null,
            PlayerOverride: null,
            ReplayProvenance: new ReplayProvenance(
                SkillId: SkillIds.TrackAssess,
                InvocationId: "inv-read-001",
                SimTick: 12,
                SimTime: 12.0,
                OrderLogFingerprintBefore: null,
                SequenceIdOnSubmit: null,
                Submitted: false));

    private static SkillEnvelope ProposeOrganicEngage() =>
        new(
            Lane: SkillLane.Propose,
            SkillId: SkillIds.PairingRecommend,
            InvocationId: "inv-propose-001",
            ProposalId: "prop-001",
            SimTick: 12,
            SimTime: 12.0,
            CommandId: "engage",
            TtlTicks: 30,
            CreatedSimTick: 12,
            RequiredApproval: RequiredApproval.WeaponsRelease,
            InputsEvidence: new[]
            {
                new EvidencePointer(EvidenceKind.Unit, "ffg-1", SequenceId: null, ProjectionType: "UnitDetailProjection", Field: null),
                new EvidencePointer(EvidenceKind.Contact, "c-12", SequenceId: 80, ProjectionType: "ContactPictureProjection", Field: null),
            },
            Assumptions: new[] { "Organic fire-control is required for any later engage submit" },
            Rationale: "ffg-1 is the only candidate with organic FC.",
            AuthorityBasis: OrganicAuthority(),
            PlayerOverride: HoldOverride(),
            ReplayProvenance: new ReplayProvenance(
                SkillId: SkillIds.PairingRecommend,
                InvocationId: "inv-propose-001",
                SimTick: 12,
                SimTime: 12.0,
                OrderLogFingerprintBefore: null,
                SequenceIdOnSubmit: null,
                Submitted: false));

    private static SkillEnvelope SubmitEngage() =>
        ProposeOrganicEngage() with
        {
            Lane = SkillLane.Submit,
            SkillId = SkillIds.Submit,
            InvocationId = "inv-submit-001",
            CommandId = "engage",
            ReplayProvenance = ProposeOrganicEngage().ReplayProvenance with
            {
                SkillId = SkillIds.Submit,
                InvocationId = "inv-submit-001",
                Submitted = true,
                OrderLogFingerprintBefore = "pre-submit",
            },
        };

    private static AuthorityBasis OrganicAuthority() =>
        new(
            PolicySnapshotId: "policy-baltic-default",
            PolicyUnavailable: false,
            Roe: "WEAPONS_TIGHT",
            Emcon: "radar-active",
            TrackSource: TrackSource.Organic,
            FireControlSatisfied: true,
            EngagementAuthorizationImplied: false);

    private static PlayerOverride HoldOverride() =>
        new(
            Path: PlayerOverride.SubmitPath,
            CommandId: "hold",
            ControllerRequirement: PlayerOverride.HumanControllerRequirement,
            RejectLeavesNoMutation: true);
}
