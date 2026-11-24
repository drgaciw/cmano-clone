namespace ProjectAegis.Delegation.Projection;

using System;
using System.Collections.Generic;
using Watch;

public enum ClockStopAction : byte
{
    None = 0,
    DropToRealTime = 1,
    Pause = 2,
}

public sealed record MessageNotificationAlert(
    ulong SequenceId,
    double SimTime,
    string Category,
    string? SubjectId,
    string Text,
    bool TriggersPause,
    bool TriggersDropToRealTime,
    bool TriggersMapBalloon,
    bool IsAcknowledged = false,
    bool IsDismissed = false);

public sealed record MapBalloonAlert(
    string BalloonId,
    ulong SequenceId,
    double SimTime,
    string Category,
    string? SubjectId,
    string Text,
    bool IsPauseTriggered);

public sealed record CoalescedCategorySummary(
    string Category,
    int Count,
    IReadOnlyList<ulong> SequenceIds,
    ulong LatestSequenceId,
    string LatestText);

public sealed record NotificationBatchEvaluation(
    ClockStopAction ClockAction,
    bool ShouldPause,
    bool ShouldDropToRealTime,
    bool ReplaySuppressed,
    IReadOnlyList<ulong> TriggeredSequenceIds,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<MessageNotificationAlert> NewlyTriggeredAlerts,
    IReadOnlyList<CoalescedCategorySummary> CoalescedRoutines,
    WatchPauseReason? PrimaryPauseReason)
{
    public static NotificationBatchEvaluation Empty { get; } = new(
        ClockStopAction.None,
        ShouldPause: false,
        ShouldDropToRealTime: false,
        ReplaySuppressed: false,
        Array.Empty<ulong>(),
        Array.Empty<string>(),
        Array.Empty<MessageNotificationAlert>(),
        Array.Empty<CoalescedCategorySummary>(),
        PrimaryPauseReason: null);
}

/// <summary>
/// Shared notification and safe-stop coordinator for DRG-408 and DRG-398.
/// Evaluates only player-visible <see cref="MessageLogLine"/> batches, processes each
/// <c>SequenceId</c> at most once, enforces <c>Pause &gt; DropToRealTime</c>, and keeps
/// unresolved pauses intact across single-alert dismissals.
/// </summary>
public sealed class MessageLogNotificationCoordinator
{
    private readonly HashSet<ulong> _processedSequenceIds = new();
    private readonly HashSet<string> _processedSyntheticKeys = new(StringComparer.Ordinal);
    private readonly List<MessageNotificationAlert> _alerts = new();
    private readonly List<MapBalloonAlert> _balloons = new();
    private readonly Dictionary<string, List<ulong>> _coalescedSequenceIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _coalescedLatestText = new(StringComparer.Ordinal);

    private MessageCategoryPolicy _policy;

    public MessageLogNotificationCoordinator(MessageCategoryPolicy? policy = null)
    {
        _policy = policy ?? MessageCategoryPolicy.LoadDefault();
    }

    public MessageCategoryPolicy Policy => _policy;

    public void UpdatePolicy(MessageCategoryPolicy policy)
    {
        if (policy is null)
        {
            throw new ArgumentNullException(nameof(policy));
        }

        _policy = policy;
    }

    public IReadOnlyList<MessageNotificationAlert> ActiveAlerts
    {
        get
        {
            if (_alerts.Count == 0)
            {
                return Array.Empty<MessageNotificationAlert>();
            }

            var list = new List<MessageNotificationAlert>(_alerts.Count);
            for (var i = 0; i < _alerts.Count; i++)
            {
                if (!_alerts[i].IsDismissed)
                {
                    list.Add(_alerts[i]);
                }
            }

            return list;
        }
    }

    public IReadOnlyList<MessageNotificationAlert> UnresolvedPauseAlerts
    {
        get
        {
            if (_alerts.Count == 0)
            {
                return Array.Empty<MessageNotificationAlert>();
            }

            var list = new List<MessageNotificationAlert>(_alerts.Count);
            for (var i = 0; i < _alerts.Count; i++)
            {
                var alert = _alerts[i];
                if (alert.TriggersPause && !alert.IsAcknowledged)
                {
                    list.Add(alert);
                }
            }

            return list;
        }
    }

    public bool HasUnresolvedPause
    {
        get
        {
            for (var i = 0; i < _alerts.Count; i++)
            {
                var alert = _alerts[i];
                if (alert.TriggersPause && !alert.IsAcknowledged)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public IReadOnlyList<MapBalloonAlert> ActiveBalloons => _balloons;

    public IReadOnlyList<CoalescedCategorySummary> CoalescedSummaries
    {
        get
        {
            if (_coalescedSequenceIds.Count == 0)
            {
                return Array.Empty<CoalescedCategorySummary>();
            }

            var categories = new string[_coalescedSequenceIds.Count];
            _coalescedSequenceIds.Keys.CopyTo(categories, 0);
            Array.Sort(categories, StringComparer.Ordinal);

            var result = new List<CoalescedCategorySummary>(categories.Length);
            for (var i = 0; i < categories.Length; i++)
            {
                var category = categories[i];
                var seqs = _coalescedSequenceIds[category];
                var latestSeq = seqs.Count > 0 ? seqs[seqs.Count - 1] : 0UL;
                var latestText = _coalescedLatestText.TryGetValue(category, out var txt) ? txt : string.Empty;
                result.Add(new CoalescedCategorySummary(
                    category,
                    seqs.Count,
                    seqs.ToArray(),
                    latestSeq,
                    latestText));
            }

            return result;
        }
    }

    /// <summary>
    /// Marks pre-existing lines as already seen without emitting clock actions or alerts.
    /// Called on planning-to-execution transition so pre-seeded smoke rows do not pause frame 1.
    /// </summary>
    public void MarkExistingAsSeen(IReadOnlyList<MessageLogLine>? lines)
    {
        if (lines is null)
        {
            return;
        }

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            if (line.SequenceId > 0)
            {
                _processedSequenceIds.Add(line.SequenceId);
            }
            else
            {
                _processedSyntheticKeys.Add(BuildSyntheticKey(line));
            }
        }
    }

    public NotificationBatchEvaluation Evaluate(
        IReadOnlyList<MessageLogLine>? liveLines,
        bool isReplayViewerAttached = false)
    {
        if (liveLines is null || liveLines.Count == 0)
        {
            return isReplayViewerAttached
                ? NotificationBatchEvaluation.Empty with { ReplaySuppressed = true }
                : NotificationBatchEvaluation.Empty;
        }

        if (isReplayViewerAttached)
        {
            MarkExistingAsSeen(liveLines);
            return NotificationBatchEvaluation.Empty with { ReplaySuppressed = true };
        }

        var anyPause = false;
        var anyDropToRealTime = false;
        var triggeredSeqIds = new List<ulong>();
        var reasons = new List<string>();
        var newAlerts = new List<MessageNotificationAlert>();
        var batchRoutines = new Dictionary<string, List<ulong>>(StringComparer.Ordinal);
        var batchRoutineLatestText = new Dictionary<string, string>(StringComparer.Ordinal);
        WatchPauseReason? primaryPauseReason = null;

        for (var i = 0; i < liveLines.Count; i++)
        {
            var line = liveLines[i];
            if (!TryClaimLine(line))
            {
                continue;
            }

            var rule = _policy.GetRule(line.Category);
            if (rule.Pause || rule.DropToRealTime || rule.MapBalloon)
            {
                var alert = new MessageNotificationAlert(
                    line.SequenceId,
                    line.SimTime,
                    line.Category,
                    line.UnitId,
                    line.Text,
                    TriggersPause: rule.Pause,
                    TriggersDropToRealTime: rule.DropToRealTime,
                    TriggersMapBalloon: rule.MapBalloon);
                _alerts.Add(alert);
                newAlerts.Add(alert);

                if (rule.MapBalloon)
                {
                    _balloons.Add(new MapBalloonAlert(
                        BuildBalloonId(line.SequenceId),
                        line.SequenceId,
                        line.SimTime,
                        line.Category,
                        line.UnitId,
                        line.Text,
                        rule.Pause));
                }

                if (rule.Pause || rule.DropToRealTime)
                {
                    triggeredSeqIds.Add(line.SequenceId);
                    reasons.Add($"{line.Category}: {line.Text}");
                }

                if (rule.Pause)
                {
                    anyPause = true;
                    primaryPauseReason ??= MapPauseReason(line.Category);
                }

                if (rule.DropToRealTime)
                {
                    anyDropToRealTime = true;
                }
            }
            else if (rule.Log)
            {
                var categoryKey = string.IsNullOrWhiteSpace(line.Category)
                    ? "LOG"
                    : line.Category.Trim().ToUpperInvariant();
                if (!_coalescedSequenceIds.TryGetValue(categoryKey, out var allSeqs))
                {
                    allSeqs = new List<ulong>();
                    _coalescedSequenceIds[categoryKey] = allSeqs;
                }

                allSeqs.Add(line.SequenceId);
                _coalescedLatestText[categoryKey] = line.Text;

                if (!batchRoutines.TryGetValue(categoryKey, out var batchSeqs))
                {
                    batchSeqs = new List<ulong>();
                    batchRoutines[categoryKey] = batchSeqs;
                }

                batchSeqs.Add(line.SequenceId);
                batchRoutineLatestText[categoryKey] = line.Text;
            }
        }

        var clockAction = anyPause
            ? ClockStopAction.Pause
            : anyDropToRealTime
                ? ClockStopAction.DropToRealTime
                : ClockStopAction.None;

        var coalescedBatch = BuildBatchCoalescedSummaries(batchRoutines, batchRoutineLatestText);
        return new NotificationBatchEvaluation(
            ClockAction: clockAction,
            ShouldPause: anyPause,
            ShouldDropToRealTime: anyDropToRealTime,
            ReplaySuppressed: false,
            TriggeredSequenceIds: triggeredSeqIds,
            Reasons: reasons,
            NewlyTriggeredAlerts: newAlerts,
            CoalescedRoutines: coalescedBatch,
            PrimaryPauseReason: primaryPauseReason);
    }

    public bool TryAcknowledgeAlert(ulong sequenceId)
    {
        var updated = false;
        for (var i = 0; i < _alerts.Count; i++)
        {
            if (_alerts[i].SequenceId == sequenceId)
            {
                _alerts[i] = _alerts[i] with { IsAcknowledged = true };
                updated = true;
            }
        }

        return updated;
    }

    public bool TryDismissAlert(ulong sequenceId)
    {
        var updated = false;
        for (var i = 0; i < _alerts.Count; i++)
        {
            if (_alerts[i].SequenceId == sequenceId)
            {
                _alerts[i] = _alerts[i] with { IsDismissed = true };
                updated = true;
            }
        }

        for (var i = _balloons.Count - 1; i >= 0; i--)
        {
            if (_balloons[i].SequenceId == sequenceId)
            {
                _balloons.RemoveAt(i);
                updated = true;
            }
        }

        return updated;
    }

    public void AcknowledgeAllPauses()
    {
        for (var i = 0; i < _alerts.Count; i++)
        {
            if (_alerts[i].TriggersPause && !_alerts[i].IsAcknowledged)
            {
                _alerts[i] = _alerts[i] with { IsAcknowledged = true };
            }
        }
    }

    public bool CanResume(bool explicitOverride = false) =>
        explicitOverride || !HasUnresolvedPause;

    public static string BuildBalloonId(ulong sequenceId) => $"msglog:{sequenceId}";

    private bool TryClaimLine(MessageLogLine line)
    {
        if (line.SequenceId > 0)
        {
            return _processedSequenceIds.Add(line.SequenceId);
        }

        return _processedSyntheticKeys.Add(BuildSyntheticKey(line));
    }

    private static string BuildSyntheticKey(MessageLogLine line) =>
        $"{line.SimTime:R}|{line.Category}|{line.UnitId ?? string.Empty}|{line.ActorId ?? string.Empty}|{line.Text}";

    private static WatchPauseReason? MapPauseReason(string? category)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            return null;
        }

        return category.Trim().ToUpperInvariant() switch
        {
            "DAMAGE" => WatchPauseReason.OwnSideLossOrDamage,
            "CONTACT" => WatchPauseReason.HostileOrUnknownContact,
            _ => null,
        };
    }

    private static IReadOnlyList<CoalescedCategorySummary> BuildBatchCoalescedSummaries(
        Dictionary<string, List<ulong>> batchRoutines,
        Dictionary<string, string> latestTexts)
    {
        if (batchRoutines.Count == 0)
        {
            return Array.Empty<CoalescedCategorySummary>();
        }

        var keys = new string[batchRoutines.Count];
        batchRoutines.Keys.CopyTo(keys, 0);
        Array.Sort(keys, StringComparer.Ordinal);

        var result = new List<CoalescedCategorySummary>(keys.Length);
        for (var i = 0; i < keys.Length; i++)
        {
            var key = keys[i];
            var seqs = batchRoutines[key];
            var latestSeq = seqs.Count > 0 ? seqs[seqs.Count - 1] : 0UL;
            var latestText = latestTexts.TryGetValue(key, out var txt) ? txt : string.Empty;
            result.Add(new CoalescedCategorySummary(key, seqs.Count, seqs.ToArray(), latestSeq, latestText));
        }

        return result;
    }
}
