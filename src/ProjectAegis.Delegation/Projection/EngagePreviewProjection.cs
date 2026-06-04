namespace ProjectAegis.Delegation.Projection;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Glossary;
using ProjectAegis.Sim.Scenario;

/// <summary>Headless engage/DLZ preview for unit panel (req 14).</summary>
public static class EngagePreviewProjection
{
    public static EngagePreview Project(ScenarioEngageDefaults? engageDefaults)
    {
        var defaults = engageDefaults ?? ScenarioEngageDefaults.MvpFallback;
        var ctx = defaults.ToEngageContext(defaults.DefaultMagazineRounds);
        return Project(in ctx, defaults.DlzPersonality);
    }

    public static EngagePreview Project(in EngageContext ctx, DlzPersonality personality)
    {
        var state = DlzEngageGate.EvaluateState(ctx.RangeMeters, ctx.Envelope);
        var canFire = DlzEngageGate.AllowsLaunch(ctx.RangeMeters, ctx.Envelope, personality)
                        && ctx.HasFireControlTrack
                        && ctx.MountOnline
                        && ctx.RadarEmconActive;

        string? abortCode = null;
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

        var dlzLabel = $"DLZ: {state} ({personality})";
        return new EngagePreview(dlzLabel, canFire, abortCode);
    }
}

public sealed record EngagePreview(string DlzLabel, bool CanFire, string? AbortPreviewCode);