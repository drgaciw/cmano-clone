namespace ProjectAegis.Data.Platform;

using ProjectAegis.Data.Validation;

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
    string? MobilityBatchId,
    string? SignatureBatchId,
    string? EmconBatchId,
    string? DamageBatchId,
    IReadOnlyList<string> Notes);