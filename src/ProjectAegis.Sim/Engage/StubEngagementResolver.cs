namespace ProjectAegis.Sim.Engage;

/// <summary>MVP stub — no launches until geometry/magazine systems exist.</summary>
public sealed class StubEngagementResolver : IEngagementResolver
{
    private ulong _nextEngagementId = 1;

    public EngageResult Resolve(in EngageRequest request)
    {
        _ = request;
        return EngageResult.Aborted(EngagementAbortReason.None);
    }

    public ulong AllocateEngagementId() => _nextEngagementId++;
}
