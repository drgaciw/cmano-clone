namespace ProjectAegis.Delegation.UnityAdapter.Bridge;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// ADR-011 Phase C: headless Unity/CLI bridge for read-only platform workbook export and diff.
/// Delegates to <see cref="PlatformCatalogExportResolver"/> + <see cref="PlatformWorkbookExporter"/> only —
/// no write-gate calls, no propose/approve, no direct SQLite writes.
/// </summary>
public static class PlatformCatalogExportBridge
{
    public static PlatformWorkbook ExportFromDatabase(
        string databasePath,
        string snapshotId,
        long clockTicks = 0,
        PlatformWorkbookExporter? exporter = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            throw new ArgumentException("Snapshot id is required.", nameof(snapshotId));
        }

        if (!PlatformCatalogExportResolver.TryResolve(databasePath, snapshotId, out var data))
        {
            throw new InvalidOperationException(
                $"Snapshot '{snapshotId}' did not resolve against database '{databasePath}'.");
        }

        return (exporter ?? new PlatformWorkbookExporter()).Export(
            data,
            snapshotId,
            new FixedCatalogClock(clockTicks));
    }

    public static PlatformWorkbook ExportBalticWorkbook(string databasePath, long clockTicks = 0) =>
        ExportFromDatabase(databasePath, CatalogValidationDefaults.BalticSnapshotId, clockTicks);

    public static void ExportToFile(
        string databasePath,
        string snapshotId,
        string outPath,
        long clockTicks = 0,
        string? ioFlag = null)
    {
        var workbook = ExportFromDatabase(databasePath, snapshotId, clockTicks);
        WriteWorkbookToFile(workbook, outPath, ioFlag);
    }

    public static void ExportBalticToFile(
        string databasePath,
        string outPath,
        long clockTicks = 0,
        string? ioFlag = null) =>
        ExportToFile(databasePath, CatalogValidationDefaults.BalticSnapshotId, outPath, clockTicks, ioFlag);

    public static void WriteWorkbookToFile(
        PlatformWorkbook workbook,
        string outPath,
        string? ioFlag = null)
    {
        if (workbook is null)
        {
            throw new ArgumentNullException(nameof(workbook));
        }

        if (string.IsNullOrWhiteSpace(outPath))
        {
            throw new ArgumentException("Output path is required.", nameof(outPath));
        }

        var io = PlatformWorkbookIoSelection.Resolve(outPath, ioFlag ?? PlatformWorkbookIoSelection.CanonicalFlag);
        new PlatformWorkbookExporter().WriteToFile(workbook, outPath, io);
    }

    public static IReadOnlyList<PlatformWorkbookChange> DiffUneditedRoundTrip(
        string databasePath,
        string snapshotId,
        long clockTicks = 0)
    {
        var workbook = ExportFromDatabase(databasePath, snapshotId, clockTicks);
        return PlatformWorkbookDiff.Compare(workbook, workbook);
    }

    public static IReadOnlyList<PlatformWorkbookChange> DiffBalticUneditedRoundTrip(
        string databasePath,
        long clockTicks = 0) =>
        DiffUneditedRoundTrip(databasePath, CatalogValidationDefaults.BalticSnapshotId, clockTicks);
}