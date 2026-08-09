namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// In-memory presentation bag for basemap layer visibility (CMD-28.2).
/// UI-local only — not replay, not DecisionLog, not file I/O.
/// Keys are <see cref="MapLayerId"/> names; values are visibility flags.
/// </summary>
public sealed class MapLayerStackStore
{
    private readonly Dictionary<string, bool> _snapshot =
        new(StringComparer.Ordinal);

    /// <summary>Copy of the current visibility bag (empty when never set).</summary>
    public IReadOnlyDictionary<string, bool> GetSnapshot()
    {
        return new Dictionary<string, bool>(_snapshot, StringComparer.Ordinal);
    }

    /// <summary>
    /// Replace the bag. Null or empty clears stored overrides (defaults apply on next restore).
    /// </summary>
    public void SetSnapshot(IReadOnlyDictionary<string, bool>? snapshot)
    {
        _snapshot.Clear();
        if (snapshot is null || snapshot.Count == 0)
        {
            return;
        }

        foreach (var pair in snapshot)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                continue;
            }

            _snapshot[pair.Key] = pair.Value;
        }
    }

    /// <summary>Capture visibility from a stack into this store.</summary>
    public void Capture(MapLayerStackState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        SetSnapshot(state.ToVisibilitySnapshot());
    }

    /// <summary>
    /// Restore visibility onto a base stack (typically defaults). Unknown keys ignored.
    /// When the bag is empty, returns <paramref name="baseState"/> unchanged.
    /// </summary>
    public MapLayerStackState Restore(MapLayerStackState baseState)
    {
        if (baseState is null)
        {
            throw new ArgumentNullException(nameof(baseState));
        }

        if (_snapshot.Count == 0)
        {
            return baseState;
        }

        return baseState.ApplyVisibilitySnapshot(_snapshot);
    }
}
