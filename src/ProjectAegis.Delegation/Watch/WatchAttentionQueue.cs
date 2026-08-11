namespace ProjectAegis.Delegation.Watch;

/// <summary>
/// S115-02: session-local ordered watch attention queue.
/// Pattern mirrors <c>PendingApprovalQueue</c> — pure, single-threaded, no Bridge.
/// Ordering: Priority (Critical first) → TriggerTick ascending → EventId ordinal.
/// Ack / dismiss are presentation-only and restorable.
/// </summary>
public sealed class WatchAttentionQueue
{
    private readonly List<WatchAttentionCard> _cards = new();

    /// <summary>Live cards in stable sort order (including acknowledged; excluding dismissed by default view).</summary>
    public IReadOnlyList<WatchAttentionCard> Cards => _cards;

    /// <summary>Count of pause-class cards that are neither acknowledged nor dismissed.</summary>
    public int UnresolvedPauseClassCount
    {
        get
        {
            var n = 0;
            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].IsUnresolved)
                {
                    n++;
                }
            }

            return n;
        }
    }

    public bool HasUnresolvedPauseClass => UnresolvedPauseClassCount > 0;

    /// <summary>
    /// Enqueues <paramref name="evt"/>. Idempotent on <see cref="WatchAttentionEvent.EventId"/>.
    /// Re-sorts after insert so priority/tick/id order is always maintained.
    /// </summary>
    public void Enqueue(WatchAttentionEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);
        if (string.IsNullOrEmpty(evt.EventId))
        {
            throw new ArgumentException("EventId must be non-empty.", nameof(evt));
        }

        for (var i = 0; i < _cards.Count; i++)
        {
            if (string.Equals(_cards[i].EventId, evt.EventId, StringComparison.Ordinal))
            {
                return;
            }
        }

        _cards.Add(new WatchAttentionCard(evt));
        SortInPlace();
    }

    /// <summary>Marks the card acknowledged. Returns false if not found.</summary>
    public bool TryAcknowledge(string eventId)
    {
        var idx = IndexOf(eventId);
        if (idx < 0)
        {
            return false;
        }

        var card = _cards[idx];
        if (card.IsAcknowledged)
        {
            return true;
        }

        _cards[idx] = card with { IsAcknowledged = true };
        return true;
    }

    /// <summary>Soft-dismisses the card (presentation). Returns false if not found.</summary>
    public bool TryDismiss(string eventId)
    {
        var idx = IndexOf(eventId);
        if (idx < 0)
        {
            return false;
        }

        var card = _cards[idx];
        if (card.IsDismissed)
        {
            return true;
        }

        _cards[idx] = card with { IsDismissed = true };
        return true;
    }

    /// <summary>Restores a dismissed card (clears IsDismissed). Returns false if not found.</summary>
    public bool TryRestore(string eventId)
    {
        var idx = IndexOf(eventId);
        if (idx < 0)
        {
            return false;
        }

        var card = _cards[idx];
        if (!card.IsDismissed)
        {
            return true;
        }

        _cards[idx] = card with { IsDismissed = false };
        return true;
    }

    /// <summary>
    /// Returns cards for the default watch panel: non-dismissed, ordered.
    /// Ack state is preserved so UI can style them.
    /// </summary>
    public IReadOnlyList<WatchAttentionCard> SnapshotVisible()
    {
        if (_cards.Count == 0)
        {
            return Array.Empty<WatchAttentionCard>();
        }

        var list = new List<WatchAttentionCard>(_cards.Count);
        for (var i = 0; i < _cards.Count; i++)
        {
            if (!_cards[i].IsDismissed)
            {
                list.Add(_cards[i]);
            }
        }

        return list;
    }

    /// <summary>Clears all cards (session reset / scenario change).</summary>
    public void Clear()
    {
        _cards.Clear();
    }

    private int IndexOf(string eventId)
    {
        if (string.IsNullOrEmpty(eventId))
        {
            return -1;
        }

        for (var i = 0; i < _cards.Count; i++)
        {
            if (string.Equals(_cards[i].EventId, eventId, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void SortInPlace()
    {
        _cards.Sort(static (a, b) =>
        {
            var c = a.Priority.CompareTo(b.Priority);
            if (c != 0)
            {
                return c;
            }

            c = a.TriggerTick.CompareTo(b.TriggerTick);
            if (c != 0)
            {
                return c;
            }

            return string.CompareOrdinal(a.EventId, b.EventId);
        });
    }
}
