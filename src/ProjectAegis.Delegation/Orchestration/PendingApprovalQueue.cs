namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Watch;

/// <summary>
/// DRG-66 / AEGIS-301: session-local queue for orders that have been gated to <see cref="GateResult.QueueForApproval"/>.
/// Orders do not execute until the player calls <see cref="TryApprove"/> or <see cref="TryReject"/>.
/// Approved orders are promoted to <see cref="DrainApproved"/> for the next <c>Tick</c>.
/// Thread-safety: single-threaded sim only (no locking).
/// </summary>
public sealed class PendingApprovalQueue
{
    private readonly List<PendingApprovalEntry> _pending = new();
    private readonly List<Order> _approved = new();

    /// <summary>Orders waiting for player approval (ordered by enqueue time).</summary>
    public IReadOnlyList<PendingApprovalEntry> Pending => _pending;

    /// <summary>Pending orders waiting for player approval.</summary>
    public IReadOnlyList<PendingApprovalEntry> PendingOrders => _pending;

    /// <summary>Count of orders currently waiting for player approval.</summary>
    public int Count => _pending.Count;

    /// <summary>True when one or more orders are waiting for player approval.</summary>
    public bool HasPendingProposals => _pending.Count > 0;

    /// <summary>Optional auto-pause gate wired to trigger sim auto-pause on enqueued proposals.</summary>
    public WatchAutoPauseGate? AutoPauseGate { get; set; }

    /// <summary>Optional callback invoked whenever a new proposal is successfully enqueued.</summary>
    public Action<PendingApprovalEntry>? OnOrderEnqueued { get; set; }

    /// <summary>
    /// Enqueues <paramref name="order"/> for player approval.
    /// Idempotent: duplicate <see cref="OrderId"/> values are silently ignored.
    /// </summary>
    public void Enqueue(Order order)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        foreach (var existing in _pending)
        {
            if (existing.Order.Id == order.Id)
            {
                return;
            }
        }

        var entry = new PendingApprovalEntry(order);
        _pending.Add(entry);
        AutoPauseGate?.ShouldAutoPause(this);
        OnOrderEnqueued?.Invoke(entry);
    }

    /// <summary>
    /// Approves the pending order with <paramref name="orderId"/>.
    /// The order is promoted and will be included in the next call to <see cref="DrainApproved"/>.
    /// Returns <c>true</c> if found and promoted; <c>false</c> if no matching entry exists.
    /// </summary>
    public bool TryApprove(OrderId orderId)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Order.Id == orderId)
            {
                _approved.Add(_pending[i].Order);
                _pending.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Rejects and discards the pending order with <paramref name="orderId"/>.
    /// Returns <c>true</c> if found and dropped; <c>false</c> if no matching entry exists.
    /// </summary>
    public bool TryReject(OrderId orderId)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Order.Id == orderId)
            {
                _pending.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns all previously approved orders and clears the approved buffer.
    /// Called once per <c>Tick</c> to inject approved orders into <c>ExecutedOrders</c>.
    /// </summary>
    public IReadOnlyList<Order> DrainApproved()
    {
        if (_approved.Count == 0)
        {
            return Array.Empty<Order>();
        }

        var copy = _approved.ToArray();
        _approved.Clear();
        return copy;
    }

    /// <summary>Clears both pending and approved buffers (e.g. on scenario change).</summary>
    public void Clear()
    {
        _pending.Clear();
        _approved.Clear();
    }
}

/// <summary>One entry in the pending-approval queue.</summary>
/// <param name="Order">The order awaiting player decision.</param>
public sealed record PendingApprovalEntry(Order Order);
