namespace ProjectAegis.Delegation.Comms;

using Decision;
using ProjectAegis.Sim.Comms;
using ProjectAegis.Sim.Sensors;

/// <summary>Outcome of reconciling a batch of <see cref="CommsGridChange"/> transitions (DRG-391 / C3-02).</summary>
public readonly record struct CommsGridRejoinSummary(
    int InformativeMergeCount,
    int SuppressedCount);

/// <summary>
/// Reconciles unit-local contact observations into the side picture upon <c>OffGrid -&gt; OnGrid</c>
/// comms-grid rejoin (DRG-391 / C3-02).
/// <para>
/// Rank rule: <c>Unknown(0) &lt; Detected(1) &lt; Classified(2) &lt; Identified(3)</c>;
/// <c>Lost(-1)</c> never overwrites an active side track. Redundant, same-rank, lower-rank,
/// or <c>Lost</c> observations are suppressed (zero-noise: emits nothing when
/// <c>InformativeMerges.Count == 0</c>).
/// </para>
/// </summary>
public sealed class CommsGridRejoinReconciler
{
    private readonly Dictionary<string, ContactLifecycleState> _sidePicture = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, ContactLifecycleState>> _unitPictures = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ulong> _offGridSinceByUnit = new(StringComparer.Ordinal);
    private int _syncedContactChangeCount;

    /// <summary>Returns the merge rank for <paramref name="state"/> (<c>Lost = -1</c>, <c>Unknown = 0..Identified = 3</c>).</summary>
    public static int Rank(ContactLifecycleState state) =>
        state switch
        {
            ContactLifecycleState.Unknown => 0,
            ContactLifecycleState.Detected => 1,
            ContactLifecycleState.Classified => 2,
            ContactLifecycleState.Identified => 3,
            ContactLifecycleState.Lost => -1,
            _ => 0,
        };

    /// <summary>True when <paramref name="state"/> represents an active track (<c>Detected</c>, <c>Classified</c>, or <c>Identified</c>).</summary>
    public static bool IsActiveState(ContactLifecycleState state) => Rank(state) > 0;

    /// <summary>Records when <paramref name="unitId"/> entered the off-grid state.</summary>
    public void NotifyOffGrid(string unitId, ulong sinceTick)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("UnitId cannot be null or whitespace.", nameof(unitId));
        }

        if (!_offGridSinceByUnit.ContainsKey(unitId))
        {
            _offGridSinceByUnit[unitId] = sinceTick;
        }
    }

    /// <summary>Updates the side-picture contact state for <paramref name="targetId"/> directly.</summary>
    public void RecordSideContact(string targetId, ContactLifecycleState state)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("TargetId cannot be null or whitespace.", nameof(targetId));
        }

        _sidePicture[targetId] = state;
    }

    /// <summary>Returns the currently known side-picture <see cref="ContactLifecycleState"/> for <paramref name="targetId"/>.</summary>
    public ContactLifecycleState GetSideContactState(string targetId) =>
        _sidePicture.TryGetValue(targetId, out var state) ? state : ContactLifecycleState.Unknown;

    /// <summary>Records an explicit unit-local contact observation for <paramref name="unitId"/>.</summary>
    public void RecordUnitObservation(string unitId, string targetId, ContactLifecycleState state)
    {
        if (string.IsNullOrWhiteSpace(unitId))
        {
            throw new ArgumentException("UnitId cannot be null or whitespace.", nameof(unitId));
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("TargetId cannot be null or whitespace.", nameof(targetId));
        }

        if (!_unitPictures.TryGetValue(unitId, out var map))
        {
            map = new Dictionary<string, ContactLifecycleState>(StringComparer.Ordinal);
            _unitPictures[unitId] = map;
        }

        map[targetId] = state;
    }

    /// <summary>Returns the pending unit-local <see cref="ContactLifecycleState"/> for <paramref name="unitId"/> and <paramref name="targetId"/>.</summary>
    public ContactLifecycleState GetUnitContactState(string unitId, string targetId) =>
        _unitPictures.TryGetValue(unitId, out var map) && map.TryGetValue(targetId, out var state)
            ? state
            : ContactLifecycleState.Unknown;

    /// <summary>Incrementally seeds/updates side-picture state from <see cref="DecisionLog.ContactChanges"/>.</summary>
    public void SyncSidePicture(DecisionLog log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var changes = log.ContactChanges;
        for (var i = _syncedContactChangeCount; i < changes.Count; i++)
        {
            var change = changes[i];
            if (!string.IsNullOrWhiteSpace(change.TargetId) &&
                Enum.TryParse<ContactLifecycleState>(change.NewState, ignoreCase: false, out var parsed))
            {
                _sidePicture[change.TargetId] = parsed;
            }
        }

        _syncedContactChangeCount = changes.Count;
    }

    /// <summary>
    /// Processes comms-grid transitions in deterministic order. On <c>OffGrid -&gt; OnGrid</c>, merges
    /// unit-local active tracks that introduce a new contact or strictly higher classification into
    /// <paramref name="log"/> (<c>dl-{targetId}</c>) and emits a single <see cref="CommsGridRejoinRecord"/>
    /// summary iff <c>InformativeMerges.Count &gt; 0</c>.
    /// </summary>
    public CommsGridRejoinSummary ReconcileChanges(
        IReadOnlyList<CommsGridChange> changes,
        DecisionLog log,
        double simTime)
    {
        if (changes is null)
        {
            throw new ArgumentNullException(nameof(changes));
        }

        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        SyncSidePicture(log);

        var totalInformative = 0;
        var totalSuppressed = 0;

        for (var i = 0; i < changes.Count; i++)
        {
            var change = changes[i];
            if (change.To == CommsGridMembership.OffGrid)
            {
                NotifyOffGrid(change.UnitId, change.SimTick);
                continue;
            }

            if (change.From == CommsGridMembership.OffGrid && change.To == CommsGridMembership.OnGrid)
            {
                var offGridSinceTick = _offGridSinceByUnit.TryGetValue(change.UnitId, out var since)
                    ? since
                    : change.SimTick;
                _offGridSinceByUnit.Remove(change.UnitId);

                var (informative, suppressed) = ReconcileUnitRejoin(change, offGridSinceTick, log, simTime);
                totalInformative += informative;
                totalSuppressed += suppressed;
            }
        }

        _syncedContactChangeCount = log.ContactChanges.Count;
        return new CommsGridRejoinSummary(totalInformative, totalSuppressed);
    }

    private (int InformativeCount, int SuppressedCount) ReconcileUnitRejoin(
        CommsGridChange change,
        ulong offGridSinceTick,
        DecisionLog log,
        double simTime)
    {
        if (!_unitPictures.TryGetValue(change.UnitId, out var unitMap) || unitMap.Count == 0)
        {
            return (0, 0);
        }

        var sortedTargets = new string[unitMap.Count];
        unitMap.Keys.CopyTo(sortedTargets, 0);
        Array.Sort(sortedTargets, StringComparer.Ordinal);

        var informative = new List<CommsGridRejoinMergeDelta>();
        var suppressedCount = 0;

        for (var i = 0; i < sortedTargets.Length; i++)
        {
            var targetId = sortedTargets[i];
            var unitState = unitMap[targetId];
            var sideState = GetSideContactState(targetId);

            if (IsInformativeMerge(sideState, unitState))
            {
                var contactId = $"dl-{targetId}";
                informative.Add(new CommsGridRejoinMergeDelta(
                    targetId,
                    contactId,
                    sideState.ToString(),
                    unitState.ToString()));
                _sidePicture[targetId] = unitState;
            }
            else
            {
                suppressedCount++;
            }
        }

        unitMap.Clear();

        if (informative.Count == 0)
        {
            return (0, suppressedCount);
        }

        for (var i = 0; i < informative.Count; i++)
        {
            var delta = informative[i];
            log.AppendContactChange(new ContactChangeRecord(
                0,
                simTime,
                change.SimTick,
                change.UnitId,
                delta.ContactId,
                delta.TargetId,
                delta.PreviousState,
                delta.NewState));
        }

        log.AppendCommsGridRejoin(new CommsGridRejoinRecord(
            0,
            simTime,
            change.SimTick,
            change.UnitId,
            offGridSinceTick,
            change.SimTick,
            informative.ToArray(),
            suppressedCount));

        return (informative.Count, suppressedCount);
    }

    private static bool IsInformativeMerge(ContactLifecycleState sideState, ContactLifecycleState unitState)
    {
        var unitRank = Rank(unitState);
        if (unitRank <= 0)
        {
            return false;
        }

        var sideRank = Rank(sideState);
        return sideRank <= 0 || unitRank > sideRank;
    }
}
