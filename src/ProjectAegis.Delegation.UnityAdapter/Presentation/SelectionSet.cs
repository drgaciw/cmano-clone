namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Ordered, de-duplicated set of friendly unit ids held as presentation state (req 20 §Selection;
/// GDD TR-c2-005). This is never sim state (ADR-010). Single-select is a set of one via
/// <see cref="ReplaceWith"/>; multi-select adds/removes/toggles.
/// </summary>
/// <remarks>
/// Ordering is insertion order. <see cref="PrimaryUnitId"/> is the anchor (first) unit and preserves
/// the pre-rev-2 single-select contract for existing consumers of
/// <see cref="C2PresentationController.SelectedUnitId"/>. Drag-box marquee, shift/ctrl handling, and
/// group-order fan-out that build on this set are implemented in Track T1.
/// </remarks>
public sealed class SelectionSet
{
    private readonly List<string> _ordered = new List<string>();
    private readonly HashSet<string> _lookup = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Selected unit ids in insertion order.</summary>
    public IReadOnlyList<string> OrderedTargetIds => _ordered;

    /// <summary>Number of units in the set.</summary>
    public int Count => _ordered.Count;

    /// <summary>True when no unit is selected.</summary>
    public bool IsEmpty => _ordered.Count == 0;

    /// <summary>Anchor unit (first selected), or null when empty. Mirrors the legacy single-select id.</summary>
    public string? PrimaryUnitId => _ordered.Count > 0 ? _ordered[0] : null;

    /// <summary>True if <paramref name="unitId"/> is currently selected.</summary>
    public bool Contains(string? unitId) => !string.IsNullOrEmpty(unitId) && _lookup.Contains(unitId!);

    /// <summary>Add a unit to the set. No-op if already present or null/empty. Returns true if added.</summary>
    public bool Add(string? unitId)
    {
        if (string.IsNullOrEmpty(unitId) || !_lookup.Add(unitId!))
        {
            return false;
        }

        _ordered.Add(unitId!);
        return true;
    }

    /// <summary>Remove a unit from the set. Returns true if it was present.</summary>
    public bool Remove(string? unitId)
    {
        if (string.IsNullOrEmpty(unitId) || !_lookup.Remove(unitId!))
        {
            return false;
        }

        _ordered.Remove(unitId!);
        return true;
    }

    /// <summary>
    /// Add if absent, remove if present (shift/ctrl-click). Returns true if the unit is in the set
    /// afterwards.
    /// </summary>
    public bool Toggle(string? unitId)
    {
        if (Contains(unitId))
        {
            Remove(unitId);
            return false;
        }

        return Add(unitId);
    }

    /// <summary>Replace the entire set with a single unit (single-select). Null/empty clears the set.</summary>
    public void ReplaceWith(string? unitId)
    {
        Clear();
        Add(unitId);
    }

    /// <summary>Clear all selection.</summary>
    public void Clear()
    {
        _ordered.Clear();
        _lookup.Clear();
    }
}
