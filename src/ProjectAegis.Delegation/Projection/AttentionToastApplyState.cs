namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Delegation.Decision;
using ProjectAegis.Delegation.Watch;

/// <summary>
/// CMD-39 (draft, Track A): headless apply path for the Unity attention toast host.
/// Consumes <see cref="AttentionTierAlertProjection"/> and <see cref="WatchAttentionQueueProjection"/>;
/// does not own the sim clock.
/// </summary>
public static class AttentionToastApplyState
{
    public const string TierCardIdPrefix = "tier:";

    /// <summary>
    /// Latest decision-time attention row per agent, derived from <see cref="DecisionLog.Records"/>.
    /// Ordering is deterministic by agent id.
    /// </summary>
    public static IReadOnlyList<AgentAttentionRow> ProjectLatestFromLog(DecisionLog? log)
    {
        if (log is null)
        {
            return Array.Empty<AgentAttentionRow>();
        }

        var records = log.Records;
        if (records.Count == 0)
        {
            return Array.Empty<AgentAttentionRow>();
        }

        var latest = new Dictionary<string, DecisionRecord>(StringComparer.Ordinal);
        for (var i = 0; i < records.Count; i++)
        {
            var record = records[i];
            latest[record.AgentId.Value] = record;
        }

        var rows = new List<AgentAttentionRow>(latest.Count);
        foreach (var pair in latest.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            rows.Add(AgentAttentionProjection.ProjectFromLoadBudget(
                pair.Key,
                pair.Value.AttentionLoad,
                pair.Value.AttentionBudget));
        }

        return rows;
    }

    public static AttentionToastCard FromAlert(AttentionTierAlert alert)
    {
        if (alert is null)
        {
            throw new ArgumentNullException(nameof(alert));
        }

        return new AttentionToastCard(
            CardId: TierCardIdPrefix + alert.AgentId + ":" + alert.NewTier,
            Title: "ATTENTION · " + alert.SeverityLabel,
            Body: alert.Text,
            AccessibleText: alert.AccessibleText,
            Severity: alert.Severity,
            SeverityLabel: alert.SeverityLabel,
            IsPauseClass: false,
            SequenceHint: null);
    }

    public static AttentionToastCard FromWatchCard(WatchAttentionCard card)
    {
        if (card is null)
        {
            throw new ArgumentNullException(nameof(card));
        }

        var reason = WatchAttentionQueueProjection.ProjectPauseReasonLabel(ReasonFromKind(card.Kind));
        var title = card.IsPauseClass ? "WATCH · PAUSE" : "WATCH";
        var detail = string.IsNullOrEmpty(card.Event.ReasonDetail)
            ? card.SubjectId
            : card.SubjectId + " (" + card.Event.ReasonDetail + ")";
        var body = string.IsNullOrEmpty(reason) ? detail : reason + " — " + detail;
        var severity = card.Priority == WatchAttentionPriority.Critical
            ? AlertSeverity.Critical
            : AlertSeverity.Notable;
        var accessible = card.IsPauseClass
            ? "Pause-class watch alert. " + body + ". Acknowledge to allow resume."
            : "Watch alert. " + body;

        return new AttentionToastCard(
            CardId: card.EventId,
            Title: title,
            Body: body,
            AccessibleText: accessible,
            Severity: severity,
            SeverityLabel: severity.ToString(),
            IsPauseClass: card.IsPauseClass,
            SequenceHint: card.EventId);
    }

    /// <summary>
    /// Merge watch-queue cards (already priority-sorted) ahead of pending tier toasts.
    /// MVP shows pause-class + Critical/Notable tier cards only.
    /// </summary>
    public static AttentionToastPresentation Apply(
        IReadOnlyList<AttentionToastCard>? pendingTierCards,
        WatchAttentionQueue? queue,
        WatchAutoPauseGate? gate,
        bool explicitResumeOverride = false)
    {
        var cards = new List<AttentionToastCard>();
        if (queue is not null)
        {
            var visible = WatchAttentionQueueProjection.ProjectVisible(queue);
            for (var i = 0; i < visible.Count; i++)
            {
                if (visible[i].IsAcknowledged)
                {
                    continue;
                }

                cards.Add(FromWatchCard(visible[i]));
            }
        }

        if (pendingTierCards is not null)
        {
            for (var i = 0; i < pendingTierCards.Count; i++)
            {
                var card = pendingTierCards[i];
                if (card.Severity is AlertSeverity.Critical or AlertSeverity.Notable)
                {
                    cards.Add(card);
                }
            }
        }

        var unresolved = queue is not null && queue.HasUnresolvedPauseClass;
        var canResume = gate is null || queue is null || gate.CanResume(queue, explicitResumeOverride);
        var reasonLabel = gate is null
            ? string.Empty
            : WatchAttentionQueueProjection.ProjectPauseReasonLabel(gate.LastPauseReason);

        if (cards.Count == 0)
        {
            return new AttentionToastPresentation(
                Active: null,
                QueuedCount: 0,
                QueueBadge: string.Empty,
                HasUnresolvedPauseClass: unresolved,
                CanResume: canResume,
                PauseReasonLabel: reasonLabel);
        }

        var queued = cards.Count - 1;
        return new AttentionToastPresentation(
            Active: cards[0],
            QueuedCount: queued,
            QueueBadge: queued > 0 ? "+" + queued + " queued" : string.Empty,
            HasUnresolvedPauseClass: unresolved,
            CanResume: canResume,
            PauseReasonLabel: reasonLabel);
    }

    private static WatchPauseReason ReasonFromKind(WatchAttentionKind kind) =>
        kind switch
        {
            WatchAttentionKind.HostileOrUnknownContact => WatchPauseReason.HostileOrUnknownContact,
            WatchAttentionKind.OwnSideLossOrDamage => WatchPauseReason.OwnSideLossOrDamage,
            _ => WatchPauseReason.None,
        };
}

/// <summary>
/// Session-local toast binder: diffs attention rows, queues Critical/Notable toasts,
/// and acknowledges either watch cards or ephemeral tier cards.
/// </summary>
public sealed class AttentionToastBinder
{
    private readonly Dictionary<string, AgentAttentionRow> _previous =
        new(StringComparer.Ordinal);
    private readonly List<AttentionToastCard> _pendingTier = new();

    public AttentionToastPresentation LastPresentation { get; private set; } =
        AttentionToastPresentation.Empty;

    /// <summary>
    /// Diff current attention vs the previous sample, then apply watch-queue + pending tier cards.
    /// Unchanged tiers produce no new toast (projection already suppresses).
    /// </summary>
    public AttentionToastPresentation Refresh(
        DecisionLog? log,
        WatchAttentionQueue? queue,
        WatchAutoPauseGate? gate,
        bool explicitResumeOverride = false)
    {
        var current = AttentionToastApplyState.ProjectLatestFromLog(log);
        var alerts = AttentionTierAlertProjection.Diff(_previous, current);
        for (var i = 0; i < alerts.Count; i++)
        {
            var alert = alerts[i];
            if (alert.Severity is AlertSeverity.Critical or AlertSeverity.Notable)
            {
                _pendingTier.Add(AttentionToastApplyState.FromAlert(alert));
            }
        }

        _previous.Clear();
        for (var i = 0; i < current.Count; i++)
        {
            var row = current[i];
            if (!string.IsNullOrEmpty(row.AgentId))
            {
                _previous[row.AgentId] = row;
            }
        }

        LastPresentation = AttentionToastApplyState.Apply(
            _pendingTier,
            queue,
            gate,
            explicitResumeOverride);
        return LastPresentation;
    }

    /// <summary>
    /// Acknowledge a watch card (resume-gating) or dismiss a pending tier toast.
    /// </summary>
    public bool TryAcknowledge(string cardId, WatchAttentionQueue? queue)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return false;
        }

        if (queue is not null && queue.TryAcknowledge(cardId))
        {
            return true;
        }

        return TryRemovePendingTier(cardId);
    }

    /// <summary>Soft-dismiss: watch dismiss or drop a pending tier toast.</summary>
    public bool TryDismiss(string cardId, WatchAttentionQueue? queue)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return false;
        }

        if (queue is not null && queue.TryDismiss(cardId))
        {
            return true;
        }

        return TryRemovePendingTier(cardId);
    }

    private bool TryRemovePendingTier(string cardId)
    {
        for (var i = 0; i < _pendingTier.Count; i++)
        {
            if (string.Equals(_pendingTier[i].CardId, cardId, StringComparison.Ordinal))
            {
                _pendingTier.RemoveAt(i);
                return true;
            }
        }

        return false;
    }
}

/// <summary>One ephemeral toast card (watch pause-class or attention-tier crossing).</summary>
public sealed record AttentionToastCard(
    string CardId,
    string Title,
    string Body,
    string AccessibleText,
    AlertSeverity Severity,
    string SeverityLabel,
    bool IsPauseClass,
    string? SequenceHint);

/// <summary>Applied toast host fields (bind onto UI Toolkit labels without re-formatting).</summary>
public sealed record AttentionToastPresentation(
    AttentionToastCard? Active,
    int QueuedCount,
    string QueueBadge,
    bool HasUnresolvedPauseClass,
    bool CanResume,
    string PauseReasonLabel)
{
    public static AttentionToastPresentation Empty { get; } = new(
        null,
        0,
        string.Empty,
        false,
        true,
        string.Empty);

    public bool HasActiveCard => Active is not null;
}
