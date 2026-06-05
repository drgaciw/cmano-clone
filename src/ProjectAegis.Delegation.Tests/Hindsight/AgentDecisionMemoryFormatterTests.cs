using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Hindsight;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Hindsight;

[TestFixture]
public sealed class AgentDecisionMemoryFormatterTests
{
    [Test]
    public void Format_includes_intent_alternatives_attention_and_rng()
    {
        var payload = new AgentDecisionPayload(
            SimTick: 1420,
            SimTime: 1420.5,
            AgentId: new AgentId("EW-02"),
            TargetId: new TargetId("BRAVO-4"),
            AutonomyLevel: AutonomyLevel.FullAutonomous,
            ChosenOrderKind: OrderKind.Engage,
            ScoredIntents:
            [
                new ScoredIntent(OrderKind.Engage, 0.82, RiskLevel.High),
                new ScoredIntent(OrderKind.Hold, 0.41, RiskLevel.Low),
            ],
            Rationale: "High-value contact within range.",
            AttentionLoad: 14.4,
            AttentionBudget: 20,
            RngDraw: 0.713);

        var text = AgentDecisionMemoryFormatter.Format(payload, personalitySlug: "EwSpecialist");

        Assert.That(text, Does.Contain("sim_time=1420.5"));
        Assert.That(text, Does.Contain("agent=EW-02"));
        Assert.That(text, Does.Contain("personality=EwSpecialist"));
        Assert.That(text, Does.Contain("target=BRAVO-4"));
        Assert.That(text, Does.Contain("Chose Engage"));
        Assert.That(text, Does.Contain("Hold"));
        Assert.That(text, Does.Contain("Attention load 72%"));
        Assert.That(text, Does.Contain("rng_draw=0.713"));
    }
}
