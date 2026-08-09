namespace ProjectAegis.Sim.Policy;

/// <summary>
/// Merged policy for a unit at evaluation time (MVP: ROE + WRA max salvo + SWARM-15 gates).
/// </summary>
public readonly record struct EffectivePolicy(
    RoeLevel Roe,
    int MaxSalvo = 8,
    bool AutoEngageAuthorized = true,
    bool ExpendAuthorized = false)
{
    public const int DefaultMaxSalvo = 8;

    /// <summary>Weapons free, auto-engage allowed, expend denied until authorized (SWARM-19).</summary>
    public static EffectivePolicy DefaultFree => new(RoeLevel.WeaponsFree);
}
