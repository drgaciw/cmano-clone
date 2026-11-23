namespace ProjectAegis.Delegation.Input;

/// <summary>
/// Input-journal command inventory kinds (DRG-348 / ADR-029).
/// Enumerates authority-bearing player commands registered for replay-to-tick journaling.
/// </summary>
public enum InputCommandJournalKind
{
    TakeDirectControl = 0,
    ReleaseDirectControl = 1,
    PauseAgentActions = 2,
    ResumeAgentActions = 3,
    EnqueueHumanOrder = 4,
    IssuePlayerCommand = 5,
    EnqueueAttackOption = 6,
    ApprovePendingOrder = 7,
    RejectPendingOrder = 8,
    RebindAgentTraits = 9,
    PauseSim = 10,
    ResumeSim = 11,
    SetTimeAccelerationFactor = 12,
    ContactReclassifyOverride = 13,
    ContactDropOverride = 14,
}

/// <summary>Descriptor for a registered authority-mutating player command (DRG-348 / ADR-029).</summary>
public sealed record InputCommandDescriptor(
    string CommandId,
    InputCommandJournalKind Kind,
    string PublicApiMethod,
    bool BlockedByReplayViewer,
    bool AppendsDecisionLog);

/// <summary>
/// Authoritative inventory of authority-mutating commands covered by the ADR-029 input-journal
/// contract (DRG-348), kept in <c>ProjectAegis.Delegation.Input</c> without UI/sim side effects.
/// </summary>
public static class InputCommandInventory
{
    public const string ContactReclassifyOverrideId = "contact_reclassify_override";
    public const string ContactDropOverrideId = "contact_drop_override";
    public const string EnqueueHumanOrderId = "enqueue_human_order";
    public const string IssuePlayerCommandId = "issue_player_command";
    public const string EnqueueAttackOptionId = "enqueue_attack_option";
    public const string ApprovePendingOrderId = "approve_pending_order";
    public const string RejectPendingOrderId = "reject_pending_order";
    public const string RebindAgentTraitsId = "rebind_agent_traits";
    public const string PauseSimId = "pause_sim";
    public const string ResumeSimId = "resume_sim";
    public const string SetTimeAccelerationId = "set_time_acceleration";

    private static readonly IReadOnlyList<InputCommandDescriptor> Entries = new InputCommandDescriptor[]
    {
        new(AgentDirectiveIssuance.TakeControlId, InputCommandJournalKind.TakeDirectControl,
            "DelegationOrchestrator.TryTakeDirectControl", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(AgentDirectiveIssuance.ReturnToAgentId, InputCommandJournalKind.ReleaseDirectControl,
            "DelegationOrchestrator.TryReleaseDirectControl", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(AgentDirectiveIssuance.PauseAgentId, InputCommandJournalKind.PauseAgentActions,
            "DelegationOrchestrator.TryPauseAgentActions", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(AgentDirectiveIssuance.ResumeAgentId, InputCommandJournalKind.ResumeAgentActions,
            "DelegationOrchestrator.TryResumeAgentActions", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(EnqueueHumanOrderId, InputCommandJournalKind.EnqueueHumanOrder,
            "DelegationBridge.TryEnqueueHumanOrder", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(IssuePlayerCommandId, InputCommandJournalKind.IssuePlayerCommand,
            "C2PlayerCommandBridge.TryIssue", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(EnqueueAttackOptionId, InputCommandJournalKind.EnqueueAttackOption,
            "PlayerSideGuard.TryEnqueueAttackOption", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(ApprovePendingOrderId, InputCommandJournalKind.ApprovePendingOrder,
            "DelegationOrchestrator.TryApprovePendingOrder", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(RejectPendingOrderId, InputCommandJournalKind.RejectPendingOrder,
            "DelegationOrchestrator.TryRejectPendingOrder", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(RebindAgentTraitsId, InputCommandJournalKind.RebindAgentTraits,
            "DelegationOrchestrator.TryRebindAgentTraits", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(PauseSimId, InputCommandJournalKind.PauseSim,
            "SimulationSession.PauseSim", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(ResumeSimId, InputCommandJournalKind.ResumeSim,
            "SimulationSession.TryResumeSim", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(SetTimeAccelerationId, InputCommandJournalKind.SetTimeAccelerationFactor,
            "SimulationSession.SetTimeAccelerationFactor", BlockedByReplayViewer: true, AppendsDecisionLog: false),
        new(ContactReclassifyOverrideId, InputCommandJournalKind.ContactReclassifyOverride,
            "ContactOverrideCommand.TryReclassify", BlockedByReplayViewer: true, AppendsDecisionLog: true),
        new(ContactDropOverrideId, InputCommandJournalKind.ContactDropOverride,
            "ContactOverrideCommand.TryDrop", BlockedByReplayViewer: true, AppendsDecisionLog: true),
    };

    private static readonly Dictionary<string, InputCommandDescriptor> ById = BuildIndex();

    public static IReadOnlyList<InputCommandDescriptor> All => Entries;

    public static bool TryGet(string? commandId, out InputCommandDescriptor? descriptor)
    {
        if (string.IsNullOrWhiteSpace(commandId))
        {
            descriptor = null;
            return false;
        }

        return ById.TryGetValue(commandId.Trim().ToLowerInvariant(), out descriptor);
    }

    private static Dictionary<string, InputCommandDescriptor> BuildIndex()
    {
        var map = new Dictionary<string, InputCommandDescriptor>(Entries.Count, StringComparer.Ordinal);
        foreach (var entry in Entries)
        {
            map[entry.CommandId] = entry;
        }

        return map;
    }
}
