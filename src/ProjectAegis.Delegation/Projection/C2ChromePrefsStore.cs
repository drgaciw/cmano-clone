namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// In-memory presentation bag for chrome collapse flags (CMD-23).
/// UI-local only — not DecisionLog, not file I/O.
/// Keys are stable preference names; values are collapse flags.
/// </summary>
public sealed class C2ChromePrefsStore
{
    public const string MessageLogKey = "MessageLogCollapsed";
    public const string LeftDrawerKey = "LeftDrawerCollapsed";

    private readonly Dictionary<string, bool> _snapshot =
        new(StringComparer.Ordinal);

    /// <summary>Current message-log collapse flag (false when never set).</summary>
    public bool MessageLogCollapsed
    {
        get => GetFlag(MessageLogKey);
        set => _snapshot[MessageLogKey] = value;
    }

    /// <summary>Current left-drawer collapse flag (false when never set).</summary>
    public bool LeftDrawerCollapsed
    {
        get => GetFlag(LeftDrawerKey);
        set => _snapshot[LeftDrawerKey] = value;
    }

    /// <summary>Copy of the current collapse bag (empty when never set).</summary>
    public IReadOnlyDictionary<string, bool> GetSnapshot()
    {
        return new Dictionary<string, bool>(_snapshot, StringComparer.Ordinal);
    }

    /// <summary>
    /// Replace the bag. Null or empty clears stored overrides (defaults apply on next restore).
    /// Unknown keys are ignored; only known chrome keys are retained.
    /// </summary>
    public void SetSnapshot(IReadOnlyDictionary<string, bool>? snapshot)
    {
        _snapshot.Clear();
        if (snapshot is null || snapshot.Count == 0)
        {
            return;
        }

        if (snapshot.TryGetValue(MessageLogKey, out var messageLog))
        {
            _snapshot[MessageLogKey] = messageLog;
        }

        if (snapshot.TryGetValue(LeftDrawerKey, out var leftDrawer))
        {
            _snapshot[LeftDrawerKey] = leftDrawer;
        }
    }

    /// <summary>Capture collapse flags from a chrome state into this store.</summary>
    public void Capture(C2ChromeCollapseState state)
    {
        if (state is null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        MessageLogCollapsed = state.MessageLogCollapsed;
        LeftDrawerCollapsed = state.LeftDrawerCollapsed;
    }

    /// <summary>
    /// Restore collapse flags onto a base state (typically <see cref="C2ChromeCollapseState.Expanded"/>).
    /// When the bag is empty, returns <paramref name="baseState"/> (or Expanded when null).
    /// </summary>
    public C2ChromeCollapseState Restore(C2ChromeCollapseState? baseState = null)
    {
        var seed = baseState ?? C2ChromeCollapseState.Expanded;
        if (_snapshot.Count == 0)
        {
            return seed;
        }

        return seed
            .SetMessageLogCollapsed(MessageLogCollapsed)
            .SetLeftDrawerCollapsed(LeftDrawerCollapsed);
    }

    private bool GetFlag(string key) =>
        _snapshot.TryGetValue(key, out var value) && value;
}
