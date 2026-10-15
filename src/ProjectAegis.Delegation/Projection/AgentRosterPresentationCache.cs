namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Reuses roster rows and controller badges until projection inputs change.
/// Each host owns its cache; frame reads of unchanged entries allocate nothing.
/// </summary>
public sealed class AgentRosterPresentationCache
{
    private AgentRosterEntry[] _entries = Array.Empty<AgentRosterEntry>();
    private AgentRosterPresentation _presentation = AgentRosterPresentation.Empty;

    /// <summary>
    /// Applies changed roster values, retaining an entry snapshot so replacements
    /// inside a reused input collection also invalidate the presentation.
    /// </summary>
    public AgentRosterPresentation Apply(IReadOnlyList<AgentRosterEntry>? entries)
    {
        var count = entries?.Count ?? 0;
        var unchanged = count == _entries.Length;
        for (var i = 0; unchanged && i < count; i++)
        {
            unchanged = _entries[i] == entries![i];
        }

        if (unchanged)
        {
            return _presentation;
        }

        if (count == 0)
        {
            _entries = Array.Empty<AgentRosterEntry>();
            _presentation = AgentRosterPresentation.Empty;
            return _presentation;
        }

        _entries = new AgentRosterEntry[count];
        for (var i = 0; i < count; i++)
        {
            _entries[i] = entries![i];
        }

        _presentation = AgentRosterApplyState.Apply(_entries);
        return _presentation;
    }
}
