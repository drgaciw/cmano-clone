namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Delegation.Core;

public sealed record DelegationTickResult(
    IReadOnlyList<Order> ExecutedOrders,
    int DispatchedToSim);
