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
}