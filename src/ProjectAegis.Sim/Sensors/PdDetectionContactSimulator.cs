namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Pd-driven contact appearances (replaces schedule seeds when detection trials present).</summary>
public sealed class PdDetectionContactSimulator
{
    private readonly SimSeed _seed;
    private readonly ScenarioDetectionTrial[] _trials;
    private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
    private readonly IReadOnlyList<ScenarioJammer> _jammers;
    private readonly HashSet<string> _detectedContacts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _destroyedTargets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContactTrack> _tracks = new(StringComparer.Ordinal);
    private readonly int _staleThresholdTicks;
    private readonly int _classifyAfterTicks;
    private readonly int _identifyAfterTicks;
    private string? _primaryTargetId;
    private bool _primaryHasTrack;

    public PdDetectionContactSimulator(
        SimSeed seed,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon = null,
        IReadOnlyList<ScenarioJammer>? jammers = null,
        ScenarioContactLifecycle? contactLifecycle = null)
    {
        _seed = seed;
        _unitRadarEmcon = unitRadarEmcon;
        _jammers = jammers ?? Array.Empty<ScenarioJammer>();
        _trials = trials.ToArray();
        var lifecycle = contactLifecycle ?? ScenarioContactLifecycle.Default;
        _staleThresholdTicks = Math.Max(1, lifecycle.StaleThresholdTicks);
        _classifyAfterTicks = Math.Max(0, lifecycle.ClassifyAfterTicks);
        _identifyAfterTicks = Math.Max(0, lifecycle.IdentifyAfterTicks);
    }

    public ulong LastDetectionHash { get; private set; }

    public int ActiveCount => _detectedContacts.Count;

    public string? PrimaryTargetId => _primaryTargetId;

    public bool PrimaryHasFireControlTrack => _primaryHasTrack;

    public IReadOnlyList<ContactTransition> Tick(ulong simTick, double simTime)
    {
        var rolls = DeterministicDetectionLoop.RollTick(
            _seed,
            simTick,
            _trials,
            _unitRadarEmcon,
            _detectedContacts,
            _jammers);
        LastDetectionHash = DetectionWorldHash.MixTick(LastDetectionHash, rolls);

        var transitions = new List<ContactTransition>();
        var seenThisTick = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roll in rolls)
        {
            if (_destroyedTargets.Contains(roll.Trial.TargetId))
            {
                continue;
            }

            if (!roll.Detected)
            {
                continue;
            }

            seenThisTick.Add(roll.Trial.ContactId);
            if (_detectedContacts.Contains(roll.Trial.ContactId))
            {
                if (_tracks.TryGetValue(roll.Trial.ContactId, out var existing))
                {
                    existing.MissedTicks = 0;
                    existing.LastSeenTick = simTick;
                }

                continue;
            }

            _detectedContacts.Add(roll.Trial.ContactId);
            _tracks[roll.Trial.ContactId] = new ContactTrack(simTick);
            UpdatePrimary(roll.Trial);
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                roll.Trial.ObserverId,
                roll.Trial.ContactId,
                roll.Trial.TargetId,
                ContactLifecycleState.Unknown,
                ContactLifecycleState.Detected));
        }

        foreach (var contactId in _detectedContacts.OrderBy(id => id, StringComparer.Ordinal))
        {
            if (!_tracks.TryGetValue(contactId, out var track) ||
                track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            var trial = _trials.First(t => t.ContactId == contactId);
            EmitLifecyclePromotions(simTick, simTime, trial, track, transitions);
        }

        EmitStaleLosses(simTick, simTime, seenThisTick, transitions);
        return transitions;
    }

    private void EmitLifecyclePromotions(
        ulong simTick,
        double simTime,
        ScenarioDetectionTrial trial,
        ContactTrack track,
        List<ContactTransition> transitions)
    {
        if (track.State == ContactLifecycleState.Lost)
        {
            return;
        }

        var age = simTick - track.FirstSeenTick;
        if (_classifyAfterTicks > 0 &&
            track.State == ContactLifecycleState.Detected &&
            age >= (ulong)_classifyAfterTicks)
        {
            var previous = track.State;
            track.State = ContactLifecycleState.Classified;
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                trial.ObserverId,
                trial.ContactId,
                trial.TargetId,
                previous,
                track.State));
        }

        if (_identifyAfterTicks > 0 &&
            track.State == ContactLifecycleState.Classified &&
            age >= (ulong)_identifyAfterTicks)
        {
            var previous = track.State;
            track.State = ContactLifecycleState.Identified;
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                trial.ObserverId,
                trial.ContactId,
                trial.TargetId,
                previous,
                track.State));
        }
    }

    private void EmitStaleLosses(
        ulong simTick,
        double simTime,
        HashSet<string> seenThisTick,
        List<ContactTransition> transitions)
    {
        var lost = new List<string>();
        foreach (var pair in _tracks)
        {
            if (pair.Value.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            if (seenThisTick.Contains(pair.Key))
            {
                continue;
            }

            pair.Value.MissedTicks++;
            if (pair.Value.MissedTicks < _staleThresholdTicks)
            {
                continue;
            }

            var trial = _trials.First(t => t.ContactId == pair.Key);
            var previous = pair.Value.State;
            pair.Value.State = ContactLifecycleState.Lost;
            lost.Add(pair.Key);
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                trial.ObserverId,
                trial.ContactId,
                trial.TargetId,
                previous,
                ContactLifecycleState.Lost));
        }

        foreach (var contactId in lost)
        {
            _detectedContacts.Remove(contactId);
            if (_primaryTargetId != null &&
                _tracks.TryGetValue(contactId, out var track) &&
                _trials.First(t => t.ContactId == contactId).TargetId == _primaryTargetId)
            {
                RecomputePrimary();
            }
        }
    }

    private void RecomputePrimary()
    {
        _primaryTargetId = null;
        _primaryHasTrack = false;
        foreach (var id in _detectedContacts)
        {
            var trial = _trials.First(t => t.ContactId == id);
            UpdatePrimary(trial);
        }
    }

    private sealed class ContactTrack(ulong firstSeenTick)
    {
        public ulong FirstSeenTick { get; } = firstSeenTick;

        public ulong LastSeenTick { get; set; } = firstSeenTick;

        public int MissedTicks { get; set; }

        public ContactLifecycleState State { get; set; } = ContactLifecycleState.Detected;
    }

    private void UpdatePrimary(ScenarioDetectionTrial trial)
    {
        if (_primaryTargetId == null ||
            string.Compare(trial.TargetId, _primaryTargetId, StringComparison.Ordinal) < 0)
        {
            _primaryTargetId = trial.TargetId;
            _primaryHasTrack = true;
        }
    }

    /// <summary>Force-remove a destroyed target from the contact picture (combat kill).</summary>
    public IReadOnlyList<ContactTransition> ApplyTargetKill(
        ulong simTick,
        double simTime,
        string targetId)
    {
        var transitions = new List<ContactTransition>();
        var lostContacts = _tracks.Keys
            .Where(contactId => _trials.First(t => t.ContactId == contactId).TargetId == targetId)
            .ToArray();

        foreach (var contactId in lostContacts)
        {
            if (!_tracks.TryGetValue(contactId, out var track) ||
                track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            var trial = _trials.First(t => t.ContactId == contactId);
            var previous = track.State;
            track.State = ContactLifecycleState.Lost;
            transitions.Add(new ContactTransition(
                simTick,
                simTime,
                trial.ObserverId,
                trial.ContactId,
                trial.TargetId,
                previous,
                ContactLifecycleState.Lost));
            _detectedContacts.Remove(contactId);
        }

        if (lostContacts.Length > 0)
        {
            _destroyedTargets.Add(targetId);
            RecomputePrimary();
        }

        return transitions;
    }

    public bool IsTargetDestroyed(string targetId) => _destroyedTargets.Contains(targetId);
}