namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public enum RoeVerdict
{
    Allow,
    Reject,
    Queue,
}

public interface IRoeFilter
{
    RoeEvaluation Evaluate(Order order);
}
