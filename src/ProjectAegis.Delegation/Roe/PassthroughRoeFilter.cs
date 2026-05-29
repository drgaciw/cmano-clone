namespace ProjectAegis.Delegation.Roe;

using ProjectAegis.Delegation.Core;

public sealed class PassthroughRoeFilter : IRoeFilter
{
    public RoeVerdict Evaluate(Order order) => RoeVerdict.Allow;
}
