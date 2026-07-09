namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;

/// <summary>
/// "Why can't I fire?" pure projection (req 20 §Unit Detail Panel / TR-c2-006 residual).
/// Composes ROE + WRA denials with engage-preview abort codes for one-click explain UI.
/// Presentation only — never mutates sim state (ADR-010).
/// </summary>
public sealed record FireDenyExplain(
    bool CanFire,
    string SummaryLine,
    string? PrimaryAbortCode,
    IReadOnlyList<string> ReasonCodes,
    bool PositiveControlRequired);

/// <summary>Builds fire-deny explain lines from doctrine policy + engage preview.</summary>
public static class FireDenyExplainProjection
{
    /// <summary>
    /// Projects a human-readable explain for the selected unit's current fire readiness.
    /// <paramref name="requestedSalvo"/> is compared against WRA max-salvo (policy GDD).
    /// WeaponsTight does not hard-deny human fire — it sets <see cref="FireDenyExplain.PositiveControlRequired"/>.
    /// </summary>
    public static FireDenyExplain Project(
        EngagePreview engagePreview,
        EffectivePolicy policy,
        int requestedSalvo = 1)
    {
        var reasons = new List<string>();

        if (policy.Roe == RoeLevel.HoldFire)
        {
            reasons.Add(AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE);
        }

        var salvo = Math.Max(1, requestedSalvo);
        if (salvo > policy.MaxSalvo)
        {
            reasons.Add(AbortReasonCatalog.Doctrine.WRA_SALVO);
        }

        if (!string.IsNullOrEmpty(engagePreview.AbortPreviewCode) &&
            !reasons.Contains(engagePreview.AbortPreviewCode, StringComparer.Ordinal))
        {
            reasons.Add(engagePreview.AbortPreviewCode!);
        }

        var positiveControl = PositiveControlRequiredProjection.IsRequired(policy);
        var hardBlocked = reasons.Count > 0 || !engagePreview.CanFire;
        // WeaponsTight alone is not a hard block for the human weapons-release path.
        if (policy.Roe == RoeLevel.WeaponsTight &&
            reasons.Count == 0 &&
            engagePreview.CanFire)
        {
            hardBlocked = false;
        }

        string? primary = reasons.Count > 0
            ? reasons[0]
            : (!engagePreview.CanFire ? engagePreview.AbortPreviewCode : null);

        var summary = FormatSummary(hardBlocked, primary, positiveControl, policy);
        return new FireDenyExplain(
            CanFire: !hardBlocked,
            SummaryLine: summary,
            PrimaryAbortCode: primary,
            ReasonCodes: reasons,
            PositiveControlRequired: positiveControl);
    }

    /// <summary>
    /// Maps a logged <see cref="FireAbortReason"/> (policy denial / domain block) to an explain view.
    /// </summary>
    public static FireDenyExplain FromFireAbortReason(FireAbortReason reason)
    {
        if (reason == FireAbortReason.None)
        {
            return new FireDenyExplain(
                CanFire: true,
                SummaryLine: "WHY: —",
                PrimaryAbortCode: null,
                ReasonCodes: Array.Empty<string>(),
                PositiveControlRequired: false);
        }

        var code = MapFireAbortCode(reason);
        var positiveControl = reason == FireAbortReason.WeaponsTight;
        // WeaponsTight denials in agent path are hard blocks; explain still notes positive control.
        var canFire = false;
        var summary = reason == FireAbortReason.WeaponsTight
            ? $"WHY: {code} — positive control required (agent denied)"
            : $"WHY: {code}";

        return new FireDenyExplain(
            CanFire: canFire,
            SummaryLine: summary,
            PrimaryAbortCode: code,
            ReasonCodes: new[] { code },
            PositiveControlRequired: positiveControl);
    }

    private static string FormatSummary(
        bool hardBlocked,
        string? primary,
        bool positiveControl,
        EffectivePolicy policy)
    {
        if (!hardBlocked)
        {
            return positiveControl
                ? "WHY: — (positive control required before release)"
                : "WHY: —";
        }

        if (primary == AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE)
        {
            return $"WHY: {primary} — ROE {policy.Roe}";
        }

        if (primary == AbortReasonCatalog.Doctrine.WRA_SALVO)
        {
            return $"WHY: {primary} — max salvo {policy.MaxSalvo}";
        }

        return primary == null ? "WHY: BLOCKED" : $"WHY: {primary}";
    }

    private static string MapFireAbortCode(FireAbortReason reason) =>
        reason switch
        {
            FireAbortReason.RoeHoldFire => AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE,
            FireAbortReason.WeaponsTight => AbortReasonCatalog.Doctrine.ROE_WEAPONS_TIGHT,
            FireAbortReason.WraRange => AbortReasonCatalog.Doctrine.WRA_RANGE,
            FireAbortReason.WraSalvo => AbortReasonCatalog.Doctrine.WRA_SALVO,
            FireAbortReason.EmconOff => AbortReasonCatalog.Doctrine.EMCON_OFF,
            FireAbortReason.NoFireControlTrack => AbortReasonCatalog.Doctrine.NO_FIRE_CONTROL_TRACK,
            FireAbortReason.CommsDenied => AbortReasonCatalog.Doctrine.COMMS_DENIED,
            FireAbortReason.AirAspectBlock => AbortReasonCatalog.Doctrine.AIR_ASPECT_BLOCK,
            FireAbortReason.SurfaceAspectBlock => AbortReasonCatalog.Doctrine.SURFACE_ASPECT_BLOCK,
            FireAbortReason.SubsurfaceAspectBlock => AbortReasonCatalog.Doctrine.SUBSURFACE_ASPECT_BLOCK,
            FireAbortReason.LandAspectBlock => AbortReasonCatalog.Doctrine.LAND_ASPECT_BLOCK,
            FireAbortReason.MineAspectBlock => AbortReasonCatalog.Doctrine.MINE_ASPECT_BLOCK,
            FireAbortReason.FacilityAspectBlock => AbortReasonCatalog.Doctrine.FACILITY_ASPECT_BLOCK,
            _ => reason.ToString(),
        };
}
