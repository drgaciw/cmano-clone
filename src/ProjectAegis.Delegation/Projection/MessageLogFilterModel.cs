namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Pure per-category message-log filter state (Track T3, req 20 §Message log filters, TR-c2-008).
/// Category set → predicate over <see cref="MessageLogLine"/>; additive to
/// <see cref="MessageLogPanelBinder"/> — callers apply this before binding, so
/// <see cref="MessageLogProjection"/>/<see cref="MessageLogPanelBinder"/> are untouched. State lives for
/// the session lifetime (owned by the presentation host, e.g. <c>MessageLogPanelHost</c> in
/// <c>unity/ProjectAegis</c>); this model does not persist to disk itself.
/// </summary>
public sealed class MessageLogFilterModel
{
    private readonly HashSet<string> _disabledCategories = new(StringComparer.Ordinal);

    /// <summary>Categories currently toggled off (hidden from the log).</summary>
    public IReadOnlyCollection<string> DisabledCategories => _disabledCategories;

    /// <summary>True unless the category has been explicitly disabled.</summary>
    public bool IsEnabled(string category) => !_disabledCategories.Contains(category);

    /// <summary>Enables or disables a single category.</summary>
    public void SetEnabled(string category, bool enabled)
    {
        if (enabled)
        {
            _disabledCategories.Remove(category);
        }
        else
        {
            _disabledCategories.Add(category);
        }
    }

    /// <summary>Flips a category's enabled state and returns the new state.</summary>
    public bool Toggle(string category)
    {
        var newState = !IsEnabled(category);
        SetEnabled(category, newState);
        return newState;
    }

    /// <summary>Re-enables every category (clears all filters).</summary>
    public void Reset() => _disabledCategories.Clear();

    /// <summary>Predicate form — true when <paramref name="line"/>'s category is enabled.</summary>
    public bool Matches(MessageLogLine line) => IsEnabled(line.Category);

    /// <summary>Applies the filter to a line list, preserving order. Returns the input unchanged
    /// (no copy) when no categories are disabled.</summary>
    public IReadOnlyList<MessageLogLine> Apply(IReadOnlyList<MessageLogLine> lines)
    {
        if (_disabledCategories.Count == 0)
        {
            return lines;
        }

        var result = new List<MessageLogLine>(lines.Count);
        foreach (var line in lines)
        {
            if (Matches(line))
            {
                result.Add(line);
            }
        }

        return result;
    }
}
