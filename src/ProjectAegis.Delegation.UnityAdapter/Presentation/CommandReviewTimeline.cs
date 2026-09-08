using System.Globalization;
using ProjectAegis.Delegation.AfterAction;
using ProjectAegis.Delegation.BdaAssess;
using ProjectAegis.Delegation.CombatEvents;
using ProjectAegis.Delegation.Projection;
using ProjectAegis.Delegation.UnityAdapter.Bridge;

namespace ProjectAegis.Delegation.UnityAdapter.Presentation;

/// <summary>Bounded map evidence archive and deterministic event navigation; never advances or rewinds the simulation.</summary>
public sealed class CommandReviewTimeline
{
    private sealed record Evidence(double Time, SliceAContactFrame Contacts, BdaAssessSnapshot Assessments,
        IReadOnlyList<MapSymbolEntry> Symbols);
    private readonly int _capacity;
    private readonly List<Evidence> _history = new();
    private CombatPresentationFrame _live = CombatPresentationFrame.Empty;
    private IReadOnlyList<MapSymbolEntry> _liveSymbols = Array.Empty<MapSymbolEntry>();
    private AfterActionLedgerSnapshot _ledger = AfterActionLedgerSnapshot.Empty;

    /// <summary>Bounds retained tick evidence; full event history remains in the authoritative event contract.</summary>
    public CommandReviewTimeline(int retainedFrames = 256)
    {
        if (retainedFrames < 1) throw new ArgumentOutOfRangeException(nameof(retainedFrames));
        _capacity = retainedFrames;
    }

    /// <summary>Whether a historical event is being inspected.</summary>
    public bool IsInspecting { get; private set; }
    /// <summary>Engagement identity shared with the map and explanation.</summary>
    public string? SelectedKey { get; private set; }
    /// <summary>As-of display data, distinct from live simulation state.</summary>
    public CombatPresentationFrame DisplayFrame { get; private set; } = CombatPresentationFrame.Empty;
    /// <summary>Captured poses only; never substitutes current positions for an evicted frame.</summary>
    public IReadOnlyList<MapSymbolEntry> DisplaySymbols { get; private set; } = Array.Empty<MapSymbolEntry>();
    /// <summary>Accessible indication of live/review state and missing historical geometry.</summary>
    public string StatusLine { get; private set; } = "LIVE";

    /// <summary>Captures immutable map/contact/assessment evidence at the simulation tick boundary.</summary>
    public void Capture(CombatPresentationFrame frame, IReadOnlyList<MapSymbolEntry> symbols)
    {
        if (frame == null) throw new ArgumentNullException(nameof(frame));
        if (symbols == null) throw new ArgumentNullException(nameof(symbols));
        if (double.IsNaN(frame.SimTime) || double.IsInfinity(frame.SimTime) || frame.SimTime < 0)
            throw new ArgumentOutOfRangeException(nameof(frame));
        if (frame.SimTime < _live.SimTime)
        {
            _history.Clear();
            IsInspecting = false;
            SelectedKey = null;
        }
        _live = frame;
        _liveSymbols = Array.AsReadOnly(symbols.ToArray());
        if (_history.Count > 0 && _history[_history.Count - 1].Time == frame.SimTime) _history.RemoveAt(_history.Count - 1);
        _history.Add(new Evidence(frame.SimTime, frame.Contacts, frame.Assessments, _liveSymbols));
        if (_history.Count > _capacity) _history.RemoveAt(0);
        _ledger = AfterActionLedgerProjection.Project(frame.Events.Events.Where(e => e.SimTime <= frame.SimTime)
            .Select(e => new CombatEventRowConsume((CombatEventPhaseConsume)e.Phase, e.ShooterId, e.TargetId,
                e.WeaponFamilyId, e.Outcome, e.CorrelationId, e.SimTime, e.SimTick, e.ExplanationRef)).ToArray());
        if (!IsInspecting) ReturnToLive();
    }

    /// <summary>Combines exact optional platform, target, family and outcome filters without reconstructing facts.</summary>
    public IReadOnlyList<AfterActionLedgerEntry> Filter(AfterActionLedgerFilter filter) =>
        Array.AsReadOnly(AfterActionLedgerProjection.Filter(_ledger, filter).Entries.ToArray());

    /// <summary>Inspects a real ledger row and its captured as-of map; returns false for invented rows.</summary>
    public bool Inspect(AfterActionLedgerEntry row)
    {
        if (!_ledger.Entries.Contains(row)) return false;
        // The contract defines a global chronological sequence. Include through the chosen row,
        // including only earlier lifecycle facts when several facts share a sim timestamp.
        var end = _ledger.Entries.ToList().IndexOf(row);
        // Tick snapshots cannot establish ordering within that tick. Before its last event,
        // use only the preceding tick's map/BDA evidence and disclose that timestamp.
        var laterInTick = _ledger.Entries.Skip(end + 1).Any(e => e.SimTime == row.SimTime);
        var evidence = _history.LastOrDefault(e => laterInTick ? e.Time < row.SimTime : e.Time <= row.SimTime);
        var events = _live.Events.Events.Where(e => e.SimTime <= _live.SimTime).Take(end + 1).ToArray();
        var keys = new HashSet<string>(events.Where(e => e.Phase == CombatEventPhase.Firing
            || e.Phase == CombatEventPhase.AuthorizationRefused).Select(CombatMapPresenter.KeyFor), StringComparer.Ordinal);
        DisplayFrame = new CombatPresentationFrame(new CombatEventSnapshot(events),
            evidence?.Contacts ?? SliceAContactFrame.Empty, evidence?.Assessments ?? BdaAssessSnapshot.Empty, row.SimTime)
        {
            Explanations = Array.AsReadOnly(_live.Explanations.Where(e => keys.Contains(
                CombatMapPresenter.KeyFor(e.ShooterId, e.TargetId, e.CorrelationId))).ToArray()),
        };
        DisplaySymbols = evidence?.Symbols ?? Array.Empty<MapSymbolEntry>();
        SelectedKey = CombatMapPresenter.KeyFor(row.ShooterId, row.TargetId, row.CorrelationId);
        IsInspecting = true;
        StatusLine = "REVIEW t=" + row.SimTime.ToString("0.###", CultureInfo.InvariantCulture)
            + (evidence == null ? " | historical map evidence unavailable" : " | captured map t=" + evidence.Time.ToString("0.###", CultureInfo.InvariantCulture));
        return true;
    }

    /// <summary>Returns the presentation to the latest captured frame without changing command selection.</summary>
    public void ReturnToLive()
    {
        IsInspecting = false;
        SelectedKey = null;
        DisplayFrame = _live;
        DisplaySymbols = _liveSymbols;
        StatusLine = "LIVE";
    }
}
