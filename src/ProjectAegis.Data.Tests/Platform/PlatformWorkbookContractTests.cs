using System.Text.RegularExpressions;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// S125-06 / AUTH-08: workbook contract CI. Fails when exporter sheets/columns drift from
/// <see cref="PlatformWorkbookContract"/> or from the documented contract in
/// <c>docs/engineering/platform-workbook-contract.md</c>, and proves rows/fields the importer does not
/// stage are counted and surfaced (never silently dropped).
/// </summary>
public sealed class PlatformWorkbookContractTests
{
    private const string SnapshotId = "baltic_patrol";

    private static PlatformCatalogExportData FullData() => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85, CitationRef: "/sensor/1/") },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Links: new[] { new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50) },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: 30, CruiseSpeedKnots: 18) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: 10) },
        Emcon: new[] { new CatalogEmcon("u1", "silent", "radar-1", "off") },
        Damage: new[] { new CatalogPlatformDamage("u1", 120, 25, 0) },
        Swarms: new[] { new CatalogSwarmPlatform("u1", MaxDrones: 4) });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    [Fact]
    public void Exporter_sheets_and_columns_match_contract_in_order()
    {
        var workbook = Export(FullData());
        var dataSheets = workbook.Sheets
            .Where(s => !string.Equals(s.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(
            PlatformWorkbookContract.Sheets.Select(s => s.Name),
            dataSheets.Select(s => s.Name));
        foreach (var sheet in dataSheets)
        {
            var contract = PlatformWorkbookContract.Find(sheet.Name)!;
            Assert.Equal(contract.Columns, sheet.Header);
        }

        Assert.Empty(PlatformWorkbookContract.CheckDrift(workbook));
        Assert.Equal(PlatformWorkbookExporter.SchemaVersion, PlatformWorkbookContract.SchemaVersion);
    }

    [Fact]
    public void Documented_contract_matches_code_contract()
    {
        var documented = ReadDocumentedContract();

        Assert.Equal(PlatformWorkbookContract.Sheets.Select(s => s.Name), documented.Select(d => d.Sheet));
        foreach (var (sheet, columns, stageable) in documented)
        {
            var contract = PlatformWorkbookContract.Find(sheet)!;
            Assert.True(contract.Columns.SequenceEqual(columns), $"{sheet} columns drifted from doc: [{string.Join(",", columns)}]");
            Assert.True(
                contract.StageableColumns.SequenceEqual(stageable),
                $"{sheet} stageable columns drifted from doc: [{string.Join(",", stageable)}]");
        }
    }

    [Fact]
    public void Every_contract_column_edit_is_classified_as_the_contract_says()
    {
        var source = FullData();
        var exported = Export(source);
        var importer = ImporterFor(source);

        foreach (var contract in PlatformWorkbookContract.Sheets)
        {
            foreach (var column in contract.Columns)
            {
                var edited = WithCell(exported, contract.Name, 0, column, "77");
                var plan = importer.Plan(edited);
                var change = Assert.Single(plan.Changes);
                var supported = plan.SupportedChanges.Contains(change);
                Assert.True(
                    supported == contract.IsColumnStageable(column),
                    $"{contract.Name}.{column}: supported={supported}, contract stageable={contract.IsColumnStageable(column)}");
            }
        }
    }

    [Fact]
    public void CheckDrift_reports_unknown_and_missing_sheets_and_columns()
    {
        var workbook = Export(FullData());
        workbook = WithExtraColumn(workbook, "Sensors", "Notes", "remember me");
        workbook = WithoutColumn(workbook, "Mounts", "ArcDeg");
        workbook = WithSheet(workbook, new PlatformWorkbookSheet("Scratch", new[] { "A" }, new[] { new[] { "1" } }));
        workbook = new PlatformWorkbook(workbook.Sheets.Where(s => s.Name != "Emcon").ToArray());

        var drift = PlatformWorkbookContract.CheckDrift(workbook);

        Assert.Contains(drift, d => d.Kind == PlatformWorkbookContractDriftKind.UnknownColumn && d.Sheet == "Sensors" && d.Column == "Notes");
        Assert.Contains(drift, d => d.Kind == PlatformWorkbookContractDriftKind.MissingColumn && d.Sheet == "Mounts" && d.Column == "ArcDeg");
        Assert.Contains(drift, d => d.Kind == PlatformWorkbookContractDriftKind.UnknownSheet && d.Sheet == "Scratch");
        Assert.Contains(drift, d => d.Kind == PlatformWorkbookContractDriftKind.MissingSheet && d.Sheet == "Emcon");
    }

    [Fact]
    public void CheckDrift_reports_column_reorder()
    {
        var workbook = Export(FullData());
        var sheet = workbook.FindSheet("Signatures")!;
        var reversed = new PlatformWorkbookSheet(
            sheet.Name,
            sheet.Header.Reverse().ToArray(),
            sheet.Rows.Select(r => (IReadOnlyList<string>)r.Reverse().ToArray()).ToArray());
        workbook = Replace(workbook, reversed);

        var drift = PlatformWorkbookContract.CheckDrift(workbook);

        Assert.Contains(drift, d => d.Kind == PlatformWorkbookContractDriftKind.ColumnOrder && d.Sheet == "Signatures");
    }

    [Fact]
    public void Clean_round_trip_reports_zero_silent_drops()
    {
        var source = FullData();

        var plan = ImporterFor(source).Plan(Export(source));

        Assert.Empty(plan.ContractDrift);
        Assert.False(plan.DropCounts.HasDrops);
        Assert.Equal(0, plan.DropCounts.IgnoredRows);
        Assert.Equal(0, plan.DropCounts.IgnoredFields);
        Assert.Contains("0 ignored", plan.DropCounts.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_column_fields_are_counted_not_silently_dropped()
    {
        var source = FullData();
        var edited = WithExtraColumn(Export(source), "Sensors", "Notes", "curator note");

        var plan = ImporterFor(source).Plan(edited);

        Assert.Equal(1, plan.DropCounts.UnknownColumnFields);
        Assert.Equal(1, plan.DropCounts.HeaderDriftRows);
        Assert.True(plan.DropCounts.HasDrops);
        Assert.Contains(plan.ContractDrift, d => d.Column == "Notes");
        Assert.Contains("unknown-column field", plan.DropCounts.Summary, StringComparison.Ordinal);
    }

    [Fact]
    public void Rows_in_unknown_sheets_are_counted()
    {
        var source = FullData();
        var edited = WithSheet(
            Export(source),
            new PlatformWorkbookSheet("SensorCatalog", new[] { "SensorId" }, new[] { new[] { "s-1" }, new[] { "s-2" } }));

        var plan = ImporterFor(source).Plan(edited);

        Assert.Equal(1, plan.DropCounts.UnknownSheets);
        Assert.Equal(2, plan.DropCounts.UnknownSheetRows);
    }

    [Fact]
    public void Removed_rows_overflow_cells_and_unstageable_platform_edits_are_counted()
    {
        var source = FullData();
        var edited = Export(source);
        edited = WithCell(edited, "Platforms", 0, "CombatRadiusNm", "450");
        edited = WithCell(edited, "Platforms", 0, "LatDeg", "58");
        edited = Replace(edited, edited.FindSheet("Loadouts")! with { Rows = Array.Empty<IReadOnlyList<string>>() });
        var comms = edited.FindSheet("Comms")!;
        edited = Replace(edited, comms with
        {
            Rows = comms.Rows.Select(r => (IReadOnlyList<string>)r.Concat(new[] { "stray" }).ToArray()).ToArray(),
        });

        var plan = ImporterFor(source).Plan(edited);

        Assert.Equal(2, plan.DropCounts.UnstageableEdits);
        Assert.Equal(1, plan.DropCounts.RemovedRowsNotStaged);
        Assert.Equal(1, plan.DropCounts.OverflowFields);
    }

    [Fact]
    public void Stage_surfaces_silent_drop_note_and_result_counts_include_stage_quarantine()
    {
        var source = FullData();
        var edited = WithExtraColumn(Export(source), "Sensors", "Notes", "curator note");
        edited = WithCell(edited, "Platforms", 0, "CombatRadiusNm", "450");

        var result = ImporterFor(source).Stage(edited, new RecordingGate(), "human", "drgamtd");

        Assert.Contains(result.Notes, n => n.StartsWith("Silent-drop guard:", StringComparison.Ordinal));
        Assert.Equal(result.QuarantineEntries.Count, result.DropCounts.QuarantinedRows);
        Assert.True(result.DropCounts.HasDrops);
    }

    [Fact]
    public void Out_of_range_platform_latlon_is_flagged_with_actionable_warning()
    {
        var source = FullData();
        var edited = WithCell(Export(source), "Platforms", 0, "LatDeg", "95");
        edited = WithCell(edited, "Platforms", 0, "LonDeg", "20°30'E");

        var plan = ImporterFor(source).Plan(edited);

        Assert.Equal(2, plan.LatLonFindings.Count);
        Assert.All(plan.LatLonFindings, f =>
        {
            Assert.Equal(PlatformWorkbookLatLonDiagnostics.FindingCode, f.Code);
            Assert.Equal(ValidationSeverity.Warning, f.Severity);
            Assert.Equal("u1", f.UnitId);
            Assert.Contains("Mission Editor", f.Message, StringComparison.Ordinal);
        });
        Assert.Contains(plan.LatLonFindings, f => f.Message.Contains("between -90 and 90", StringComparison.Ordinal));
        Assert.Contains(plan.LatLonFindings, f => f.Message.Contains("decimal", StringComparison.Ordinal));
        Assert.False(plan.Blocked);
    }

    [Fact]
    public void Valid_platform_latlon_produces_no_messages()
    {
        var source = FullData();

        Assert.Empty(PlatformWorkbookLatLonDiagnostics.Check(Export(source)));
    }

    private static IReadOnlyList<(string Sheet, string[] Columns, string[] Stageable)> ReadDocumentedContract()
    {
        var path = Path.Combine(FindRepoRoot(), "docs", "engineering", "platform-workbook-contract.md");
        var text = File.ReadAllText(path);
        var start = text.IndexOf("<!-- contract:start -->", StringComparison.Ordinal);
        var end = text.IndexOf("<!-- contract:end -->", StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "contract markers missing from platform-workbook-contract.md");

        var rows = new List<(string, string[], string[])>();
        foreach (var line in text[start..end].Split('\n'))
        {
            var cells = line.Trim().Trim('|').Split('|').Select(c => c.Trim()).ToArray();
            if (cells.Length != 3 || !cells[0].StartsWith('`'))
            {
                continue;
            }

            rows.Add((Ticks(cells[0]).Single(), Ticks(cells[1]), Ticks(cells[2])));
        }

        return rows;
    }

    private static string[] Ticks(string cell) =>
        Regex.Matches(cell, "`([^`]+)`").Select(m => m.Groups[1].Value).ToArray();

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 10; i++)
        {
            if (File.Exists(Path.Combine(dir, "ProjectAegis.sln")))
            {
                return dir;
            }

            dir = Path.GetDirectoryName(dir.TrimEnd(Path.DirectorySeparatorChar)) ?? dir;
        }

        throw new DirectoryNotFoundException("ProjectAegis.sln not found above test output.");
    }

    private static PlatformWorkbook Replace(PlatformWorkbook workbook, PlatformWorkbookSheet sheet) =>
        new(workbook.Sheets.Select(s => s.Name == sheet.Name ? sheet : s).ToArray());

    private static PlatformWorkbook WithSheet(PlatformWorkbook workbook, PlatformWorkbookSheet sheet) =>
        new(workbook.Sheets.Concat(new[] { sheet }).ToArray());

    private static PlatformWorkbook WithCell(PlatformWorkbook workbook, string sheetName, int row, string column, string value)
    {
        var sheet = workbook.FindSheet(sheetName)!;
        var col = sheet.Header.ToList().IndexOf(column);
        var rows = sheet.Rows.Select((r, i) =>
        {
            if (i != row) return r;
            var copy = r.ToArray();
            copy[col] = copy[col] == value ? value + "1" : value;
            return (IReadOnlyList<string>)copy;
        }).ToArray();
        return Replace(workbook, sheet with { Rows = rows });
    }

    private static PlatformWorkbook WithExtraColumn(PlatformWorkbook workbook, string sheetName, string column, string value)
    {
        var sheet = workbook.FindSheet(sheetName)!;
        return Replace(workbook, new PlatformWorkbookSheet(
            sheet.Name,
            sheet.Header.Concat(new[] { column }).ToArray(),
            sheet.Rows.Select(r => (IReadOnlyList<string>)r.Concat(new[] { value }).ToArray()).ToArray()));
    }

    private static PlatformWorkbook WithoutColumn(PlatformWorkbook workbook, string sheetName, string column)
    {
        var sheet = workbook.FindSheet(sheetName)!;
        var col = sheet.Header.ToList().IndexOf(column);
        return Replace(workbook, new PlatformWorkbookSheet(
            sheet.Name,
            sheet.Header.Where((_, i) => i != col).ToArray(),
            sheet.Rows.Select(r => (IReadOnlyList<string>)r.Where((_, i) => i != col).ToArray()).ToArray()));
    }

    private sealed class RecordingGate : IWriteGate
    {
        private int _next;

        private string Next() => $"batch-{++_next}";

        public string ProposeSensorBatch(IReadOnlyList<CatalogSensorBinding> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeMountBatch(IReadOnlyList<CatalogMount> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeLoadoutBatch(IReadOnlyList<CatalogLoadout> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeMagazineBatch(IReadOnlyList<CatalogMagazineEntry> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeCommsBatch(IReadOnlyList<CatalogCommsBinding> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeLinkCatalogBatch(IReadOnlyList<CatalogLinkEntry> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformBinding> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeWeaponBatch(IReadOnlyList<CatalogWeaponRecord> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeMobilityBatch(IReadOnlyList<CatalogMobility> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeSignatureBatch(IReadOnlyList<CatalogSignature> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeEmconBatch(IReadOnlyList<CatalogEmcon> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposePlatformDamageBatch(IReadOnlyList<CatalogPlatformDamage> proposed, string actorType, string actorId, string rationale = "") => Next();
        public string ProposeSwarmBatch(IReadOnlyList<CatalogSwarmPlatform> proposed, string actorType, string actorId, string rationale = "") => Next();
        public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId) => throw new InvalidOperationException("import must not approve");
        public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "") => throw new InvalidOperationException("import must not reject");
        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() => Array.Empty<CatalogStagingBatchSummary>();
    }
}
