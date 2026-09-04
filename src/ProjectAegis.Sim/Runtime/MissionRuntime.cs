namespace ProjectAegis.Sim.Runtime;

using ProjectAegis.Sim.Scenario;
using ProjectAegis.Sim.Sensors;

/// <summary>
/// Simulation mission runtime managing execution ticks for Play Mode interactive simulation and headless runs.
/// Wires <see cref="DatalinkSidePictureMerger"/> into the simulation tick loop so datalink picture updates follow
/// comms status and share lag rather than sharing an instantaneous picture (AEGIS-306 / DRG-236).
/// </summary>
public sealed class MissionRuntime
{
    private readonly ScenarioMissionEvent[] _timelineEvents;
    private int _nextEventIndex;

    /// <summary>
    /// Creates a new <see cref="MissionRuntime"/> with an optional datalink merger, timeline, and tick delta.
    /// </summary>
    public MissionRuntime(
        DatalinkSidePictureMerger? datalinkMerger = null,
        ScenarioMissionTimeline? timeline = null,
        double fixedDeltaSeconds = 1.0 / 60.0)
    {
        DatalinkMerger = datalinkMerger;
        Timeline = timeline;
        FixedDeltaSeconds = fixedDeltaSeconds > 0 ? fixedDeltaSeconds : 1.0 / 60.0;
        _timelineEvents = InitializeTimelineEvents(timeline);
    }

    /// <summary>
    /// Creates a new <see cref="MissionRuntime"/>, constructing a <see cref="DatalinkSidePictureMerger"/> from
    /// doctrine and detection trials.
    /// </summary>
    public MissionRuntime(
        ScenarioDatalinkDoctrine doctrine,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        ScenarioMissionTimeline? timeline = null,
        double fixedDeltaSeconds = 1.0 / 60.0)
        : this(new DatalinkSidePictureMerger(doctrine, trials), timeline, fixedDeltaSeconds)
    {
    }

    /// <summary>
    /// The active datalink side-picture merger, or null if datalink sharing is disabled.
    /// </summary>
    public DatalinkSidePictureMerger? DatalinkMerger { get; set; }

    /// <summary>
    /// Optional scenario mission timeline for scheduled event milestones.
    /// </summary>
    public ScenarioMissionTimeline? Timeline { get; }

    /// <summary>
    /// Current contested C2 comms quality governing peer datalink sharing.
    /// </summary>
    public DatalinkCommsShareState CommsState { get; set; } = DatalinkCommsShareState.Nominal;

    /// <summary>
    /// Current simulation tick count.
    /// </summary>
    public ulong CurrentTick { get; private set; }

    /// <summary>
    /// Current simulated time in seconds.
    /// </summary>
    public double CurrentSimTime { get; private set; }

    /// <summary>
    /// Fixed timestep delta in seconds per tick (default 1/60 s).
    /// </summary>
    public double FixedDeltaSeconds { get; set; }

    /// <summary>
    /// Organic contact transitions provided on the most recent tick.
    /// </summary>
    public IReadOnlyList<ContactTransition> LastOrganicTransitions { get; private set; } = Array.Empty<ContactTransition>();

    /// <summary>
    /// Shared datalink contact transitions emitted on the most recent tick.
    /// </summary>
    public IReadOnlyList<ContactTransition> LastSharedTransitions { get; private set; } = Array.Empty<ContactTransition>();

    /// <summary>
    /// Combined contact transitions (organic followed by shared) on the most recent tick.
    /// </summary>
    public IReadOnlyList<ContactTransition> LastMergedTransitions { get; private set; } = Array.Empty<ContactTransition>();

    /// <summary>
    /// Timeline events fired on the most recent tick.
    /// </summary>
    public IReadOnlyList<ScenarioMissionEvent> LastFiredEvents { get; private set; } = Array.Empty<ScenarioMissionEvent>();

    /// <summary>
    /// True when a datalink merger is configured.
    /// </summary>
    public bool IsSharingEnabled => DatalinkMerger != null;

    /// <summary>
    /// Advances the mission runtime by one tick with optional organic contact transitions and optional comms state override.
    /// Internal clock advances by <see cref="FixedDeltaSeconds"/>.
    /// </summary>
    public IReadOnlyList<ContactTransition> Tick(
        IReadOnlyList<ContactTransition>? organicTransitions = null,
        DatalinkCommsShareState? commsState = null)
    {
        CurrentTick++;
        CurrentSimTime += FixedDeltaSeconds;
        return Tick(CurrentTick, CurrentSimTime, organicTransitions, commsState);
    }

    /// <summary>
    /// Executes a tick at the specified tick index and sim time.
    /// Evaluates mission timeline events and runs <see cref="DatalinkSidePictureMerger.Merge"/> according
    /// to current comms status and share lag.
    /// </summary>
    public IReadOnlyList<ContactTransition> Tick(
        ulong simTick,
        double simTime,
        IReadOnlyList<ContactTransition>? organicTransitions = null,
        DatalinkCommsShareState? commsState = null)
    {
        CurrentTick = simTick;
        CurrentSimTime = simTime;

        if (commsState.HasValue)
        {
            CommsState = commsState.Value;
        }

        // 1. Process timeline events if configured.
        ProcessTimelineEvents(simTick);

        // 2. Process organic and shared datalink transitions.
        var organic = organicTransitions ?? Array.Empty<ContactTransition>();
        LastOrganicTransitions = organic;

        var shared = DatalinkMerger != null
            ? DatalinkMerger.Merge(organic, simTick, simTime, CommsState)
            : Array.Empty<ContactTransition>();
        LastSharedTransitions = shared;

        if (organic.Count == 0)
        {
            LastMergedTransitions = shared;
        }
        else if (shared.Count == 0)
        {
            LastMergedTransitions = organic;
        }
        else
        {
            var merged = new List<ContactTransition>(organic.Count + shared.Count);
            merged.AddRange(organic);
            merged.AddRange(shared);
            LastMergedTransitions = merged;
        }

        return LastMergedTransitions;
    }

    /// <summary>
    /// Convenience overload for ticking datalink updates with a specific comms state.
    /// </summary>
    public IReadOnlyList<ContactTransition> Tick(
        ulong simTick,
        double simTime,
        DatalinkCommsShareState commsState) =>
        Tick(simTick, simTime, null, commsState);

    /// <summary>
    /// Resets the runtime cursor, clock, and cached transition lists.
    /// </summary>
    public void Reset(ulong startTick = 0, double startTime = 0.0)
    {
        CurrentTick = startTick;
        CurrentSimTime = startTime;
        _nextEventIndex = 0;
        LastOrganicTransitions = Array.Empty<ContactTransition>();
        LastSharedTransitions = Array.Empty<ContactTransition>();
        LastMergedTransitions = Array.Empty<ContactTransition>();
        LastFiredEvents = Array.Empty<ScenarioMissionEvent>();
    }

    private void ProcessTimelineEvents(ulong simTick)
    {
        if (_timelineEvents.Length == 0 || _nextEventIndex >= _timelineEvents.Length)
        {
            LastFiredEvents = Array.Empty<ScenarioMissionEvent>();
            return;
        }

        var fired = new List<ScenarioMissionEvent>();
        while (_nextEventIndex < _timelineEvents.Length && _timelineEvents[_nextEventIndex].FireAtTick <= simTick)
        {
            fired.Add(_timelineEvents[_nextEventIndex++]);
        }

        LastFiredEvents = fired;
    }

    private static ScenarioMissionEvent[] InitializeTimelineEvents(ScenarioMissionTimeline? timeline)
    {
        if (timeline == null || timeline.Events.Count == 0)
        {
            return Array.Empty<ScenarioMissionEvent>();
        }

        var orderIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < timeline.FireOrder.Count; i++)
        {
            orderIndex[timeline.FireOrder[i]] = i;
        }

        return timeline.Events
            .OrderBy(e => e.FireAtTick)
            .ThenBy(e => orderIndex.TryGetValue(e.EventId, out var idx) ? idx : int.MaxValue)
            .ThenBy(e => e.EventId, StringComparer.Ordinal)
            .ToArray();
    }
}
