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

    public int PendingCount => _pending.Count;
}