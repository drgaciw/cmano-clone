namespace ProjectAegis.Sim.Engage;

using ProjectAegis.Sim.Policy;

/// <summary>Stable order-log codes for engagement aborts (doc 14 / order-log-replay).</summary>
public static class EngagementAbortReasonCodes
{
    public const string Launched = "Launched";

    public const string NoResult = "NoResult";

    public static string ToLogCode(EngagementAbortReason reason) =>
        reason switch
        {
            EngagementAbortReason.None => "Unknown",
            EngagementAbortReason.OutOfEnvelope => nameof(EngagementAbortReason.OutOfEnvelope),
            EngagementAbortReason.DlzOut => nameof(EngagementAbortReason.DlzOut),
            EngagementAbortReason.MagazineEmpty => nameof(EngagementAbortReason.MagazineEmpty),
            EngagementAbortReason.NoFireControlTrack => nameof(EngagementAbortReason.NoFireControlTrack),
            EngagementAbortReason.EmconOff => nameof(FireAbortReason.EmconOff),
            EngagementAbortReason.RoeHoldFire => nameof(FireAbortReason.RoeHoldFire),
            EngagementAbortReason.WeaponsTight => nameof(FireAbortReason.WeaponsTight),
            EngagementAbortReason.TargetDestroyed => nameof(EngagementAbortReason.TargetDestroyed),
            _ => reason.ToString(),
        };
}