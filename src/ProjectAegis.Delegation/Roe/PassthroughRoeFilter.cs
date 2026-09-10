namespace ProjectAegis.Delegation.Roe;

using Core;

public sealed class PassthroughRoeFilter : IRoeFilter
{
    public RoeEvaluation Evaluate(Order order)
    {
        _ = order;
        return RoeEvaluation.Allow();
    }
}
