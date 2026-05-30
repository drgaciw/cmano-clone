namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Sim.Policy;

public static class OrderActionMapper
{
    public static ActionRequest ToActionRequest(Order order) =>
        new(MapKind(order.Kind), TargetIdToUlong(order.Target), 0);

    public static ulong TargetIdToUlong(TargetId target)
    {
        ulong hash = 0;
        foreach (var c in target.Value)
        {
            hash = (hash * 31) + c;
        }

        return hash;
    }

    private static ActionKind MapKind(OrderKind kind) =>
        kind switch
        {
            OrderKind.Engage => ActionKind.FireGuided,
            OrderKind.SetEwPosture => ActionKind.Jam,
            OrderKind.Move or OrderKind.Hold or OrderKind.ReturnToBase => ActionKind.Observe,
            _ => ActionKind.Observe,
        };
}
