using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.Presentation;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Policy;

namespace ProjectAegis.Delegation.UnityAdapter.CommandReview;

/// <summary>Explicit player accept request for a supervised advice card (DRG-255).</summary>
public sealed record SupervisedAdviceAcceptRequest(
    string UnitId,
    AdviceFrame Advice,
    string CommandId,
    string? AttackOptionId = null,
    bool ExplicitFullAutonomousLethalOptIn = false,
    bool AllowTakeControlFromAssistedAgent = true);

/// <summary>Outcome of supervised advice accept validation and order enqueue.</summary>
public sealed record SupervisedAdviceAcceptResult(
    bool Accepted,
    string UnitId,
    string ContactId,
    OrderKind? EnqueuedKind,
    ulong? PlayerOrderSequenceId,
    ulong? ResolvedExecuteSimTick,
    string? FailureReason,
    IReadOnlyList<SupervisedAdviceEvidenceLine> EvidenceLines);

/// <summary>
/// Validates supervised advice evidence pointers, <c>FULL_AUTONOMOUS</c> lethal opt-in, ROE/policy,
/// comms-grid, and engage gates BEFORE taking direct control or enqueueing a normal <c>PlayerOrder</c>
/// through <see cref="PlayerSideGuard"/> / <see cref="C2PlayerCommandBridge"/> (DRG-255 / ADR-010).
/// </summary>
public static class SupervisedAdviceAcceptBridge
{
    public const string ReasonMissingEvidencePointer = "MISSING_EVIDENCE_POINTER";
    public const string ReasonFullAutonomousOptInRequired = "FULL_AUTONOMOUS_OPT_IN_REQUIRED";
    public const string ReasonAdviceUnavailable = "ADVICE_UNAVAILABLE";
    public const string ReasonAuthorityWithheld = "AUTHORITY_WITHHELD";
    public const string ReasonTakeControlFailed = "TAKE_CONTROL_FAILED";
    public const string ReasonEngageGateBlocked = "ENGAGE_GATE_BLOCKED";

    public static SupervisedAdviceAcceptResult TryAccept(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        SupervisedAdviceAcceptRequest? request)
    {
        if (bridge is null || snapshot is null || request is null)
        {
            return Reject(request?.UnitId ?? string.Empty, request?.Advice.ContactId ?? string.Empty, C2PlayerCommandBridge.ReasonUnknownUnit);
        }

        if (bridge.AttachReplayViewer)
        {
            return Reject(request.UnitId, request.Advice.ContactId, C2PlayerCommandBridge.ReasonReplayAttached);
        }

        if (!double.IsFinite(snapshot.SimTime) || snapshot.SimTime < 0)
        {
            return Reject(request.UnitId, request.Advice.ContactId, CoordinationCommandBridge.ReasonInvalidSimTime);
        }

        if (request.Advice.Availability != AdviceAvailability.Available)
        {
            return Reject(request.UnitId, request.Advice.ContactId, ReasonAdviceUnavailable);
        }

        if (request.Advice.Evidence.Count == 0
            || string.IsNullOrWhiteSpace(request.Advice.ContactId))
        {
            return Reject(request.UnitId, request.Advice.ContactId, ReasonMissingEvidencePointer);
        }

        var evidenceLines = SupervisedAdviceCardBinder.BuildEvidenceLines(bridge, request.Advice, request.UnitId);

        if (string.IsNullOrWhiteSpace(request.UnitId)
            || !TryFindBinding(bridge, request.UnitId, out var binding))
        {
            return Reject(request.UnitId, request.Advice.ContactId, C2PlayerCommandBridge.ReasonUnknownUnit, evidenceLines);
        }

        if (PlayerSideGuard.IsOpposingAiUnit(bridge, binding.Entity)
            || !PlayerSideGuard.IsPlayerControlledUnit(bridge, binding.Entity))
        {
            return Reject(request.UnitId, request.Advice.ContactId, C2PlayerCommandBridge.ReasonNotPlayerControlled, evidenceLines);
        }

        if (!TryResolveCommand(request.CommandId, request.AttackOptionId, out var kind, out var effectiveCommandId, out var resolveReason))
        {
            return Reject(request.UnitId, request.Advice.ContactId, resolveReason ?? C2CommandIssuance.ReasonUnknownCommand, evidenceLines);
        }

        var resolvedAutonomy = ResolveUnitAutonomy(bridge, binding);
        if (resolvedAutonomy == AutonomyLevel.FullAutonomous
            && kind == OrderKind.Engage
            && !request.ExplicitFullAutonomousLethalOptIn)
        {
            return Reject(request.UnitId, request.Advice.ContactId, ReasonFullAutonomousOptInRequired, evidenceLines);
        }

        var simTick = (ulong)Math.Max(0, (long)snapshot.SimTime);
        var hasSnapshot = bridge.Orchestrator.PolicySnapshots.TryGet(binding.TargetId, out var snap);
        var effectivePolicy = hasSnapshot
            ? snap.Effective
            : bridge.Orchestrator.ResolveScenarioPolicyForTarget(binding.TargetId, isFriendly: true);
        var policyContext = new PolicyContext(
            OrderActionMapper.TargetIdToUlong(binding.TargetId),
            hasSnapshot ? snap.PolicySnapshotId : 0UL,
            simTick,
            effectivePolicy,
            SalvoSize: string.Equals(request.AttackOptionId, "fire-salvo", StringComparison.Ordinal) ? 2 : 1);
        var probeOrder = new Order(
            new OrderId(0),
            binding.TargetId,
            snapshot.SimTime,
            kind,
            DefaultRiskClassifier.Classify(kind));
        var actionRequest = OrderActionMapper.ToActionRequest(probeOrder);
        var verdict = bridge.Orchestrator.PolicyEvaluator.Evaluate(in policyContext, in actionRequest);
        if (!verdict.Allowed)
        {
            return Reject(request.UnitId, request.Advice.ContactId, verdict.Reason.ToString(), evidenceLines);
        }

        if (kind == OrderKind.Engage
            && request.Advice.Authority?.Targeting.Disposition == C2AuthorityDisposition.Withheld)
        {
            return Reject(
                request.UnitId,
                request.Advice.ContactId,
                request.Advice.Authority.Targeting.ReasonCode ?? ReasonAuthorityWithheld,
                evidenceLines);
        }

        if (OffGridOrderGate.EvaluateAt(bridge.Orchestrator.CommsGrid, binding.TargetId.Value, simTick) != null)
        {
            return Reject(request.UnitId, request.Advice.ContactId, C2PlayerCommandBridge.ReasonOffGrid, evidenceLines);
        }

        if (kind == OrderKind.Engage)
        {
            var preview = bridge.GetEngagePreviewForUnit(binding.TargetId.Value, snapshot);
            if (preview is null || !preview.CanFire)
            {
                return Reject(
                    request.UnitId,
                    request.Advice.ContactId,
                    preview?.AbortPreviewCode ?? ReasonEngageGateBlocked,
                    evidenceLines);
            }

            var optionId = string.IsNullOrWhiteSpace(request.AttackOptionId) ? "fire-single" : request.AttackOptionId!;
            var options = PlayerSideGuard.GetAttackMenuOptions(bridge, binding.TargetId.Value, snapshot);
            EngageAttackOptions.AttackOption? matchedOption = null;
            for (var i = 0; i < options.Count; i++)
            {
                if (string.Equals(options[i].Id, optionId, StringComparison.Ordinal))
                {
                    matchedOption = options[i];
                    break;
                }
            }

            if (matchedOption is null)
            {
                return Reject(
                    request.UnitId,
                    request.Advice.ContactId,
                    "UNKNOWN_OPTION",
                    evidenceLines);
            }

            if (!matchedOption.Enabled)
            {
                return Reject(
                    request.UnitId,
                    request.Advice.ContactId,
                    matchedOption.DisabledReason ?? ReasonEngageGateBlocked,
                    evidenceLines);
            }
        }
        else if (!C2CommandIssuance.TryResolve(effectiveCommandId, out var resolvedNonEngageKind, out var commandFailure)
            || resolvedNonEngageKind != kind)
        {
            return Reject(
                request.UnitId,
                request.Advice.ContactId,
                commandFailure ?? C2CommandIssuance.ReasonUnknownCommand,
                evidenceLines);
        }

        var tookDirectControl = false;
        if (binding.Target.Slot.Active is not HumanController)
        {
            if (!request.AllowTakeControlFromAssistedAgent)
            {
                return Reject(request.UnitId, request.Advice.ContactId, C2PlayerCommandBridge.ReasonNotHumanControl, evidenceLines);
            }

            if (!bridge.TryTakeDirectControl(binding.Entity, snapshot.SimTime))
            {
                return Reject(request.UnitId, request.Advice.ContactId, ReasonTakeControlFailed, evidenceLines);
            }

            tookDirectControl = true;
        }

        var playerOrdersBefore = bridge.Orchestrator.DecisionLog.PlayerOrders.Count;
        bool enqueued;
        string? failureReason;
        if (kind == OrderKind.Engage)
        {
            var optionId = string.IsNullOrWhiteSpace(request.AttackOptionId) ? "fire-single" : request.AttackOptionId!;
            enqueued = PlayerSideGuard.TryEnqueueAttackOption(bridge, binding.Entity, optionId, snapshot, out failureReason);
        }
        else
        {
            enqueued = C2PlayerCommandBridge.TryIssue(bridge, binding.Entity, effectiveCommandId, snapshot.SimTime, out failureReason);
        }

        if (!enqueued || bridge.Orchestrator.DecisionLog.PlayerOrders.Count <= playerOrdersBefore)
        {
            if (tookDirectControl)
            {
                bridge.TryReleaseDirectControl(binding.Entity, snapshot.SimTime);
            }

            return Reject(
                request.UnitId,
                request.Advice.ContactId,
                failureReason ?? C2PlayerCommandBridge.ReasonEnqueueFailed,
                evidenceLines);
        }

        var latestOrder = bridge.Orchestrator.DecisionLog.PlayerOrders[^1];
        if (latestOrder.Kind != kind)
        {
            if (tookDirectControl)
            {
                bridge.TryReleaseDirectControl(binding.Entity, snapshot.SimTime);
            }

            return Reject(
                request.UnitId,
                request.Advice.ContactId,
                C2PlayerCommandBridge.ReasonEnqueueFailed,
                evidenceLines);
        }

        return new SupervisedAdviceAcceptResult(
            Accepted: true,
            UnitId: request.UnitId,
            ContactId: request.Advice.ContactId,
            EnqueuedKind: latestOrder.Kind,
            PlayerOrderSequenceId: latestOrder.SequenceId,
            ResolvedExecuteSimTick: latestOrder.ResolvedExecuteSimTick,
            FailureReason: null,
            EvidenceLines: evidenceLines);
    }

    public static AutonomyLevel ResolveUnitAutonomy(DelegationBridge bridge, SimEntityBinding binding)
    {
        var agent = binding.Target.Slot.Active as AgentController
            ?? binding.Target.Slot.SuspendedAgent;
        if (agent is not null)
        {
            return agent.Autonomy;
        }

        if (binding.Target is UnitTarget unit)
        {
            var parent = bridge.Orchestrator.FindParentGroup(unit.Id);
            if (parent?.Slot.Active is AgentController parentAgent)
            {
                return parentAgent.Autonomy;
            }
        }

        return AutonomyLevel.Manual;
    }

    private static bool TryFindBinding(DelegationBridge bridge, string unitId, out SimEntityBinding binding)
    {
        foreach (var candidate in bridge.Registry.Bindings)
        {
            if (string.Equals(candidate.TargetId.Value, unitId, StringComparison.Ordinal))
            {
                binding = candidate;
                return true;
            }
        }

        binding = null!;
        return false;
    }

    internal static bool TryResolveCommand(
        string? commandId,
        string? attackOptionId,
        out OrderKind kind,
        out string? reason) =>
        TryResolveCommand(commandId, attackOptionId, out kind, out _, out reason);

    internal static bool TryResolveCommand(
        string? commandId,
        string? attackOptionId,
        out OrderKind kind,
        out string effectiveCommandId,
        out string? reason)
    {
        if (C2CommandIssuance.TryResolve(commandId, out kind, out reason))
        {
            if (kind == OrderKind.Engage && !string.IsNullOrWhiteSpace(attackOptionId))
            {
                if (IsEngageAttackOption(attackOptionId))
                {
                    kind = OrderKind.Engage;
                    effectiveCommandId = commandId!;
                    reason = null;
                    return true;
                }

                if (string.Equals(attackOptionId, "hold-fire", StringComparison.Ordinal))
                {
                    kind = OrderKind.Hold;
                    effectiveCommandId = "hold";
                    reason = null;
                    return true;
                }

                kind = default;
                effectiveCommandId = string.Empty;
                reason = "UNKNOWN_OPTION";
                return false;
            }

            effectiveCommandId = commandId!;
            reason = null;
            return true;
        }

        if (string.IsNullOrWhiteSpace(commandId) && !string.IsNullOrWhiteSpace(attackOptionId))
        {
            if (IsEngageAttackOption(attackOptionId))
            {
                kind = OrderKind.Engage;
                effectiveCommandId = "engage";
                reason = null;
                return true;
            }

            if (string.Equals(attackOptionId, "hold-fire", StringComparison.Ordinal))
            {
                kind = default;
                effectiveCommandId = string.Empty;
                reason = C2CommandIssuance.ReasonUnknownCommand;
                return false;
            }

            kind = default;
            effectiveCommandId = string.Empty;
            reason = "UNKNOWN_OPTION";
            return false;
        }

        kind = default;
        effectiveCommandId = string.Empty;
        reason = C2CommandIssuance.ReasonUnknownCommand;
        return false;
    }

    private static bool IsEngageAttackOption(string? attackOptionId) =>
        string.Equals(attackOptionId, "fire-single", StringComparison.Ordinal)
        || string.Equals(attackOptionId, "fire-salvo", StringComparison.Ordinal);

    private static SupervisedAdviceAcceptResult Reject(
        string unitId,
        string contactId,
        string reason,
        IReadOnlyList<SupervisedAdviceEvidenceLine>? evidenceLines = null) =>
        new(
            Accepted: false,
            UnitId: unitId,
            ContactId: contactId,
            EnqueuedKind: null,
            PlayerOrderSequenceId: null,
            ResolvedExecuteSimTick: null,
            FailureReason: reason,
            EvidenceLines: evidenceLines ?? Array.Empty<SupervisedAdviceEvidenceLine>());
}
