namespace ProjectAegis.Sim.Engage;

/// <summary>ADR-009: pluggable combat-domain validator (runs after policy, before geometry/magazine).</summary>
public interface IDomainValidator
{
    CombatDomain Domain { get; }

    DomainValidateResult Validate(in EngageContext ctx);
}