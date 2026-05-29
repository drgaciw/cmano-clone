namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;

public static class OrderDispatcher
{
    public static int Dispatch(
        IReadOnlyList<Order> orders,
        TargetRegistry registry,
        IOrderSink sink)
    {
        var count = 0;
        foreach (var order in orders)
        {
            if (!registry.TryGetBinding(order.Target, out var binding))
            {
                continue;
            }

            sink.ApplyOrder(binding.Entity, order);
            count++;
        }

        return count;
    }
}
