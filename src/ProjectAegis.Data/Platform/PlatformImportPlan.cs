namespace ProjectAegis.Data.Platform;

using Validation;

/// <summary>
/// Req-21 / ADR-011: the pure, side-effect-free result of analysing an edited workbook against its
/// source snapshot — the diff, validation findings, and how changes split across write-gate-supported
/// (sensors, P0) vs not-yet-supported entities. <see cref="PlatformWorkbookImporter.Stage"/> turns an
/// unblocked plan into staged batches.
/// </summary>
public sealed record PlatformImportPlan(
    string SourceSnapshotId,
    bool SnapshotResolved,
    IReadOnlyList<PlatformWorkbookChange> Changes,
    IReadOnlyList<ValidationFinding> Findings,
    IReadOnlyList<PlatformWorkbookChange> SupportedChanges,
    IReadOnlyList<PlatformWorkbookChange> UnsupportedChanges,
    bool RequiresHumanApproval)
{
    /// <summary>True if any finding is an error — staging is refused until resolved (PLE-4.2).</summary>
    public bool Blocked => Findings.Any(f => f.Severity == ValidationSeverity.Error);

    public bool HasChanges => Changes.Count > 0;

    /// <summary>
    /// PLE-2.3: quarantine-style report entries for unresolved FK / orphan rows detected at plan time.
    /// Never committed — staging refuses blocked plans and omits quarantined rows.
    /// </summary>
    public IReadOnlyList<PlatformImportQuarantineEntry> QuarantineEntries { get; init; } =
        Array.Empty<PlatformImportQuarantineEntry>();

    /// <summary>S125-06 / AUTH-08: sheet/column deviations from <see cref="PlatformWorkbookContract"/>.</summary>
    public IReadOnlyList<PlatformWorkbookContractDrift> ContractDrift { get; init; } =
        Array.Empty<PlatformWorkbookContractDrift>();

    /// <summary>S125-06 / AUTH-08: rows/fields that will not be staged (plan-time quarantine only).</summary>
    public PlatformImportDropCounts DropCounts { get; init; } = PlatformImportDropCounts.Empty;

    /// <summary>S125-08 / AUTH-09: non-blocking, actionable warnings for invalid Platforms lat/lon cells.</summary>
    public IReadOnlyList<ValidationFinding> LatLonFindings { get; init; } = Array.Empty<ValidationFinding>();
}

/// <summary>Outcome of staging an unblocked plan through <see cref="ProjectAegis.Data.WriteGate.IWriteGate"/>.</summary>
public sealed record PlatformImportResult(
    PlatformImportPlan Plan,
    bool Staged,
    string? SensorBatchId,
    string? MountBatchId,
    string? LoadoutBatchId,
    string? MagazineBatchId,
    string? CommsBatchId,
    string? LinkBatchId,
    string? MobilityBatchId,
    string? SignatureBatchId,
    string? EmconBatchId,
    string? DamageBatchId,
    IReadOnlyList<string> Notes,
    string? SwarmBatchId = null)
{
    /// <summary>
    /// PLE-2.3 / PLE-4.4: plan-time FK quarantine plus stage-time TRL/orphan quarantine entries.
    /// </summary>
    public IReadOnlyList<PlatformImportQuarantineEntry> QuarantineEntries { get; init; } =
        Array.Empty<PlatformImportQuarantineEntry>();

    /// <summary>S125-06 / AUTH-08: plan drop counts with <c>QuarantinedRows</c> including stage-time quarantine.</summary>
    public PlatformImportDropCounts DropCounts { get; init; } = PlatformImportDropCounts.Empty;
}