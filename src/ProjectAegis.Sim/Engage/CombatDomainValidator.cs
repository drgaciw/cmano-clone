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
            // Mine domain has no legacy-specific gate (matches Facility): ADR-009's
            // MineAspectDomainValidator is the authoritative gate when combatDomainsEnabled is
            // on. Previously this arm unconditionally denied every Mine-domain engagement with
            // DomainNoSolution regardless of the aspect validator's verdict or the
            // combatDomainsEnabled flag, making Mine the only domain that could never launch.
            _ => null,
        };
    }
}