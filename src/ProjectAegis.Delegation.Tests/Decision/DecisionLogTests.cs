namespace ProjectAegis.Delegation.Tests.Decision;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

[TestFixture]
public sealed class DecisionLogTests
{
    [Test]
    public void Append_preserves_order_for_aar_stream()
    {
        var log = new DecisionLog();
        log.Append(new DecisionRecord(
            SimTime: 1,
            AgentId: new AgentId("a1"),
            TargetId: new TargetId("u1"),
            AutonomyLevel.FullAutonomous,
            ChosenKind: OrderKind.Hold,
            Alternatives: Array.Empty<ScoredIntent>(),
            Rationale: "test",
            AttentionLoad: 5,
            AttentionBudget: 10,
            RngDraw: 0.42));

        Assert.That(log.Records, Has.Count.EqualTo(1));
        Assert.That(log.Records[0].ChosenKind, Is.EqualTo(OrderKind.Hold));
    }
}
