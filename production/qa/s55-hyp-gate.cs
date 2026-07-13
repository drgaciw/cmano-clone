namespace ProjectAegis.Sim.Engage;

/// <summary>Hypersonic defense layer gate (req 09 HACM / TL-2 preview).</summary>
public static class HypersonicEngageGate
{
    public static EngagementAbortReason? Evaluate(in EngageContext ctx)
    {
        if (!ctx.IsHypersonicTarget)
        {
            return null;
        }

        return ctx.HasHypersonicDefenseLayer
            ? null
            : EngagementAbortReason.DomainNoSolution;
    }
}