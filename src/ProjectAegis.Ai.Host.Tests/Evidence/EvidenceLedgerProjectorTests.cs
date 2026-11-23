using NUnit.Framework;
using ProjectAegis.Ai.Contracts;
using ProjectAegis.Ai.Host.Evidence;
using ProjectAegis.Data.Evidence;
using ProjectAegis.Delegation.Comms;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Ai.Host.Tests.Evidence;

[TestFixture]
public sealed class EvidenceLedgerProjectorTests
{
    [Test]
    public void Adapts_evidence_document_into_typed_mission_thread_ledger_without_mutating_order_log()
    {
        var log = new DecisionLog();
        log.Append(OrderLogEntryFactories.FromContactChange(
            new ContactChangeRecord(1, 1.0, 1, "u1", "c-1", "hostile-1", "Unknown", "Tracked")));
        log.Append(OrderLogEntryFactories.FromPolicyDenial(
            new PolicyDenialRecord(2, 2.0, 2, new AgentId("a1"), new TargetId("u1"), 42UL, FireAbortReason.WeaponsTight, OrderKind.Engage)));
        log.Append(OrderLogEntryFactories.FromCommsStateChange(
            new CommsStateChangeRecord(3, 3.0, 3, "u1", CommsState.Nominal, CommsState.Denied, "JAMMED")));
        log.Append(OrderLogEntry.FromDecisionRecord(
            new DecisionRecord(
                4.0,
                new AgentId("a1"),
                new TargetId("u1"),
                AutonomyLevel.SemiAutonomous,
                OrderKind.Engage,
                [new ScoredIntent(OrderKind.Engage, 0.9, RiskLevel.High)],
                "lock",
                2.0,
                10.0,
                0.25,
                SimTick: 4,
                PolicySnapshotId: 42UL,
                Personality: "aggressive"),
            simTick: 4));
        log.Append(OrderLogEntryFactories.FromEngagement(
            new EngagementRecord(5, 5.0, 5, new TargetId("u1"), 7001UL, true, null, new TargetId("hostile-1"), "asm", 2)));
        log.Append(OrderLogEntryFactories.FromPlatformDamageChange(
            new PlatformDamageChangeRecord(6, 6.0, 6, new TargetId("hostile-1"), 1.0, 0.3, "Hit", 2)));

        var beforeFp = log.ComputeFingerprint();
        var projected = new EvidenceProjector().Project(
            log,
            new EvidenceProjectionRequest("build-1", "baltic-patrol", "rev-1", ViewingSide.Blue, DebriefMode: true, FingerprintPrefix: "fp"));

        var ledger = EvidenceLedgerAdapter.FromEvidenceDocument(
            projected.Document,
            ledgerId: "ledger-host-1",
            seed: 42,
            replayWorldHash: "17144800277401907079");

        Assert.That(log.ComputeFingerprint(), Is.EqualTo(beforeFp));
        Assert.That(AiContractSchema.Version, Is.EqualTo(2));
        Assert.That(ledger.Entries.Select(e => e.Kind).Distinct().Count(), Is.EqualTo(6));
        Assert.That(ledger.Entries.All(e => e.HasValidPayload()), Is.True);
        Assert.That(ledger.ReplayFingerprint, Is.EqualTo(projected.Document.Header.OrderLogFingerprint));
        Assert.That(ledger.ReplayWorldHash, Is.EqualTo("17144800277401907079"));
    }
}
