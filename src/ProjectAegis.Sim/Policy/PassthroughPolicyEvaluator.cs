namespace ProjectAegis.Sim.Policy;

/// <summary>MVP: allow all actions until policy GDD rules are implemented.</summary>
public sealed class PassthroughPolicyEvaluator : IPolicyEvaluator
{
    public PolicyVerdict Evaluate(in PolicyContext ctx, in ActionRequest request)
    {
        _ = ctx;
        _ = request;
        return PolicyVerdict.Allow();
    }
}
