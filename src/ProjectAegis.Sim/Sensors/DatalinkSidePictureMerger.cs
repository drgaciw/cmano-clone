namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim.Scenario;

/// <summary>
/// Bounded datalink side-picture merge (TR-sensor-004): peers on the same side receive
/// shared contact transitions with deterministic sort order and optional share lag.
/// </summary>
public sealed class DatalinkSidePictureMerger
{
    private readonly ScenarioDatalinkDoctrine _doctrine;
    private readonly IReadOnlyDictionary<string, string> _targetByContactId;
    private readonly IReadOnlyDictionary<(string ObserverId, string TargetId), string> _sensorByObserverTarget;
    private readonly SideObservers[] _sidesOrdered;
    private readonly Dictionary<string, ContactLifecycleState> _organicByObserverTarget = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ContactLifecycleState> _shareableOrganicByObserverTarget = new(StringComparer.Ordinal);
    private readonly List<PendingShareableOrganic> _pendingShareable = new();
    private readonly List<PendingShareableOrganic> _readyPending = new();
    private readonly Dictionary<string, ContactLifecycleState> _sharedByObserverTarget = new(StringComparer.Ordinal);
    private readonly HashSet<string> _targetDedup = new(StringComparer.Ordinal);
    private readonly List<string> _targetsScratch = new();

    public DatalinkSidePictureMerger(
        ScenarioDatalinkDoctrine doctrine,
        IReadOnlyList<ScenarioDetectionTrial> trials)
    {
        _doctrine = doctrine;
        _targetByContactId = trials
            .GroupBy(t => t.ContactId, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().TargetId, StringComparer.Ordinal);
        _sensorByObserverTarget = trials
            .GroupBy(t => (t.ObserverId, t.TargetId))
            .ToDictionary(g => g.Key, g => g.First().SensorId);
        _sidesOrdered = BuildObserversBySideSorted();
    }

    public IReadOnlyList<ContactTransition> Merge(
        IReadOnlyList<ContactTransition> organic,
        ulong simTick,
        double simTime,
        DatalinkCommsShareState commsState = DatalinkCommsShareState.Nominal)
    {
        if (!_doctrine.IsSharingEnabled)
        {
            return Array.Empty<ContactTransition>();
        }

        ApplyOrganicTransitions(organic, simTick);
        FlushShareableOrganic(simTick);

        if (commsState == DatalinkCommsShareState.Denied)
        {
            return Array.Empty<ContactTransition>();
        }

        return EmitSharedTransitions(simTick, simTime, commsState);
    }

    private void ApplyOrganicTransitions(IReadOnlyList<ContactTransition> organic, ulong simTick)
    {
        foreach (var transition in organic)
        {
            if (!_targetByContactId.TryGetValue(transition.ContactId, out var targetId))
            {
                targetId = transition.TargetId;
            }

            var key = ObserverTargetKey(transition.ObserverId, targetId);
            if (transition.NewState == ContactLifecycleState.Lost)
            {
                _organicByObserverTarget.Remove(key);
                QueueShareableOrganic(transition.ObserverId, targetId, ContactLifecycleState.Lost, simTick);
                continue;
            }

            _organicByObserverTarget[key] = transition.NewState;
            QueueShareableOrganic(transition.ObserverId, targetId, transition.NewState, simTick);
        }
    }

    private void QueueShareableOrganic(
        string observerId,
        string targetId,
        ContactLifecycleState newState,
        ulong simTick)
    {
        if (_doctrine.ShareLagTicks == 0)
        {
            var key = ObserverTargetKey(observerId, targetId);
            if (newState == ContactLifecycleState.Lost)
            {
                _shareableOrganicByObserverTarget.Remove(key);
            }
            else
            {
                _shareableOrganicByObserverTarget[key] = newState;
            }

            return;
        }

        if (newState == ContactLifecycleState.Lost)
        {
            CancelPendingShareable(observerId, targetId);
        }

        _pendingShareable.Add(new PendingShareableOrganic(
            observerId,
            targetId,
            newState,
            simTick + (ulong)_doctrine.ShareLagTicks));
    }

    private void CancelPendingShareable(string observerId, string targetId)
    {
        for (var i = _pendingShareable.Count - 1; i >= 0; i--)
        {
            var pending = _pendingShareable[i];
            if (string.Equals(pending.ObserverId, observerId, StringComparison.Ordinal) &&
                string.Equals(pending.TargetId, targetId, StringComparison.Ordinal))
            {
                _pendingShareable.RemoveAt(i);
            }
        }
    }

    private void FlushShareableOrganic(ulong simTick)
    {
        if (_doctrine.ShareLagTicks == 0 || _pendingShareable.Count == 0)
        {
            return;
        }

        _readyPending.Clear();
        foreach (var pending in _pendingShareable)
        {
            if (pending.ApplyTick <= simTick)
            {
                _readyPending.Add(pending);
            }
        }

        _readyPending.Sort(ComparePendingShareable);

        foreach (var pending in _readyPending)
        {
            _pendingShareable.Remove(pending);
            var key = ObserverTargetKey(pending.ObserverId, pending.TargetId);
            if (pending.State == ContactLifecycleState.Lost)
            {
                _shareableOrganicByObserverTarget.Remove(key);
                continue;
            }

            _shareableOrganicByObserverTarget[key] = pending.State;
        }
    }

    private List<ContactTransition> EmitSharedTransitions(
        ulong simTick,
        double simTime,
        DatalinkCommsShareState commsState)
    {
        var shared = new List<ContactTransition>();

        foreach (var side in _sidesOrdered)
        {
            CollectTargetsForSide(side.Observers, _targetsScratch);
            foreach (var targetId in _targetsScratch)
            {
                var bestOrganic = ResolveBestOrganicState(side.Observers, targetId);
                foreach (var observerId in side.Observers)
                {
                    var observerTargetKey = ObserverTargetKey(observerId, targetId);
                    if (HasActiveOrganicAtOrAbove(observerTargetKey, bestOrganic.State))
                    {
                        _sharedByObserverTarget.Remove(observerTargetKey);
                        continue;
                    }

                    if (bestOrganic.State == ContactLifecycleState.Lost)
                    {
                        EmitSharedTransition(
                            shared,
                            observerId,
                            targetId,
                            observerTargetKey,
                            ContactLifecycleState.Lost,
                            simTick,
                            simTime,
                            commsState);
                        continue;
                    }

                    if (bestOrganic.State == ContactLifecycleState.Unknown)
                    {
                        continue;
                    }

                    EmitSharedTransition(
                        shared,
                        observerId,
                        targetId,
                        observerTargetKey,
                        bestOrganic.State,
                        simTick,
                        simTime,
                        commsState);
                }
            }
        }

        shared.Sort(CompareSharedTransitions);
        return shared;
    }

    private void EmitSharedTransition(
        List<ContactTransition> shared,
        string observerId,
        string targetId,
        string observerTargetKey,
        ContactLifecycleState newState,
        ulong simTick,
        double simTime,
        DatalinkCommsShareState commsState)
    {
        var previous = _sharedByObserverTarget.TryGetValue(observerTargetKey, out var existing)
            ? existing
            : ContactLifecycleState.Unknown;

        if (previous == newState)
        {
            return;
        }

        if (commsState == DatalinkCommsShareState.Degraded &&
            previous == ContactLifecycleState.Unknown &&
            newState != ContactLifecycleState.Lost)
        {
            return;
        }

        if (newState == ContactLifecycleState.Lost)
        {
            _sharedByObserverTarget.Remove(observerTargetKey);
        }
        else
        {
            _sharedByObserverTarget[observerTargetKey] = newState;
        }

        shared.Add(new ContactTransition(
            simTick,
            simTime,
            observerId,
            SharedContactId(targetId),
            targetId,
            previous,
            newState));
    }

    private (ContactLifecycleState State, string SourceObserverId) ResolveBestOrganicState(
        IReadOnlyList<string> observers,
        string targetId)
    {
        ContactLifecycleState best = ContactLifecycleState.Unknown;
        string? bestObserver = null;

        foreach (var observerId in observers)
        {
            var key = ObserverTargetKey(observerId, targetId);
            if (!TryGetShareableOrganic(key, out var state))
            {
                continue;
            }

            if (bestObserver == null || LifecycleRank(state) > LifecycleRank(best))
            {
                best = state;
                bestObserver = observerId;
            }
            else if (LifecycleRank(state) == LifecycleRank(best) &&
                     string.Compare(observerId, bestObserver, StringComparison.Ordinal) < 0)
            {
                best = state;
                bestObserver = observerId;
            }
        }

        if (bestObserver == null)
        {
            foreach (var observerId in observers)
            {
                var key = ObserverTargetKey(observerId, targetId);
                if (_sharedByObserverTarget.ContainsKey(key))
                {
                    return (ContactLifecycleState.Lost, observerId);
                }
            }

            return (ContactLifecycleState.Unknown, string.Empty);
        }

        return (best, bestObserver);
    }

    private bool TryGetShareableOrganic(string observerTargetKey, out ContactLifecycleState state)
    {
        if (_doctrine.ShareLagTicks == 0)
        {
            return _organicByObserverTarget.TryGetValue(observerTargetKey, out state);
        }

        return _shareableOrganicByObserverTarget.TryGetValue(observerTargetKey, out state);
    }

    private bool HasActiveOrganicAtOrAbove(string observerTargetKey, ContactLifecycleState sharedState)
    {
        if (!_organicByObserverTarget.TryGetValue(observerTargetKey, out var organic))
        {
            return false;
        }

        return LifecycleRank(organic) >= LifecycleRank(sharedState);
    }

    private void CollectTargetsForSide(IReadOnlyList<string> observers, List<string> targets)
    {
        targets.Clear();
        _targetDedup.Clear();

        foreach (var observerId in observers)
        {
            foreach (var pair in _organicByObserverTarget)
            {
                if (!TryParseObserverTargetKey(pair.Key, out var keyObserverId, out var targetId) ||
                    !string.Equals(keyObserverId, observerId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_targetDedup.Add(targetId))
                {
                    targets.Add(targetId);
                }
            }

            foreach (var pair in _shareableOrganicByObserverTarget)
            {
                if (!TryParseObserverTargetKey(pair.Key, out var keyObserverId, out var targetId) ||
                    !string.Equals(keyObserverId, observerId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_targetDedup.Add(targetId))
                {
                    targets.Add(targetId);
                }
            }

            foreach (var pair in _sharedByObserverTarget)
            {
                if (!TryParseObserverTargetKey(pair.Key, out var keyObserverId, out var targetId) ||
                    !string.Equals(keyObserverId, observerId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (_targetDedup.Add(targetId))
                {
                    targets.Add(targetId);
                }
            }
        }

        targets.Sort(StringComparer.Ordinal);
    }

    private SideObservers[] BuildObserversBySideSorted()
    {
        if (_doctrine.UnitSides == null)
        {
            return Array.Empty<SideObservers>();
        }

        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unitIds = _doctrine.UnitSides.Keys.ToList();
        unitIds.Sort(StringComparer.Ordinal);

        foreach (var unitId in unitIds)
        {
            var side = _doctrine.UnitSides[unitId];
            if (string.IsNullOrEmpty(side))
            {
                continue;
            }

            if (!map.TryGetValue(side, out var observers))
            {
                observers = new List<string>();
                map[side] = observers;
            }

            observers.Add(unitId);
        }

        var sides = map.Keys.ToList();
        sides.Sort(StringComparer.Ordinal);
        var result = new SideObservers[sides.Count];
        for (var i = 0; i < sides.Count; i++)
        {
            var observers = map[sides[i]];
            observers.Sort(StringComparer.Ordinal);
            result[i] = new SideObservers(sides[i], observers.ToArray());
        }

        return result;
    }

    private int ComparePendingShareable(PendingShareableOrganic a, PendingShareableOrganic b)
    {
        var cmp = a.ApplyTick.CompareTo(b.ApplyTick);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.ObserverId, b.ObserverId, StringComparison.Ordinal);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(a.TargetId, b.TargetId, StringComparison.Ordinal);
        if (cmp != 0)
        {
            return cmp;
        }

        return LifecycleRank(a.State).CompareTo(LifecycleRank(b.State));
    }

    private int CompareSharedTransitions(ContactTransition a, ContactTransition b)
    {
        var cmp = string.Compare(a.ObserverId, b.ObserverId, StringComparison.Ordinal);
        if (cmp != 0)
        {
            return cmp;
        }

        cmp = string.Compare(
            ResolveSensorId(a.ObserverId, a.TargetId),
            ResolveSensorId(b.ObserverId, b.TargetId),
            StringComparison.Ordinal);
        if (cmp != 0)
        {
            return cmp;
        }

        return string.Compare(a.TargetId, b.TargetId, StringComparison.Ordinal);
    }

    private string ResolveSensorId(string observerId, string targetId) =>
        _sensorByObserverTarget.TryGetValue((observerId, targetId), out var sensorId)
            ? sensorId
            : "datalink";

    private static string SharedContactId(string targetId) => $"dl-{targetId}";

    private static string ObserverTargetKey(string observerId, string targetId) => $"{observerId}|{targetId}";

    private static bool TryParseObserverTargetKey(string key, out string observerId, out string targetId)
    {
        var separator = key.IndexOf('|', StringComparison.Ordinal);
        if (separator <= 0 || separator >= key.Length - 1)
        {
            observerId = string.Empty;
            targetId = string.Empty;
            return false;
        }

        observerId = key[..separator];
        targetId = key[(separator + 1)..];
        return true;
    }

    private static int LifecycleRank(ContactLifecycleState state) =>
        state switch
        {
            ContactLifecycleState.Unknown => 0,
            ContactLifecycleState.Detected => 1,
            ContactLifecycleState.Classified => 2,
            ContactLifecycleState.Identified => 3,
            ContactLifecycleState.Lost => -1,
            _ => 0,
        };

    private readonly record struct PendingShareableOrganic(
        string ObserverId,
        string TargetId,
        ContactLifecycleState State,
        ulong ApplyTick);

    private sealed class SideObservers
    {
        public SideObservers(string side, string[] observers)
        {
            Side = side;
            Observers = observers;
        }

        public string Side { get; }

        public string[] Observers { get; }
    }
}