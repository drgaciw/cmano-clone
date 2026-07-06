namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Sim.Core;
using ProjectAegis.Sim.Policy;
using ProjectAegis.Sim.Scenario;

/// <summary>Pd-driven contact appearances (replaces schedule seeds when detection trials present).</summary>
public sealed class PdDetectionContactSimulator
{
    private readonly SimSeed _seed;
    private readonly ScenarioDetectionTrial[] _trials;
    private readonly Dictionary<string, ScenarioDetectionTrial> _trialsByContactId;
    private readonly IReadOnlyDictionary<string, EmconState>? _unitRadarEmcon;
    private readonly ICatalogReader? _catalog;
    private readonly IReadOnlyList<ScenarioJammer> _jammers;
    // P2 allocation follow-up (S37-09): SortedSet for deterministic ordinal iteration without per-tick OrderBy/alloc.
    // Iteration order identical to previous explicit OrderBy; HashSet elsewhere unchanged.
    private readonly SortedSet<string> _detectedContacts = new(StringComparer.Ordinal);
    private readonly HashSet<string> _destroyedTargets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _bdaLostTargets = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContactTrack> _tracks = new(StringComparer.Ordinal);
    private readonly int _staleThresholdTicks;
    private int _commsStaleThresholdDivisor = 1;
    private readonly int _classifyAfterTicks;
    private readonly int _identifyAfterTicks;
    private string? _primaryTargetId;
    private string? _primaryBlueForceTargetId;
    private bool _primaryHasTrack;

    public PdDetectionContactSimulator(
        SimSeed seed,
        IReadOnlyList<ScenarioDetectionTrial> trials,
        IReadOnlyDictionary<string, EmconState>? unitRadarEmcon = null,
        IReadOnlyList<ScenarioJammer>? jammers = null,
        ScenarioContactLifecycle? contactLifecycle = null,
        ICatalogReader? catalog = null)
    {
        _seed = seed;
        _unitRadarEmcon = unitRadarEmcon;
        _catalog = catalog;
        _jammers = jammers ?? Array.Empty<ScenarioJammer>();
        _trials = trials.ToArray();
        DeterministicDetectionLoop.SortTrials(_trials);
        _trialsByContactId = new Dictionary<string, ScenarioDetectionTrial>(_trials.Length, StringComparer.Ordinal);
        foreach (var trial in _trials)
        {
            _trialsByContactId[trial.ContactId] = trial;
        }

        var lifecycle = contactLifecycle ?? ScenarioContactLifecycle.Default;
        _staleThresholdTicks = Math.Max(1, lifecycle.StaleThresholdTicks);
        _classifyAfterTicks = Math.Max(0, lifecycle.ClassifyAfterTicks);
        _identifyAfterTicks = Math.Max(0, lifecycle.IdentifyAfterTicks);
    }

    public ulong LastDetectionHash { get; private set; }

    public void SetCommsStaleThresholdDivisor(int divisor) =>
        _commsStaleThresholdDivisor = Math.Max(1, divisor);

    private int EffectiveStaleThresholdTicks =>
        Math.Max(1, _staleThresholdTicks / _commsStaleThresholdDivisor);

    public int ActiveCount => _detectedContacts.Count;

    public string? PrimaryTargetId => _primaryTargetId;

    public string? PrimaryBlueForceTargetId => _primaryBlueForceTargetId;

    public bool PrimaryHasFireControlTrack => _primaryHasTrack;

    public IReadOnlyList<ContactTransition> Tick(ulong simTick, double simTime)
    {
        var rolls = DeterministicDetectionLoop.RollTick(
            _seed,
            simTick,
            _trials,
            _unitRadarEmcon,
            _detectedContacts,
            _jammers,
            catalog: _catalog,
            trialsPreSorted: true);
        LastDetectionHash = DetectionWorldHash.MixTick(LastDetectionHash, rolls);

        var transitions = new List<ContactTransition>();
        var seenThisTick = new HashSet<string>(StringComparer.Ordinal);
        foreach (var roll in rolls)
        {
            if (_destroyedTargets.Contains(roll.Trial.TargetId) ||
                _bdaLostTargets.Contains(roll.Trial.TargetId))
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

        foreach (var contactId in _detectedContacts)
        {
            if (!_tracks.TryGetValue(contactId, out var track) ||
                track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            var trial = _trialsByContactId[contactId];
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
        var candidateContactIds = _tracks.Keys
            .OrderBy(contactId => contactId, StringComparer.Ordinal)
            .ToArray();
        foreach (var contactId in candidateContactIds)
        {
            var track = _tracks[contactId];
            if (track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            if (seenThisTick.Contains(contactId))
            {
                continue;
            }

            track.MissedTicks++;
            if (track.MissedTicks < EffectiveStaleThresholdTicks)
            {
                continue;
            }

            var trial = _trialsByContactId[contactId];
            var previous = track.State;
            track.State = ContactLifecycleState.Lost;
            lost.Add(contactId);
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
                _trialsByContactId[contactId].TargetId == _primaryTargetId)
            {
                RecomputePrimary();
            }
        }
    }

    private void RecomputePrimary()
    {
        _primaryTargetId = null;
        _primaryBlueForceTargetId = null;
        _primaryHasTrack = false;
        foreach (var id in _detectedContacts)
        {
            var trial = _trialsByContactId[id];
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
        if (HostileContactFilter.IsEngageableHostileTarget(trial.TargetId))
        {
            if (_primaryTargetId == null ||
                string.Compare(trial.TargetId, _primaryTargetId, StringComparison.Ordinal) < 0)
            {
                _primaryTargetId = trial.TargetId;
                _primaryHasTrack = true;
            }
        }

        if (BalticV3SideRegistry.IsBlueForceUnit(trial.TargetId))
        {
            if (_primaryBlueForceTargetId == null ||
                string.Compare(trial.TargetId, _primaryBlueForceTargetId, StringComparison.Ordinal) < 0)
            {
                _primaryBlueForceTargetId = trial.TargetId;
            }
        }
    }

    /// <summary>Promote contacts for a damaged target to Lost without marking the target destroyed (BDA hook).</summary>
    public IReadOnlyList<ContactTransition> ApplyTargetBdaLost(
        ulong simTick,
        double simTime,
        string targetId)
    {
        var transitions = new List<ContactTransition>();
        var lostContacts = _tracks.Keys
            .Where(contactId => _trialsByContactId[contactId].TargetId == targetId)
            .OrderBy(contactId => contactId, StringComparer.Ordinal)
            .ToArray();

        foreach (var contactId in lostContacts)
        {
            if (!_tracks.TryGetValue(contactId, out var track) ||
                track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            var trial = _trialsByContactId[contactId];
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
            _bdaLostTargets.Add(targetId);
            RecomputePrimary();
        }

        return transitions;
    }

    /// <summary>Force-remove a destroyed target from the contact picture (combat kill).</summary>
    public IReadOnlyList<ContactTransition> ApplyTargetKill(
        ulong simTick,
        double simTime,
        string targetId)
    {
        var transitions = new List<ContactTransition>();
        var lostContacts = _tracks.Keys
            .Where(contactId => _trialsByContactId[contactId].TargetId == targetId)
            .OrderBy(contactId => contactId, StringComparer.Ordinal)
            .ToArray();

        foreach (var contactId in lostContacts)
        {
            if (!_tracks.TryGetValue(contactId, out var track) ||
                track.State == ContactLifecycleState.Lost)
            {
                continue;
            }

            var trial = _trialsByContactId[contactId];
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