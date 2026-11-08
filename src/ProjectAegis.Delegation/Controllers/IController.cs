namespace ProjectAegis.Delegation.Controllers;

using Core;

public interface IController
{
    bool IsHuman { get; }

    IReadOnlyList<Order> DrainIssuedOrders(ulong currentSimTick);
}
