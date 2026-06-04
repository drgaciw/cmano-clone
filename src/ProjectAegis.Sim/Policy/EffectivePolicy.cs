namespace ProjectAegis.Sim.Policy;

/// <summary>Merged policy for a unit at evaluation time (MVP: ROE + WRA max salvo).</summary>
public readonly record struct EffectivePolicy(RoeLevel Roe, int MaxSalvo = 8)
{
    public const int DefaultMaxSalvo = 8;

    public static EffectivePolicy DefaultFree => new(RoeLevel.WeaponsFree);
}