namespace ProjectAegis.Delegation.Decision;

using ProjectAegis.Delegation.Core;

/// <summary>Holds player orders until <see cref="ExecuteSimTick"/> (req 19 comms delay).</summary>
public sealed class PlayerOrderExecutionQueue
{
    private readonly List<(Order Order, ulong ExecuteSimTick)> _pending = new();

    public void Enqueue(Order order, ulong executeSimTick) =>
        _pending.Add((order, executeSimTick));

    public IReadOnlyList<Order> DrainReady(ulong currentSimTick)
    {
        if (_pending.Count == 0)
        {
            return Array.Empty<Order>();
        }

        var ready = new List<Order>();
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var (order, executeTick) = _pending[i];
            if (executeTick > currentSimTick)
            {
                continue;
            }

            ready.Add(order);
            _pending.RemoveAt(i);
        }

        ready.Reverse();
        return ready;
    }

    /// <summary>
    /// Remove the oldest still-pending order for <paramref name="target"/> before it drains
    /// (req 20 rev 2 §Order lifecycle cancel/replan). Returns false when the target has no pending order.
    /// Additive — <see cref="DrainReady"/> and <see cref="Enqueue"/> are unchanged, so replays that never
    /// cancel behave identically.
    /// </summary>
    public bool TryRemove(TargetId target, out Order removed, out ulong executeSimTick)
    {
        for (var i = 0; i < _pending.Count; i++)
        {
            if (_pending[i].Order.Target.Equals(target))
            {
                removed = _pending[i].Order;
                executeSimTick = _pending[i].ExecuteSimTick;
                _pending.RemoveAt(i);
                return true;
            }
        }

        removed = default!;
        executeSimTick = 0;
        return false;
    }

    public int PendingCount => _pending.Count;
}