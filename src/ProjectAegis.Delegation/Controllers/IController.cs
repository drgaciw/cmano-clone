namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;

public interface IController
{
    bool IsHuman { get; }

    IReadOnlyList<Order> DrainIssuedOrders();
}
