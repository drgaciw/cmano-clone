namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Req-21 / ADR-011 PLE-2.* / PLE-3.*: turns an edited workbook into staged write-gate batches.
/// Re-exports the bound snapshot (via an injected provider) to diff against, validates fitting rules,
/// and stages the entities the P0 write gate supports (sensors). Mount/loadout/magazine/comms changes
/// are reported as <see cref="PlatformImportPlan.UnsupportedChanges"/> pending a gate extension — the
/// importer never invents a commit path that bypasses <see cref="IWriteGate"/> (DBI-8.3 guardrail).
/// </summary>
public sealed class PlatformWorkbookImporter
{
    /// <summary>DBI-2.4: commits above this row count require explicit human approval.</summary>
    public const int HumanApprovalRecordThreshold = 10;

    private const string SupportedSheet = "Sensors";
    private static readonly string[] SupportedSheets = { "Sensors", "Mounts", "Loadouts", "Magazines", "Comms" };
    private static readonly string[] UnsupportedSheets = { "Platforms" };

    private readonly Func<string, PlatformCatalogExportData?> _snapshotProvider;
    private readonly ICatalogClock _clock;
    private readonly PlatformWorkbookExporter _exporter;

    public PlatformWorkbookImporter(
        Func<string, PlatformCatalogExportData?> snapshotProvider,
        ICatalogClock clock,
        PlatformWorkbookExporter? exporter = null)
    {
        _snapshotProvider = snapshotProvider ?? throw new ArgumentNullException(nameof(snapshotProvider));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _exporter = exporter ?? new PlatformWorkbookExporter();
    }

    /// <summary>Pure analysis: snapshot guard → re-export source → diff → validate → classify.</summary>
    public PlatformImportPlan Plan(PlatformWorkbook edited)
    {
        if (edited is null) throw new ArgumentNullException(nameof(edited));

        var snapshotId = ReadMeta(edited, "SourceSnapshotId");
        var source = string.IsNullOrEmpty(snapshotId) ? null : _snapshotProvider(snapshotId);
        if (source is null)
        {
            // PLE-2.2: unknown / stale source snapshot — cannot safely diff.
            return new PlatformImportPlan(snapshotId, false, [], [], [], [], RequiresHumanApproval: false);
        }

        var sourceWorkbook = _exporter.Export(source, snapshotId, _clock);
        var changes = PlatformWorkbookDiff.Compare(sourceWorkbook, edited);
        var findings = PlatformWorkbookValidator.Validate(edited);

        var supported = changes
            .Where(c => SupportedSheets.Contains(c.Sheet, StringComparer.Ordinal))
            .ToArray();
        var unsupported = changes
            .Where(c => UnsupportedSheets.Contains(c.Sheet, StringComparer.Ordinal))
            .ToArray();

        var requiresApproval = changes.Count > HumanApprovalRecordThreshold;

        return new PlatformImportPlan(snapshotId, true, changes, findings, supported, unsupported, requiresApproval);
    }

    /// <summary>
    /// Side-effecting: stage the supported changes through the write gate. Refused when the
    /// snapshot is unresolved or validation is blocked. Commit still requires a separate
    /// <see cref="IWriteGate.ApproveBatch"/> — this only proposes (PLE-3.1).
    /// </summary>
    public PlatformImportResult Stage(
        PlatformWorkbook edited,
        IWriteGate gate,
        string actorType,
        string actorId,
        string rationale = "")
    {
        if (gate is null) throw new ArgumentNullException(nameof(gate));

        var plan = Plan(edited);
        var notes = new List<string>();

        if (!plan.SnapshotResolved)
        {
            notes.Add($"Source snapshot '{plan.SourceSnapshotId}' did not resolve; nothing staged.");
            return new PlatformImportResult(plan, Staged: false, SensorBatchId: null, MountBatchId: null, LoadoutBatchId: null, MagazineBatchId: null, CommsBatchId: null, notes);
        }

        if (plan.Blocked)
        {
            notes.Add($"{plan.Findings.Count} validation finding(s); resolve errors before staging.");
            return new PlatformImportResult(plan, Staged: false, SensorBatchId: null, MountBatchId: null, LoadoutBatchId: null, MagazineBatchId: null, CommsBatchId: null, notes);
        }

        string? sensorBatchId = null;
        var sensorRows = BuildChangedSensorRows(edited, plan.SupportedChanges);
        if (sensorRows.Count > 0)
        {
            sensorBatchId = gate.ProposeSensorBatch(sensorRows, actorType, actorId, rationale);
            notes.Add($"Proposed {sensorRows.Count} sensor row(s) as batch '{sensorBatchId}'.");
        }

        string? mountBatchId = null;
        var mountRows = BuildChangedMountRows(edited, plan.SupportedChanges);
        if (mountRows.Count > 0)
        {
            mountBatchId = gate.ProposeMountBatch(mountRows, actorType, actorId, rationale);
            notes.Add($"Proposed {mountRows.Count} mount row(s) as batch '{mountBatchId}'.");
        }

        string? loadoutBatchId = null;
        var loadoutRows = BuildChangedLoadoutRows(edited, plan.SupportedChanges);
        if (loadoutRows.Count > 0)
        {
            loadoutBatchId = gate.ProposeLoadoutBatch(loadoutRows, actorType, actorId, rationale);
            notes.Add($"Proposed {loadoutRows.Count} loadout row(s) as batch '{loadoutBatchId}'.");
        }

        string? magazineBatchId = null;
        var magazineRows = BuildChangedMagazineRows(edited, plan.SupportedChanges);
        if (magazineRows.Count > 0)
        {
            magazineBatchId = gate.ProposeMagazineBatch(magazineRows, actorType, actorId, rationale);
            notes.Add($"Proposed {magazineRows.Count} magazine row(s) as batch '{magazineBatchId}'.");
        }

        string? commsBatchId = null;
        var commsRows = BuildChangedCommsRows(edited, plan.SupportedChanges);
        if (commsRows.Count > 0)
        {
            commsBatchId = gate.ProposeCommsBatch(commsRows, actorType, actorId, rationale);
            notes.Add($"Proposed {commsRows.Count} comms row(s) as batch '{commsBatchId}'.");
        }

        if (plan.UnsupportedChanges.Count > 0)
        {
            notes.Add($"{plan.UnsupportedChanges.Count} change(s) to platforms are not yet stageable (P0 write gate excludes platform changes) — pending gate extension.");
        }

        var staged = sensorBatchId is not null || mountBatchId is not null || loadoutBatchId is not null || magazineBatchId is not null || commsBatchId is not null;
        return new PlatformImportResult(plan, Staged: staged, sensorBatchId, mountBatchId, loadoutBatchId, magazineBatchId, commsBatchId, notes);
    }

    /// <summary>
    /// Reconstruct <see cref="CatalogSensorBinding"/> rows for the added/changed Sensors rows. Only the
    /// editor-surfaced columns are read; unsurfaced provenance (SourceFactId, Confidence, ImportBatchId…)
    /// takes record defaults here and would be merged from the source row in a full implementation.
    /// </summary>
    private static IReadOnlyList<CatalogSensorBinding> BuildChangedSensorRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges)
    {
        var sheet = edited.FindSheet(SupportedSheet);
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, SupportedSheet, StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<CatalogSensorBinding>();
        foreach (var i in changedRowIndices)
        {
            var row = sheet.Rows[i];
            rows.Add(new CatalogSensorBinding(
                PlatformId: Get(row, col, "PlatformId"),
                SensorId: Get(row, col, "SensorId"),
                BasePd: ParseDouble(Get(row, col, "BasePd")),
                ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional),
                TrlLevel: ParseInt(Get(row, col, "TrlLevel"), 9),
                ValueTier: CatalogProvenanceTier.Normalize(Get(row, col, "ValueTier")),
                CitationRef: Get(row, col, "CitationRef")));
        }

        return rows;
    }

    private static IReadOnlyList<CatalogMount> BuildChangedMountRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges)
    {
        var sheet = edited.FindSheet("Mounts");
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, "Mounts", StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<CatalogMount>();
        foreach (var i in changedRowIndices)
        {
            var row = sheet.Rows[i];
            rows.Add(new CatalogMount(
                PlatformId: Get(row, col, "PlatformId"),
                MountId: Get(row, col, "MountId"),
                MountType: Get(row, col, "MountType", "rail"),
                ArcDeg: ParseDouble(Get(row, col, "ArcDeg", "360")),
                Capacity: ParseInt(Get(row, col, "Capacity"), 1),
                ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional)));
        }

        return rows;
    }

    private static IReadOnlyList<CatalogLoadout> BuildChangedLoadoutRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges)
    {
        var sheet = edited.FindSheet("Loadouts");
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, "Loadouts", StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<CatalogLoadout>();
        foreach (var i in changedRowIndices)
        {
            var row = sheet.Rows[i];
            rows.Add(new CatalogLoadout(
                PlatformId: Get(row, col, "PlatformId"),
                LoadoutId: Get(row, col, "LoadoutId"),
                LoadoutName: Get(row, col, "LoadoutName"),
                Role: Get(row, col, "Role"),
                IsDefault: ParseBool(Get(row, col, "IsDefault"))));
        }

        return rows;
    }

    private static IReadOnlyList<CatalogMagazineEntry> BuildChangedMagazineRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges)
    {
        var sheet = edited.FindSheet("Magazines");
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, "Magazines", StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<CatalogMagazineEntry>();
        foreach (var i in changedRowIndices)
        {
            var row = sheet.Rows[i];
            rows.Add(new CatalogMagazineEntry(
                PlatformId: Get(row, col, "PlatformId"),
                LoadoutId: Get(row, col, "LoadoutId"),
                MountId: Get(row, col, "MountId"),
                WeaponId: Get(row, col, "WeaponId"),
                Quantity: ParseInt(Get(row, col, "Quantity")),
                ReloadTimeSec: ParseInt(Get(row, col, "ReloadTimeSec")),
                Depth: ParseInt(Get(row, col, "Depth"))));
        }

        return rows;
    }

    private static IReadOnlyList<CatalogCommsBinding> BuildChangedCommsRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges)
    {
        var sheet = edited.FindSheet("Comms");
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, "Comms", StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<CatalogCommsBinding>();
        foreach (var i in changedRowIndices)
        {
            var row = sheet.Rows[i];
            rows.Add(new CatalogCommsBinding(
                PlatformId: Get(row, col, "PlatformId"),
                LinkId: Get(row, col, "LinkId"),
                Role: Get(row, col, "Role", "txrx"),
                SatcomCapable: ParseBool(Get(row, col, "SatcomCapable")),
                ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional),
                TrlLevel: ParseInt(Get(row, col, "TrlLevel"), 9),
                ValueTier: CatalogProvenanceTier.Normalize(Get(row, col, "ValueTier")),
                CitationRef: Get(row, col, "CitationRef")));
        }

        return rows;
    }

    private static Dictionary<string, int> HeaderIndex(PlatformWorkbookSheet sheet)
    {
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < sheet.Header.Count; i++)
        {
            map[sheet.Header[i]] = i;
        }

        return map;
    }

    private static string Get(IReadOnlyList<string> row, Dictionary<string, int> col, string name, string fallback = "") =>
        col.TryGetValue(name, out var i) && i < row.Count && row[i].Length > 0 ? row[i] : fallback;

    private static string ReadMeta(PlatformWorkbook workbook, string key)
    {
        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        if (meta is null)
        {
            return string.Empty;
        }

        foreach (var row in meta.Rows)
        {
            if (row.Count >= 2 && string.Equals(row[0], key, StringComparison.Ordinal))
            {
                return row[1];
            }
        }

        return string.Empty;
    }

    private static int ParseInt(string value, int fallback = 0) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : fallback;

    private static bool ParseBool(string value) =>
        bool.TryParse(value, out var v) && v;

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
