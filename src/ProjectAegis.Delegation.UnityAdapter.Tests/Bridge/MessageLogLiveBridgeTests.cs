namespace ProjectAegis.Delegation.UnityAdapter.Tests.Bridge;

using System;
using Core;
using Decision;
using Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

/// <summary>
/// DRG-248: live HUD message log binds <see cref="DelegationBridge.GetLiveOrderLogView"/>;
/// replay/AAR binds the full <see cref="DecisionLog"/>.
/// </summary>
[TestFixture]
public sealed class MessageLogLiveBridgeTests
{
    [TestCase(PlayerInfoModel.DelegationFog)]
    [TestCase(PlayerInfoModel.TieredByAutonomy)]
    public void ProjectLive_omits_fogged_decisions_while_replay_keeps_them(PlayerInfoModel model)
    {
        var bridge = BridgeWith(model);

        var live = MessageLogBridge.ProjectLive(bridge);
        var replay = MessageLogBridge.ProjectReplay(bridge.Orchestrator.DecisionLog);

        Assert.That(live.Any(l => l.Text.Contains("full-agent")), Is.False);
        Assert.That(live.Count(l => l.Category == "AGENT_DECISION"), Is.EqualTo(1));
        Assert.That(live.Any(l => l.Category == "POLICY_DENIAL"), Is.True);

        Assert.That(replay.Count(l => l.Category == "AGENT_DECISION"), Is.EqualTo(2));
        Assert.That(replay.Any(l => l.Text.Contains("full-agent")), Is.True);
        Assert.That(bridge.Orchestrator.DecisionLog.ChronologicalEntries(), Has.Count.EqualTo(3));
    }

    [Test]
    public void ProjectLive_matches_projection_of_GetLiveOrderLogView()
    {
        var bridge = BridgeWith(PlayerInfoModel.DelegationFog);

        var live = MessageLogBridge.ProjectLive(bridge);
        var expected = MessageLogProjection.Project(bridge.GetLiveOrderLogView());

        Assert.That(live.Select(l => (l.SequenceId, l.Category, l.Text)),
            Is.EqualTo(expected.Select(l => (l.SequenceId, l.Category, l.Text))));
    }

    [Test]
    public void ProjectLive_FullTransparency_matches_ProjectFrom()
    {
        var bridge = BridgeWith(PlayerInfoModel.FullTransparency);

        var live = MessageLogBridge.ProjectLive(bridge);
        var full = MessageLogBridge.ProjectFrom(bridge.Orchestrator.DecisionLog);

        Assert.That(live.Select(l => (l.SequenceId, l.Category, l.Text)),
            Is.EqualTo(full.Select(l => (l.SequenceId, l.Category, l.Text))));
    }

    [Test]
    public void BindLive_and_BindReplay_document_their_source()
    {
        var bridge = BridgeWith(PlayerInfoModel.DelegationFog);

        var livePanel = MessageLogBridge.BindLive(bridge);
        var replayPanel = MessageLogBridge.BindReplay(bridge.Orchestrator.DecisionLog);

        Assert.That(livePanel.Source, Is.EqualTo(MessageLogBindingSource.LiveOrderLogView));
        Assert.That(replayPanel.Source, Is.EqualTo(MessageLogBindingSource.FullDecisionLog));
        Assert.That(livePanel.Rows.Count, Is.LessThan(replayPanel.Rows.Count));
    }

    [Test]
    public void Live_and_replay_null_arguments_throw()
    {
        Assert.Throws<ArgumentNullException>(() => MessageLogBridge.ProjectLive(null!));
        Assert.Throws<ArgumentNullException>(() => MessageLogBridge.BindLive(null!));
        Assert.Throws<ArgumentNullException>(() => MessageLogBridge.ProjectReplay(null!));
        Assert.Throws<ArgumentNullException>(() => MessageLogBridge.BindReplay(null!));
    }

    private static DelegationBridge BridgeWith(PlayerInfoModel model)
    {
        var bridge = new DelegationBridge(42, mvpEngagement: false);
        bridge.Orchestrator.ScenarioPolicy = new ScenarioPolicyProfile(
            EffectivePolicy.DefaultFree,
            playerInfoModel: model);
        var log = bridge.Orchestrator.DecisionLog;
        log.Append(MakeDecision("assisted-agent", AutonomyLevel.Assisted));
        log.Append(MakeDecision("full-agent", AutonomyLevel.FullAutonomous));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId("full-agent"), new TargetId("t1"), 1,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));
        return bridge;
    }

    private static DecisionRecord MakeDecision(string agentId, AutonomyLevel autonomy) =>
        new(
            0,
            new AgentId(agentId),
            new TargetId("t1"),
            autonomy,
            OrderKind.Hold,
            [],
            "test",
            1,
            10,
            0.5);
}
