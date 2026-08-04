namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;

/// <summary>
/// CMD-11: plain-language engage refusal / FireAbort explain for unit and contact panels.
/// Presentation-only — does not change resolve path.
/// </summary>
public static class EngageExplainProjection
{
    public const string CanFireLabel = "ENGAGE: CLEAR";
    public const string NoPreviewLabel = "ENGAGE: —";

    public static EngageExplain Project(EngagePreview? preview)
    {
        if (preview is null)
        {
            return EngageExplain.Empty;
        }

        if (preview.CanFire)
        {
            return new EngageExplain(
                StatusLine: CanFireLabel,
                ReasonCode: null,
                ReasonPlain: "Launch is permitted under current DLZ, track, mount, and EMCON constraints.",
                IsBlocked: false);
        }

        var code = preview.AbortPreviewCode;
        var plain = ExplainCode(code);
        return new EngageExplain(
            StatusLine: string.IsNullOrEmpty(code)
                ? "ENGAGE: BLOCKED"
                : $"ENGAGE: BLOCKED — {code}",
            ReasonCode: code,
            ReasonPlain: plain,
            IsBlocked: true);
    }

    public static EngageExplain ProjectFromContext(
        in EngageContext ctx,
        DlzPersonality personality = DlzPersonality.Normal)
    {
        var preview = EngagePreviewProjection.Project(in ctx, personality);
        return Project(preview);
    }

    public static string ExplainCode(string? abortCode)
    {
        if (string.IsNullOrWhiteSpace(abortCode))
        {
            return "Engagement is not available (no abort code).";
        }

        return abortCode switch
        {
            var c when c.Contains("MOUNT_OFFLINE", StringComparison.OrdinalIgnoreCase)
                => "Weapon mount is offline — restore mount before engaging.",
            var c when c.Contains("EMCON", StringComparison.OrdinalIgnoreCase)
                => "Radar EMCON is not active — emissions policy blocks fire control.",
            var c when c.Contains("NO_FIRE_CONTROL", StringComparison.OrdinalIgnoreCase)
                => "No fire-control track — acquire or designate a track first.",
            var c when c.Contains("DLZ", StringComparison.OrdinalIgnoreCase)
                => "Target is outside the dynamic launch zone for this weapon envelope.",
            var c when c.Contains("SPOOF", StringComparison.OrdinalIgnoreCase)
                => "Track is assessed as spoofed — engagement withheld.",
            var c when c.Contains("COMMS", StringComparison.OrdinalIgnoreCase)
                => "Comms denied — unit is out of communications.",
            var c when c.Contains("AIR_NOT_READY", StringComparison.OrdinalIgnoreCase)
                => "Air operations readiness gate — platform not ready for launch.",
            var c when c.Contains("NO_AMMO", StringComparison.OrdinalIgnoreCase)
                || c.Contains("WINCHESTER", StringComparison.OrdinalIgnoreCase)
                => "Magazine empty or insufficient rounds for this engagement.",
            var c when c.Contains("ROE", StringComparison.OrdinalIgnoreCase)
                || c.Contains("WEAPONS_TIGHT", StringComparison.OrdinalIgnoreCase)
                => "Rules of engagement deny this engagement.",
            _ => $"Engagement blocked ({abortCode}).",
        };
    }
}

public sealed record EngageExplain(
    string StatusLine,
    string? ReasonCode,
    string ReasonPlain,
    bool IsBlocked)
{
    public static EngageExplain Empty { get; } = new(
        EngageExplainProjection.NoPreviewLabel,
        null,
        "No engage preview for current selection.",
        IsBlocked: false);
}
