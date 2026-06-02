using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Policy;
using NUnit.Framework;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class IOrderLogContractTests
{
    [Test]
    public void Orchestrator_OrderLog_surfaces_decision_log_entries()
    {
        var orchestrator = new DelegationOrchestrator(1);
        IOrderLog log = orchestrator.OrderLog;

        Assert.That(log, Is.SameAs(orchestrator.DecisionLog));
        Assert.That(log.ChronologicalEntries(), Is.Empty);
        Assert.That(log.ComputeFingerprint(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Append_OrderLogEntry_from_decision_record_appears_in_chronology()
    {
        var log = new DecisionLog();
        var record = new DecisionRecord(
            1.0,
            new("a1"),
            new("u1"),
            AutonomyLevel.FullAutonomous,
            OrderKind.Hold,
            Array.Empty<ScoredIntent>(),
            "test",
            0,
            20,
            0.5);

        log.Append(OrderLogEntry.FromDecisionRecord(record, simTick: 1ul));

        var entries = log.ChronologicalEntries();
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Kind, Is.EqualTo(OrderLogEntryKind.AgentDecision));
        Assert.That(entries[0].Payload, Is.TypeOf<AgentDecisionPayload>());
        var payload = (AgentDecisionPayload)entries[0].Payload!;
        Assert.That(payload.SimTick, Is.EqualTo(1ul));
        Assert.That(payload.ChosenOrderKind, Is.EqualTo(record.ChosenKind));
        Assert.That(payload.ToDecisionRecord() with { SimTick = 1 }, Is.EqualTo(record with { SimTick = 1 }));
    }
}