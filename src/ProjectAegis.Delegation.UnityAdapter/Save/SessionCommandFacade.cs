namespace ProjectAegis.Delegation.UnityAdapter.Save;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Bridge;
using Controllers;
using Core;
using Decision;
using Input;
using Orchestration;
using PlayDelegation;
using PlayEntry;
using Replay;
using Traits;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class SessionCommandAttribute : Attribute
{
    public SessionCommandAttribute(InputCommandJournalKind kind) => Kind = kind;

    public InputCommandJournalKind Kind { get; }
}

/// <summary>
/// H6-03 / DRG-349: Command façade wrapping every authority-mutating external public API call
/// in <see cref="InputCommandInventory"/>. Records accepted calls in <see cref="SessionInputJournal"/>
/// and enforces unjournaled-mutation parity before saving without editing <see cref="DelegationBridge"/>
/// or <see cref="SimulationSession"/>.
/// </summary>
public sealed class SessionCommandFacade
{
    private readonly Func<ulong>? _observedTickProvider;
    private readonly Func<ulong>? _combinedWorldHashProvider;
    private AuthorityShadowState _shadowState;

    public SessionCommandFacade(
        DelegationBridge bridge,
        SessionInputJournal? journal = null,
        Func<ulong>? observedTickProvider = null,
        Func<ulong>? combinedWorldHashProvider = null)
    {
        Bridge = bridge ?? throw new ArgumentNullException(nameof(bridge));
        Journal = journal ?? new SessionInputJournal();
        _observedTickProvider = observedTickProvider;
        _combinedWorldHashProvider = combinedWorldHashProvider;
        _shadowState = CaptureAuthorityShadowState();
    }

    public DelegationBridge Bridge { get; }

    public SessionInputJournal Journal { get; }

    public void SyncShadowStateAfterTick() =>
        _shadowState = CaptureAuthorityShadowState();

    public bool VerifyNoUnjournaledMutations(out string? violation)
    {
        var live = CaptureAuthorityShadowState();
        return _shadowState.TryCompare(live, out violation);
    }

    public SaveResult<SaveEnvelope> TryCaptureEnvelope(
        SaveIdentityContext? identity,
        ReplayCheckpointStore? checkpointStore = null,
        ulong combinedWorldHash = 0)
    {
        if (identity is null)
        {
            return SaveResult<SaveEnvelope>.Fail(SaveFailureCode.InvalidArgument, "SaveIdentityContext is null.");
        }

        if (!VerifyNoUnjournaledMutations(out var violation))
        {
            return SaveResult<SaveEnvelope>.Fail(
                SaveFailureCode.UnjournaledMutationDetected,
                violation ?? "Unjournaled authority mutation detected prior to save.");
        }

        var observedTick = CurrentObservedTick();
        var pipelineTick = CurrentPipelineTick();
        var log = Bridge.Orchestrator.DecisionLog;
        var lastSeqId = log.ChronologicalEntries().LastOrDefault()?.SequenceId ?? 0UL;
        var orderLogSha = OrderLogReplayFingerprint.ComputeSha256Hex(log);
        var lastWorldHash = Bridge.Session?.Sim.LastWorldHash ?? 0UL;
        var providedCombined = _combinedWorldHashProvider?.Invoke() ?? 0UL;
        var effectiveCombined = combinedWorldHash != 0UL
            ? combinedWorldHash
            : (providedCombined != 0UL ? providedCombined : lastWorldHash);

        var checkpoints = new List<SaveCheckpointRow>();
        if (checkpointStore != null)
        {
            foreach (var cp in checkpointStore.Checkpoints)
            {
                if (cp.SimTick <= observedTick)
                {
                    checkpoints.Add(new SaveCheckpointRow(
                        cp.SimTick,
                        cp.WorldHash,
                        cp.LogFingerprint,
                        cp.LastSequenceId));
                }
            }
        }

        var envelope = SaveEnvelopeSerializer.CreateSigned(
            identity.ToIdentityBlock(),
            new SavePositionBlock(observedTick, pipelineTick, lastSeqId),
            Journal.Rows,
            orderLogSha,
            lastWorldHash,
            effectiveCombined,
            checkpoints);

        return SaveResult<SaveEnvelope>.Ok(envelope);
    }

    [SessionCommand(InputCommandJournalKind.TakeDirectControl)]
    public bool TryTakeDirectControl(EntityKey entity, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (!Bridge.TryTakeDirectControl(entity, simTime))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.TakeDirectControl,
            new SaveJournalArguments(EntityKeyValue: entity.Value, SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ReleaseDirectControl)]
    public bool TryReleaseDirectControl(EntityKey entity, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (!Bridge.TryReleaseDirectControl(entity, simTime))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ReleaseDirectControl,
            new SaveJournalArguments(EntityKeyValue: entity.Value, SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.PauseAgentActions)]
    public bool TryPauseAgentActions(TargetId targetId, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !Bridge.Registry.TryGetBinding(targetId, out var binding))
        {
            return false;
        }

        if (!Bridge.Orchestrator.TryPauseAgentActions(binding.Target, simTime))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.PauseAgentActions,
            new SaveJournalArguments(TargetId: targetId.Value, SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ResumeAgentActions)]
    public bool TryResumeAgentActions(TargetId targetId, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !Bridge.Registry.TryGetBinding(targetId, out var binding))
        {
            return false;
        }

        if (!Bridge.Orchestrator.TryResumeAgentActions(binding.Target, simTime))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ResumeAgentActions,
            new SaveJournalArguments(TargetId: targetId.Value, SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.EnqueueHumanOrder)]
    public bool TryEnqueueHumanOrder(EntityKey entity, OrderKind kind, double simTime, RiskLevel? risk = null)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (!Bridge.TryEnqueueHumanOrder(entity, kind, simTime, risk))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.EnqueueHumanOrder,
            new SaveJournalArguments(
                EntityKeyValue: entity.Value,
                OrderKind: kind,
                RiskLevel: risk,
                SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.IssuePlayerCommand)]
    public bool TryIssuePlayerCommand(EntityKey entity, string commandId, double simTime, out string? failureReason)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (!Bridge.TryIssuePlayerCommand(entity, commandId, simTime, out failureReason))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.IssuePlayerCommand,
            new SaveJournalArguments(
                EntityKeyValue: entity.Value,
                SubCommandId: commandId,
                SimTime: simTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.EnqueueAttackOption)]
    public bool TryEnqueueAttackOption(
        EntityKey entity,
        string optionId,
        ISimWorldSnapshot? snapshot,
        out string? failureReason)
    {
        failureReason = null;
        if (snapshot is null)
        {
            failureReason = "NO_SNAPSHOT";
            return false;
        }

        var obsTick = ResolveObservedTick(snapshot.SimTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer ||
            !PlayerSideGuard.TryEnqueueAttackOption(Bridge, entity, optionId, snapshot, out failureReason))
        {
            failureReason ??= C2PlayerCommandBridge.ReasonReplayAttached;
            return false;
        }

        var resolvedSalvo = Bridge.Session?.NextEngageSalvoOverride;
        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.EnqueueAttackOption,
            new SaveJournalArguments(
                EntityKeyValue: entity.Value,
                OptionId: optionId,
                ResolvedSalvo: resolvedSalvo,
                SimTime: snapshot.SimTime));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ApprovePendingOrder)]
    public bool TryApprovePendingOrder(OrderId orderId)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !Bridge.Orchestrator.TryApprovePendingOrder(orderId))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ApprovePendingOrder,
            new SaveJournalArguments(OrderId: orderId.Value, SimTime: obsTick));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.RejectPendingOrder)]
    public bool TryRejectPendingOrder(OrderId orderId)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !Bridge.Orchestrator.TryRejectPendingOrder(orderId))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.RejectPendingOrder,
            new SaveJournalArguments(OrderId: orderId.Value, SimTime: obsTick));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.RebindAgentTraits)]
    public bool TryRebindAgentTraits(TargetId targetId, TraitVector? traits)
    {
        if (traits is null)
        {
            return false;
        }

        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !TryResolveAgent(targetId, out var agent))
        {
            return false;
        }

        var verdict = Bridge.Orchestrator.TryRebindAgentTraits(agent!, traits);
        if (!verdict.Allowed)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.RebindAgentTraits,
            new SaveJournalArguments(
                TargetId: targetId.Value,
                AgentId: agent!.Id.Value,
                SimTime: obsTick,
                Traits: SaveTraitValues.FromTraitVector(traits)));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.PauseSim)]
    public bool TryPauseSim(out string? reason)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (!C2ClockCommand.TryPause(Bridge.Session, out reason))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.PauseSim,
            new SaveJournalArguments(SimTime: obsTick));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ResumeSim)]
    public bool TryResumeSim(bool explicitOverride, out string? reason)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (!C2ClockCommand.TryResume(Bridge.Session, explicitOverride, out reason))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ResumeSim,
            new SaveJournalArguments(SimTime: obsTick, ExplicitOverride: explicitOverride));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.SetTimeAccelerationFactor)]
    public bool TrySetTimeAccelerationFactor(int factor, out string? reason)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (!C2ClockCommand.TrySetAcceleration(Bridge.Session, factor, out reason))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.SetTimeAccelerationFactor,
            new SaveJournalArguments(SimTime: obsTick, AccelerationFactor: Bridge.Session!.TimeAccelerationFactor));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ContactReclassifyOverride)]
    public bool TryReclassifyContact(
        string? contactId,
        string? affiliation,
        double simTime,
        out ContactOverrideResult result)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        result = ContactOverrideCommand.TryReclassify(Bridge, contactId, affiliation, simTime);
        if (!result.Accepted)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ContactReclassifyOverride,
            new SaveJournalArguments(
                SimTime: simTime,
                ContactId: contactId,
                Affiliation: affiliation));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.ContactDropOverride)]
    public bool TryDropContact(
        string? contactId,
        double simTime,
        out ContactOverrideResult result)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        result = ContactOverrideCommand.TryDrop(Bridge, contactId, simTime);
        if (!result.Accepted)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.ContactDropOverride,
            new SaveJournalArguments(SimTime: simTime, ContactId: contactId));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.RebriefAgent)]
    public bool TryRebriefAgent(TargetId targetId, string presetName, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !TryResolveAgent(targetId, out var agent))
        {
            return false;
        }

        var preset = PlayDelegationCommands.FindPreset(presetName);
        if (preset is null)
        {
            return false;
        }

        var verdict = Bridge.Orchestrator.TryRebriefAgent(agent!, preset, simTime);
        if (!verdict.Allowed)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.RebriefAgent,
            new SaveJournalArguments(
                TargetId: targetId.Value,
                AgentId: agent!.Id.Value,
                SimTime: simTime,
                PresetName: preset.Name));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.AssignAgentController)]
    public bool TryAssignAgentController(
        TargetId targetId,
        string agentId,
        TraitVector? traits,
        AutonomyLevel autonomy,
        double attentionBudget,
        string? presetName,
        bool isFriendly,
        double simTime)
    {
        if (string.IsNullOrWhiteSpace(agentId) || traits is null)
        {
            return false;
        }

        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || !Bridge.Registry.TryGetBinding(targetId, out var binding))
        {
            return false;
        }

        var preset = string.IsNullOrWhiteSpace(presetName) ? null : PlayDelegationCommands.FindPreset(presetName);
        var agent = preset != null
            ? Bridge.Orchestrator.CreateAgentFromPreset(new AgentId(agentId), preset, autonomy)
            : Bridge.Orchestrator.CreateAgent(new AgentId(agentId), traits, autonomy, attentionBudget);

        var verdict = Bridge.Orchestrator.TryAssignAgentController(binding.Target, agent, isFriendly, simTime);
        if (!verdict.Allowed)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.AssignAgentController,
            new SaveJournalArguments(
                TargetId: targetId.Value,
                AgentId: agentId,
                SimTime: simTime,
                Traits: SaveTraitValues.FromTraitVector(agent.Traits),
                PresetName: preset?.Name,
                AutonomyLevel: autonomy,
                AttentionBudget: agent.AttentionBudget,
                IsFriendly: isFriendly));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.DoctrineOverride)]
    public bool TryApplyDoctrineOverride(TargetId unitId, string roeLevelLabel, double simTime)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer ||
            !DoctrineOverrideCommand.TryApply(Bridge.Orchestrator, unitId, roeLevelLabel, simTime))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.DoctrineOverride,
            new SaveJournalArguments(
                TargetId: unitId.Value,
                SimTime: simTime,
                RoeLevelLabel: roeLevelLabel));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.AcknowledgeWatchAttention)]
    public bool TryAcknowledgeWatchAttention(string eventId)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || Bridge.Session is null || !Bridge.Session.WatchQueue.TryAcknowledge(eventId))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.AcknowledgeWatchAttention,
            new SaveJournalArguments(SimTime: obsTick, WatchEventId: eventId));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.DismissWatchAttention)]
    public bool TryDismissWatchAttention(string eventId)
    {
        var obsTick = CurrentObservedTick();
        var pipeTick = CurrentPipelineTick();
        if (Bridge.AttachReplayViewer || Bridge.Session is null || !Bridge.Session.WatchQueue.TryDismiss(eventId))
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.DismissWatchAttention,
            new SaveJournalArguments(SimTime: obsTick, WatchEventId: eventId));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.SetAgentPersonality)]
    public bool TrySetAgentPersonality(
        PlayEntrySession? playEntry,
        TargetId targetId,
        string presetName,
        double simTime,
        out PlayEntryResult result)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();

        if (playEntry != null)
        {
            result = new PlayDelegationCommands(playEntry).TrySetPersonality(targetId, presetName, simTime);
        }
        else
        {
            result = ExecuteHeadlessSetPersonality(targetId, presetName, simTime);
        }

        if (!result.Succeeded)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.SetAgentPersonality,
            new SaveJournalArguments(
                TargetId: targetId.Value,
                SimTime: simTime,
                PresetName: presetName));
        SyncShadowStateAfterTick();
        return true;
    }

    [SessionCommand(InputCommandJournalKind.SetAgentAutonomy)]
    public bool TrySetAgentAutonomy(
        PlayEntrySession? playEntry,
        TargetId targetId,
        AutonomyLevel autonomy,
        double simTime,
        out PlayEntryResult result)
    {
        var obsTick = ResolveObservedTick(simTime);
        var pipeTick = CurrentPipelineTick();

        if (playEntry != null)
        {
            result = new PlayDelegationCommands(playEntry).TrySetAutonomy(targetId, autonomy, simTime);
        }
        else
        {
            result = ExecuteHeadlessSetAutonomy(targetId, autonomy, simTime);
        }

        if (!result.Succeeded)
        {
            return false;
        }

        Journal.Append(obsTick, pipeTick, InputCommandJournalKind.SetAgentAutonomy,
            new SaveJournalArguments(
                TargetId: targetId.Value,
                SimTime: simTime,
                AutonomyLevel: autonomy));
        SyncShadowStateAfterTick();
        return true;
    }

    private PlayEntryResult ExecuteHeadlessSetPersonality(TargetId targetId, string presetName, double simTime)
    {
        if (Bridge.Phase != SimulationPhase.Executing)
        {
            return PlayEntryResult.Fail(PlayDelegationErrorCodes.NotExecuting, "Not executing.");
        }

        if (!Bridge.Registry.TryGetBinding(targetId, out var binding) || binding.Target.Slot.Active is not AgentController agent)
        {
            return PlayEntryResult.Fail(PlayDelegationErrorCodes.NoAgent, PlayDelegationCommands.NoAgentReason(targetId.Value));
        }

        var preset = PlayDelegationCommands.FindPreset(presetName);
        if (preset is null)
        {
            return PlayEntryResult.Fail(PlayDelegationErrorCodes.UnknownPreset, $"Unknown preset '{presetName}'.");
        }

        var availability = PlayDelegationCommands.PersonalityEditAvailability(
            Bridge.Orchestrator.ScenarioPolicy,
            Bridge.Orchestrator.Phase,
            agent.Autonomy,
            Bridge.Session?.IsSimPaused == true);
        if (!availability.Enabled)
        {
            return PlayEntryResult.Fail(PlayDelegationCommands.PersonalityDenied, availability.DeniedReason ?? "Denied.");
        }

        if (Bridge.AttachReplayViewer)
        {
            return PlayEntryResult.Fail(PlayDelegationCommands.PersonalityDenied, PlayDelegationCommands.ReplayViewerPersonalityReason);
        }

        var previous = agent.PersonalitySlug ?? "custom";
        var verdict = Bridge.Orchestrator.TryRebriefAgent(agent, preset, simTime);
        if (!verdict.Allowed)
        {
            return PlayEntryResult.Fail(PlayDelegationCommands.PersonalityDenied, verdict.DenialReason ?? "Denied.");
        }

        var simTick = (ulong)Math.Max(0, (long)simTime);
        Bridge.Orchestrator.DecisionLog.AppendPolicyUpdate(new PolicyUpdateRecord(
            0, simTime, simTick, agent.PolicySnapshotId, PlayDelegationCommands.PersonalityRebindField, previous, preset.Name));
        return PlayEntryResult.Ok($"Agent {agent.Id.Value} personality rebound {previous} -> {preset.Name}.");
    }

    private PlayEntryResult ExecuteHeadlessSetAutonomy(TargetId targetId, AutonomyLevel autonomy, double simTime)
    {
        if (Bridge.Phase != SimulationPhase.Executing)
        {
            return PlayEntryResult.Fail(PlayDelegationErrorCodes.NotExecuting, "Not executing.");
        }

        if (!Bridge.Registry.TryGetBinding(targetId, out var binding) || binding.Target.Slot.Active is not AgentController agent)
        {
            return PlayEntryResult.Fail(PlayDelegationErrorCodes.NoAgent, PlayDelegationCommands.NoAgentReason(targetId.Value));
        }

        var availability = PlayDelegationCommands.AutonomyEditAvailability(
            Bridge.Orchestrator.ScenarioPolicy,
            Bridge.Orchestrator.Phase);
        if (!availability.Enabled)
        {
            return PlayEntryResult.Fail(PlayDelegationCommands.AutonomyDenied, availability.DeniedReason ?? "Denied.");
        }

        if (Bridge.AttachReplayViewer)
        {
            return PlayEntryResult.Fail(PlayDelegationCommands.AutonomyDenied, PlayDelegationCommands.ReplayViewerAutonomyReason);
        }

        if (agent.Autonomy == autonomy)
        {
            return PlayEntryResult.Ok($"Agent {agent.Id.Value} autonomy already {autonomy}.");
        }

        var previous = agent.Autonomy;
        agent.Autonomy = autonomy;
        var simTick = (ulong)Math.Max(0, (long)simTime);
        Bridge.Orchestrator.DecisionLog.AppendPolicyUpdate(new PolicyUpdateRecord(
            0, simTime, simTick, agent.PolicySnapshotId, PlayDelegationCommands.AutonomyRebindField, previous.ToString(), autonomy.ToString()));
        return PlayEntryResult.Ok($"Agent {agent.Id.Value} autonomy rebound {previous} -> {autonomy}.");
    }

    private bool TryResolveAgent(TargetId targetId, out AgentController? agent)
    {
        agent = null;
        if (!Bridge.Registry.TryGetBinding(targetId, out var binding))
        {
            return false;
        }

        agent = binding.Target.Slot.Active as AgentController ?? binding.Target.Slot.SuspendedAgent;
        return agent != null;
    }

    private ulong CurrentObservedTick() =>
        _observedTickProvider != null ? _observedTickProvider() : 0UL;

    private ulong ResolveObservedTick(double simTime) =>
        _observedTickProvider != null
            ? _observedTickProvider()
            : (ulong)Math.Max(0, (long)simTime);

    private ulong CurrentPipelineTick() =>
        Bridge.Session?.Sim.Clock.SimTick ?? 0UL;

    private AuthorityShadowState CaptureAuthorityShadowState()
    {
        var orch = Bridge.Orchestrator;
        var log = orch.DecisionLog;
        var decisionLogDigest = OrderLogReplayFingerprint.ComputeSha256Hex(log);
        var decisionLogCount = log.ChronologicalEntries().Count;

        var pendingIds = orch.PendingApprovalQueue.Pending
            .Select(p => p.Order.Id.Value)
            .ToArray();

        var policySb = new StringBuilder();
        foreach (var kv in orch.PolicySnapshots.Snapshots.OrderBy(k => k.Key.Value, StringComparer.Ordinal))
        {
            policySb.Append(kv.Key.Value).Append(':')
                .Append(kv.Value.PolicySnapshotId).Append(':')
                .Append(kv.Value.Effective.Roe).Append(':')
                .Append(kv.Value.Effective.MaxSalvo).Append('\n');
        }

        var watchSb = new StringBuilder();
        bool isPaused = false;
        int accel = 1;
        ulong simTick = 0;
        int? nextSalvo = null;
        if (Bridge.Session is { } session)
        {
            isPaused = session.IsSimPaused;
            accel = session.TimeAccelerationFactor;
            simTick = session.Sim.Clock.SimTick;
            nextSalvo = session.NextEngageSalvoOverride;
            watchSb.Append("reason=").Append((int)session.LastWatchPauseReason).Append(';');
            foreach (var c in session.WatchQueue.Cards)
            {
                watchSb.Append(c.EventId).Append(':')
                    .Append(c.IsAcknowledged ? '1' : '0')
                    .Append(c.IsDismissed ? '1' : '0')
                    .Append(';');
            }
        }

        var ctrlSb = new StringBuilder();
        var expSb = new StringBuilder();
        foreach (var b in Bridge.Registry.Bindings.OrderBy(b => b.TargetId.Value, StringComparer.Ordinal))
        {
            var slot = b.Target.Slot;
            var activeKind = slot.Active is HumanController ? "H"
                : slot.Active is AgentController ? "A"
                : "N";
            var suspendedId = slot.SuspendedAgent?.Id.Value ?? string.Empty;
            ctrlSb.Append(b.TargetId.Value).Append('|').Append(activeKind).Append('|').Append(suspendedId);
            if (slot.Active is HumanController human)
            {
                ctrlSb.Append("|H:").Append(human.PendingOrderCount);
            }

            var agent = slot.Active as AgentController ?? slot.SuspendedAgent;
            if (agent != null)
            {
                ctrlSb.Append("|A:").Append(agent.Id.Value)
                    .Append(':').Append(agent.Autonomy)
                    .Append(':').Append(agent.AttentionBudget.ToString("R", CultureInfo.InvariantCulture))
                    .Append(':').Append(agent.PersonalitySlug ?? string.Empty)
                    .Append(':').Append(agent.IsActionsPaused ? '1' : '0')
                    .Append(':').Append(agent.PolicySnapshotId)
                    .Append(':').Append(agent.Traits.Aggression.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',').Append(agent.Traits.RiskTolerance.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',').Append(agent.Traits.ReactionDelay.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',').Append(agent.Traits.ErrorRate.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',').Append(agent.Traits.SituationalAwareness.ToString("R", CultureInfo.InvariantCulture))
                    .Append(',').Append(agent.Traits.Decisiveness.ToString("R", CultureInfo.InvariantCulture));

                expSb.Append(agent.Id.Value).Append(':').Append(agent.Experience.Metrics.Count).Append(';');
                foreach (var mkv in agent.Experience.Metrics.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    expSb.Append(mkv.Key).Append('=').Append(mkv.Value.ToString("R", CultureInfo.InvariantCulture)).Append(',');
                }
            }

            ctrlSb.Append('\n');
        }

        return new AuthorityShadowState(
            decisionLogCount,
            decisionLogDigest,
            pendingIds,
            Sha256String(policySb.ToString()),
            watchSb.ToString(),
            isPaused,
            accel,
            simTick,
            nextSalvo,
            Sha256String(ctrlSb.ToString()),
            orch.TrustSignals.Count,
            expSb.ToString());
    }

    private static string Sha256String(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        var chars = new char[hash.Length * 2];
        for (var i = 0; i < hash.Length; i++)
        {
            var b = hash[i];
            chars[i * 2] = (char)((b >> 4) < 10 ? '0' + (b >> 4) : 'a' + ((b >> 4) - 10));
            chars[i * 2 + 1] = (char)((b & 0xF) < 10 ? '0' + (b & 0xF) : 'a' + ((b & 0xF) - 10));
        }

        return new string(chars);
    }

    private sealed record AuthorityShadowState(
        int DecisionLogCount,
        string DecisionLogDigest,
        long[] PendingApprovalIds,
        string PolicySnapshotsHash,
        string WatchQueueState,
        bool IsSimPaused,
        int TimeAccelerationFactor,
        ulong SimTick,
        int? NextEngageSalvoOverride,
        string ControllersHash,
        int TrustSignalsCount,
        string ExperienceMetricsState)
    {
        public bool TryCompare(AuthorityShadowState live, out string? violation)
        {
            if (DecisionLogCount != live.DecisionLogCount ||
                !string.Equals(DecisionLogDigest, live.DecisionLogDigest, StringComparison.Ordinal))
            {
                violation = "Unjournaled mutation detected on DecisionLog outside SessionCommandFacade.";
                return false;
            }

            if (!PendingApprovalIds.SequenceEqual(live.PendingApprovalIds))
            {
                violation = "Unjournaled mutation detected on PendingApprovalQueue outside SessionCommandFacade.";
                return false;
            }

            if (!string.Equals(PolicySnapshotsHash, live.PolicySnapshotsHash, StringComparison.Ordinal))
            {
                violation = "Unjournaled mutation detected on PolicySnapshots outside SessionCommandFacade.";
                return false;
            }

            if (!string.Equals(WatchQueueState, live.WatchQueueState, StringComparison.Ordinal))
            {
                violation = "Unjournaled mutation detected on WatchQueue outside SessionCommandFacade.";
                return false;
            }

            if (IsSimPaused != live.IsSimPaused ||
                TimeAccelerationFactor != live.TimeAccelerationFactor ||
                SimTick != live.SimTick)
            {
                violation = "Unjournaled mutation detected on SimClock outside SessionCommandFacade.";
                return false;
            }

            if (NextEngageSalvoOverride != live.NextEngageSalvoOverride)
            {
                violation = "Unjournaled mutation detected on NextEngageSalvoOverride outside SessionCommandFacade.";
                return false;
            }

            if (!string.Equals(ControllersHash, live.ControllersHash, StringComparison.Ordinal))
            {
                violation = "Unjournaled mutation detected on Controller / AgentController outside SessionCommandFacade.";
                return false;
            }

            if (TrustSignalsCount != live.TrustSignalsCount ||
                !string.Equals(ExperienceMetricsState, live.ExperienceMetricsState, StringComparison.Ordinal))
            {
                violation = "Unjournaled mutation detected on TrustSignals or AgentExperienceBlob outside SessionCommandFacade.";
                return false;
            }

            violation = null;
            return true;
        }
    }
}
