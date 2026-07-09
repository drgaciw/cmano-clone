namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless engage/DLZ preview for unit panel (req 14 / req 20 T3 WRA+ROE).</summary>
public static class EngagePreviewProjection
{
    public static EngagePreview Project(ScenarioEngageDefaults? engageDefaults)
    {
        var defaults = engageDefaults ?? ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(defaults.DefaultMagazineRounds);
        return Project(in ctx, defaults.DlzPersonality, doctrine: null);
    }

    public static EngagePreview Project(in EngageContext ctx, DlzPersonality personality) =>
        Project(in ctx, personality, doctrine: null);

    /// <summary>
    /// Engage preview with optional doctrine policy so ROE HoldFire and WRA max-salvo surface
    /// as abort codes (req 20 "Why can't I fire?" / T3).
    /// </summary>
    public static EngagePreview Project(
        in EngageContext ctx,
        DlzPersonality personality,
        EffectivePolicy? doctrine)
    {
        var state = DlzEngageGate.EvaluateState(ctx.RangeMeters, ctx.Envelope);
        var canFire = DlzEngageGate.AllowsLaunch(ctx.RangeMeters, ctx.Envelope, personality)
                        && ctx.HasFireControlTrack
                        && ctx.MountOnline
                        && ctx.RadarEmconActive
                        && !ctx.TrackSpoofed;

        string? abortCode = null;

        // Doctrine gates first so "Why can't I fire?" prefers ROE/WRA over sensor/DLZ.
        if (doctrine is { } policy)
        {
            if (policy.Roe == RoeLevel.HoldFire)
            {
                abortCode = AbortReasonCatalog.Doctrine.ROE_HOLD_FIRE;
                canFire = false;
            }
            else if (Math.Max(1, ctx.SalvoSize) > policy.MaxSalvo)
            {
                abortCode = AbortReasonCatalog.Doctrine.WRA_SALVO;
                canFire = false;
            }
        }

        if (abortCode == null)
        {
            if (!ctx.MountOnline)
            {
                abortCode = AbortReasonCatalog.Engage.MOUNT_OFFLINE;
            }
            else if (!ctx.RadarEmconActive)
            {
                abortCode = AbortReasonCatalog.Doctrine.EMCON_OFF;
            }
            else if (!ctx.HasFireControlTrack)
            {
                abortCode = AbortReasonCatalog.Engage.NO_FIRE_CONTROL_TRACK;
            }
            else if (!DlzEngageGate.AllowsLaunch(ctx.RangeMeters, ctx.Envelope, personality))
            {
                abortCode = AbortReasonCatalog.Engage.DLZ_OUT;
            }
            else if (ctx.TrackSpoofed)
            {
                abortCode = AbortReasonCatalog.Cyber.CYBER_SPOOF_TRACK;
            }
        }

        var dlzLabel = $"DLZ: {state} ({personality})";
        return new EngagePreview(dlzLabel, canFire, abortCode);
    }
}

public sealed record EngagePreview(string DlzLabel, bool CanFire, string? AbortPreviewCode);
