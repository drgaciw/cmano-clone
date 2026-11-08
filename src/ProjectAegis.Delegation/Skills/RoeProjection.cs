namespace ProjectAegis.Delegation.Skills;

using ProjectAegis.Sim.Policy;

/// <summary>
/// DRG-209: ROE doctrine slice of authority projection for DRG-182.
/// Surfaces whether current rules permit, withhold, or gate targeting.
/// </summary>
public sealed record RoeProjection(
    RoeLevel Roe,
    string RoeLabel,
    C2AuthorityDisposition TargetingDisposition,
    string? TargetingReasonCode,
    bool EngageAllowedByRoe)
{
    /// <summary>Stable label for UI and explain surfaces.</summary>
    public static string FormatRoeLabel(RoeLevel roe) =>
        roe switch
        {
            RoeLevel.HoldFire => "HOLD_FIRE",
            RoeLevel.WeaponsTight => "WEAPONS_TIGHT",
            RoeLevel.WeaponsFree => "WEAPONS_FREE",
            _ => roe.ToString().ToUpperInvariant(),
        };
}
