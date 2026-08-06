namespace ProjectAegis.Delegation.UnityAdapter.Authoring;

using ProjectAegis.Data.Validation;

/// <summary>
/// Export-gate state derived from a live validation report, shaped for a UI to render without
/// re-deriving policy (DRG-56).
///
/// The gate itself is owned by the Validation Engine — <see cref="ValidationReport.CanExport"/>
/// against <see cref="ValidationConfig.ExportBlockSeverityFloor"/> (ADR-008). This type carries the
/// decision and the counts a view needs to explain it; a view must never recompute either.
///
/// Ordering of <see cref="Findings"/> and <see cref="Codes"/> is the ADR-008 tuple
/// (Severity DESC, Code ASC, MissionId, UnitId, TargetId, Message) as produced by
/// <see cref="ValidationReport.FromFindings"/>. It is preserved here verbatim.
///
/// Plain class rather than a record: this assembly also targets netstandard2.1 for the Unity
/// plugin and declares no <c>IsExternalInit</c> polyfill.
/// </summary>
public sealed class ScenarioExportGateState
{
    /// <summary>Creates a gate state. Prefer <see cref="LiveFindingsPresenter.EvaluateGate"/>.</summary>
    public ScenarioExportGateState(
        IReadOnlyList<ValidationFinding> findings,
        IReadOnlyList<string> codes,
        int errorCount,
        int warningCount,
        int infoCount,
        bool canExport,
        string? blockingReason)
    {
        Findings = findings ?? throw new ArgumentNullException(nameof(findings));
        Codes = codes ?? throw new ArgumentNullException(nameof(codes));
        ErrorCount = errorCount;
        WarningCount = warningCount;
        InfoCount = infoCount;
        CanExport = canExport;
        BlockingReason = blockingReason;
    }

    /// <summary>Findings in ADR-008 order.</summary>
    public IReadOnlyList<ValidationFinding> Findings { get; }

    /// <summary>Finding codes, in the same order as <see cref="Findings"/>.</summary>
    public IReadOnlyList<string> Codes { get; }

    /// <summary>Count of <see cref="ValidationSeverity.Error"/> findings.</summary>
    public int ErrorCount { get; }

    /// <summary>Count of <see cref="ValidationSeverity.Warning"/> findings.</summary>
    public int WarningCount { get; }

    /// <summary>Count of <see cref="ValidationSeverity.Info"/> findings.</summary>
    public int InfoCount { get; }

    /// <summary>
    /// True when export / publish / brief / simulate-sample are permitted. Save is never gated
    /// (ADR-008 / AME-6.5) — this flag must not be used to disable saving.
    /// </summary>
    public bool CanExport { get; }

    /// <summary>
    /// Human-readable reason the gate is closed, or <c>null</c> when <see cref="CanExport"/> is true.
    /// Intended for a disabled control's tooltip and accessible name.
    /// </summary>
    public string? BlockingReason { get; }

    /// <summary>Gate state before any validation has run: nothing known, nothing blocked.</summary>
    public static ScenarioExportGateState Empty { get; } = new(
        Array.Empty<ValidationFinding>(),
        Array.Empty<string>(),
        errorCount: 0,
        warningCount: 0,
        infoCount: 0,
        canExport: true,
        blockingReason: null);
}
