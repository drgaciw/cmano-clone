namespace ProjectAegis.Delegation.Roe;

using Core;

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
