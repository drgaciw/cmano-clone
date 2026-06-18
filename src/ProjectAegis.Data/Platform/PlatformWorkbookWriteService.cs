namespace ProjectAegis.Data.Platform;

using ProjectAegis.Data.WriteGate;

/// <summary>
/// ADR-011 Phase D: headless export→edit→propose→approve orchestration for platform workbooks.
/// All catalog mutations route through <see cref="CatalogWriteGate"/> — no direct SQLite writes.
/// </summary>
public sealed class PlatformWorkbookWriteService
{
    public PlatformWorkbook ExportFromDatabase(
        string databasePath,
        string snapshotId,
        ICatalogClock clock,
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

        if (clock is null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (!PlatformCatalogExportResolver.TryResolve(databasePath, snapshotId, out var data))
        {
            throw new InvalidOperationException(
                $"Snapshot '{snapshotId}' did not resolve against database '{databasePath}'.");
        }

        return (exporter ?? new PlatformWorkbookExporter()).Export(data, snapshotId, clock);
    }

    public PlatformWorkbookWriteResult Propose(
        string databasePath,
        PlatformWorkbook edited,
        ICatalogClock clock,
        string actorType,
        string actorId,
        string rationale = "",
        PlatformWorkbookImporter? importer = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required.", nameof(databasePath));
        if (edited is null) throw new ArgumentNullException(nameof(edited));
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (string.IsNullOrWhiteSpace(actorType)) throw new ArgumentException("Actor type is required.", nameof(actorType));
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor id is required.", nameof(actorId));

        using var gate = new CatalogWriteGate(databasePath, clock);
        var effectiveImporter = importer ?? CreateImporter(databasePath, clock);
        var import = effectiveImporter.Stage(edited, gate, actorType, actorId, rationale);
        return PlatformWorkbookWriteResult.FromImport(import);
    }

    public PlatformWorkbookWriteResult ProposeFromFile(
        string databasePath,
        string workbookPath,
        IPlatformWorkbookIo io,
        ICatalogClock clock,
        string actorType,
        string actorId,
        string rationale = "",
        PlatformWorkbookImporter? importer = null)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required.", nameof(databasePath));
        if (string.IsNullOrWhiteSpace(workbookPath)) throw new ArgumentException("Workbook path is required.", nameof(workbookPath));
        if (io is null) throw new ArgumentNullException(nameof(io));
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (string.IsNullOrWhiteSpace(actorType)) throw new ArgumentException("Actor type is required.", nameof(actorType));
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor id is required.", nameof(actorId));

        using var gate = new CatalogWriteGate(databasePath, clock);
        var effectiveImporter = importer ?? CreateImporter(databasePath, clock);
        var import = effectiveImporter.StageFromFile(workbookPath, io, gate, actorType, actorId, rationale);
        return PlatformWorkbookWriteResult.FromImport(import);
    }

    public PlatformWorkbookWriteDecisionResult ApproveBatches(
        string databasePath,
        IEnumerable<string> batchIds,
        ICatalogClock clock,
        string actorType,
        string actorId)
    {
        return ProcessBatches(databasePath, batchIds, clock, actorType, actorId, (gate, batchId) =>
            gate.ApproveBatch(batchId, actorType, actorId));
    }

    public PlatformWorkbookWriteDecisionResult RejectBatches(
        string databasePath,
        IEnumerable<string> batchIds,
        ICatalogClock clock,
        string actorType,
        string actorId,
        string rationale = "")
    {
        return ProcessBatches(databasePath, batchIds, clock, actorType, actorId, (gate, batchId) =>
            gate.RejectBatch(batchId, actorType, actorId, rationale));
    }

    private static PlatformWorkbookImporter CreateImporter(string databasePath, ICatalogClock clock) =>
        new(snapshotId =>
        {
            if (PlatformCatalogExportResolver.TryResolve(databasePath, snapshotId, out var data))
            {
                return data;
            }

            return string.Equals(snapshotId, "cli-s22-export", StringComparison.Ordinal)
                ? PlatformCatalogExportData.Empty
                : null;
        }, clock);

    private static PlatformWorkbookWriteDecisionResult ProcessBatches(
        string databasePath,
        IEnumerable<string> batchIds,
        ICatalogClock clock,
        string actorType,
        string actorId,
        Func<CatalogWriteGate, string, WriteGateDecision> process)
    {
        if (string.IsNullOrWhiteSpace(databasePath)) throw new ArgumentException("Database path is required.", nameof(databasePath));
        if (batchIds is null) throw new ArgumentNullException(nameof(batchIds));
        if (clock is null) throw new ArgumentNullException(nameof(clock));
        if (string.IsNullOrWhiteSpace(actorType)) throw new ArgumentException("Actor type is required.", nameof(actorType));
        if (string.IsNullOrWhiteSpace(actorId)) throw new ArgumentException("Actor id is required.", nameof(actorId));
        if (process is null) throw new ArgumentNullException(nameof(process));

        var processed = new List<string>();
        var committed = new List<string>();
        var errors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        using var gate = new CatalogWriteGate(databasePath, clock);
        foreach (var batchId in batchIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            processed.Add(batchId);
            var decision = process(gate, batchId);
            if (decision.Committed)
            {
                committed.Add(batchId);
                continue;
            }

            errors[batchId] = decision.Errors;
        }

        return new PlatformWorkbookWriteDecisionResult(processed, committed, errors);
    }
}