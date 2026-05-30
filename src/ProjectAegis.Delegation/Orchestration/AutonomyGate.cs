namespace ProjectAegis.Delegation.Orchestration;

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

    public AutonomyGate(IRoeFilter roe) => _roe = roe;

    public GateResult Evaluate(AutonomyLevel autonomy, Order order, bool playerApproved)
    {
        var roe = _roe.Evaluate(order);
        if (roe.Verdict == RoeVerdict.Reject)
        {
            return new GateResult(false, false, true, roe.Reason);
        }

        return autonomy switch
        {
            AutonomyLevel.Manual => new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.Assisted when order.Risk == RiskLevel.Low =>
                new GateResult(true, false, false),
            AutonomyLevel.Assisted =>
                new GateResult(playerApproved, !playerApproved, false),
            AutonomyLevel.SemiAutonomous or AutonomyLevel.FullAutonomous =>
                new GateResult(true, false, false),
            _ => new GateResult(false, true, false),
        };
    }
}
