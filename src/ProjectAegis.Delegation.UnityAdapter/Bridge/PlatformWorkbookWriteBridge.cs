namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// ADR-011 Phase D: headless Unity/CLI bridge for platform workbook propose→approve.
/// Delegates to <see cref="PlatformWorkbookWriteService"/> only — no direct SQLite, no gate bypass.
/// </summary>
public static class PlatformWorkbookWriteBridge
{
    private static readonly PlatformWorkbookWriteService WriteService = new();

    public static PlatformWorkbook ExportBalticWorkbook(string databasePath, long clockTicks = 0) =>
        WriteService.ExportFromDatabase(
            databasePath,
            CatalogValidationDefaults.BalticSnapshotId,
            new FixedCatalogClock(clockTicks));

    public static PlatformWorkbookWriteResult ProposeWorkbook(
        string databasePath,
        PlatformWorkbook workbook,
        string actorType,
        string actorId,
        long clockTicks = 0,
        string rationale = "") =>
        WriteService.Propose(
            databasePath,
            workbook,
            new FixedCatalogClock(clockTicks),
            actorType,
            actorId,
            rationale);

    public static PlatformWorkbookWriteDecisionResult ApproveBatches(
        string databasePath,
        IEnumerable<string> batchIds,
        string actorType,
        string actorId,
        long clockTicks = 0) =>
        WriteService.ApproveBatches(
            databasePath,
            batchIds,
            new FixedCatalogClock(clockTicks),
            actorType,
            actorId);

    public static PlatformWorkbookWriteDecisionResult RejectBatches(
        string databasePath,
        IEnumerable<string> batchIds,
        string actorType,
        string actorId,
        long clockTicks = 0,
        string rationale = "") =>
        WriteService.RejectBatches(
            databasePath,
            batchIds,
            new FixedCatalogClock(clockTicks),
            actorType,
            actorId,
            rationale);
}