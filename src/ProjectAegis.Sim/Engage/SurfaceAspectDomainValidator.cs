namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 surface domain validator: envelope + aspect gate before launch.</summary>
public sealed class SurfaceAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Surface;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.SurfaceAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.SurfaceAspectBlock);
    }
}