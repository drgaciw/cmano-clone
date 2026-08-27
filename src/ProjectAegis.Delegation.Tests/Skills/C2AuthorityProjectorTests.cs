namespace ProjectAegis.Delegation.Tests.Skills;

using ProjectAegis.Delegation.Skills;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

/// <summary>
/// DRG-209: headless authority and ROE projection for Combat UX Slice A.
/// </summary>
[TestFixture]
public sealed class C2AuthorityProjectorTests
{
    [Test]
    public void WeaponsTight_withholds_targeting_and_engage()
    {
        var ctx = OrganicContext(
            roe: RoeLevel.WeaponsTight,
            lane: SkillLane.Read,
            commandId: null);

        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Roe.Roe, Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(projection.Roe.TargetingDisposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(projection.Roe.TargetingReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsTight));
        Assert.That(projection.Roe.EngageAllowedByRoe, Is.False);

        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(projection.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsTight));
        Assert.That(projection.Targeting.PendingApproval, Is.Null);

        AssertAction(projection, C2AuthorityActionKind.Engage, C2AuthorityDisposition.Withheld, C2AuthorityProjector.ReasonWeaponsTight);
        AssertAction(projection, C2AuthorityActionKind.Observe, C2AuthorityDisposition.Permitted, null);
    }

    [Test]
    public void WeaponsFree_organic_fire_control_allows_engage()
    {
        var ctx = OrganicContext(
            roe: RoeLevel.WeaponsFree,
            lane: SkillLane.Read,
            commandId: null);

        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Roe.Roe, Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(projection.Roe.EngageAllowedByRoe, Is.True);
        Assert.That(projection.Roe.TargetingDisposition, Is.EqualTo(C2AuthorityDisposition.Permitted));

        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Permitted));
        Assert.That(projection.Targeting.ReasonCode, Is.Null);
        Assert.That(projection.Targeting.PendingApproval, Is.Null);

        AssertAction(projection, C2AuthorityActionKind.Engage, C2AuthorityDisposition.Permitted, null);
        AssertAction(projection, C2AuthorityActionKind.Recommend, C2AuthorityDisposition.Permitted, null);
    }

    [Test]
    public void Propose_engage_requires_weapons_release_approval()
    {
        var basis = OrganicAuthority(roeLabel: "WEAPONS_FREE");
        var ctx = C2AuthorityProjectionContext.FromEnvelope(
            basis,
            SkillLane.Propose,
            RequiredApproval.WeaponsRelease,
            commandId: "engage");

        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.ApprovalRequired));
        Assert.That(projection.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonWeaponsReleaseRequired));
        Assert.That(projection.Targeting.PendingApproval, Is.EqualTo(RequiredApproval.WeaponsRelease));

        AssertAction(
            projection,
            C2AuthorityActionKind.Engage,
            C2AuthorityDisposition.ApprovalRequired,
            C2AuthorityProjector.ReasonWeaponsReleaseRequired);
        AssertAction(
            projection,
            C2AuthorityActionKind.Approve,
            C2AuthorityDisposition.ApprovalRequired,
            C2AuthorityProjector.ReasonWeaponsReleaseRequired);
    }

    [Test]
    public void Propose_hold_requires_operator_approval()
    {
        var ctx = OrganicContext(
            roe: RoeLevel.WeaponsFree,
            lane: SkillLane.Propose,
            commandId: "hold",
            requiredApproval: RequiredApproval.Operator);

        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.ApprovalRequired));
        Assert.That(projection.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonApprovalRequired));
        Assert.That(projection.Targeting.PendingApproval, Is.EqualTo(RequiredApproval.Operator));

        AssertAction(
            projection,
            C2AuthorityActionKind.Retask,
            C2AuthorityDisposition.ApprovalRequired,
            C2AuthorityProjector.ReasonApprovalRequired);
    }

    [Test]
    public void Shared_track_withholds_targeting_even_under_weapons_free()
    {
        var ctx = new C2AuthorityProjectionContext(
            Roe: RoeLevel.WeaponsFree,
            Lane: SkillLane.Propose,
            RequiredApproval: RequiredApproval.WeaponsRelease,
            TrackSource: TrackSource.DatalinkShared,
            FireControlSatisfied: false,
            CommandId: "engage");

        var projection = C2AuthorityProjector.Project(ctx);

        Assert.That(projection.Targeting.Disposition, Is.EqualTo(C2AuthorityDisposition.Withheld));
        Assert.That(projection.Targeting.ReasonCode, Is.EqualTo(C2AuthorityProjector.ReasonSharedTrackNoRelease));
        AssertAction(
            projection,
            C2AuthorityActionKind.Engage,
            C2AuthorityDisposition.Withheld,
            C2AuthorityProjector.ReasonSharedTrackNoRelease);
    }

    [Test]
    public void Project_includes_all_slice_a_authority_verbs()
    {
        var projection = C2AuthorityProjector.Project(OrganicContext());

        Assert.That(projection.Actions.Select(a => a.Action), Is.EquivalentTo(new[]
        {
            C2AuthorityActionKind.Observe,
            C2AuthorityActionKind.Recommend,
            C2AuthorityActionKind.Approve,
            C2AuthorityActionKind.Engage,
            C2AuthorityActionKind.Abort,
            C2AuthorityActionKind.Retask,
        }));
    }

    [Test]
    public void ParseRoeLabel_accepts_contract_strings()
    {
        Assert.That(C2AuthorityProjector.ParseRoeLabel("WEAPONS_TIGHT"), Is.EqualTo(RoeLevel.WeaponsTight));
        Assert.That(C2AuthorityProjector.ParseRoeLabel("WeaponsFree"), Is.EqualTo(RoeLevel.WeaponsFree));
        Assert.That(C2AuthorityProjector.ParseRoeLabel("hold fire"), Is.EqualTo(RoeLevel.HoldFire));
    }

    private static void AssertAction(
        C2AuthorityProjection projection,
        C2AuthorityActionKind action,
        C2AuthorityDisposition disposition,
        string? reasonCode)
    {
        var state = projection.Actions.Single(a => a.Action == action);
        Assert.That(state.Disposition, Is.EqualTo(disposition), () => action.ToString());
        Assert.That(state.ReasonCode, Is.EqualTo(reasonCode), () => action.ToString());
    }

    private static C2AuthorityProjectionContext OrganicContext(
        RoeLevel roe = RoeLevel.WeaponsFree,
        SkillLane lane = SkillLane.Read,
        string? commandId = null,
        RequiredApproval requiredApproval = RequiredApproval.None) =>
        new(
            roe,
            lane,
            requiredApproval,
            TrackSource.Organic,
            FireControlSatisfied: true,
            commandId,
            HumanControlled: true);

    private static AuthorityBasis OrganicAuthority(string roeLabel = "WEAPONS_FREE") =>
        new(
            PolicySnapshotId: "policy-baltic-default",
            PolicyUnavailable: false,
            Roe: roeLabel,
            Emcon: "radar-active",
            TrackSource: TrackSource.Organic,
            FireControlSatisfied: true,
            EngagementAuthorizationImplied: false);
}
