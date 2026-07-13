namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 air domain validator: envelope + aspect gate before launch.</summary>
public sealed class AirAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Air;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.AirAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.AirAspectBlock);
    }
}