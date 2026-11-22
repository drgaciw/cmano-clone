using System.Globalization;
using ProjectAegis.Delegation.Controllers;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Input;
using ProjectAegis.Delegation.Skills;
using ProjectAegis.Delegation.Targets;
using ProjectAegis.Delegation.UnityAdapter.Bridge;
using ProjectAegis.Delegation.UnityAdapter.CommandReview;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Formatted evidence pointer row for supervised advice cards (DRG-255):
/// <c>unitId | contactId | policySnapshotId | sequenceId</c>.
/// </summary>
public sealed record SupervisedAdviceEvidenceLine(
    string FormattedPointer,
    EvidenceKind Kind,
    string Id,
    string PolicySnapshotId,
    string SequenceId);

/// <summary>Read-only view state for the supervised advice accept/dismiss panel (ADR-010 §2–3).</summary>
public sealed record SupervisedAdviceCardState(
    bool CardVisible,
    bool CanAccept,
    bool RequiresFullAutonomousOptIn,
    bool IsLethalOrder,
    string UnitId,
    string ContactId,
    string ProposedCommandId,
    string? AttackOptionId,
    string Rationale,
    string AuthoritySummary,
    AutonomyLevel ResolvedAutonomy,
    IReadOnlyList<SupervisedAdviceEvidenceLine> EvidenceLines,
    string? BlockReason)
{
    public static SupervisedAdviceCardState Empty { get; } = new(
        CardVisible: false,
        CanAccept: false,
        RequiresFullAutonomousOptIn: false,
        IsLethalOrder: false,
        UnitId: string.Empty,
        ContactId: string.Empty,
        ProposedCommandId: string.Empty,
        AttackOptionId: null,
        Rationale: string.Empty,
        AuthoritySummary: string.Empty,
        ResolvedAutonomy: AutonomyLevel.Manual,
        EvidenceLines: Array.Empty<SupervisedAdviceEvidenceLine>(),
        BlockReason: null);
}

/// <summary>
/// Pure projection and client-side dismiss state for supervised advice cards (DRG-255).
/// Dismiss mutates only UI-local state and never touches <c>DecisionLog</c>.
/// </summary>
public sealed class SupervisedAdviceCardBinder
{
    private string? _dismissedContactId;

    public string? DismissedContactId => _dismissedContactId;

    public void Dismiss(string? contactId)
    {
        if (!string.IsNullOrWhiteSpace(contactId))
        {
            _dismissedContactId = contactId;
        }
    }

    public void ClearDismiss() => _dismissedContactId = null;

    public bool ShouldDisplay(AdviceFrame? advice) =>
        advice is not null
        && advice.Availability == AdviceAvailability.Available
        && !string.IsNullOrWhiteSpace(advice.ContactId)
        && !string.Equals(_dismissedContactId, advice.ContactId, StringComparison.Ordinal);

    public SupervisedAdviceCardState ProjectForUnit(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        AdviceFrame? advice,
        string? selectedUnitId,
        string proposedCommandId,
        string? attackOptionId = null,
        bool explicitFullAutonomousLethalOptIn = false)
    {
        if (!ShouldDisplay(advice))
        {
            return SupervisedAdviceCardState.Empty;
        }

        return Project(
            bridge,
            snapshot,
            advice,
            selectedUnitId,
            proposedCommandId,
            attackOptionId,
            explicitFullAutonomousLethalOptIn);
    }

    public static SupervisedAdviceCardState Project(
        DelegationBridge? bridge,
        ISimWorldSnapshot? snapshot,
        AdviceFrame? advice,
        string? selectedUnitId,
        string proposedCommandId,
        string? attackOptionId = null,
        bool explicitFullAutonomousLethalOptIn = false)
    {
        if (bridge is null || snapshot is null || advice is null)
        {
            return SupervisedAdviceCardState.Empty;
        }

        var unitId = string.IsNullOrWhiteSpace(selectedUnitId) ? "unassigned" : selectedUnitId!;
        var evidenceLines = BuildEvidenceLines(bridge, advice, unitId);
        var isLethalOrder = IsLethalProposal(proposedCommandId, attackOptionId);
        var autonomy = ResolveAutonomy(bridge, selectedUnitId);
        var requiresOptIn = autonomy == AutonomyLevel.FullAutonomous && isLethalOrder;
        var authoritySummary = FormatAuthoritySummary(advice.Authority);

        if (advice.Availability != AdviceAvailability.Available)
        {
            return new SupervisedAdviceCardState(
                CardVisible: false,
                CanAccept: false,
                RequiresFullAutonomousOptIn: requiresOptIn,
                IsLethalOrder: isLethalOrder,
                UnitId: unitId,
                ContactId: advice.ContactId,
                ProposedCommandId: proposedCommandId,
                AttackOptionId: attackOptionId,
                Rationale: advice.Rationale,
                AuthoritySummary: authoritySummary,
                ResolvedAutonomy: autonomy,
                EvidenceLines: evidenceLines,
                BlockReason: SupervisedAdviceAcceptBridge.ReasonAdviceUnavailable);
        }

        string? blockReason = null;
        if (evidenceLines.Count == 0 || string.IsNullOrWhiteSpace(advice.ContactId) || string.IsNullOrWhiteSpace(selectedUnitId))
        {
            blockReason = SupervisedAdviceAcceptBridge.ReasonMissingEvidencePointer;
        }
        else if (bridge.AttachReplayViewer)
        {
            blockReason = C2PlayerCommandBridge.ReasonReplayAttached;
        }
        else if (!double.IsFinite(snapshot.SimTime) || snapshot.SimTime < 0)
        {
            blockReason = CoordinationCommandBridge.ReasonInvalidSimTime;
        }
        else if (requiresOptIn && !explicitFullAutonomousLethalOptIn)
        {
            blockReason = SupervisedAdviceAcceptBridge.ReasonFullAutonomousOptInRequired;
        }

        return new SupervisedAdviceCardState(
            CardVisible: true,
            CanAccept: blockReason is null,
            RequiresFullAutonomousOptIn: requiresOptIn,
            IsLethalOrder: isLethalOrder,
            UnitId: unitId,
            ContactId: advice.ContactId,
            ProposedCommandId: proposedCommandId,
            AttackOptionId: attackOptionId,
            Rationale: advice.Rationale,
            AuthoritySummary: authoritySummary,
            ResolvedAutonomy: autonomy,
            EvidenceLines: evidenceLines,
            BlockReason: blockReason);
    }

    public static IReadOnlyList<SupervisedAdviceEvidenceLine> BuildEvidenceLines(
        DelegationBridge bridge,
        AdviceFrame advice,
        string unitId)
    {
        if (advice.Evidence is null || advice.Evidence.Count == 0)
        {
            return Array.Empty<SupervisedAdviceEvidenceLine>();
        }

        var policySnapshotId = !string.IsNullOrWhiteSpace(unitId)
            && bridge.Orchestrator.PolicySnapshots.TryGet(new TargetId(unitId), out var snap)
                ? snap.PolicySnapshotId.ToString(CultureInfo.InvariantCulture)
                : "0";
        var fallbackSeq = bridge.Orchestrator.DecisionLog.ContactChanges.Count > 0
            ? bridge.Orchestrator.DecisionLog.ContactChanges[^1].SequenceId.ToString(CultureInfo.InvariantCulture)
            : "0";

        var lines = new List<SupervisedAdviceEvidenceLine>(advice.Evidence.Count);
        for (var i = 0; i < advice.Evidence.Count; i++)
        {
            var ptr = advice.Evidence[i];
            var seq = ptr.SequenceId.HasValue
                ? ptr.SequenceId.Value.ToString(CultureInfo.InvariantCulture)
                : fallbackSeq;
            var contact = string.IsNullOrWhiteSpace(advice.ContactId) ? ptr.Id : advice.ContactId;
            var formatted = $"{unitId} | {contact} | {policySnapshotId} | {seq}";
            lines.Add(new SupervisedAdviceEvidenceLine(
                formatted,
                ptr.Kind,
                ptr.Id,
                policySnapshotId,
                seq));
        }

        return lines;
    }

    internal static bool IsLethalProposal(string? commandId, string? attackOptionId) =>
        SupervisedAdviceAcceptBridge.TryResolveCommand(commandId, attackOptionId, out var kind, out _)
        && kind == OrderKind.Engage;

    private static AutonomyLevel ResolveAutonomy(DelegationBridge bridge, string? unitId)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            return AutonomyLevel.Manual;
        }

        if (!bridge.Registry.TryGetBinding(new TargetId(unitId!), out var binding))
        {
            return AutonomyLevel.Manual;
        }

        return SupervisedAdviceAcceptBridge.ResolveUnitAutonomy(bridge, binding);
    }

    private static string FormatAuthoritySummary(C2AuthorityProjection? authority)
    {
        if (authority is null)
        {
            return "AUTHORITY: unknown (policy gates enforced on accept)";
        }

        return $"ROE: {authority.Roe.RoeLabel} | TARGETING: {authority.Targeting.Disposition} ({authority.Targeting.ReasonCode ?? "OK"})";
    }
}
