namespace ProjectAegis.Delegation.Controllers;

using ProjectAegis.Delegation.Core;

public sealed class HumanController : IController
{
    private readonly List<Order> _pending = new();

    public bool IsHuman => true;

    public void Enqueue(Order order) => _pending.Add(order);

    public IReadOnlyList<Order> DrainIssuedOrders()
    {
        if (_pending.Count == 0)
        {
            return Array.Empty<Order>();
        }

        var copy = _pending.ToArray();
        _pending.Clear();
        return copy;
    }
}
