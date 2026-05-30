namespace ProjectAegis.Sim.Engage;

/// <summary>Test/dev resolver that records requests and optionally simulates launch.</summary>
public sealed class RecordingEngagementResolver : IEngagementResolver
{
    private ulong _nextId = 1;

    public RecordingEngagementResolver(bool simulateLaunch = true) =>
        SimulateLaunch = simulateLaunch;

    public bool SimulateLaunch { get; set; }

    public List<EngageRequest> Requests { get; } = new();

    public EngageResult Resolve(in EngageRequest request)
    {
        Requests.Add(request);
        return SimulateLaunch
            ? EngageResult.Launch(_nextId++)
            : EngageResult.Aborted(EngagementAbortReason.None);
    }
}
