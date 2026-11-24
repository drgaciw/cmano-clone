using NUnit.Framework;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Orchestration;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Replay;
using ProjectAegis.Delegation.Sim;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.Traits;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

namespace ProjectAegis.Delegation.Tests.Decision;

[TestFixture]
public sealed class AgentDecisionProvenanceTests
{
    [Test]
    public void AgentDecisionPayload_and_DecisionRecord_round_trip_provenance_and_null_back_compat()
    {
        var withProvenance = new DecisionRecord(
            2.0,
            new AgentId("agent-1"),
            new TargetId("u1"),
            AutonomyLevel.SemiAutonomous,
            OrderKind.Engage,
            [new ScoredIntent(OrderKind.Engage, 0.8, RiskLevel.High)],
            "track confirmed",
            1.5,
            10.0,
            0.25,
            SimTick: 2,
            PolicySnapshotId: 77UL,
            Personality: "Aggressive");

        var payload = AgentDecisionPayload.FromDecisionRecord(withProvenance, simTick: 2);
        Assert.That(payload.PolicySnapshotId, Is.EqualTo(77UL));
        Assert.That(payload.Personality, Is.EqualTo("Aggressive"));
        Assert.That(payload.ToDecisionRecord(), Is.EqualTo(withProvenance));

        var withoutProvenance = withProvenance with { PolicySnapshotId = null, Personality = null };
        var nullPayload = AgentDecisionPayload.FromDecisionRecord(withoutProvenance, simTick: 2);
        Assert.That(nullPayload.PolicySnapshotId, Is.Null);
        Assert.That(nullPayload.Personality, Is.Null);
        Assert.That(nullPayload.ToDecisionRecord(), Is.EqualTo(withoutProvenance));
    }

    [Test]
    public void AgentController_TryDecide_records_policySnapshotId_and_personality_without_altering_replay_fingerprint()
    {
        var orchestratorPreset = new DelegationOrchestrator(4242);
        var presetUnit = new UnitTarget(new TargetId("u-preset"));
        var presetAgent = orchestratorPreset.CreateAgentFromPreset(
            new AgentId("a-preset"),
            PersonalityCatalog.All[0],
            AutonomyLevel.Assisted);
        orchestratorPreset.AssignAgentToTarget(
            presetAgent,
            presetUnit,
            new EffectivePolicy(RoeLevel.WeaponsTight, 2),
            capturedAtSimTick: 0);
        orchestratorPreset.Register(presetUnit);

        var customUnit = new UnitTarget(new TargetId("u-custom"));
        var customAgent = orchestratorPreset.CreateAgent(
            new AgentId("a-custom"),
            new TraitVector(0.8, 0.4, 1.0, 0.05, 0.75, 0.65),
            AutonomyLevel.FullAutonomous);
        orchestratorPreset.AssignAgentToTarget(
            customAgent,
            customUnit,
            EffectivePolicy.DefaultFree,
            capturedAtSimTick: 0);
        orchestratorPreset.Register(customUnit);

        orchestratorPreset.BeginExecution();
        orchestratorPreset.Tick(new ObservedState(
            SimTime: 1.0,
            ContactCount: 1,
            ActiveEngagementCount: 0,
            MemberAlive: new Dictionary<TargetId, bool> { [presetUnit.Id] = true, [customUnit.Id] = true },
            PrimaryHostileDestroyed: false));

        var decisionEntries = orchestratorPreset.DecisionLog.ChronologicalEntries()
            .Where(e => e.Kind == OrderLogEntryKind.AgentDecision)
            .ToArray();
        Assert.That(decisionEntries, Has.Length.EqualTo(2));

        foreach (var entry in decisionEntries)
        {
            var payload = (AgentDecisionPayload)entry.Payload;
            Assert.That(payload.PolicySnapshotId, Is.Not.Null.And.GreaterThan(0UL));
            Assert.That(string.IsNullOrWhiteSpace(payload.Personality), Is.False);

            var detail = DecisionLogDeepLinkProjection.Inspect(orchestratorPreset.DecisionLog, entry.SequenceId);
            Assert.That(detail.PolicySnapshotId, Is.EqualTo(payload.PolicySnapshotId));
            Assert.That(detail.Personality, Is.EqualTo(payload.Personality));
            Assert.That(detail.Fields.Any(f => f.Key == "PolicySnapshotId"), Is.True);
            Assert.That(detail.Fields.Any(f => f.Key == "Personality"), Is.True);
        }

        // Fogged FullAutonomous decision still returns NotFound without existence oracle in InspectLive
        var foggedEntry = decisionEntries.Single(e => ((AgentDecisionPayload)e.Payload).AutonomyLevel == AutonomyLevel.FullAutonomous);
        var liveFogged = DecisionLogDeepLinkProjection.InspectLive(
            orchestratorPreset.DecisionLog,
            PlayerInfoModel.DelegationFog,
            foggedEntry.SequenceId);
        Assert.That(liveFogged.Found, Is.False);
        Assert.That(liveFogged.UnavailableReason, Is.EqualTo(DecisionLogDeepLinkProjection.UnavailableReasonCode));
        Assert.That(liveFogged.Personality, Is.Null);
        Assert.That(liveFogged.PolicySnapshotId, Is.Null);

        // DecisionLog fingerprint & SHA-256 invariance with vs without provenance metadata on equivalent entries
        var strippedLog = new DecisionLog();
        foreach (var entry in orchestratorPreset.DecisionLog.ChronologicalEntries())
        {
            if (entry.Payload is AgentDecisionPayload p)
            {
                strippedLog.Append(new OrderLogEntry(
                    entry.SequenceId,
                    entry.Kind,
                    entry.SimTime,
                    p with { PolicySnapshotId = null, Personality = null }));
            }
            else
            {
                strippedLog.Append(entry);
            }
        }

        Assert.That(
            OrderLogReplayFingerprint.ComputeSha256Hex(orchestratorPreset.DecisionLog),
            Is.EqualTo(OrderLogReplayFingerprint.ComputeSha256Hex(strippedLog)));

        // Legacy record with null provenance projects non-null fallback labels in Inspect
        var legacySeq = strippedLog.ChronologicalEntries().First(e => e.Kind == OrderLogEntryKind.AgentDecision).SequenceId;
        var legacyDetail = DecisionLogDeepLinkProjection.Inspect(strippedLog, legacySeq);
        Assert.That(legacyDetail.PolicySnapshotId, Is.EqualTo(0UL));
        Assert.That(legacyDetail.Personality, Is.EqualTo("unspecified"));
    }
}
