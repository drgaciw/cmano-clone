namespace ProjectAegis.Delegation.UnityAdapter.Tests.Save;

using System.Globalization;
using NUnit.Framework;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Replay;
using ProjectAegis.Delegation.UnityAdapter.Save;

/// <summary>
/// Genuine deep-state differential proof assertion helper for DRG-349 (H6 save/resume).
/// Compares continuous N+M execution against N save/restore + M resumed execution across
/// clocks, hashes, DecisionLog, checkpoints, ReplayableSimWorld side effects, SimulationSession
/// magazines/kills/craft-ops/watch-queue, PendingApprovalQueue, and controller/agent states.
/// </summary>
internal static class SaveDifferentialAssert
{
    public static void AssertPendingApprovalsEqual(
        SaveResumeSessionUnit expected,
        SaveResumeSessionUnit actual,
        string context)
    {
        var expPending = expected.Bridge.Orchestrator.PendingApprovalQueue.Pending;
        var actPending = actual.Bridge.Orchestrator.PendingApprovalQueue.Pending;
        Assert.That(actPending.Count, Is.EqualTo(expPending.Count), $"{context}: PendingApprovalQueue.Count");
        for (var i = 0; i < expPending.Count; i++)
        {
            Assert.That(actPending[i].Order, Is.EqualTo(expPending[i].Order), $"{context}: Pending[{i}].Order");
        }
    }

    public static void AssertFullParity(
        SaveResumeSessionUnit expected,
        SaveResumeSessionUnit actual,
        string context)
    {
        // 1. Clocks & world hashes
        Assert.That(actual.Stepper.ObservedTick, Is.EqualTo(expected.Stepper.ObservedTick), $"{context}: ObservedTick");
        Assert.That(actual.Stepper.PipelineTick, Is.EqualTo(expected.Stepper.PipelineTick), $"{context}: PipelineTick");
        Assert.That(actual.Stepper.LastSimWorldHash, Is.EqualTo(expected.Stepper.LastSimWorldHash), $"{context}: LastSimWorldHash");
        Assert.That(actual.Stepper.DetectionWorldHash, Is.EqualTo(expected.Stepper.DetectionWorldHash), $"{context}: DetectionWorldHash");
        Assert.That(actual.Stepper.CombinedWorldHash, Is.EqualTo(expected.Stepper.CombinedWorldHash), $"{context}: CombinedWorldHash");

        // 2. SimulationSession runtime state
        var expSession = expected.Bridge.Session;
        var actSession = actual.Bridge.Session;
        Assert.That(actSession is null, Is.EqualTo(expSession is null), $"{context}: Session presence");
        if (expSession != null && actSession != null)
        {
            Assert.That(actSession.TimeAccelerationFactor, Is.EqualTo(expSession.TimeAccelerationFactor), $"{context}: TimeAccelerationFactor");
            Assert.That(actSession.IsSimPaused, Is.EqualTo(expSession.IsSimPaused), $"{context}: IsSimPaused");
            Assert.That(actSession.LastWatchPauseReason, Is.EqualTo(expSession.LastWatchPauseReason), $"{context}: LastWatchPauseReason");
            Assert.That(actSession.NextEngageSalvoOverride, Is.EqualTo(expSession.NextEngageSalvoOverride), $"{context}: NextEngageSalvoOverride");

            // Magazines
            var expMags = expSession.Magazines?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Engage.MagazineLedgerEntry>();
            var actMags = actSession.Magazines?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Engage.MagazineLedgerEntry>();
            Assert.That(actMags, Is.EqualTo(expMags), $"{context}: Magazines.Snapshot");

            // KilledTargets
            Assert.That(actSession.KilledTargets?.Count ?? 0, Is.EqualTo(expSession.KilledTargets?.Count ?? 0), $"{context}: KilledTargets.Count");
            Assert.That(actSession.KilledTargets?.MixHash() ?? 0UL, Is.EqualTo(expSession.KilledTargets?.MixHash() ?? 0UL), $"{context}: KilledTargets.MixHash");

            // AirOps
            var expAir = expSession.AirOps?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Logistics.AirOpsUnitState>();
            var actAir = actSession.AirOps?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Logistics.AirOpsUnitState>();
            Assert.That(actAir, Is.EqualTo(expAir), $"{context}: AirOps.Snapshot");

            // BoatOps
            var expBoat = expSession.BoatOps?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Logistics.BoatOpsUnitState>();
            var actBoat = actSession.BoatOps?.Snapshot() ?? Array.Empty<ProjectAegis.Sim.Logistics.BoatOpsUnitState>();
            Assert.That(actBoat, Is.EqualTo(expBoat), $"{context}: BoatOps.Snapshot");

            // WatchQueue
            Assert.That(actSession.WatchQueue.Cards.Count, Is.EqualTo(expSession.WatchQueue.Cards.Count), $"{context}: WatchQueue.Cards.Count");
            for (var i = 0; i < expSession.WatchQueue.Cards.Count; i++)
            {
                var ec = expSession.WatchQueue.Cards[i];
                var ac = actSession.WatchQueue.Cards[i];
                Assert.That(ac.EventId, Is.EqualTo(ec.EventId), $"{context}: WatchQueue[{i}].EventId");
                Assert.That(ac.IsAcknowledged, Is.EqualTo(ec.IsAcknowledged), $"{context}: WatchQueue[{i}].IsAcknowledged");
                Assert.That(ac.IsDismissed, Is.EqualTo(ec.IsDismissed), $"{context}: WatchQueue[{i}].IsDismissed");
            }
        }

        // 3. DecisionLog fingerprint, SHA-256, and chronological sequence/content
        var expLog = expected.Bridge.Orchestrator.DecisionLog;
        var actLog = actual.Bridge.Orchestrator.DecisionLog;
        Assert.That(
            OrderLogReplayFingerprint.ComputeSha256Hex(actLog),
            Is.EqualTo(OrderLogReplayFingerprint.ComputeSha256Hex(expLog)),
            $"{context}: OrderLogReplayFingerprint.ComputeSha256Hex");
        Assert.That(
            actLog.ComputeFingerprint(),
            Is.EqualTo(expLog.ComputeFingerprint()),
            $"{context}: DecisionLog.ComputeFingerprint");

        var expEntries = expLog.ChronologicalEntries();
        var actEntries = actLog.ChronologicalEntries();
        Assert.That(actEntries.Count, Is.EqualTo(expEntries.Count), $"{context}: DecisionLog chronological count");
        for (var i = 0; i < expEntries.Count; i++)
        {
            Assert.That(actEntries[i].SequenceId, Is.EqualTo(expEntries[i].SequenceId), $"{context}: Log[{i}].SequenceId");
            Assert.That(actEntries[i].Kind, Is.EqualTo(expEntries[i].Kind), $"{context}: Log[{i}].Kind");
            Assert.That(actEntries[i].SimTime, Is.EqualTo(expEntries[i].SimTime), $"{context}: Log[{i}].SimTime");
            if (expEntries[i].Payload is AgentDecisionPayload expDec &&
                actEntries[i].Payload is AgentDecisionPayload actDec)
            {
                Assert.That(actDec.SimTick, Is.EqualTo(expDec.SimTick), $"{context}: Log[{i}].SimTick");
                Assert.That(actDec.SimTime, Is.EqualTo(expDec.SimTime), $"{context}: Log[{i}].Payload.SimTime");
                Assert.That(actDec.AgentId, Is.EqualTo(expDec.AgentId), $"{context}: Log[{i}].AgentId");
                Assert.That(actDec.TargetId, Is.EqualTo(expDec.TargetId), $"{context}: Log[{i}].TargetId");
                Assert.That(actDec.AutonomyLevel, Is.EqualTo(expDec.AutonomyLevel), $"{context}: Log[{i}].AutonomyLevel");
                Assert.That(actDec.ChosenOrderKind, Is.EqualTo(expDec.ChosenOrderKind), $"{context}: Log[{i}].ChosenOrderKind");
                Assert.That(actDec.ScoredIntents, Is.EqualTo(expDec.ScoredIntents), $"{context}: Log[{i}].ScoredIntents");
                Assert.That(actDec.Rationale, Is.EqualTo(expDec.Rationale), $"{context}: Log[{i}].Rationale");
                Assert.That(actDec.AttentionLoad, Is.EqualTo(expDec.AttentionLoad), $"{context}: Log[{i}].AttentionLoad");
                Assert.That(actDec.AttentionBudget, Is.EqualTo(expDec.AttentionBudget), $"{context}: Log[{i}].AttentionBudget");
                Assert.That(actDec.RngDraw, Is.EqualTo(expDec.RngDraw), $"{context}: Log[{i}].RngDraw");
            }
            else
            {
                Assert.That(actEntries[i].Payload, Is.EqualTo(expEntries[i].Payload), $"{context}: Log[{i}].Payload");
            }
        }

        // 4. ReplayCheckpointStore boundaries
        var expCp = expected.Stepper.CheckpointStore.Checkpoints;
        var actCp = actual.Stepper.CheckpointStore.Checkpoints;
        Assert.That(actCp.Count, Is.EqualTo(expCp.Count), $"{context}: CheckpointStore.Count");
        for (var i = 0; i < expCp.Count; i++)
        {
            Assert.That(actCp[i].SimTick, Is.EqualTo(expCp[i].SimTick), $"{context}: Checkpoint[{i}].SimTick");
            Assert.That(actCp[i].WorldHash, Is.EqualTo(expCp[i].WorldHash), $"{context}: Checkpoint[{i}].WorldHash");
            Assert.That(actCp[i].LogFingerprint, Is.EqualTo(expCp[i].LogFingerprint), $"{context}: Checkpoint[{i}].LogFingerprint");
            Assert.That(actCp[i].LastSequenceId, Is.EqualTo(expCp[i].LastSequenceId), $"{context}: Checkpoint[{i}].LastSequenceId");
        }

        // 5. ReplayableSimWorld / IOrderSink side effects
        Assert.That(actual.World.AppliedOrders, Is.EqualTo(expected.World.AppliedOrders), $"{context}: AppliedOrders");
        Assert.That(actual.World.EmconPostures, Is.EquivalentTo(expected.World.EmconPostures), $"{context}: EmconPostures");
        Assert.That(actual.World.SensorPostures, Is.EquivalentTo(expected.World.SensorPostures), $"{context}: SensorPostures");
        Assert.That(actual.World.EwPostures, Is.EquivalentTo(expected.World.EwPostures), $"{context}: EwPostures");
        Assert.That(actual.World.CraftOpsCounts, Is.EquivalentTo(expected.World.CraftOpsCounts), $"{context}: CraftOpsCounts");
        Assert.That(
            actual.World.ComputeSideEffectsDigestSha256(),
            Is.EqualTo(expected.World.ComputeSideEffectsDigestSha256()),
            $"{context}: ComputeSideEffectsDigestSha256");

        // 6. Pending approvals & controller/agent state
        AssertPendingApprovalsEqual(expected, actual, context);

        var expBindings = expected.Bridge.Registry.Bindings.OrderBy(b => b.TargetId.Value, StringComparer.Ordinal).ToArray();
        var actBindings = actual.Bridge.Registry.Bindings.OrderBy(b => b.TargetId.Value, StringComparer.Ordinal).ToArray();
        Assert.That(actBindings.Length, Is.EqualTo(expBindings.Length), $"{context}: Registry.Bindings.Length");

        for (var i = 0; i < expBindings.Length; i++)
        {
            var eb = expBindings[i];
            var ab = actBindings[i];
            Assert.That(ab.TargetId, Is.EqualTo(eb.TargetId), $"{context}: Binding[{i}].TargetId");
            Assert.That(ab.Entity, Is.EqualTo(eb.Entity), $"{context}: Binding[{i}].Entity");
            Assert.That(ab.Target.Slot.Active is HumanController, Is.EqualTo(eb.Target.Slot.Active is HumanController), $"{context}: Binding[{i}].IsHumanControlled");

            if (eb.Target.Slot.Active is HumanController eh && ab.Target.Slot.Active is HumanController ah)
            {
                Assert.That(ah.PendingOrderCount, Is.EqualTo(eh.PendingOrderCount), $"{context}: Human[{eb.TargetId.Value}].PendingOrderCount");
            }

            var expAgent = eb.Target.Slot.Active as AgentController ?? eb.Target.Slot.SuspendedAgent;
            var actAgent = ab.Target.Slot.Active as AgentController ?? ab.Target.Slot.SuspendedAgent;
            Assert.That(actAgent is null, Is.EqualTo(expAgent is null), $"{context}: Agent presence for {eb.TargetId.Value}");
            if (expAgent != null && actAgent != null)
            {
                Assert.That(actAgent.Id, Is.EqualTo(expAgent.Id), $"{context}: Agent[{eb.TargetId.Value}].Id");
                Assert.That(actAgent.Traits, Is.EqualTo(expAgent.Traits), $"{context}: Agent[{eb.TargetId.Value}].Traits");
                Assert.That(actAgent.Autonomy, Is.EqualTo(expAgent.Autonomy), $"{context}: Agent[{eb.TargetId.Value}].Autonomy");
                Assert.That(actAgent.AttentionBudget, Is.EqualTo(expAgent.AttentionBudget), $"{context}: Agent[{eb.TargetId.Value}].AttentionBudget");
                Assert.That(actAgent.PersonalitySlug, Is.EqualTo(expAgent.PersonalitySlug), $"{context}: Agent[{eb.TargetId.Value}].PersonalitySlug");
                Assert.That(actAgent.IsActionsPaused, Is.EqualTo(expAgent.IsActionsPaused), $"{context}: Agent[{eb.TargetId.Value}].IsActionsPaused");
                Assert.That(actAgent.PolicySnapshotId, Is.EqualTo(expAgent.PolicySnapshotId), $"{context}: Agent[{eb.TargetId.Value}].PolicySnapshotId");
                Assert.That(actAgent.EffectivePolicy, Is.EqualTo(expAgent.EffectivePolicy), $"{context}: Agent[{eb.TargetId.Value}].EffectivePolicy");
            }
        }
    }

    public static void AssertHostUnchanged(
        HeadlessSaveResumeHost host,
        SaveResumeSessionUnit expectedUnit,
        int expectedPublishCount,
        ulong expectedCombinedWorldHash,
        string expectedOrderLogSha256,
        int expectedPendingCount,
        int expectedDecisionCount,
        string context)
    {
        Assert.That(ReferenceEquals(host.Active, expectedUnit), Is.True, $"{context}: Active unit reference changed");
        Assert.That(ReferenceEquals(host.Active.World, expectedUnit.World), Is.True, $"{context}: World reference changed");
        Assert.That(ReferenceEquals(host.Active.Snapshot, expectedUnit.Snapshot), Is.True, $"{context}: Snapshot reference changed");
        Assert.That(ReferenceEquals(host.Active.Sink, expectedUnit.Sink), Is.True, $"{context}: Sink reference changed");
        Assert.That(ReferenceEquals(host.Active.Bridge, expectedUnit.Bridge), Is.True, $"{context}: Bridge reference changed");
        Assert.That(host.PublishCount, Is.EqualTo(expectedPublishCount), $"{context}: PublishCount changed");
        Assert.That(host.Active.Stepper.CombinedWorldHash, Is.EqualTo(expectedCombinedWorldHash), $"{context}: CombinedWorldHash changed");
        Assert.That(
            OrderLogReplayFingerprint.ComputeSha256Hex(host.Active.Bridge.Orchestrator.DecisionLog),
            Is.EqualTo(expectedOrderLogSha256),
            $"{context}: DecisionLog SHA-256 changed");
        Assert.That(
            host.Active.Bridge.Orchestrator.PendingApprovalQueue.Pending.Count,
            Is.EqualTo(expectedPendingCount),
            $"{context}: PendingApprovalQueue count changed");
        Assert.That(
            host.Active.Bridge.Orchestrator.DecisionLog.ChronologicalEntries().Count,
            Is.EqualTo(expectedDecisionCount),
            $"{context}: DecisionLog chronological count changed");
    }

    public static void AssertRestoreLatencyUnderCeiling(
        double maxSeconds,
        double ceilingSeconds,
        int tickCount,
        int accelFactor)
    {
        Assert.That(
            maxSeconds,
            Is.LessThan(ceilingSeconds),
            string.Format(
                CultureInfo.InvariantCulture,
                "Restore latency max={0:F4}s at T={1}, {2}x exceeded ceiling {3:F1}s.",
                maxSeconds,
                tickCount,
                accelFactor,
                ceilingSeconds));
    }
}
