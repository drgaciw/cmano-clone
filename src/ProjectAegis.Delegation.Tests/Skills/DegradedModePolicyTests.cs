using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.Tests.Skills;

[TestFixture]
public sealed class DegradedModePolicyTests
{
    [Test]
    public void DegradedModePolicy_explicitly_covers_all_five_conditions_with_zero_hidden_fallback()
    {
        Assert.That(DegradedModePolicy.Rules, Has.Count.EqualTo(5));
        var conditions = DegradedModePolicy.Rules.Select(r => r.Condition).ToArray();
        Assert.That(conditions, Is.EquivalentTo(new[]
        {
            DegradedModeCondition.ModelOrSkillUnavailable,
            DegradedModeCondition.StaleData,
            DegradedModeCondition.NetworkPartition,
            DegradedModeCondition.AuthorityWithheld,
            DegradedModeCondition.ResourceConflict,
        }));

        foreach (var rule in DegradedModePolicy.Rules)
        {
            Assert.That(rule.AllowHiddenFallback, Is.False);
            Assert.That(rule.RequireOperatorDisclosure, Is.True);
            Assert.That(string.IsNullOrWhiteSpace(rule.DisclosureCode), Is.False);
            Assert.That(string.IsNullOrWhiteSpace(rule.PolicyStatement), Is.False);
        }

        var combined = DegradedModePolicy.Evaluate(new DegradedModeContext(
            ModelOrSkillUnavailable: true,
            StaleData: true,
            NetworkPartition: true,
            AuthorityWithheld: true,
            ResourceConflict: true,
            Detail: "multi-fault"));

        Assert.That(combined.IsDegraded, Is.True);
        Assert.That(combined.AllowAutonomousOrSilentFallback, Is.False);
        Assert.That(combined.RequiresOperatorDisclosure, Is.True);
        Assert.That(combined.ActiveConditions, Has.Count.EqualTo(5));
        Assert.That(combined.Disposition, Is.EqualTo(DegradedModeDisposition.FailClosedWithhold));
        Assert.That(combined.OperatorDisclosure, Does.Contain("multi-fault"));
    }

    [Test]
    public void SkillEnvelopeValidator_fails_closed_on_policy_unavailable_and_expired_proposals()
    {
        var log = new DecisionLog();
        var fpBefore = log.ComputeFingerprint();

        var policyUnavailablePropose = ProposeEnvelope() with
        {
            AuthorityBasis = ProposeEnvelope().AuthorityBasis! with { PolicyUnavailable = true },
        };
        var resPolicy = SkillEnvelopeValidator.Validate(policyUnavailablePropose);
        Assert.That(resPolicy.Ok, Is.False);
        Assert.That(resPolicy.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonPolicyUnavailable));

        var zeroTtlPropose = ProposeEnvelope() with { TtlTicks = 0 };
        var resZeroTtl = SkillEnvelopeValidator.Validate(zeroTtlPropose);
        Assert.That(resZeroTtl.Ok, Is.False);
        Assert.That(resZeroTtl.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonMissingTtl));

        var expiredPropose = ProposeEnvelope() with { SimTick = 50, CreatedSimTick = 10, TtlTicks = 15 };
        var resExpiredPropose = SkillEnvelopeValidator.Validate(expiredPropose, proposalApproved: false, currentSimTick: 50);
        Assert.That(resExpiredPropose.Ok, Is.False);
        Assert.That(resExpiredPropose.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonProposalExpired));

        var expiredSubmit = ProposeEnvelope() with
        {
            Lane = SkillLane.Submit,
            SkillId = SkillIds.Submit,
            SimTick = 50,
            CreatedSimTick = 10,
            TtlTicks = 15,
        };
        var resExpiredSubmit = SkillEnvelopeValidator.Validate(expiredSubmit, proposalApproved: true, currentSimTick: 50);
        Assert.That(resExpiredSubmit.Ok, Is.False);
        Assert.That(resExpiredSubmit.FailureReason, Is.EqualTo(SkillEnvelopeValidator.ReasonProposalExpired));

        var degraded = SkillEnvelopeValidator.EvaluateDegradedMode(policyUnavailablePropose, currentSimTick: 12);
        Assert.That(degraded.IsDegraded, Is.True);
        Assert.That(degraded.ActiveConditions, Does.Contain(DegradedModeCondition.AuthorityWithheld));
        Assert.That(degraded.AllowAutonomousOrSilentFallback, Is.False);

        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fpBefore));
    }

    [Test]
    public void SkillEnvelopeValidator_ttl_boundary_near_ulong_max_does_not_wrap_around()
    {
        const ulong created = ulong.MaxValue - 5;
        const int ttl = 10;

        var proposeNearMax = ProposeEnvelope() with
        {
            SimTick = ulong.MaxValue,
            CreatedSimTick = created,
            TtlTicks = ttl,
        };
        var submitNearMax = proposeNearMax with
        {
            Lane = SkillLane.Submit,
            SkillId = SkillIds.Submit,
        };

        var proposeOk = SkillEnvelopeValidator.Validate(proposeNearMax, proposalApproved: false, currentSimTick: ulong.MaxValue);
        var submitOk = SkillEnvelopeValidator.Validate(submitNearMax, proposalApproved: true, currentSimTick: ulong.MaxValue);
        var degradedCheck = SkillEnvelopeValidator.EvaluateDegradedMode(proposeNearMax, currentSimTick: ulong.MaxValue);

        Assert.That(proposeOk.Ok, Is.True);
        Assert.That(submitOk.Ok, Is.True);
        Assert.That(degradedCheck.IsDegraded, Is.False);
    }

    [Test]
    public void C2AuthorityProjector_FromEnvelope_withholds_when_policy_unavailable_without_weapons_free_fallback()
    {
        var basis = new AuthorityBasis(
            PolicySnapshotId: null,
            PolicyUnavailable: true,
            Roe: null,
            Emcon: "radar-active",
            TrackSource: TrackSource.Organic,
            FireControlSatisfied: true,
            EngagementAuthorizationImplied: false);

        var ctx = C2AuthorityProjectionContext.FromEnvelope(
            basis,
            SkillLane.Propose,
            RequiredApproval.WeaponsRelease,
            commandId: "engage");

        Assert.That(ctx.PolicyUnavailable, Is.True);
        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Roe.EngageAllowedByRoe, Is.False);
        Assert.That(projection.Roe.TargetingDisposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(projection.Roe.TargetingReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonPolicyUnavailable));
        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(projection.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonPolicyUnavailable));

        var engageState = projection.Actions.Single(a => a.Action == C2AuthorityActionKind.Engage);
        var approveState = projection.Actions.Single(a => a.Action == C2AuthorityActionKind.Approve);
        Assert.That(engageState.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(approveState.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
    }

    private static SkillEnvelope ProposeEnvelope() =>
        new(
            Lane: SkillLane.Propose,
            SkillId: SkillIds.PairingRecommend,
            InvocationId: "inv-1",
            ProposalId: "prop-1",
            SimTick: 12,
            SimTime: 12.0,
            CommandId: "engage",
            TtlTicks: 30,
            CreatedSimTick: 12,
            RequiredApproval: RequiredApproval.WeaponsRelease,
            InputsEvidence: [new EvidencePointer(EvidenceKind.Contact, "c-1", 1, "ContactPictureProjection", null)],
            Assumptions: ["organic track"],
            Rationale: "test",
            AuthorityBasis: new AuthorityBasis("pol-1", false, "WEAPONS_FREE", "active", TrackSource.Organic, true, false),
            PlayerOverride: new PlayerOverride(PlayerOverride.SubmitPath, "hold", PlayerOverride.HumanControllerRequirement, true),
            ReplayProvenance: new ReplayProvenance(SkillIds.PairingRecommend, "inv-1", 12, 12.0, null, null, false));
}
