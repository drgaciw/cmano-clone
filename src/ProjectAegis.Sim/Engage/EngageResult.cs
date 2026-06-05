namespace ProjectAegis.Sim.Engage;

public readonly record struct EngageResult(
    bool Launched,
    ulong EngagementId = 0,
    EngagementAbortReason AbortReason = EngagementAbortReason.None,
    string? OutcomeCode = null,
    double PkDraw = 0)
{
    public static EngageResult Launch(ulong engagementId) => new(true, engagementId);

    public static EngageResult Aborted(EngagementAbortReason reason) => new(false, 0, reason);
}
