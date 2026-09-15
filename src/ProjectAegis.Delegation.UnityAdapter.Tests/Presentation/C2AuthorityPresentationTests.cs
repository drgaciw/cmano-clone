using NUnit.Framework;
using ProjectAegis.Delegation.EscalationGate;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

[TestFixture]
public sealed class C2AuthorityPresentationTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("missing")]
    public void Unknown_selection_returns_empty_presentation(string? contactId)
    {
        Assert.That(
            C2AuthorityPresenter.Build(contactId, null),
            Is.EqualTo(C2AuthorityPresentation.Empty));
    }

    [Test]
    public void Weapons_tight_surfaces_withheld_targeting_and_named_gate()
    {
        var presentation = C2AuthorityPresenter.Build("c-tight", WeaponsTightProjection());

        Assert.That(presentation.AdvisoryBadge, Does.Contain("IsOrder=false"));
        Assert.That(presentation.RoeLine, Does.Contain("WEAPONS_TIGHT").And.Contain("WITHHELD"));
        Assert.That(presentation.TargetingLine, Does.Contain("WITHHELD"));
        Assert.That(presentation.GateLines, Has.Some.Contains(EscalationGateCode.WeaponsTight));
        Assert.That(presentation.VerbRows.Select(row => row.VerbLabel), Does.Contain("ENGAGE"));
        Assert.That(presentation.VerbRows.First(row => row.VerbLabel == "ENGAGE").DispositionLabel,
            Is.EqualTo("WITHHELD"));
        Assert.That(presentation.NextActionLine, Does.Contain("authority restriction"));
    }

    [Test]
    public void Approval_required_surfaces_higher_hq_gate_and_verb_rows()
    {
        var basis = new AuthorityBasis(
            PolicySnapshotId: "policy-baltic-default",
            PolicyUnavailable: false,
            Roe: "WEAPONS_FREE",
            Emcon: "radar-active",
            TrackSource: TrackSource.Organic,
            FireControlSatisfied: true,
            EngagementAuthorizationImplied: false);
        var context = C2AuthorityProjectionContext.FromEnvelope(
            basis,
            SkillLane.Propose,
            RequiredApproval.WeaponsRelease,
            commandId: "engage");
        var authority = C2AuthorityProjector.Project(context);

        var presentation = C2AuthorityPresenter.Build("order-1", authority);

        Assert.That(presentation.TargetingLine, Does.Contain("APPROVAL REQUIRED"));
        Assert.That(presentation.GateLines, Has.Some.Contains(EscalationGateCode.HigherHq));
        Assert.That(presentation.VerbRows.First(row => row.VerbLabel == "APPROVE").DispositionLabel,
            Is.EqualTo("APPROVAL REQUIRED"));
        Assert.That(presentation.NextActionLine, Does.Contain("approval"));
    }

    [Test]
    public void Weapons_free_permitted_targeting_reports_no_active_gate()
    {
        var context = new C2AuthorityProjectionContext(
            RoeLevel.WeaponsFree,
            SkillLane.Read,
            RequiredApproval.None,
            TrackSource.Organic,
            FireControlSatisfied: true);
        var authority = C2AuthorityProjector.Project(context);

        var presentation = C2AuthorityPresenter.Build("c-free", authority);

        Assert.That(presentation.GateLines, Has.Some.Contains("none active"));
        Assert.That(presentation.TargetingLine, Does.Contain("PERMITTED"));
        Assert.That(presentation.VerbRows.First(row => row.VerbLabel == "OBSERVE").DispositionLabel,
            Is.EqualTo("PERMITTED"));
    }

    [Test]
    public void Summary_line_preserves_policy_truth_without_order_semantics()
    {
        var summary = C2AuthorityPresenter.FormatSummaryLine(WeaponsTightProjection());
        Assert.That(summary, Does.Contain("WEAPONS_TIGHT").And.Contain("WITHHELD"));
        Assert.That(C2AuthorityPresenter.FormatSummaryLine(null), Does.Contain("UNKNOWN"));
    }

    [Test]
    public void Repeated_projection_is_replay_stable()
    {
        var authority = WeaponsTightProjection();
        var first = C2AuthorityPresenter.Build("c-tight", authority);
        var second = C2AuthorityPresenter.Build("c-tight", authority);

        Assert.That(second.HeaderLine, Is.EqualTo(first.HeaderLine));
        Assert.That(second.RoeLine, Is.EqualTo(first.RoeLine));
        Assert.That(second.TargetingLine, Is.EqualTo(first.TargetingLine));
        Assert.That(second.NextActionLine, Is.EqualTo(first.NextActionLine));
        Assert.That(second.VerbRows.Select(row => row.VerbLabel), Is.EqualTo(first.VerbRows.Select(row => row.VerbLabel)));
        Assert.That(second.GateLines, Is.EqualTo(first.GateLines));
    }

    private static C2AuthorityProjection WeaponsTightProjection()
    {
        var context = new C2AuthorityProjectionContext(
            RoeLevel.WeaponsTight,
            SkillLane.Read,
            RequiredApproval.None,
            TrackSource.Organic,
            FireControlSatisfied: true,
            CommandId: "engage");
        return C2AuthorityProjector.Project(context);
    }
}
