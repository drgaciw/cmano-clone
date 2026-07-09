namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// A single queued toast (req 20 §Alerting and Interruption, TR-c2-007). Colour is never the sole cue —
/// the ToastStack presentation host pairs <see cref="Severity"/> with an icon and a text label
/// (a11y §5); this model only carries the data needed to render and route a click.
/// </summary>
public sealed record ToastEntry(
    ulong SequenceId,
    AlertSeverity Severity,
    string Category,
    string Text,
    string? FocusUnitId);

/// <summary>
/// Pure queue/eviction model for the C2 ToastStack (Track T3, req 20 §Alerting and Interruption,
/// TR-c2-007). At most <see cref="MaxVisible"/> toasts are visible at once; the 4th and later queued
/// toasts collapse into an overflow count for a "+N" chip. A toast click resolves to a focus target
/// (unit id, falling back to the sequence id) for the presentation layer to hand to selection/focus —
/// this model never touches the sim or the bridge (ADR-010).
/// </summary>
/// <remarks>
/// Replay suppression (req 20 §Replay suppression): when constructed with
/// <paramref name="isReplaySuppressed"/> true, <see cref="Add"/> is a no-op, so replay (read-only)
/// playback never produces a toast.
/// </remarks>
public sealed class ToastStackModel
{
    /// <summary>Maximum toasts rendered at once; the rest collapse into <see cref="OverflowCount"/>.</summary>
    public const int MaxVisible = 3;

    private readonly List<ToastEntry> _queue = new();

    public ToastStackModel(bool isReplaySuppressed = false)
    {
        IsReplaySuppressed = isReplaySuppressed;
    }

    /// <summary>True when toasts are suppressed (replay/read-only mode) — <see cref="Add"/> becomes a no-op.</summary>
    public bool IsReplaySuppressed { get; }

    /// <summary>The oldest-first, at-most-<see cref="MaxVisible"/> toasts to render.</summary>
    public IReadOnlyList<ToastEntry> VisibleToasts =>
        _queue.Count <= MaxVisible ? _queue : _queue.GetRange(0, MaxVisible);

    /// <summary>Count of queued toasts beyond <see cref="MaxVisible"/> — drives the "+N" overflow chip.</summary>
    public int OverflowCount => Math.Max(0, _queue.Count - MaxVisible);

    /// <summary>Total queued toasts (visible + overflow).</summary>
    public int TotalCount => _queue.Count;

    /// <summary>
    /// Enqueues a toast. No-op when <see cref="IsReplaySuppressed"/> is true (req 20 §Replay suppression),
    /// or when a toast with the same <see cref="ToastEntry.SequenceId"/> is already queued — sequence ids
    /// are unique message-log line identifiers, so a repeat means the caller re-delivered the same alert
    /// and must not duplicate it in the stack.
    /// </summary>
    public void Add(ToastEntry entry)
    {
        if (IsReplaySuppressed)
        {
            return;
        }

        foreach (var existing in _queue)
        {
            if (existing.SequenceId == entry.SequenceId)
            {
                return;
            }
        }

        _queue.Add(entry);
    }

    /// <summary>Removes a queued/visible toast by sequence id (dismiss or expiry). Returns true if evicted.</summary>
    public bool Evict(ulong sequenceId) => _queue.RemoveAll(e => e.SequenceId == sequenceId) > 0;

    /// <summary>Clears every queued toast.</summary>
    public void Clear() => _queue.Clear();

    /// <summary>
    /// Resolves a toast click to its focus target: the referenced unit id if present, otherwise the
    /// toast's sequence id (stringified) so the presentation layer can still focus/scroll the message
    /// log to that entry. Returns null if no queued toast has this sequence id.
    /// </summary>
    public string? ResolveFocusTarget(ulong sequenceId)
    {
        foreach (var entry in _queue)
        {
            if (entry.SequenceId == sequenceId)
            {
                return string.IsNullOrEmpty(entry.FocusUnitId)
                    ? entry.SequenceId.ToString()
                    : entry.FocusUnitId;
            }
        }

        return null;
    }
}
