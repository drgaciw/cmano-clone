namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Headless façade over <see cref="ScenarioEditCommandBus.RefreshFindings"/> for live
/// validation codes in the authoring host. Debounce interval is stored for a future
/// Unity host tick; P2.1 always refreshes immediately (including when <c>debounceMs &gt; 0</c>).
/// </summary>
public sealed class LiveFindingsPresenter
{
    private readonly ScenarioAuthoringSession _session;
    private readonly int _debounceMs;

    /// <summary>
    /// Creates a findings presenter bound to the given authoring session.
    /// </summary>
    /// <param name="session">Open scenario authoring session.</param>
    /// <param name="debounceMs">
    /// Intended host debounce in milliseconds (default 300).
    /// When <c>&lt;= 0</c>, <see cref="ScheduleRefresh"/> is documented as immediate (tests).
    /// When <c>&gt; 0</c>, P2.1 still calls <see cref="RefreshImmediate"/> immediately;
    /// a Unity host may later honor this interval on an EditorWindow tick.
    /// </param>
    public LiveFindingsPresenter(ScenarioAuthoringSession session, int debounceMs = 300)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _debounceMs = debounceMs;
    }

    /// <summary>Most recent finding codes, ordered by ordinal string comparison.</summary>
    public IReadOnlyList<string> LastCodes { get; private set; } = Array.Empty<string>();

    /// <summary>Most recent live validation report, or <c>null</c> before the first refresh.</summary>
    public ValidationReport? LastReport { get; private set; }

    /// <summary>
    /// True when <see cref="LastReport"/> contains at least one finding with
    /// <see cref="ValidationSeverity.Error"/>.
    /// </summary>
    public bool HasErrorSeverity =>
        LastReport?.Findings.Any(f => f.Severity == ValidationSeverity.Error) == true;

    /// <summary>
    /// Re-runs live validation via the session bus and updates <see cref="LastReport"/> /
    /// <see cref="LastCodes"/> (codes sorted ordinal ascending).
    /// </summary>
    public void RefreshImmediate()
    {
        LastReport = _session.Bus.RefreshFindings();
        LastCodes = LastReport.Findings
            .Select(f => f.Code)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Schedules a findings refresh. When <c>debounceMs &lt;= 0</c>, refreshes immediately
    /// (test-friendly). When <c>debounceMs &gt; 0</c>, P2.1 minimum still refreshes immediately;
    /// the stored interval is reserved for a future host-driven debounce.
    /// </summary>
    public void ScheduleRefresh()
    {
        // P2.1: always-immediate. debounceMs is retained for a future Unity host that may
        // coalesce ScheduleRefresh calls over EditorWindow Update (200–400 ms design range).
        // When debounceMs <= 0, immediate is the contract for headless tests.
        _ = _debounceMs;
        RefreshImmediate();
    }
}
