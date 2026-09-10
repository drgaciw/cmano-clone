namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using Core;

public sealed record DelegationTickResult(
    IReadOnlyList<Order> ExecutedOrders,
    int DispatchedToSim,
    int EngagementsResolved = 0);
