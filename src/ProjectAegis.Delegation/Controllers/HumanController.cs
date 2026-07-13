namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Decision;

public sealed class HumanController : IController
{
    private readonly PlayerOrderExecutionQueue _queue = new();

    public bool IsHuman => true;

    public void Enqueue(Order order, ulong executeSimTick) => _queue.Enqueue(order, executeSimTick);

    public int PendingOrderCount => _queue.PendingCount;

    public IReadOnlyList<Order> DrainIssuedOrders(ulong currentSimTick) =>
        _queue.DrainReady(currentSimTick);

    /// <summary>Cancel the oldest pending order for <paramref name="target"/> before it executes
    /// (req 20 rev 2 §Order lifecycle cancel/replan). Returns false when nothing is pending for it.</summary>
    public bool TryCancel(TargetId target, out Order removed, out ulong executeSimTick) =>
        _queue.TryRemove(target, out removed, out executeSimTick);
}