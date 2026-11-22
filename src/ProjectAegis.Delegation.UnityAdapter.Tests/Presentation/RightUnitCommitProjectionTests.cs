using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;

/// <summary>DRG-370: read-only projection adapter feeding RightUnitPanelHost commit strip and drop bark.</summary>
[TestFixture]
public sealed class RightUnitCommitProjectionTests
{
    [Test]
    public void Project_strip_returns_empty_without_selection_or_preview()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        Assert.That(
            RightUnitCommitProjection.ProjectStrip(bridge, null, null, null, null, null),
            Is.EqualTo(CommitConstraintStripState.Empty));
    }

    [Test]
    public void Project_strip_binds_constraints_and_magazine_cost_read_only()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        var defaults = ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(defaults.DefaultMagazineRounds);
        var preview = EngagePreviewProjection.Project(in ctx, defaults.DlzPersonality);
        var menu = AttackOptionsPreviewBinder.Bind(EngageAttackOptions.Build(in ctx, preview));

        var strip = RightUnitCommitProjection.ProjectStrip(
            bridge, "u1", preview, menu, WraSalvoRemainingState.Empty, WeaponAbortTooltipState.Empty, "fire-single");

        Assert.That(strip.CanCommit, Is.True);
        Assert.That(strip.ConstraintLine, Is.EqualTo("COMMIT: READY"));
        Assert.That(strip.CostLine, Does.StartWith("COST: 1 rd | MAG "));
        Assert.That(strip.IsFireOrder, Is.False);
    }

    [Test]
    public void Project_latest_dropped_bark_coalesces_correlated_policy_denial_and_synthetic_engagement()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Orchestrator.DecisionLog.Append(OrderLogEntryFactories.FromPolicyDenial(
            new PolicyDenialRecord(0, 5.0, 50, new AgentId("a1"), new TargetId("u1"), 1, FireAbortReason.WeaponsTight, OrderKind.Engage)));
        bridge.Orchestrator.DecisionLog.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(0, 5.0, 50, new TargetId("u1"), EngagementId: 0, Launched: false, AbortReasonCode: AbortReasonCatalog.Engage.ROE_WEAPONS_TIGHT)));

        var bark = RightUnitCommitProjection.ProjectLatestDroppedBark(bridge, "u1");

        Assert.That(bark, Is.Not.Null);
        Assert.That(bark!.Kind, Is.EqualTo(CommitBarkKind.Dropped));
        Assert.That(bark.ReasonCode, Is.EqualTo(AbortReasonCatalog.Engage.ROE_WEAPONS_TIGHT));
        Assert.That(bark.BarkText, Is.EqualTo("u1 DROPPED: ROE_WEAPONS_TIGHT"));
    }
}
