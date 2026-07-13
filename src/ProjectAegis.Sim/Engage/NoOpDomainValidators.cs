namespace ProjectAegis.Sim.Engage;

/// <summary>ADR-009 MVP stubs: allow-all validators for air and surface domains.</summary>
public sealed class NoOpAirDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Air;

    public DomainValidateResult Validate(in EngageContext ctx) => DomainValidateResult.Allow;
}

public sealed class NoOpSurfaceDomainValidator : IDomainValidator
{
    public CombatDomain Domain => CombatDomain.Surface;

    public DomainValidateResult Validate(in EngageContext ctx) => DomainValidateResult.Allow;
}