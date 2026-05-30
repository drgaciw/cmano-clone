namespace ProjectAegis.Sim.Policy;

/// <summary>Merged policy for a unit at evaluation time (MVP: ROE only).</summary>
public readonly record struct EffectivePolicy(RoeLevel Roe)
{
    public static EffectivePolicy DefaultFree => new(RoeLevel.WeaponsFree);
}
