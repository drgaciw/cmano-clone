namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;

/// <summary>
/// Headless façade over <see cref="ScenarioEditCommandBus.RefreshFindings"/> for live
/// validation in the authoring host. Debounce interval is stored for a future
/// Unity host tick; P2.1 always refreshes immediately (including when <c>debounceMs &gt; 0</c>).
///
/// DRG-55: findings arrive from <see cref="ValidationReport.FromFindings"/> already sorted by the
/// ADR-008 tuple (Severity DESC, Code ASC, MissionId, UnitId, TargetId, Message). This presenter
/// preserves that order verbatim — it previously re-sorted codes by ordinal, which dropped the
/// severity term and buried blocking errors among non-blocking warnings.
///
/// DRG-56: the presenter also surfaces <see cref="ScenarioExportGateState"/> so a view can render
/// the export gate without re-deriving policy.
/// </summary>
public sealed class LiveFindingsPresenter
{
    private readonly ScenarioAuthoringSession _session;
    private readonly int _debounceMs;
    private readonly ValidationConfig _config;

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
    /// <param name="config">
    /// Validation knobs used for the export gate decision. Defaults to
    /// <see cref="ValidationConfig"/> defaults (export blocked at <see cref="ValidationSeverity.Error"/>).
    /// </param>
    public LiveFindingsPresenter(
        ScenarioAuthoringSession session,
        int debounceMs = 300,
        ValidationConfig? config = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _debounceMs = debounceMs;
        _config = config ?? new ValidationConfig();
    }

    /// <summary>Most recent live validation report, or <c>null</c> before the first refresh.</summary>
    public ValidationReport? LastReport { get; private set; }

    /// <summary>
    /// Export-gate state for the most recent refresh. <see cref="ScenarioExportGateState.Empty"/>
    /// before the first refresh.
    /// </summary>
    public ScenarioExportGateState Gate { get; private set; } = ScenarioExportGateState.Empty;

    /// <summary>Most recent findings, in ADR-008 order.</summary>
    public IReadOnlyList<ValidationFinding> LastFindings => Gate.Findings;

    /// <summary>Most recent finding codes, in the same order as <see cref="LastFindings"/>.</summary>
    public IReadOnlyList<string> LastCodes => Gate.Codes;

    /// <summary>Count of error-severity findings in the most recent report.</summary>
    public int ErrorCount => Gate.ErrorCount;

    /// <summary>Count of warning-severity findings in the most recent report.</summary>
    public int WarningCount => Gate.WarningCount;

    /// <summary>Count of info-severity findings in the most recent report.</summary>
    public int InfoCount => Gate.InfoCount;

    /// <summary>
    /// True when export / publish / brief / simulate-sample are permitted for the most recent
    /// report. Save is never gated (ADR-008 / AME-6.5).
    /// </summary>
    public bool CanExport => Gate.CanExport;

    /// <summary>Why the export gate is closed, or <c>null</c> when it is open.</summary>
    public string? BlockingReason => Gate.BlockingReason;

    /// <summary>
    /// True when <see cref="LastReport"/> contains at least one finding with
    /// <see cref="ValidationSeverity.Error"/>.
    /// </summary>
    public bool HasErrorSeverity => Gate.ErrorCount > 0;

    /// <summary>
    /// Derives export-gate state from a validation report. Pure: no session access, no I/O.
    /// The report's finding order is preserved (ADR-008); this method never re-sorts.
    /// </summary>
    /// <param name="report">Report to evaluate, or <c>null</c> when nothing has been validated yet.</param>
    /// <param name="config">Validation knobs; defaults to <see cref="ValidationConfig"/> defaults.</param>
    public static ScenarioExportGateState EvaluateGate(
        ValidationReport? report,
        ValidationConfig? config = null)
    {
        if (report is null)
        {
            return ScenarioExportGateState.Empty;
        }

        config ??= new ValidationConfig();
        var findings = report.Findings;

        var errorCount = 0;
        var warningCount = 0;
        var infoCount = 0;
        var codes = new string[findings.Count];

        for (var i = 0; i < findings.Count; i++)
        {
            var finding = findings[i];
            codes[i] = finding.Code;
            switch (finding.Severity)
            {
                case ValidationSeverity.Error:
                    errorCount++;
                    break;
                case ValidationSeverity.Warning:
                    warningCount++;
                    break;
                default:
                    infoCount++;
                    break;
            }
        }

        var canExport = report.CanExport(config);

        return new ScenarioExportGateState(
            findings,
            codes,
            errorCount,
            warningCount,
            infoCount,
            canExport,
            canExport ? null : DescribeBlock(errorCount, warningCount, config));
    }

    /// <summary>
    /// Re-runs live validation via the session bus and refreshes <see cref="LastReport"/> and
    /// <see cref="Gate"/>.
    /// </summary>
    public void RefreshImmediate()
    {
        LastReport = _session.Bus.RefreshFindings();
        Gate = EvaluateGate(LastReport, _config);
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

    private static string DescribeBlock(int errorCount, int warningCount, ValidationConfig config)
    {
        var parts = new List<string>(2);
        if (errorCount > 0)
        {
            parts.Add(Pluralize(errorCount, "error"));
        }

        if (warningCount > 0 && config.ExportBlockSeverityFloor <= ValidationSeverity.Warning)
        {
            parts.Add(Pluralize(warningCount, "warning"));
        }

        // CanExport was false, so at least one finding met the floor.
        var subject = parts.Count == 0 ? "a blocking finding" : string.Join(" and ", parts);
        return $"Export blocked — {subject} must be resolved. Save is still available.";
    }

    private static string Pluralize(int count, string noun) =>
        count == 1 ? $"{count} {noun}" : $"{count} {noun}s";
}
