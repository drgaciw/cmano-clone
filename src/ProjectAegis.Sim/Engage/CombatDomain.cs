namespace ProjectAegis.Sim.Engage;

/// <summary>Combat domain for validator dispatch (req 18).</summary>
public enum CombatDomain
{
    Air = 0,
    Surface = 1,
    Subsurface = 2,
    Land = 3,
    Mine = 4,
    Facility = 5,
}

public static class CombatDomainParser
{
    public static CombatDomain Parse(string? value) =>
        Enum.TryParse<CombatDomain>(value, ignoreCase: true, out var parsed)
            ? parsed
            : CombatDomain.Air;
}