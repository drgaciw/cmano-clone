using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Orchestration;

[TestFixture]
public sealed class PlayerInfoFilterTests
{
    [Test]
    public void FullTransparency_returns_all_entries()
    {
        var entries = SampleEntries();
        var filtered = PlayerInfoFilter.FilterLiveEntries(entries, PlayerInfoModel.FullTransparency);
        Assert.That(filtered.Count, Is.EqualTo(entries.Count));
    }

    [Test]
    public void DelegationFog_hides_full_autonomous_decisions_but_keeps_alerts()
    {
        var entries = SampleEntries();
        var filtered = PlayerInfoFilter.FilterLiveEntries(entries, PlayerInfoModel.DelegationFog);

        Assert.That(filtered, Has.Count.EqualTo(2));
        var decisions = filtered.Where(e => e.Kind == OrderLogEntryKind.AgentDecision).ToList();
        Assert.That(decisions, Has.Count.EqualTo(1));
        Assert.That(GetAutonomy(decisions[0]), Is.EqualTo(AutonomyLevel.Assisted));
        Assert.That(filtered.Any(e => e.Kind == OrderLogEntryKind.PolicyDenial), Is.True);
    }

    [Test]
    public void Orchestrator_GetLiveOrderLogView_uses_scenario_player_info_model()
    {
        var orchestrator = new DelegationOrchestrator(1)
        {
            ScenarioPolicy = new ScenarioPolicyProfile(
                EffectivePolicy.DefaultFree,
                playerInfoModel: PlayerInfoModel.DelegationFog),
        };

        orchestrator.DecisionLog.Append(MakeDecision(AutonomyLevel.Assisted));
        orchestrator.DecisionLog.Append(MakeDecision(AutonomyLevel.FullAutonomous));
        orchestrator.DecisionLog.AppendPolicyDenial(new PolicyDenialRecord(
            0, 1, 1, new AgentId("a1"), new TargetId("t1"), 1,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));

        var live = orchestrator.GetLiveOrderLogView();

        Assert.That(live, Has.Count.EqualTo(2));
        Assert.That(live.Count(e => e.Kind == OrderLogEntryKind.AgentDecision), Is.EqualTo(1));
        Assert.That(live.Any(e => e.Kind == OrderLogEntryKind.PolicyDenial), Is.True);
    }

    private static IReadOnlyList<OrderLogEntry> SampleEntries() =>
    [
        new(1, OrderLogEntryKind.AgentDecision, 0, MakeDecision(AutonomyLevel.Assisted)),
        new(2, OrderLogEntryKind.AgentDecision, 1, MakeDecision(AutonomyLevel.FullAutonomous)),
        new(3, OrderLogEntryKind.PolicyDenial, 1, new PolicyDenialRecord(
            3, 1, 1, new AgentId("a1"), new TargetId("t1"), 1,
            FireAbortReason.RoeHoldFire, OrderKind.Engage)),
    ];

    private static AutonomyLevel GetAutonomy(OrderLogEntry entry) =>
        entry.Payload switch
        {
            AgentDecisionPayload p => p.AutonomyLevel,
            DecisionRecord r => r.AutonomyLevel,
            _ => throw new InvalidOperationException("Unexpected agent decision payload"),
        };

    private static DecisionRecord MakeDecision(AutonomyLevel autonomy) =>
        new(
            0,
            new AgentId("a1"),
            new TargetId("t1"),
            autonomy,
            OrderKind.Hold,
            [],
            "test",
            1,
            10,
            0.5);
}
