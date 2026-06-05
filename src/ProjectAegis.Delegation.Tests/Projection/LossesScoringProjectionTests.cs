using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Projection;

public sealed class LossesScoringProjectionTests
{
    [Test]
    public void Project_counts_kills_and_denials_in_score()
    {
        var log = new DecisionLog();
        log.AppendEngagementOutcome(new EngagementOutcomeRecord(
            1, 1, 1, new TargetId("u1"), new TargetId("hostile-1"), 1,
            EngagementOutcomeCodes.Kill, 0.1));
        log.AppendPolicyDenial(new PolicyDenialRecord(
            2, 2.0, 2,
            new AgentId("a1"),
            new TargetId("hostile-1"),
            0,
            FireAbortReason.RoeHoldFire,
            OrderKind.Engage));

        var tally = LossesScoringProjection.Project(log);
        Assert.That(tally.HostileKills, Is.EqualTo(1));
        Assert.That(tally.PolicyDenials, Is.EqualTo(1));
        Assert.That(tally.Score, Is.EqualTo(100 - 5));
    }
}