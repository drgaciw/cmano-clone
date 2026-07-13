namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 land domain validator: envelope + aspect gate before launch.</summary>
public sealed class LandAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Land;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.LandAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.LandAspectBlock);
    }
}