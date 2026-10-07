namespace ProjectAegis.Sim.Comms;

using Policy;
using Scenario;

/// <summary>Per-unit comms-grid membership (C3-01 / DRG-390).</summary>
public enum CommsGridMembership
{
    OnGrid = 0,
    OffGrid = 1,
}

/// <summary>Last position a unit reported while on grid (what the side picture may show once it is off grid).</summary>
public readonly record struct UnitLastReport(
    string UnitId,
    ulong ReportedAtTick,
    double LatitudeDeg,
    double LongitudeDeg);

/// <summary>Emitted when a unit leaves or rejoins its side's comms grid.</summary>
public readonly record struct CommsGridChange(
    ulong SimTick,
    string UnitId,
    CommsGridMembership From,
    CommsGridMembership To,
    string Reason);

/// <summary>
/// Sim-authoritative comms-grid membership (C3-01 / DRG-390). Off-grid units keep executing
/// their last orders / doctrine but accept no new direct orders, and their position is frozen at
/// the last on-grid report. Deterministic: transitions apply in (AtTick, UnitId ordinal) order and
/// state iterates in ordinal unit order for hashing.
/// </summary>
public sealed class CommsGridRegistry
{
    private readonly ScenarioCommsGridTransition[] _transitions;
    private readonly SortedDictionary<string, CommsGridMembership> _membership = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, ulong> _offGridSince = new(StringComparer.Ordinal);
    private readonly Dictionary<string, UnitLastReport> _lastReports = new(StringComparer.Ordinal);
    private int _nextIndex;
    private ulong _lastAdvancedTick;
    private bool _hasAdvanced;

    public CommsGridRegistry(IReadOnlyList<ScenarioCommsGridTransition> transitions)
    {
        _transitions = transitions
            .Select((t, i) => (t, i))
            .OrderBy(x => x.t.AtTick)
            .ThenBy(x => x.t.UnitId, StringComparer.Ordinal)
            .ThenBy(x => x.i)
            .Select(x => x.t)
            .ToArray();
    }

    public static CommsGridRegistry? TryCreate(ScenarioPolicyProfile? profile) =>
        profile is { CommsGridTransitions.Count: > 0 }
            ? new CommsGridRegistry(profile.CommsGridTransitions)
            : null;

    public static CommsGridMembership ParseMembership(string? value) =>
        Enum.TryParse<CommsGridMembership>(value, ignoreCase: true, out var parsed)
            ? parsed
            : throw new InvalidDataException($"Unknown comms-grid membership '{value}' (expected OnGrid|OffGrid).");

    /// <summary>Apply all transitions with AtTick ≤ <paramref name="simTick"/>. Idempotent for a repeated tick.</summary>
    public IReadOnlyList<CommsGridChange> Advance(ulong simTick)
    {
        if (_hasAdvanced && simTick < _lastAdvancedTick)
        {
            // Never rewind: state is a pure function of the highest tick reached.
            return Array.Empty<CommsGridChange>();
        }

        _hasAdvanced = true;
        _lastAdvancedTick = simTick;
        List<CommsGridChange>? changes = null;
        while (_nextIndex < _transitions.Length && _transitions[_nextIndex].AtTick <= simTick)
        {
            var t = _transitions[_nextIndex++];
            var next = ParseMembership(t.Membership);
            var current = GetMembership(t.UnitId);
            if (next == current)
            {
                continue;
            }

            _membership[t.UnitId] = next;
            if (next == CommsGridMembership.OffGrid)
            {
                _offGridSince[t.UnitId] = t.AtTick;
            }
            else
            {
                _offGridSince.Remove(t.UnitId);
            }

            (changes ??= new List<CommsGridChange>()).Add(
                new CommsGridChange(t.AtTick, t.UnitId, current, next, t.Reason));
        }

        return changes ?? (IReadOnlyList<CommsGridChange>)Array.Empty<CommsGridChange>();
    }

    public CommsGridMembership GetMembership(string unitId) =>
        _membership.TryGetValue(unitId, out var m) ? m : CommsGridMembership.OnGrid;

    public bool IsOffGrid(string unitId) => GetMembership(unitId) == CommsGridMembership.OffGrid;

    public bool TryGetOffGridSince(string unitId, out ulong sinceTick) =>
        _offGridSince.TryGetValue(unitId, out sinceTick);

    /// <summary>Ordinal-sorted ids of units currently off grid.</summary>
    public IReadOnlyList<string> OffGridUnitIds => _offGridSince.Keys.ToArray();

    /// <summary>
    /// Record a position report. Ignored while the unit is off grid so the side picture keeps the
    /// last on-grid position. Returns true when the report was accepted.
    /// </summary>
    public bool ReportPosition(string unitId, ulong simTick, double latitudeDeg, double longitudeDeg)
    {
        if (IsOffGrid(unitId))
        {
            return false;
        }

        _lastReports[unitId] = new UnitLastReport(unitId, simTick, latitudeDeg, longitudeDeg);
        return true;
    }

    public bool TryGetLastReport(string unitId, out UnitLastReport report) =>
        _lastReports.TryGetValue(unitId, out report);

    /// <summary>Order-independent FNV-1a fold of off-grid state for world hashing.</summary>
    public ulong ComputeHash()
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var h = offset;
        foreach (var pair in _offGridSince)
        {
            foreach (var c in pair.Key)
            {
                h ^= c;
                h *= prime;
            }

            h ^= pair.Value;
            h *= prime;
        }

        return h;
    }
}

/// <summary>Command-façade gate: off-grid units reject new direct orders (C3-01 / DRG-390).</summary>
public static class OffGridOrderGate
{
    public static FireAbortReason? Evaluate(CommsGridRegistry? registry, string unitId) =>
        registry != null && registry.IsOffGrid(unitId) ? FireAbortReason.OffGrid : null;
}
