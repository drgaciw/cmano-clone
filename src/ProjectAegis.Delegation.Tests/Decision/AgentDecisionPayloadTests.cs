using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class AgentDecisionPayloadTests
{
    [Test]
    public void FromDecisionRecord_maps_all_fields_including_simTick()
    {
        var record = new DecisionRecord(
            2.5,
            new AgentId("a1"),
            new TargetId("u1"),
            AutonomyLevel.Assisted,
            OrderKind.Move,
            new[] { new ScoredIntent(OrderKind.Hold, 1.0, RiskLevel.Low) },
            "rationale",
            0.3,
            20,
            0.42,
            SimTick: 99);

        var payload = AgentDecisionPayload.FromDecisionRecord(record, simTick: 7);

        Assert.That(payload.SimTick, Is.EqualTo(7ul));
        Assert.That(payload.SimTime, Is.EqualTo(2.5));
        Assert.That(payload.AgentId, Is.EqualTo(record.AgentId));
        Assert.That(payload.TargetId, Is.EqualTo(record.TargetId));
        Assert.That(payload.AutonomyLevel, Is.EqualTo(record.AutonomyLevel));
        Assert.That(payload.ChosenOrderKind, Is.EqualTo(record.ChosenKind));
        Assert.That(payload.ScoredIntents, Is.SameAs(record.Alternatives));
        Assert.That(payload.Rationale, Is.EqualTo("rationale"));
        Assert.That(payload.AttentionLoad, Is.EqualTo(0.3));
        Assert.That(payload.AttentionBudget, Is.EqualTo(20));
        Assert.That(payload.RngDraw, Is.EqualTo(0.42));
    }

    [Test]
    public void ToDecisionRecord_round_trips_for_replay_compat()
    {
        var original = new DecisionRecord(
            1.0,
            new AgentId("a2"),
            new TargetId("u2"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Engage,
            Array.Empty<ScoredIntent>(),
            "x",
            1,
            10,
            0.1,
            SimTick: 4);

        var payload = AgentDecisionPayload.FromDecisionRecord(original, simTick: 4);
        var roundTrip = payload.ToDecisionRecord();

        Assert.That(roundTrip, Is.EqualTo(original));
    }

    [Test]
    public void OrderLogEntry_FromDecisionRecord_uses_typed_payload_not_legacy_record()
    {
        var record = new DecisionRecord(
            0,
            new AgentId("a1"),
            new TargetId("u1"),
            AutonomyLevel.Manual,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "",
            0,
            0,
            0);

        var entry = OrderLogEntry.FromDecisionRecord(record, simTick: 12);

        Assert.That(entry.Payload, Is.TypeOf<AgentDecisionPayload>());
        Assert.That(((AgentDecisionPayload)entry.Payload).SimTick, Is.EqualTo(12ul));
        Assert.That(entry.Payload, Is.Not.SameAs(record));
    }
}