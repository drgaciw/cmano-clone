namespace ProjectAegis.Delegation.UnityAdapter.Save;

using Baltic;
using Bridge;
using Controllers;
using Core;
using Data.Catalog;
using Data.Scenario;
using Data.Snapshots;
using Input;
using PlayDelegation;
using Replay;
using Targets;
using Watch;
using ProjectAegis.Sim.Scenario;

public sealed record SaveResumeSessionUnit(
    ReplayableSimWorld World,
    ISimWorldSnapshot Snapshot,
    IOrderSink Sink,
    DelegationBridge Bridge,
    BalticReplayStepper Stepper,
    SessionCommandFacade Facade,
    SaveIdentityContext Identity);

public interface ISaveResumeHost
{
    SaveResumeSessionUnit Active { get; }

    void SwapUnit(SaveResumeSessionUnit candidate);

    void PublishProjections(SaveResumeSessionUnit restored);
}

public sealed class HeadlessSaveResumeHost : ISaveResumeHost
{
    private readonly Action<SaveResumeSessionUnit>? _onPublish;

    public HeadlessSaveResumeHost(
        SaveResumeSessionUnit initial,
        Action<SaveResumeSessionUnit>? onPublish = null)
    {
        Active = initial ?? throw new ArgumentNullException(nameof(initial));
        _onPublish = onPublish;
    }

    public SaveResumeSessionUnit Active { get; private set; }

    public int PublishCount { get; private set; }

    public void SwapUnit(SaveResumeSessionUnit candidate)
    {
        Active = candidate ?? throw new ArgumentNullException(nameof(candidate));
    }

    public void PublishProjections(SaveResumeSessionUnit restored)
    {
        PublishCount++;
        _onPublish?.Invoke(restored);
    }
}

public sealed record SaveCandidateRuntimeArgs(
    bool MvpEngagement = true,
    ICatalogReader? Catalog = null,
    IReadOnlyDictionary<string, bool>? UnitReadiness = null,
    IReadOnlyList<ScenarioNearFutureUnitRequest>? NearFutureUnits = null,
    int MaxTechnologyLevel = 2,
    bool ReportWatchTransitions = false);

/// <summary>
/// H6-05 / DRG-349: 9-step isolated candidate restore-from-tick-0 driver (ADR-029 Option B, Waiver W0).
/// Validates envelope syntax, integrity, compatibility, and shape before building any candidate;
/// replays scenario setup + ordered journal from tick 0; verifies all checksums; clears watch
/// acknowledgement/dismissal flags and pauses the candidate; atomically swaps world/snapshot/sink/bridge;
/// and leaves the active host and static <see cref="BalticV3SideRegistry"/> untouched on any failure.
/// </summary>
public static class SaveResumeDriver
{
    public static void ClearWatchFlags(WatchAttentionQueue? queue)
    {
        if (queue is null || queue.Cards.Count == 0)
        {
            return;
        }

        var events = new WatchAttentionEvent[queue.Cards.Count];
        for (var i = 0; i < queue.Cards.Count; i++)
        {
            events[i] = queue.Cards[i].Event;
        }

        queue.Clear();
        for (var i = 0; i < events.Length; i++)
        {
            queue.Enqueue(events[i]);
        }
    }

    public static SaveResult<SaveResumeSessionUnit> TryRestoreFromSlot(
        ISaveResumeHost host,
        SaveSlotStore store,
        string slotId,
        Func<SaveIdentityContext, SaveCandidateRuntimeArgs>? candidateArgsFactory = null)
    {
        if (host is null) return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.InvalidArgument, "Host is null.");
        if (store is null) return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.InvalidArgument, "Slot store is null.");

        var loaded = store.Load(slotId);
        if (!loaded.Succeeded || loaded.Value is null)
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(
                loaded.FailureCode,
                loaded.FailureReason ?? $"Failed to load slot '{slotId}'.");
        }

        return TryRestoreEnvelope(host, loaded.Value, candidateArgsFactory);
    }

    public static SaveResult<SaveResumeSessionUnit> TryRestore(
        ISaveResumeHost host,
        ReadOnlySpan<byte> utf8Bytes,
        Func<SaveIdentityContext, SaveCandidateRuntimeArgs>? candidateArgsFactory = null)
    {
        if (host is null)
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.InvalidArgument, "Host is null.");
        }

        // Steps 1 & 2 & 4 (parse, schema version, bounds, network-field rejection, journal digest, envelope SHA-256)
        var parsed = SaveEnvelopeSerializer.TryDeserializeAndVerify(utf8Bytes);
        if (!parsed.Succeeded || parsed.Value is null)
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(
                parsed.FailureCode,
                parsed.FailureReason ?? "Failed to parse save envelope.");
        }

        return TryRestoreEnvelope(host, parsed.Value, candidateArgsFactory);
    }

    public static SaveResult<SaveResumeSessionUnit> TryRestoreEnvelope(
        ISaveResumeHost host,
        SaveEnvelope envelope,
        Func<SaveIdentityContext, SaveCandidateRuntimeArgs>? candidateArgsFactory = null)
    {
        if (host is null) return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.InvalidArgument, "Host is null.");
        if (envelope is null) return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.InvalidArgument, "Envelope is null.");

        // Step 2 re-check in case envelope was constructed in-memory
        var expectedDigest = SaveEnvelopeSerializer.ComputeJournalDigestSha256(envelope.InputJournal.Rows);
        if (!string.Equals(expectedDigest, envelope.Checksums.JournalDigest, StringComparison.Ordinal))
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.IntegrityMismatch, "Journal digest mismatch.");
        }

        var expectedEnvHash = SaveEnvelopeSerializer.ComputeEnvelopeHashSha256(
            envelope.Identity, envelope.Position, envelope.InputJournal, envelope.Checksums);
        if (!string.Equals(expectedEnvHash, envelope.EnvelopeHashSha256, StringComparison.Ordinal))
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.IntegrityMismatch, "Envelope hash mismatch.");
        }

        // Step 3 (Compatibility — BEFORE constructing any candidate)
        var activeIdentity = host.Active.Identity;
        if (!TryValidateIdentityCompatibility(envelope.Identity, activeIdentity, out var compatCode, out var compatReason))
        {
            return SaveResult<SaveResumeSessionUnit>.Fail(compatCode, compatReason!);
        }

        // Snapshot active side registry state so any candidate setup/replay failure restores it cleanly
        var activeSideSnapshot = CaptureSideRegistrySnapshot(host.Active);
        try
        {
            // Step 5 (Build isolated candidate off to the side)
            var runtimeArgs = candidateArgsFactory?.Invoke(activeIdentity) ?? new SaveCandidateRuntimeArgs();
            BalticV3SideRegistry.ClearScenarioSides();

            var candidateWorld = new ReplayableSimWorld();
            var candidateStepper = BalticReplayStepper.Create(
                seed: activeIdentity.GlobalSeed,
                scenarioPolicyId: activeIdentity.ScenarioId,
                mvpEngagement: runtimeArgs.MvpEngagement,
                catalog: runtimeArgs.Catalog,
                unitReadiness: runtimeArgs.UnitReadiness,
                nearFutureUnits: runtimeArgs.NearFutureUnits,
                maxTechnologyLevel: runtimeArgs.MaxTechnologyLevel,
                externalOrderSink: candidateWorld,
                reportWatchTransitions: runtimeArgs.ReportWatchTransitions);

            candidateStepper.Bridge.AttachReplayViewer = false;
            candidateWorld.BindInnerSnapshot(
                candidateStepper.Snapshot,
                candidateStepper.Bridge.Registry,
                activeIdentity.GlobalSeed);
            var candidateFacade = new SessionCommandFacade(
                candidateStepper.Bridge,
                observedTickProvider: () => candidateStepper.ObservedTick,
                combinedWorldHashProvider: () => candidateStepper.CombinedWorldHash);

            // Step 6 (Replay journal from tick 0 through shared stepper)
            var rows = envelope.InputJournal.Rows;
            for (var i = 0; i < rows.Count; i++)
            {
                if (rows[i].ObservedTick > envelope.Position.ObservedTick)
                {
                    RestoreSideRegistrySnapshot(activeSideSnapshot);
                    return SaveResult<SaveResumeSessionUnit>.Fail(
                        SaveFailureCode.IntegrityMismatch,
                        $"Journal row {i} observedTick={rows[i].ObservedTick} exceeds save observedTick={envelope.Position.ObservedTick}.");
                }

                if (i > 0 && rows[i].ObservedTick < rows[i - 1].ObservedTick)
                {
                    RestoreSideRegistrySnapshot(activeSideSnapshot);
                    return SaveResult<SaveResumeSessionUnit>.Fail(
                        SaveFailureCode.IntegrityMismatch,
                        $"Journal row {i} observedTick={rows[i].ObservedTick} decreases below row {i - 1}.");
                }
            }

            var rowCursor = 0;
            while (rowCursor < rows.Count && rows[rowCursor].ObservedTick == 0)
            {
                if (!TryDispatchJournalRow(candidateFacade, candidateWorld, rows[rowCursor], out var dispatchErr))
                {
                    RestoreSideRegistrySnapshot(activeSideSnapshot);
                    return SaveResult<SaveResumeSessionUnit>.Fail(
                        SaveFailureCode.ReplayCommandFailed,
                        dispatchErr ?? $"Journal row {rowCursor} failed during tick 0 replay.");
                }

                rowCursor++;
            }

            for (ulong tick = 1; tick <= envelope.Position.ObservedTick; tick++)
            {
                candidateStepper.StepOneObservedTick();
                candidateWorld.AdvanceKinematics(1.0);
                candidateFacade.SyncShadowStateAfterTick();

                while (rowCursor < rows.Count && rows[rowCursor].ObservedTick == tick)
                {
                    if (!TryDispatchJournalRow(candidateFacade, candidateWorld, rows[rowCursor], out var dispatchErr))
                    {
                        RestoreSideRegistrySnapshot(activeSideSnapshot);
                        return SaveResult<SaveResumeSessionUnit>.Fail(
                            SaveFailureCode.ReplayCommandFailed,
                            dispatchErr ?? $"Journal row {rowCursor} failed at observedTick={tick}.");
                    }

                    rowCursor++;
                }
            }

            if (rowCursor != rows.Count)
            {
                RestoreSideRegistrySnapshot(activeSideSnapshot);
                return SaveResult<SaveResumeSessionUnit>.Fail(
                    SaveFailureCode.ReplayCommandFailed,
                    $"Unconsumed journal rows remaining after tick {envelope.Position.ObservedTick}.");
            }

            // Step 7 (Verify all checksums & position before swap)
            if (!TryVerifyCandidateChecksums(candidateStepper, candidateFacade, envelope, out var verifyErr))
            {
                RestoreSideRegistrySnapshot(activeSideSnapshot);
                return SaveResult<SaveResumeSessionUnit>.Fail(SaveFailureCode.ReplayMismatch, verifyErr!);
            }

            // Step 8 (Clear watch flags, pause candidate, atomic swap)
            ClearWatchFlags(candidateStepper.Bridge.Session?.WatchQueue);
            candidateStepper.Bridge.Session?.PauseSim();
            candidateFacade.SyncShadowStateAfterTick();

            var candidateUnit = new SaveResumeSessionUnit(
                candidateWorld,
                candidateWorld,
                candidateWorld,
                candidateStepper.Bridge,
                candidateStepper,
                candidateFacade,
                activeIdentity);

            host.SwapUnit(candidateUnit);

            // Step 9 (Publish projections)
            host.PublishProjections(candidateUnit);

            return SaveResult<SaveResumeSessionUnit>.Ok(candidateUnit);
        }
        catch (Exception ex)
        {
            RestoreSideRegistrySnapshot(activeSideSnapshot);
            return SaveResult<SaveResumeSessionUnit>.Fail(
                SaveFailureCode.ReplayCommandFailed,
                $"Candidate restore threw exception: {ex.Message}");
        }
    }

    private static bool TryValidateIdentityCompatibility(
        SaveIdentityBlock saved,
        SaveIdentityContext live,
        out SaveFailureCode code,
        out string? reason)
    {
        if (saved.SchemaVersion != SaveEnvelope.CurrentSchemaVersion)
        {
            code = SaveFailureCode.UnknownSchemaVersion;
            reason = $"SchemaVersion mismatch: saved={saved.SchemaVersion}, expected={SaveEnvelope.CurrentSchemaVersion}.";
            return false;
        }

        if (!string.Equals(saved.BuildId, live.BuildId, StringComparison.Ordinal))
        {
            code = SaveFailureCode.BuildMismatch;
            reason = $"BuildId mismatch: saved='{saved.BuildId}', live='{live.BuildId}'.";
            return false;
        }

        if (!string.Equals(saved.ScenarioId, live.ScenarioId, StringComparison.Ordinal) ||
            !string.Equals(saved.ScenarioContentHash, live.ScenarioContentHash, StringComparison.Ordinal))
        {
            code = SaveFailureCode.ScenarioMismatch;
            reason = $"Scenario identity mismatch: saved='{saved.ScenarioId}', live='{live.ScenarioId}'.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(saved.CatalogSnapshotId) ||
            string.IsNullOrWhiteSpace(live.CatalogSnapshotId) ||
            !string.Equals(saved.CatalogSnapshotId, live.CatalogSnapshotId, StringComparison.Ordinal) ||
            !CatalogFullSnapshotHasher.AreCompatibleHashes(saved.WholeCatalogHash, live.WholeCatalogHash))
        {
            code = SaveFailureCode.CatalogMismatch;
            reason = $"Catalog identity mismatch or empty hash: saved='{saved.CatalogSnapshotId}', live='{live.CatalogSnapshotId}'.";
            return false;
        }

        var savedPolicies = saved.PolicyHashes.OrderBy(p => p.PolicyId, StringComparer.Ordinal).ToArray();
        var livePolicies = live.PolicyHashes.OrderBy(p => p.PolicyId, StringComparer.Ordinal).ToArray();
        if (savedPolicies.Length != livePolicies.Length)
        {
            code = SaveFailureCode.PolicyMismatch;
            reason = $"Bound policy count mismatch: saved={savedPolicies.Length}, live={livePolicies.Length}.";
            return false;
        }

        for (var i = 0; i < savedPolicies.Length; i++)
        {
            if (!string.Equals(savedPolicies[i].PolicyId, livePolicies[i].PolicyId, StringComparison.Ordinal) ||
                !string.Equals(savedPolicies[i].PolicySha256, livePolicies[i].PolicySha256, StringComparison.OrdinalIgnoreCase))
            {
                code = SaveFailureCode.PolicyMismatch;
                reason = $"Policy hash mismatch for '{livePolicies[i].PolicyId}'.";
                return false;
            }
        }

        if (saved.GlobalSeed != live.GlobalSeed)
        {
            code = SaveFailureCode.SeedMismatch;
            reason = $"GlobalSeed mismatch: saved={saved.GlobalSeed}, live={live.GlobalSeed}.";
            return false;
        }

        code = SaveFailureCode.None;
        reason = null;
        return true;
    }

    private static bool TryVerifyCandidateChecksums(
        BalticReplayStepper candidateStepper,
        SessionCommandFacade candidateFacade,
        SaveEnvelope envelope,
        out string? error)
    {
        if (candidateStepper.PipelineTick != envelope.Position.PipelineTick)
        {
            error = $"PipelineTick mismatch: candidate={candidateStepper.PipelineTick}, expected={envelope.Position.PipelineTick}.";
            return false;
        }

        var candidateDigest = candidateFacade.Journal.ComputeDigestSha256();
        if (!string.Equals(candidateDigest, envelope.Checksums.JournalDigest, StringComparison.Ordinal))
        {
            error = $"Journal digest mismatch after replay: candidate={candidateDigest}, expected={envelope.Checksums.JournalDigest}.";
            return false;
        }

        var log = candidateStepper.Bridge.Orchestrator.DecisionLog;
        var candidateLogSha = OrderLogReplayFingerprint.ComputeSha256Hex(log);
        if (!string.Equals(candidateLogSha, envelope.Checksums.OrderLogFingerprintSha256, StringComparison.Ordinal))
        {
            error = $"OrderLog SHA-256 mismatch: candidate={candidateLogSha}, expected={envelope.Checksums.OrderLogFingerprintSha256}.";
            return false;
        }

        var candidateLastWorldHash = candidateStepper.LastSimWorldHash;
        if (candidateLastWorldHash != envelope.Checksums.LastWorldHash)
        {
            error = $"LastWorldHash mismatch: candidate={candidateLastWorldHash}, expected={envelope.Checksums.LastWorldHash}.";
            return false;
        }

        if (candidateStepper.CombinedWorldHash != envelope.Checksums.CombinedWorldHash)
        {
            error = $"CombinedWorldHash mismatch: candidate={candidateStepper.CombinedWorldHash}, expected={envelope.Checksums.CombinedWorldHash}.";
            return false;
        }

        var candidateLastSeqId = log.ChronologicalEntries().LastOrDefault()?.SequenceId ?? 0UL;
        if (candidateLastSeqId != envelope.Position.LastSequenceId)
        {
            error = $"LastSequenceId mismatch: candidate={candidateLastSeqId}, expected={envelope.Position.LastSequenceId}.";
            return false;
        }

        var candidateCheckpoints = candidateStepper.CheckpointStore.Checkpoints
            .Where(c => c.SimTick <= envelope.Position.ObservedTick)
            .ToArray();
        var expectedCheckpoints = envelope.Checksums.CheckpointRows;
        if (candidateCheckpoints.Length != expectedCheckpoints.Count)
        {
            error = $"Checkpoint count mismatch: candidate={candidateCheckpoints.Length}, expected={expectedCheckpoints.Count}.";
            return false;
        }

        for (var i = 0; i < candidateCheckpoints.Length; i++)
        {
            var a = candidateCheckpoints[i];
            var b = expectedCheckpoints[i];
            if (a.SimTick != b.SimTick ||
                a.WorldHash != b.WorldHash ||
                !string.Equals(a.LogFingerprint, b.LogFingerprint, StringComparison.Ordinal) ||
                a.LastSequenceId != b.LastSequenceId)
            {
                error = $"Checkpoint[{i}] mismatch at tick {a.SimTick}.";
                return false;
            }
        }

        error = null;
        return true;
    }

    private static bool TryDispatchJournalRow(
        SessionCommandFacade facade,
        ReplayableSimWorld world,
        SaveJournalRow row,
        out string? error)
    {
        error = null;
        if (!InputCommandInventory.TryGet(row.CommandKind, out var descriptor) || descriptor is null)
        {
            error = $"Unknown journal command '{row.CommandKind}' at sequence {row.SequenceOrder}.";
            return false;
        }

        var args = row.Arguments;
        bool ok;
        switch (descriptor.Kind)
        {
            case InputCommandJournalKind.TakeDirectControl:
                ok = args.EntityKeyValue.HasValue && facade.TryTakeDirectControl(new EntityKey((int)args.EntityKeyValue.Value), args.SimTime);
                break;
            case InputCommandJournalKind.ReleaseDirectControl:
                ok = args.EntityKeyValue.HasValue && facade.TryReleaseDirectControl(new EntityKey((int)args.EntityKeyValue.Value), args.SimTime);
                break;
            case InputCommandJournalKind.PauseAgentActions:
                ok = !string.IsNullOrEmpty(args.TargetId) && facade.TryPauseAgentActions(new TargetId(args.TargetId!), args.SimTime);
                break;
            case InputCommandJournalKind.ResumeAgentActions:
                ok = !string.IsNullOrEmpty(args.TargetId) && facade.TryResumeAgentActions(new TargetId(args.TargetId!), args.SimTime);
                break;
            case InputCommandJournalKind.EnqueueHumanOrder:
                ok = args.EntityKeyValue.HasValue && args.OrderKind.HasValue &&
                    facade.TryEnqueueHumanOrder(new EntityKey((int)args.EntityKeyValue.Value), args.OrderKind.Value, args.SimTime, args.RiskLevel);
                break;
            case InputCommandJournalKind.IssuePlayerCommand:
                ok = args.EntityKeyValue.HasValue && !string.IsNullOrEmpty(args.SubCommandId) &&
                    facade.TryIssuePlayerCommand(new EntityKey((int)args.EntityKeyValue.Value), args.SubCommandId!, args.SimTime, out _);
                break;
            case InputCommandJournalKind.EnqueueAttackOption:
                ok = args.EntityKeyValue.HasValue && !string.IsNullOrEmpty(args.OptionId) &&
                    facade.TryEnqueueAttackOption(new EntityKey((int)args.EntityKeyValue.Value), args.OptionId!, world, out _);
                if (ok && args.ResolvedSalvo.HasValue && facade.Bridge.Session?.NextEngageSalvoOverride != args.ResolvedSalvo)
                {
                    error = $"ResolvedSalvo mismatch at seq {row.SequenceOrder}.";
                    return false;
                }
                break;
            case InputCommandJournalKind.ApprovePendingOrder:
                ok = args.OrderId.HasValue && facade.TryApprovePendingOrder(new OrderId(args.OrderId.Value));
                break;
            case InputCommandJournalKind.RejectPendingOrder:
                ok = args.OrderId.HasValue && facade.TryRejectPendingOrder(new OrderId(args.OrderId.Value));
                break;
            case InputCommandJournalKind.RebindAgentTraits:
                ok = !string.IsNullOrEmpty(args.TargetId) && args.Traits is not null &&
                    facade.TryRebindAgentTraits(new TargetId(args.TargetId!), args.Traits.ToTraitVector());
                break;
            case InputCommandJournalKind.PauseSim:
                ok = facade.TryPauseSim(out _);
                break;
            case InputCommandJournalKind.ResumeSim:
                ok = args.ExplicitOverride.HasValue && facade.TryResumeSim(args.ExplicitOverride.Value, out _);
                break;
            case InputCommandJournalKind.SetTimeAccelerationFactor:
                ok = args.AccelerationFactor.HasValue && facade.TrySetTimeAccelerationFactor(args.AccelerationFactor.Value, out _);
                break;
            case InputCommandJournalKind.ContactReclassifyOverride:
                ok = facade.TryReclassifyContact(args.ContactId, args.Affiliation, args.SimTime, out _);
                break;
            case InputCommandJournalKind.ContactDropOverride:
                ok = facade.TryDropContact(args.ContactId, args.SimTime, out _);
                break;
            case InputCommandJournalKind.RebriefAgent:
                ok = !string.IsNullOrEmpty(args.TargetId) && !string.IsNullOrEmpty(args.PresetName) &&
                    facade.TryRebriefAgent(new TargetId(args.TargetId!), args.PresetName!, args.SimTime);
                break;
            case InputCommandJournalKind.AssignAgentController:
                ok = !string.IsNullOrEmpty(args.TargetId) && !string.IsNullOrEmpty(args.AgentId) &&
                    args.Traits is not null && args.AutonomyLevel.HasValue &&
                    facade.TryAssignAgentController(
                        new TargetId(args.TargetId!),
                        args.AgentId!,
                        args.Traits.ToTraitVector(),
                        args.AutonomyLevel.Value,
                        args.AttentionBudget ?? 20.0,
                        args.PresetName,
                        args.IsFriendly ?? true,
                        args.SimTime);
                break;
            case InputCommandJournalKind.DoctrineOverride:
                ok = !string.IsNullOrEmpty(args.TargetId) && !string.IsNullOrEmpty(args.RoeLevelLabel) &&
                    facade.TryApplyDoctrineOverride(new TargetId(args.TargetId!), args.RoeLevelLabel!, args.SimTime);
                break;
            case InputCommandJournalKind.AcknowledgeWatchAttention:
                ok = !string.IsNullOrEmpty(args.WatchEventId) && facade.TryAcknowledgeWatchAttention(args.WatchEventId!);
                break;
            case InputCommandJournalKind.DismissWatchAttention:
                ok = !string.IsNullOrEmpty(args.WatchEventId) && facade.TryDismissWatchAttention(args.WatchEventId!);
                break;
            case InputCommandJournalKind.SetAgentPersonality:
                ok = !string.IsNullOrEmpty(args.TargetId) && !string.IsNullOrEmpty(args.PresetName) &&
                    facade.TrySetAgentPersonality(null, new TargetId(args.TargetId!), args.PresetName!, args.SimTime, out _);
                break;
            case InputCommandJournalKind.SetAgentAutonomy:
                ok = !string.IsNullOrEmpty(args.TargetId) && args.AutonomyLevel.HasValue &&
                    facade.TrySetAgentAutonomy(null, new TargetId(args.TargetId!), args.AutonomyLevel.Value, args.SimTime, out _);
                break;
            default:
                ok = false;
                break;
        }

        if (!ok)
        {
            error = $"Journal replay command '{row.CommandKind}' (seq={row.SequenceOrder}, tick={row.ObservedTick}) returned false.";
        }

        return ok;
    }

    private static List<(string UnitId, string Side)> CaptureSideRegistrySnapshot(SaveResumeSessionUnit active)
    {
        var snapshot = new List<(string UnitId, string Side)>();
        var dto = ScenarioPolicyJsonCatalog.TryGetJson(active.Identity.ScenarioId);
        if (dto?.Gauntlet?.Units is { Count: > 0 } gauntletUnits)
        {
            foreach (var u in gauntletUnits)
            {
                if (!string.IsNullOrWhiteSpace(u.PlatformId))
                {
                    var side = BalticV3SideRegistry.GetSideForUnit(u.PlatformId);
                    if (!string.IsNullOrEmpty(side))
                    {
                        snapshot.Add((u.PlatformId, side!));
                    }
                }
            }
        }

        return snapshot;
    }

    private static void RestoreSideRegistrySnapshot(List<(string UnitId, string Side)> snapshot)
    {
        BalticV3SideRegistry.ClearScenarioSides();
        foreach (var (unitId, side) in snapshot)
        {
            BalticV3SideRegistry.RegisterScenarioSide(unitId, side);
        }
    }
}
