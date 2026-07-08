using System;
using System.Collections.Generic;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>
/// Dirty-detector for <c>ListView</c>-backed panel rows (req 20 AC-10, NFR virtualization —
/// see design/gdd/command-and-control-ui.md, Game-Requirements/requirements/20-Command-And-Control-UI.md).
/// <para>
/// Unity-side panel hosts (message log / OOB tree / C2 drawer) previously called
/// <c>itemsSource = rows.ToList()</c> followed by <c>ListView.Rebuild()</c> unconditionally on
/// every <c>LateUpdate</c>, even when the bound row data had not changed since the previous
/// frame. <c>Rebuild()</c> tears down and re-creates every pooled/visible element (defeating
/// Unity's ListView element-recycling virtualization) — at 5k+ rows this reliably drops frames.
/// </para>
/// <para>
/// This gate compares a newly bound row snapshot against the last snapshot that was actually
/// applied to the ListView, using structural (record) equality per row. Callers only reassign
/// <c>itemsSource</c> and call <c>Rebuild()</c> when <see cref="IsDirty"/> reports a real change,
/// then record the new snapshot via <see cref="MarkApplied"/>. Pure — no UnityEngine dependency,
/// headlessly testable.
/// </para>
/// </summary>
public sealed class PanelRefreshGate<TRow>
{
    private IReadOnlyList<TRow> _lastApplied = Array.Empty<TRow>();
    private bool _hasApplied;

    /// <summary>Number of times <see cref="MarkApplied"/> has been called — exposed for tests
    /// that need to assert "no work when data is unchanged" across repeated frames.</summary>
    public int AppliedCount { get; private set; }

    /// <summary>
    /// True when <paramref name="candidate"/> differs from the last snapshot recorded via
    /// <see cref="MarkApplied"/> (or when nothing has been applied yet) — i.e. a real ListView
    /// refresh (itemsSource assignment + Rebuild) is required. False means the caller should skip
    /// all render work this frame.
    /// </summary>
    public bool IsDirty(IReadOnlyList<TRow> candidate)
    {
        if (candidate is null)
        {
            throw new ArgumentNullException(nameof(candidate));
        }

        if (!_hasApplied)
        {
            return true;
        }

        if (ReferenceEquals(candidate, _lastApplied))
        {
            return false;
        }

        if (candidate.Count != _lastApplied.Count)
        {
            return true;
        }

        var comparer = EqualityComparer<TRow>.Default;
        for (var i = 0; i < candidate.Count; i++)
        {
            if (!comparer.Equals(candidate[i], _lastApplied[i]))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Records <paramref name="applied"/> as the last-applied snapshot. Call only after
    /// actually pushing a refresh (i.e. only when <see cref="IsDirty"/> returned true) so
    /// <see cref="AppliedCount"/> reflects real render work.</summary>
    public void MarkApplied(IReadOnlyList<TRow> applied)
    {
        _lastApplied = applied ?? throw new ArgumentNullException(nameof(applied));
        _hasApplied = true;
        AppliedCount++;
    }
}
