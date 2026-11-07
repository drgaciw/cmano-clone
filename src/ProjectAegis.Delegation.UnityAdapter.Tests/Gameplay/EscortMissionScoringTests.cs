namespace ProjectAegis.Delegation.UnityAdapter.Tests.Gameplay;

using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.UnityAdapter.Gameplay;
using ProjectAegis.Sim.Policy;

/// <summary>Mission scoring must reward outcomes without penalizing correctly withheld fire.</summary>
[TestFixture]
public sealed class EscortMissionScoringTests
{
    [TestCase("u1")]
    [TestCase("hostile-1")]
    public void Refused_orders_remain_diagnostics_and_do_not_change_mission_score(string unitId)
    {
        var mission = new EscortMissionSession();
        var before = mission.Presentation.Score;
        mission.Bridge.Orchestrator.DecisionLog.AppendPolicyDenial(new PolicyDenialRecord(
            0, 0, 0, new AgentId("test-officer"), new TargetId(unitId), 0,
            FireAbortReason.RoeHoldFire, OrderKind.Engage));

        Assert.That(mission.Bridge.Orchestrator.DecisionLog.PolicyDenials.Count, Is.EqualTo(1));
        Assert.That(mission.Presentation.Score, Is.EqualTo(before));
        Assert.That(mission.Presentation.RoundsRemaining, Is.EqualTo(4));
    }
}
