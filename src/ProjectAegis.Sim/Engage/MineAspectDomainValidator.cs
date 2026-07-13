namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 mine domain validator: envelope + aspect gate before launch.</summary>
public sealed class MineAspectDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Mine;

    public DomainValidateResult Validate(in EngageContext ctx)
    {
        if (ctx.Envelope.Contains(ctx.RangeMeters) && ctx.MineAspectInEnvelope)
        {
            return DomainValidateResult.Allow;
        }

        return DomainValidateResult.Deny(FireAbortReason.MineAspectBlock);
    }
}