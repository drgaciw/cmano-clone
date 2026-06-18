namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>ADR-009 registry with stable domain ordering (ordinal by CombatDomain).</summary>
public sealed class DomainValidatorRegistry
{
    private readonly IDomainValidator[] _validators;

    public DomainValidatorRegistry(IEnumerable<IDomainValidator> validators)
    {
        _validators = validators
            .OrderBy(v => (int)v.Domain)
            .ToArray();
    }

    public IReadOnlyList<IDomainValidator> Validators => _validators;

    public DomainValidateResult Validate(CombatDomain domain, in EngageContext ctx)
    {
        foreach (var validator in _validators)
        {
            if (validator.Domain != domain)
            {
                continue;
            }

            var result = validator.Validate(in ctx);
            if (!result.Allowed)
            {
                return result;
            }
        }

        return DomainValidateResult.Allow;
    }

    public static DomainValidatorRegistry MvpStubs { get; } = new(
    [
        new AirAspectDomainValidator(),
        new SurfaceAspectDomainValidator(),
        new SubsurfaceAspectDomainValidator(),
    ]);
}