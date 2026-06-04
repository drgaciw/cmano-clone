namespace ProjectAegis.Sim.Engage;

/// <summary>Domain-specific engage checks after generic pipeline steps (req 18).</summary>
public static class CombatDomainValidator
{
    public static EngagementAbortReason? Validate(CombatDomain domain, in EngageContext ctx)
    {
        if (!ctx.MountOnline)
        {
            return EngagementAbortReason.MountOffline;
        }

        return domain switch
        {
            CombatDomain.Subsurface when !ctx.HasFireControlTrack => EngagementAbortReason.DomainNoSolution,
            CombatDomain.Subsurface when !ctx.ContactIdentified => EngagementAbortReason.DomainNoSolution,
            CombatDomain.Land when ctx.RangeMeters > ctx.Envelope.MaxRangeMeters * 0.5 =>
                EngagementAbortReason.OutOfEnvelope,
            CombatDomain.Mine => EngagementAbortReason.DomainNoSolution,
            _ => null,
        };
    }
}