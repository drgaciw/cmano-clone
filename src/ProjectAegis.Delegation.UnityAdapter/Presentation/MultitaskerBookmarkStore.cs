namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// In-memory multitasker bookmark store (req 20 §Simulation Controls / Multitasker mode).
/// Save/restore of camera pose + selection set ids + optional agent-pause flag. While a capture or
/// restore is in flight the store pushes/pops <see cref="PauseReasonIds.MultitaskerBookmark"/> on the
/// shared <see cref="PauseReasonStack"/> so the sim does not advance mid-transition (T5 owns this
/// reason; T4 owns <see cref="PauseReasonIds.AgentGate"/>).
/// </summary>
public sealed class MultitaskerBookmarkStore
{
    private readonly Dictionary<string, MultitaskerBookmark> _slots =
        new(StringComparer.Ordinal);
    private readonly PauseReasonStack _pauseStack;

    /// <summary>Create a store bound to the session <paramref name="pauseStack"/>.</summary>
    public MultitaskerBookmarkStore(PauseReasonStack pauseStack)
    {
        _pauseStack = pauseStack ?? throw new ArgumentNullException(nameof(pauseStack));
    }

    /// <summary>Number of saved bookmarks.</summary>
    public int Count => _slots.Count;

    /// <summary>True if a bookmark with <paramref name="id"/> exists.</summary>
    public bool Contains(string? id) =>
        !string.IsNullOrEmpty(id) && _slots.ContainsKey(id!);

    /// <summary>All saved bookmarks in insertion order of first capture (dict enumeration order is undefined; use TryGet for lookup).</summary>
    public IReadOnlyCollection<MultitaskerBookmark> All => _slots.Values;

    /// <summary>
    /// Capture a bookmark under <paramref name="id"/>, holding the sim paused with
    /// <see cref="PauseReasonIds.MultitaskerBookmark"/> for the duration of the write.
    /// Overwrites an existing slot with the same id. Returns the stored bookmark.
    /// </summary>
    public MultitaskerBookmark Capture(
        string id,
        CameraPose camera,
        IReadOnlyList<string>? selectionUnitIds,
        bool? agentPaused = null)
    {
        if (string.IsNullOrEmpty(id))
        {
            throw new ArgumentException("Bookmark id is required.", nameof(id));
        }

        _pauseStack.Push(PauseReasonIds.MultitaskerBookmark);
        try
        {
            var selection = SnapshotSelection(selectionUnitIds);
            var bookmark = new MultitaskerBookmark(id, camera, selection, agentPaused);
            _slots[id] = bookmark;
            return bookmark;
        }
        finally
        {
            _pauseStack.Remove(PauseReasonIds.MultitaskerBookmark);
        }
    }

    /// <summary>
    /// Restore a previously captured bookmark. Pushes/pops
    /// <see cref="PauseReasonIds.MultitaskerBookmark"/> around the read so the host can safely apply
    /// camera + selection without the sim advancing. Returns false if the slot is missing.
    /// </summary>
    public bool TryRestore(string? id, out MultitaskerBookmark? bookmark)
    {
        bookmark = null;
        if (string.IsNullOrEmpty(id) || !_slots.TryGetValue(id!, out var stored))
        {
            return false;
        }

        _pauseStack.Push(PauseReasonIds.MultitaskerBookmark);
        try
        {
            // Defensive copy of selection so callers cannot mutate the stored list.
            bookmark = stored with
            {
                SelectionUnitIds = SnapshotSelection(stored.SelectionUnitIds),
            };
            return true;
        }
        finally
        {
            _pauseStack.Remove(PauseReasonIds.MultitaskerBookmark);
        }
    }

    /// <summary>Lookup without pause push/pop (read-only inspect).</summary>
    public bool TryGet(string? id, out MultitaskerBookmark? bookmark)
    {
        bookmark = null;
        if (string.IsNullOrEmpty(id) || !_slots.TryGetValue(id!, out var stored))
        {
            return false;
        }

        bookmark = stored;
        return true;
    }

    /// <summary>Remove a bookmark slot. Returns true if it existed.</summary>
    public bool Remove(string? id) =>
        !string.IsNullOrEmpty(id) && _slots.Remove(id!);

    /// <summary>Clear every bookmark.</summary>
    public void Clear() => _slots.Clear();

    private static IReadOnlyList<string> SnapshotSelection(IReadOnlyList<string>? selectionUnitIds)
    {
        if (selectionUnitIds == null || selectionUnitIds.Count == 0)
        {
            return Array.Empty<string>();
        }

        var copy = new List<string>(selectionUnitIds.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var unitId in selectionUnitIds)
        {
            if (string.IsNullOrEmpty(unitId) || !seen.Add(unitId))
            {
                continue;
            }

            copy.Add(unitId);
        }

        return copy;
    }
}
