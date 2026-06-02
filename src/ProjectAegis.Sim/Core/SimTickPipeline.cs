namespace ProjectAegis.Sim.Core;

using ProjectAegis.Sim.Engage;
using ProjectAegis.Sim.Sensors;
using ProjectAegis.Sim.Time;

/// <summary>ADR-004 tick runner with engagement phase (step 8) wired.</summary>
public sealed class SimTickPipeline : ISimTickRunner
{
    private readonly SimTickRunner _core;
    private readonly IEngagementResolver _engagement;
    private readonly List<EngageRequest> _pending = new();
    private readonly List<EngageRequest> _lastProcessed = new();
    private readonly List<EngageResult> _lastResults = new();

    public SimTickPipeline(SimSeed seed, IEngagementResolver engagement, double fixedDeltaSeconds = 1.0 / 60.0)
    {
        _core = new SimTickRunner(seed, fixedDeltaSeconds);
        _engagement = engagement;
    }

    public SimClock Clock => _core.Clock;

    public SimSeed Seed => _core.Seed;

    public ulong LastWorldHash { get; private set; }

    public IReadOnlyList<EngageRequest> PendingEngagements => _pending;

    public IReadOnlyList<EngageRequest> LastProcessedEngagements => _lastProcessed;

    public IReadOnlyList<EngageResult> LastEngagementResults => _lastResults;

    public void EnqueueEngagement(in EngageRequest request) => _pending.Add(request);

    public void TickOnce(TimeCompressionMode mode)
    {
        _core.TickOnce(mode);
        _lastResults.Clear();
        _lastProcessed.Clear();
        _lastProcessed.AddRange(_pending);

        foreach (var request in _pending)
        {
            _lastResults.Add(_engagement.Resolve(request));
        }

        _pending.Clear();

        var engageMix = MixEngagements(_lastResults);
        LastWorldHash = SimWorldHash.Combine(_core.LastWorldHash, DetectionSubhash, engageMix);
    }

    /// <summary>Detection phase sub-hash (tick step 4); call before engagement resolves.</summary>
    public ulong DetectionSubhash { get; private set; }

    public void MixDetectionTick(IReadOnlyList<DetectionRollResult> rolls)
    {
        DetectionSubhash = DetectionWorldHash.MixTick(DetectionSubhash, rolls);
        var engageMix = MixEngagements(_lastResults);
        LastWorldHash = SimWorldHash.Combine(_core.LastWorldHash, DetectionSubhash, engageMix);
    }

    private static ulong MixEngagements(IReadOnlyList<EngageResult> results)
    {
        ulong x = 0;
        foreach (var r in results)
        {
            x ^= r.Launched ? r.EngagementId : 0UL;
        }

        return x;
    }

}
