using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Delegation.UnityAdapter.Tests.Presentation;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

[TestFixture]
public sealed class MessageLogDeepLinkBridgeTests
{
    [Test]
    public void MessageLogBridge_InspectDecisionLogLive_matches_live_view_and_omits_fogged_decisions()
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            playerInfoModel: PlayerInfoModel.DelegationFog);
        var log = bridge.Orchestrator.DecisionLog;
        log.Append(new DecisionRecord(
            1.0, new AgentId("assisted-1"), new TargetId("u1"), AutonomyLevel.Assisted,
            OrderKind.Hold, Array.Empty<ScoredIntent>(), "assisted hold", 1, 10, 0.1, SimTick: 1));
        log.Append(new DecisionRecord(
            2.0, new AgentId("full-1"), new TargetId("u1"), AutonomyLevel.FullAutonomous,
            OrderKind.Engage, Array.Empty<ScoredIntent>(), "hidden full", 2, 10, 0.2, SimTick: 2));

        var visibleSeq = log.ChronologicalEntries()[0].SequenceId;
        var foggedSeq = log.ChronologicalEntries()[1].SequenceId;
        var fingerprintBefore = log.ComputeFingerprint();

        var liveVisible = MessageLogBridge.InspectDecisionLogLive(bridge, visibleSeq);
        var liveFogged = MessageLogBridge.InspectDecisionLogLive(bridge, foggedSeq);
        var replayFogged = MessageLogBridge.InspectDecisionLogReplay(log, foggedSeq);

        Assert.That(liveVisible.Found, Is.True);
        Assert.That(liveVisible.AgentId, Is.EqualTo("assisted-1"));

        Assert.That(liveFogged.Found, Is.False);
        Assert.That(liveFogged.UnavailableReason, Is.EqualTo(DecisionLogDeepLinkProjection.UnavailableReasonCode));

        Assert.That(replayFogged.Found, Is.True);
        Assert.That(replayFogged.AgentId, Is.EqualTo("full-1"));

        var bound = DecisionLogDeepLinkBinder.Bind(liveVisible);
        Assert.That(bound.Visible, Is.True);
        Assert.That(bound.Found, Is.True);
        Assert.That(bound.SequenceId, Is.EqualTo(visibleSeq));
        Assert.That(bound.HeaderLine, Does.Contain($"SEQ #{visibleSeq}"));
        Assert.That(bound.AttributionLine, Does.Contain("assisted-1"));
        Assert.That(bound.RationaleLine, Does.Contain("assisted hold"));
        Assert.That(bound.TelemetryLine, Does.Contain("Assisted"));
        Assert.That(bound.AlternativeLines, Is.Empty);
        Assert.That(bound.FieldLines, Is.Not.Empty);
        Assert.That(bound.UnavailableText, Is.Empty);

        var boundFogged = DecisionLogDeepLinkBinder.Bind(liveFogged);
        Assert.That(boundFogged.Visible, Is.True);
        Assert.That(boundFogged.Found, Is.False);
        Assert.That(boundFogged.UnavailableText, Is.EqualTo(DecisionLogDeepLinkProjection.UnavailableReasonCode));
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(fingerprintBefore));
    }

    [Test]
    public void MessageCategoryPolicyBinder_projects_all_rule_flags_and_tokens()
    {
        var rows = MessageCategoryPolicyBinder.Bind(MessageCategoryPolicy.LoadDefault());
        Assert.That(rows, Is.Not.Empty);
        var damage = rows.Single(r => r.Category == "DAMAGE");
        Assert.That(damage.Log, Is.True);
        Assert.That(damage.Pause, Is.True);
        Assert.That(damage.MapBalloon, Is.True);
        Assert.That(damage.DropToRealTime, Is.True);
        Assert.That(damage.ActionTokens, Is.Not.Empty);
    }

    [Test]
    public void Hold_boundary_fence_DRG268_and_DRG269_not_wired_into_MessageLogPanelHost_or_DeepLink()
    {
        var messageHost = UiIaSourceReader.ReadRuntime("MessageLogPanelHost.cs");
        var deepLinkProj = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation", "Projection", "DecisionLogDeepLinkProjection.cs");
        var deepLinkBinder = UiIaSourceReader.ReadUnder(
            "src", "ProjectAegis.Delegation.UnityAdapter", "Presentation", "DecisionLogDeepLinkBinder.cs");

        foreach (var source in new[] { messageHost, deepLinkProj, deepLinkBinder })
        {
            Assert.That(source, Does.Not.Contain("EngageExplainPanelHost"));
            Assert.That(source, Does.Not.Contain("EngageExplainProjection"));
            Assert.That(source, Does.Not.Contain("InspectCombatEvent"));
            Assert.That(source, Does.Not.Contain("DisplayCombatKey"));
            Assert.That(source, Does.Not.Contain("PendingApprovalQueue"));
            Assert.That(source, Does.Not.Contain("PendingApprovalPanelHost"));
        }
    }
}
