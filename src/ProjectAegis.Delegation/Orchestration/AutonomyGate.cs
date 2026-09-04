namespace ProjectAegis.Delegation.Orchestration;

using System;
using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Roe;
using ProjectAegis.Sim.Policy;

public sealed record GateResult(
    bool ExecuteNow,
    bool QueueForApproval,
    bool Rejected,
    FireAbortReason PolicyDenialReason = FireAbortReason.None);

public sealed class AutonomyGate
{
    private readonly IRoeFilter _roe;

    public AutonomyGate(IRoeFilter roe) =>
        _roe = roe ?? throw new ArgumentNullException(nameof(roe));

    /// <summary>
    /// Evaluates whether an order may execute immediately, must be routed to hold/review,
    /// or is rejected by ROE.
    /// Interim safety gate HOL-04B (AEGIS-302 / DRG-232):
    /// For SemiAutonomous controllers, lethal fire orders requested without prior authorization
    /// or outside pre-cleared ROE parameters route to hold or pending review rather than
    /// instantaneous uninhibited ExecuteNow.
    /// </summary>
    public GateResult Evaluate(AutonomyLevel autonomy, Order order, bool playerApproved)
    {
        if (order is null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        var roe = _roe.Evaluate(order);
        if (roe.Verdict == RoeVerdict.Reject)
        {
            return new GateResult(false, false, true, roe.Reason);
        }

        if (roe.Verdict == RoeVerdict.Queue)
        {
            return new GateResult(false, true, false, roe.Reason);
        }

        return autonomy switch
        {
            AutonomyLevel.Manual => new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.Assisted when order.Risk == RiskLevel.Low =>
                new GateResult(true, false, false),
            AutonomyLevel.Assisted =>
                new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.SemiAutonomous when IsLethalFireOrder(order) =>
                new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.SemiAutonomous =>
                new GateResult(true, false, false),
            AutonomyLevel.FullAutonomous =>
                new GateResult(true, false, false),
            _ => new GateResult(false, true, false),
        };
    }

    /// <summary>
    /// Identifies whether the order constitutes a lethal fire order (e.g. engage or high-risk action).
    /// </summary>
    public static bool IsLethalFireOrder(Order order)
    {
        if (order is null)
        {
            return false;
        }

        return order.Kind == OrderKind.Engage || order.Risk == RiskLevel.High;
    }

    /// <summary>
    /// Alias for <see cref="IsLethalFireOrder(Order)"/>.
    /// </summary>
    public static bool IsLethalOrder(Order order) => IsLethalFireOrder(order);
}
