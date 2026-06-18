namespace ProjectAegis.Data.Platform;

using ProjectAegis.Data.Telemetry;

/// <summary>ADR-011 Phase D: outcome of staging an edited workbook through <see cref="CatalogWriteGate"/>.</summary>
public sealed record PlatformWorkbookWriteResult(
    PlatformImportResult Import,
    bool Proposed,
    BalanceDriftReport BalanceDriftAdvisory)
{
    public IReadOnlyList<string> BatchIds => CollectBatchIds(Import);

    public static PlatformWorkbookWriteResult FromImport(
        PlatformImportResult import,
        BalanceDriftReport? balanceDriftAdvisory = null) =>
        new(import, import.Staged, balanceDriftAdvisory ?? BalanceDriftReport.EmptyDisabled);

    public static IReadOnlyList<string> CollectBatchIds(PlatformImportResult import)
    {
        var ids = new List<string>(9);
        AddIfPresent(ids, import.SensorBatchId);
        AddIfPresent(ids, import.MountBatchId);
        AddIfPresent(ids, import.LoadoutBatchId);
        AddIfPresent(ids, import.MagazineBatchId);
        AddIfPresent(ids, import.CommsBatchId);
        AddIfPresent(ids, import.MobilityBatchId);
        AddIfPresent(ids, import.SignatureBatchId);
        AddIfPresent(ids, import.EmconBatchId);
        AddIfPresent(ids, import.DamageBatchId);
        return ids;
    }

    private static void AddIfPresent(ICollection<string> ids, string? batchId)
    {
        if (!string.IsNullOrWhiteSpace(batchId))
        {
            ids.Add(batchId);
        }
    }
}

/// <summary>Outcome of approving or rejecting staged platform workbook batches.</summary>
public sealed record PlatformWorkbookWriteDecisionResult(
    IReadOnlyList<string> ProcessedBatchIds,
    IReadOnlyList<string> CommittedBatchIds,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors,
    BalanceDriftReport BalanceDriftAdvisory)
{
    public bool AllCommitted =>
        ProcessedBatchIds.Count > 0
        && CommittedBatchIds.Count == ProcessedBatchIds.Count
        && Errors.Count == 0;
}