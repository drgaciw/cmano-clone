namespace ProjectAegis.Delegation.Orchestration;

using ProjectAegis.Delegation.Core;
using ProjectAegis.Delegation.Roe;

public sealed record GateResult(bool ExecuteNow, bool QueueForApproval, bool Rejected);

public sealed class AutonomyGate
{
    private readonly IRoeFilter _roe;

    public AutonomyGate(IRoeFilter roe) => _roe = roe;

    public GateResult Evaluate(AutonomyLevel autonomy, Order order, bool playerApproved)
    {
        if (_roe.Evaluate(order) == RoeVerdict.Reject)
        {
            return new GateResult(false, false, true);
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
