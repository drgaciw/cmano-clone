namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 subsurface domain validator: envelope + aspect gate before launch.</summary>
public sealed class SubsurfaceAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Subsurface;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.SubsurfaceAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.SubsurfaceAspectBlock);
    }
}