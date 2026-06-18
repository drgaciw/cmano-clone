namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.WriteGate;

/// <summary>
/// Req-21 / ADR-011 PLE-2.* / PLE-3.*: turns an edited workbook into staged write-gate batches.
/// Re-exports the bound snapshot (via an injected provider) to diff against, validates fitting rules,
/// and stages supported entity changes (Phase A + Phase B) through <see cref="IWriteGate"/>.
/// </summary>
public sealed class PlatformWorkbookImporter
{
    /// <summary>DBI-2.4: commits above this row count require explicit human approval.</summary>
    public const int HumanApprovalRecordThreshold = 10;

    private const string SupportedSheet = "Sensors";
    private const string PlatformsSheet = "Platforms";
    private static readonly string[] SupportedSheets =
    {
        "Sensors", "Mounts", "Loadouts", "Magazines", "Comms",
        "Mobility", "Signatures", "Emcon",
    };
    private static readonly string[] SupportedPlatformDamageColumns =
    {
        "MaxHp", "WithdrawThresholdPct", "CriticalFlags",
    };

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

    public PlatformWorkbook ReadFromFile(string path, IPlatformWorkbookIo io)
    {
        if (io is null) throw new ArgumentNullException(nameof(io));
        return io.Read(path);
    }

    public PlatformImportPlan PlanFromFile(string path, IPlatformWorkbookIo io) => Plan(ReadFromFile(path, io));

    public PlatformImportResult StageFromFile(
        string path,
        IPlatformWorkbookIo io,
        IWriteGate gate,
        string actorType,
        string actorId,
        string rationale = "") => Stage(ReadFromFile(path, io), gate, actorType, actorId, rationale);

    public PlatformImportPlan Plan(PlatformWorkbook edited)
    {
        if (edited is null) throw new ArgumentNullException(nameof(edited));

        var snapshotId = ReadMeta(edited, "SourceSnapshotId");
        var source = string.IsNullOrEmpty(snapshotId) ? null : _snapshotProvider(snapshotId);
        if (source is null)
        {
            return new PlatformImportPlan(snapshotId, false, [], [], [], [], RequiresHumanApproval: false);
        }

        var sourceWorkbook = _exporter.Export(source, snapshotId, _clock);
        var changes = PlatformWorkbookDiff.Compare(sourceWorkbook, edited);
        var findings = PlatformWorkbookValidator.Validate(edited);

        var supported = new List<PlatformWorkbookChange>(changes.Count);
        var unsupported = new List<PlatformWorkbookChange>();
        foreach (var change in changes)
        {
            if (SupportedSheets.Contains(change.Sheet, StringComparer.Ordinal))
            {
                supported.Add(change);
                continue;
            }

            if (string.Equals(change.Sheet, PlatformsSheet, StringComparison.Ordinal))
            {
                if (IsSupportedPlatformDamageChange(change))
                {
                    supported.Add(change);
                }
                else
                {
                    unsupported.Add(change);
                }

                continue;
            }

            unsupported.Add(change);
        }

        var requiresApproval = changes.Count > HumanApprovalRecordThreshold;

        return new PlatformImportPlan(snapshotId, true, changes, findings, supported, unsupported, requiresApproval);
    }

    private static bool IsSupportedPlatformDamageChange(PlatformWorkbookChange change)
    {
        if (!string.Equals(change.Sheet, PlatformsSheet, StringComparison.Ordinal))
        {
            return false;
        }

        if (change.Kind != PlatformWorkbookChangeKind.CellChanged)
        {
            return false;
        }

        var column = change.Detail.Split(':', 2)[0];
        return SupportedPlatformDamageColumns.Contains(column, StringComparer.Ordinal);
    }

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
            return EmptyImportResult(plan, notes);
        }

        if (plan.Blocked)
        {
            notes.Add($"{plan.Findings.Count} validation finding(s); resolve errors before staging.");
            return EmptyImportResult(plan, notes);
        }

        var source = _snapshotProvider(plan.SourceSnapshotId);
        var knownPlatformIds = source?.Platforms
            .Select(p => p.PlatformId)
            .ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);

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

        string? mobilityBatchId = null;
        var mobilityRows = FilterKnownPlatforms(
            BuildChangedMobilityRows(edited, plan.SupportedChanges),
            knownPlatformIds,
            row => row.PlatformId,
            notes,
            "mobility");
        if (mobilityRows.Count > 0)
        {
            mobilityBatchId = gate.ProposeMobilityBatch(mobilityRows, actorType, actorId, rationale);
            notes.Add($"Proposed {mobilityRows.Count} mobility row(s) as batch '{mobilityBatchId}'.");
        }

        string? signatureBatchId = null;
        var signatureRows = FilterKnownPlatforms(
            BuildChangedSignatureRows(edited, plan.SupportedChanges),
            knownPlatformIds,
            row => row.PlatformId,
            notes,
            "signature");
        if (signatureRows.Count > 0)
        {
            signatureBatchId = gate.ProposeSignatureBatch(signatureRows, actorType, actorId, rationale);
            notes.Add($"Proposed {signatureRows.Count} signature row(s) as batch '{signatureBatchId}'.");
        }

        string? emconBatchId = null;
        var emconRows = FilterKnownPlatforms(
            BuildChangedEmconRows(edited, plan.SupportedChanges),
            knownPlatformIds,
            row => row.PlatformId,
            notes,
            "emcon");
        if (emconRows.Count > 0)
        {
            emconBatchId = gate.ProposeEmconBatch(emconRows, actorType, actorId, rationale);
            notes.Add($"Proposed {emconRows.Count} emcon row(s) as batch '{emconBatchId}'.");
        }

        string? damageBatchId = null;
        var damageRows = FilterKnownPlatforms(
            BuildChangedDamageRows(edited, plan.SupportedChanges),
            knownPlatformIds,
            row => row.PlatformId,
            notes,
            "damage");
        if (damageRows.Count > 0)
        {
            damageBatchId = gate.ProposePlatformDamageBatch(damageRows, actorType, actorId, rationale);
            notes.Add($"Proposed {damageRows.Count} damage row(s) as batch '{damageBatchId}'.");
        }

        if (plan.UnsupportedChanges.Count > 0)
        {
            var platformCoreChanges = plan.UnsupportedChanges.Count(c =>
                string.Equals(c.Sheet, PlatformsSheet, StringComparison.Ordinal));
            if (platformCoreChanges > 0)
            {
                notes.Add($"{platformCoreChanges} change(s) to platform core fields (LatDeg/LonDeg/CombatRadiusNm/row adds) are not yet stageable — pending gate extension.");
            }

            var otherUnsupported = plan.UnsupportedChanges.Count - platformCoreChanges;
            if (otherUnsupported > 0)
            {
                notes.Add($"{otherUnsupported} unsupported change(s) remain unstageable.");
            }
        }

        var staged = sensorBatchId is not null
            || mountBatchId is not null
            || loadoutBatchId is not null
            || magazineBatchId is not null
            || commsBatchId is not null
            || mobilityBatchId is not null
            || signatureBatchId is not null
            || emconBatchId is not null
            || damageBatchId is not null;
        return new PlatformImportResult(
            plan,
            Staged: staged,
            sensorBatchId,
            mountBatchId,
            loadoutBatchId,
            magazineBatchId,
            commsBatchId,
            mobilityBatchId,
            signatureBatchId,
            emconBatchId,
            damageBatchId,
            notes);
    }

    private static PlatformImportResult EmptyImportResult(PlatformImportPlan plan, IReadOnlyList<string> notes) =>
        new(plan, Staged: false, null, null, null, null, null, null, null, null, null, notes);

    private static IReadOnlyList<T> FilterKnownPlatforms<T>(
        IReadOnlyList<T> rows,
        HashSet<string> knownPlatformIds,
        Func<T, string> platformIdSelector,
        ICollection<string> notes,
        string entityLabel)
    {
        if (rows.Count == 0)
        {
            return rows;
        }

        var accepted = new List<T>(rows.Count);
        foreach (var row in rows)
        {
            var platformId = platformIdSelector(row);
            if (knownPlatformIds.Contains(platformId))
            {
                accepted.Add(row);
            }
            else
            {
                notes.Add($"Rejected orphan {entityLabel} row for unknown PlatformId '{platformId}'.");
            }
        }

        return accepted;
    }

    private static IReadOnlyList<CatalogSensorBinding> BuildChangedSensorRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, SupportedSheet, (row, col) => new CatalogSensorBinding(
            PlatformId: Get(row, col, "PlatformId"),
            SensorId: Get(row, col, "SensorId"),
            BasePd: ParseDouble(Get(row, col, "BasePd")),
            ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional),
            TrlLevel: ParseInt(Get(row, col, "TrlLevel"), 9),
            ValueTier: CatalogProvenanceTier.Normalize(Get(row, col, "ValueTier")),
            CitationRef: Get(row, col, "CitationRef")));

    private static IReadOnlyList<CatalogMount> BuildChangedMountRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Mounts", (row, col) => new CatalogMount(
            PlatformId: Get(row, col, "PlatformId"),
            MountId: Get(row, col, "MountId"),
            MountType: Get(row, col, "MountType", "rail"),
            ArcDeg: ParseDouble(Get(row, col, "ArcDeg", "360")),
            Capacity: ParseInt(Get(row, col, "Capacity"), 1),
            ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional)));

    private static IReadOnlyList<CatalogLoadout> BuildChangedLoadoutRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Loadouts", (row, col) => new CatalogLoadout(
            PlatformId: Get(row, col, "PlatformId"),
            LoadoutId: Get(row, col, "LoadoutId"),
            LoadoutName: Get(row, col, "LoadoutName"),
            Role: Get(row, col, "Role"),
            IsDefault: ParseBool(Get(row, col, "IsDefault"))));

    private static IReadOnlyList<CatalogMagazineEntry> BuildChangedMagazineRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Magazines", (row, col) => new CatalogMagazineEntry(
            PlatformId: Get(row, col, "PlatformId"),
            LoadoutId: Get(row, col, "LoadoutId"),
            MountId: Get(row, col, "MountId"),
            WeaponId: Get(row, col, "WeaponId"),
            Quantity: ParseInt(Get(row, col, "Quantity")),
            ReloadTimeSec: ParseInt(Get(row, col, "ReloadTimeSec")),
            Depth: ParseInt(Get(row, col, "Depth"))));

    private static IReadOnlyList<CatalogCommsBinding> BuildChangedCommsRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Comms", (row, col) => new CatalogCommsBinding(
            PlatformId: Get(row, col, "PlatformId"),
            LinkId: Get(row, col, "LinkId"),
            Role: Get(row, col, "Role", "txrx"),
            SatcomCapable: ParseBool(Get(row, col, "SatcomCapable")),
            ReviewState: Get(row, col, "ReviewState", CatalogReviewStates.Provisional),
            TrlLevel: ParseInt(Get(row, col, "TrlLevel"), 9),
            ValueTier: CatalogProvenanceTier.Normalize(Get(row, col, "ValueTier")),
            CitationRef: Get(row, col, "CitationRef")));

    private static IReadOnlyList<CatalogMobility> BuildChangedMobilityRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Mobility", (row, col) => new CatalogMobility(
            PlatformId: Get(row, col, "PlatformId"),
            MaxSpeedKnots: ParseDouble(Get(row, col, "MaxSpeedKnots")),
            CruiseSpeedKnots: ParseDouble(Get(row, col, "CruiseSpeedKnots")),
            MaxAltitudeFt: ParseDouble(Get(row, col, "MaxAltitudeFt")),
            MaxDepthM: ParseDouble(Get(row, col, "MaxDepthM")),
            FuelCapacity: ParseDouble(Get(row, col, "FuelCapacity")),
            RangeNm: ParseDouble(Get(row, col, "RangeNm")),
            EnduranceHr: ParseDouble(Get(row, col, "EnduranceHr"))));

    private static IReadOnlyList<CatalogSignature> BuildChangedSignatureRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Signatures", (row, col) => new CatalogSignature(
            PlatformId: Get(row, col, "PlatformId"),
            RcsBandDbsm: ParseDouble(Get(row, col, "RcsBandDbsm")),
            IrSignature: ParseDouble(Get(row, col, "IrSignature")),
            AcousticSignatureDb: ParseDouble(Get(row, col, "AcousticSignatureDb")),
            MagneticSignature: ParseDouble(Get(row, col, "MagneticSignature"))));

    private static IReadOnlyList<CatalogEmcon> BuildChangedEmconRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, "Emcon", (row, col) => new CatalogEmcon(
            PlatformId: Get(row, col, "PlatformId"),
            Condition: Get(row, col, "Condition", "silent"),
            EmitterId: Get(row, col, "EmitterId"),
            Posture: Get(row, col, "Posture", "off")));

    private static IReadOnlyList<CatalogPlatformDamage> BuildChangedDamageRows(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges) =>
        BuildChangedRows(edited, supportedChanges, PlatformsSheet, (row, col) => new CatalogPlatformDamage(
            PlatformId: Get(row, col, "PlatformId"),
            MaxHp: ParseDouble(Get(row, col, "MaxHp", "100")),
            WithdrawThresholdPct: ParseDouble(Get(row, col, "WithdrawThresholdPct", "0")),
            CriticalFlags: ParseInt(Get(row, col, "CriticalFlags"), 0)));

    private static IReadOnlyList<T> BuildChangedRows<T>(
        PlatformWorkbook edited,
        IReadOnlyList<PlatformWorkbookChange> supportedChanges,
        string sheetName,
        Func<IReadOnlyList<string>, Dictionary<string, int>, T> mapRow)
    {
        var sheet = edited.FindSheet(sheetName);
        if (sheet is null)
        {
            return [];
        }

        var changedRowIndices = supportedChanges
            .Where(c => c.Kind is PlatformWorkbookChangeKind.CellChanged or PlatformWorkbookChangeKind.RowAdded)
            .Where(c => string.Equals(c.Sheet, sheetName, StringComparison.Ordinal))
            .Select(c => c.RowIndex)
            .Where(i => i >= 0 && i < sheet.Rows.Count)
            .Distinct()
            .OrderBy(i => i)
            .ToArray();

        var col = HeaderIndex(sheet);
        var rows = new List<T>(changedRowIndices.Length);
        foreach (var i in changedRowIndices)
        {
            rows.Add(mapRow(sheet.Rows[i], col));
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