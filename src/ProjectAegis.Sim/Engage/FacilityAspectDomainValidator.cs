namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 facility domain validator: envelope + aspect gate before launch.</summary>
public sealed class FacilityAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Facility;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.FacilityAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.FacilityAspectBlock);
    }
}