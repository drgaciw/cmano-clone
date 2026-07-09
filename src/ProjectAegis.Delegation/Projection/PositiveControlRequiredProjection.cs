namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Policy;

/// <summary>
/// Req 20 P0 / TR-c2-006 residual: policy projection flag for the weapons-release confirmation gate.
/// Replaces ad-hoc <c>WeaponsTight</c> checks in hosts — call sites must use this API so a future
/// explicit doctrine field can land without another UI rewrite.
/// </summary>
/// <remarks>
/// Mapping (v1): <see cref="RoeLevel.WeaponsTight"/> requires positive control (confirm before fire).
/// <see cref="RoeLevel.HoldFire"/> does not surface a weapons-release gate (fire is denied upstream).
/// <see cref="RoeLevel.WeaponsFree"/> does not require the gate.
/// </remarks>
public static class PositiveControlRequiredProjection
{
    /// <summary>True when the weapons-release confirmation gate applies for this ROE.</summary>
    public static bool IsRequired(RoeLevel roe) =>
        roe == RoeLevel.WeaponsTight;

    /// <summary>True when the weapons-release confirmation gate applies for this effective policy.</summary>
    public static bool IsRequired(EffectivePolicy policy) =>
        IsRequired(policy.Roe);
}
