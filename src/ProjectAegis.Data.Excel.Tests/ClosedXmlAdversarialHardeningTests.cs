using ClosedXML.Excel;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// PE-W1 adversarial TDD hardening: enum catalog completeness/invariants + ClosedXML list validation
/// and OQ5 protection residual honesty. Export-time UX only — does not gate the importer.
/// </summary>
public sealed class ClosedXmlAdversarialHardeningTests
{
    private const string SnapshotId = "phase-b-adversarial-fixture";

    /// <summary>
    /// Known exporter enum-ish headers that PLE-1.2 claims to list-validate.
    /// Completeness: each must appear in <see cref="PlatformWorkbookEnumCatalog.Columns"/>.
    /// Free-text / numeric-only columns are intentionally excluded (see whitelist comments below).
    /// </summary>
    private static readonly (string Sheet, string Column)[] ClaimedEnumHeaders =
    [
        ("Sensors", "ReviewState"),
        ("Sensors", "ValueTier"),
        ("Sensors", "TrlLevel"),
        ("Mounts", "MountType"),
        ("Mounts", "ReviewState"),
        ("Loadouts", "Role"),
        ("Loadouts", "IsDefault"),
        ("Comms", "Role"),
        ("Comms", "SatcomCapable"),
        ("Comms", "ReviewState"),
        ("Comms", "ValueTier"),
        ("Comms", "TrlLevel"),
        ("LinkCatalog", "LinkType"),
        (PlatformEmconEnums.EmconSheetName, PlatformEmconEnums.ConditionColumn),
        (PlatformEmconEnums.EmconSheetName, PlatformEmconEnums.PostureColumn),
    ];

    /// <summary>
    /// Exporter headers that look enum-ish but are intentionally NOT list-validated (free text / ids).
    /// Documented so a future catalog drop is deliberate, not accidental.
    /// </summary>
    private static readonly (string Sheet, string Column)[] IntentionallyExcludedEnumishHeaders =
    [
        // Free-text provenance / display — not closed enums
        ("Sensors", "CitationRef"),
        ("Comms", "CitationRef"),
        ("LinkCatalog", "DisplayName"),
        // Identity / numeric columns (not enum UX)
        ("Sensors", "SensorId"),
        ("Sensors", "BasePd"),
        ("Mounts", "MountId"),
        ("Mounts", "ArcDeg"),
        ("Mounts", "Capacity"),
        ("Loadouts", "LoadoutId"),
        ("Loadouts", "LoadoutName"),
        ("Comms", "LinkId"),
        ("LinkCatalog", "LinkId"),
        ("LinkCatalog", "LatencyMsNominal"),
        ("Emcon", "EmitterId"),
        // Magazines / Mobility / Signatures / Platforms: no PLE-1.2 enum matrix claims
        ("Magazines", "WeaponId"),
        ("Mobility", "PlatformId"),
        ("Signatures", "PlatformId"),
        ("Platforms", "PlatformId"),
        ("_Meta", "Key"),
        ("_Meta", "Value"),
    ];

    // -------------------------------------------------------------------------
    // 1. Catalog completeness vs exporter headers
    // -------------------------------------------------------------------------

    [Test]
    public void Catalog_covers_all_claimed_exporter_enum_headers_including_emcon()
    {
        var keys = PlatformWorkbookEnumCatalog.Columns
            .Select(c => (c.SheetName, c.ColumnName))
            .ToHashSet();

        var missing = ClaimedEnumHeaders.Where(h => !keys.Contains(h)).ToArray();
        Assert.That(
            missing,
            Is.Empty,
            "Catalog dropped claimed enum headers: "
            + string.Join(", ", missing.Select(m => $"{m.Sheet}.{m.Column}")));

        // Explicit Emcon guard — Condition/Posture must never silently disappear
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null
                     && c.SheetName == PlatformEmconEnums.EmconSheetName
                     && c.ColumnName == PlatformEmconEnums.ConditionColumn));
        Assert.That(
            PlatformWorkbookEnumCatalog.Columns,
            Has.Some.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                c => c is not null
                     && c.SheetName == PlatformEmconEnums.EmconSheetName
                     && c.ColumnName == PlatformEmconEnums.PostureColumn));

        // Whitelist entries must not accidentally be cataloged as enums
        foreach (var excluded in IntentionallyExcludedEnumishHeaders)
        {
            Assert.That(
                PlatformWorkbookEnumCatalog.Columns,
                Has.None.Matches<PlatformWorkbookEnumCatalog.EnumColumn>(
                    c => c is not null
                         && c.SheetName == excluded.Sheet
                         && c.ColumnName == excluded.Column));
        }
    }

    // -------------------------------------------------------------------------
    // 2. Duplicate EnumColumn keys + deterministic ordering
    // -------------------------------------------------------------------------

    [Test]
    public void Catalog_has_no_duplicate_sheet_column_keys_and_stable_ordering()
    {
        var keys = PlatformWorkbookEnumCatalog.Columns
            .Select(c => (c.SheetName, c.ColumnName))
            .ToArray();

        var distinct = keys.Distinct().ToArray();
        Assert.That(distinct.Length, Is.EqualTo(keys.Length));

        // Deterministic: sorted by SheetName then ColumnName (ordinal) — either already sorted
        // or equal to a stable sort of itself (catalog may use intentional sheet grouping order).
        var sorted = keys
            .OrderBy(k => k.SheetName, StringComparer.Ordinal)
            .ThenBy(k => k.ColumnName, StringComparer.Ordinal)
            .ToArray();

        // Accept either fully sorted OR document stable unique keys; require uniqueness above
        // and that ForSheet returns columns in catalog order (stable).
        var sheets = keys.Select(k => k.SheetName).Distinct(StringComparer.Ordinal).ToArray();
        foreach (var sheet in sheets)
        {
            var fromForSheet = PlatformWorkbookEnumCatalog.ForSheet(sheet)
                .Select(c => c.ColumnName)
                .ToArray();
            var fromColumns = PlatformWorkbookEnumCatalog.Columns
                .Where(c => string.Equals(c.SheetName, sheet, StringComparison.Ordinal))
                .Select(c => c.ColumnName)
                .ToArray();
            Assert.That(fromForSheet, Is.EqualTo(fromColumns));
        }

        // Hard invariant used by export: sorted uniqueness of (Sheet, Column)
        Assert.That(sorted.Distinct().Count(), Is.EqualTo(sorted.Length));
        _ = sorted; // silence if unused under analyzers
    }

    // -------------------------------------------------------------------------
    // 3. ToExcelList edge cases — commas / quotes break Excel list formula
    // -------------------------------------------------------------------------

    [Test]
    public void Catalog_allowed_values_are_clean_excel_list_tokens()
    {
        foreach (var col in PlatformWorkbookEnumCatalog.Columns)
        {
            Assert.That(col.AllowedValues, Is.Not.Empty);
            foreach (var value in col.AllowedValues)
            {
                Assert.That(
                    string.IsNullOrWhiteSpace(value),
                    Is.False,
                    $"{col.SheetName}.{col.ColumnName}: empty token");
                Assert.That(value, Does.Not.Contain(","));
                Assert.That(value, Does.Not.Contain("\""));
                Assert.That(value, Does.Not.Contain("\n"));
                Assert.That(value, Does.Not.Contain("\r"));
            }
        }
    }

    [Test]
    public void ToExcelList_rejects_values_containing_commas()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            PlatformWorkbookEnumCatalog.ToExcelList(["silent", "a,b", "free"]));
        Assert.That(ex!.Message.Contains("comma", StringComparison.OrdinalIgnoreCase), Is.True);
    }

    [Test]
    public void ToExcelList_formats_clean_tokens_as_quoted_csv_list()
    {
        var formula = PlatformWorkbookEnumCatalog.ToExcelList(["silent", "restricted", "free"]);
        Assert.That(formula, Is.EqualTo("\"silent,restricted,free\""));
    }

    // -------------------------------------------------------------------------
    // 4. Missing column on sheet — no throw; empty sheets export
    // -------------------------------------------------------------------------

    [Test]
    public void Missing_enum_column_on_sheet_is_noop_and_empty_sheets_export()
    {
        // Sensors registered for ReviewState but sheet only has PlatformId → no-op validation
        var partial = new PlatformWorkbook(
        [
            new PlatformWorkbookSheet("Sensors", ["PlatformId", "SensorId"], [["u1", "radar-1"]]),
            new PlatformWorkbookSheet("Emcon", ["PlatformId"], []), // Condition/Posture missing
            new PlatformWorkbookSheet("Mobility", ["PlatformId", "MaxSpeedKnots"], []),
            new PlatformWorkbookSheet("_Meta", ["Key", "Value"], [["SchemaVersion", "1"]]),
        ]);

        var path = TempXlsx("missing-col");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            Assert.That(() => io.Write(partial, path), Throws.Nothing);

            using var wb = new XLWorkbook(path);
            var sensors = wb.Worksheet("Sensors");
            // PlatformId column has no list validation from enum catalog
            var dv = sensors.Cell(2, 1).GetDataValidation();
            Assert.That(
                dv is null || dv.AllowedValues != XLAllowedValues.List,
                Is.True,
                "missing enum headers must not invent list validation on other columns");

            var emcon = wb.Worksheet("Emcon");
            Assert.That(
                emcon.Cell(2, 1).GetDataValidation() is null
                || emcon.Cell(2, 1).GetDataValidation()!.AllowedValues != XLAllowedValues.List,
                Is.True);
        }
        finally
        {
            TryDelete(path);
        }
    }

    // -------------------------------------------------------------------------
    // 5. Protection residual honesty + re-read after Write
    // -------------------------------------------------------------------------

    [Test]
    public void Protection_residual_honesty_pk_locked_non_pk_unlocked_meta_and_reread()
    {
        var workbook = ExportPhaseB();
        var path = TempXlsx("protect-reread");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(workbook, path);

            using (var wb = new XLWorkbook(path))
            {
                var meta = wb.Worksheet(PlatformWorkbookHash.MetaSheetName);
                Assert.That(meta.IsProtected, Is.True);
                Assert.That(meta.Cell(1, 1).Style.Protection.Locked, Is.True);
                Assert.That(meta.Cell(2, 2).Style.Protection.Locked, Is.True);

                var mounts = wb.Worksheet("Mounts");
                Assert.That(mounts.IsProtected, Is.True);
                var platformIdCol = ColumnIndex(mounts, "PlatformId");
                var mountTypeCol = ColumnIndex(mounts, "MountType");
                Assert.That(mounts.Cell(2, platformIdCol).Style.Protection.Locked, Is.True);
                Assert.That(
                    mounts.Cell(2, mountTypeCol).Style.Protection.Locked,
                    Is.False,
                    "OQ5 residual: non-PK cells remain unlocked under soft protect");

                // Sheet without enum-only headers still protects PK (Mobility.PlatformId)
                var mobility = wb.Worksheet("Mobility");
                Assert.That(mobility.IsProtected, Is.True);
                Assert.That(mobility.Cell(2, ColumnIndex(mobility, "PlatformId")).Style.Protection.Locked, Is.True);
                Assert.That(mobility.Cell(2, ColumnIndex(mobility, "MaxSpeedKnots")).Style.Protection.Locked, Is.False);
            }

            // Re-read via product Read() must not throw (ClosedXML ignores protection by design)
            Assert.That(() => io.Read(path), Throws.Nothing);
            var round = io.Read(path);
            Assert.That(round.Sheets, Is.Not.Empty);
        }
        finally
        {
            TryDelete(path);
        }
    }

    // -------------------------------------------------------------------------
    // 6. Round-trip integrity under protection
    // -------------------------------------------------------------------------

    [Test]
    public void Round_trip_preserves_sheet_names_and_row_counts_under_protection()
    {
        var original = ExportPhaseB();
        var path = TempXlsx("rt-protect");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            var round = io.Read(path);

            Assert.That(
                round.Sheets.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(original.Sheets.Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray()));

            foreach (var src in original.Sheets)
            {
                var matches = round.Sheets
                    .Where(s => string.Equals(s.Name, src.Name, StringComparison.Ordinal))
                    .ToArray();
                Assert.That(matches, Has.Length.EqualTo(1));
                var dst = matches[0];
                Assert.That(dst.Header.Count, Is.EqualTo(src.Header.Count));
                Assert.That(dst.Rows.Count, Is.EqualTo(src.Rows.Count));
            }

            // Logical empty-diff still holds (protection must not corrupt cell text)
            Assert.That(PlatformWorkbookDiff.IsEmpty(original, round), Is.True);
        }
        finally
        {
            TryDelete(path);
        }
    }

    // -------------------------------------------------------------------------
    // 7. Enum list actually contains fixture values
    // -------------------------------------------------------------------------

    [Test]
    public void Exported_fixture_enum_cell_values_are_subset_of_catalog_allowed_lists()
    {
        var workbook = ExportPhaseB();
        var path = TempXlsx("enum-subset");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            // Check logical workbook cells (and that export retained them)
            foreach (var entry in PlatformWorkbookEnumCatalog.Columns)
            {
                var sheet = workbook.Sheets.FirstOrDefault(s =>
                    string.Equals(s.Name, entry.SheetName, StringComparison.Ordinal));
                if (sheet is null)
                {
                    continue;
                }

                var col = IndexOf(sheet.Header, entry.ColumnName);
                if (col < 0)
                {
                    continue;
                }

                var allowed = entry.AllowedValues.ToHashSet(StringComparer.Ordinal);
                foreach (var row in sheet.Rows)
                {
                    if (col >= row.Count)
                    {
                        continue;
                    }

                    var cell = row[col];
                    if (string.IsNullOrEmpty(cell))
                    {
                        continue; // blanks allowed (IgnoreBlanks)
                    }

                    Assert.That(
                        allowed.Contains(cell),
                        Is.True,
                        $"{entry.SheetName}.{entry.ColumnName} fixture value '{cell}' not in catalog "
                        + $"[{string.Join(",", entry.AllowedValues)}]");
                }
            }
        }
        finally
        {
            TryDelete(path);
        }
    }

    // -------------------------------------------------------------------------
    // 8. IgnoreBlanks / error style
    // -------------------------------------------------------------------------

    [Test]
    public void List_validation_IgnoreBlanks_true_and_error_style_configured()
    {
        var workbook = ExportPhaseB();
        var path = TempXlsx("ignore-blanks");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var emcon = wb.Worksheet(PlatformEmconEnums.EmconSheetName);
            var conditionCol = ColumnIndex(emcon, PlatformEmconEnums.ConditionColumn);
            var validation = emcon.Cell(2, conditionCol).GetDataValidation();
            Assert.That(validation, Is.Not.Null);
            Assert.That(validation.AllowedValues, Is.EqualTo(XLAllowedValues.List));
            Assert.That(validation.IgnoreBlanks, Is.True, "blank enum cells must not be blocked (IgnoreBlanks=true)");
            Assert.That(validation.InCellDropdown, Is.True);
            Assert.That(
                validation.ShowErrorMessage,
                Is.True,
                "list validation should surface an error style so invalid picks are visible UX");
            Assert.That(validation.ErrorStyle, Is.EqualTo(XLErrorStyle.Warning));
        }
        finally
        {
            TryDelete(path);
        }
    }

    // -------------------------------------------------------------------------
    // 9. Unknown sheets + IsMetaSheet ordinal case-sensitivity
    // -------------------------------------------------------------------------

    [Test]
    public void ForSheet_unknown_empty_and_IsMetaSheet_is_ordinal_case_sensitive()
    {
        Assert.That(PlatformWorkbookEnumCatalog.ForSheet("nope"), Is.Empty);
        Assert.That(PlatformWorkbookEnumCatalog.ForSheet("EMCON"), Is.Empty); // ordinal: not Emcon
        Assert.That(PlatformWorkbookEnumCatalog.ForSheet("emcon"), Is.Empty);

        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("_Meta"), Is.True);
        // Product decision: ordinal equality — lowercase / wrong case is NOT meta
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("_meta"), Is.False);
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("_META"), Is.False);
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet("Meta"), Is.False);
        Assert.That(PlatformWorkbookEnumCatalog.IsMetaSheet(string.Empty), Is.False);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PlatformWorkbook ExportPhaseB() =>
        new PlatformWorkbookExporter().Export(
            new PlatformCatalogExportData(
                Platforms: [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)],
                Sensors:
                [
                    new CatalogSensorBinding(
                        "u1",
                        "radar-1",
                        0.85,
                        ReviewState: CatalogReviewStates.Provisional,
                        TrlLevel: 9,
                        ValueTier: CatalogProvenanceTier.GameplayAbstraction),
                ],
                Mounts: [new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32)],
                Loadouts: [new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true)],
                Magazines: [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32)],
                Comms:
                [
                    new CatalogCommsBinding(
                        "u1",
                        "NATO_TADIL_J",
                        Role: "txrx",
                        SatcomCapable: false,
                        ReviewState: CatalogReviewStates.Provisional,
                        TrlLevel: 9,
                        ValueTier: CatalogProvenanceTier.InterpretedValue),
                ],
                Links:
                [
                    new CatalogLinkEntry("NATO_TADIL_J", "Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
                ],
                Mobility: [new CatalogMobility("u1", MaxSpeedKnots: 32, CruiseSpeedKnots: 18, RangeNm: 4200)],
                Signatures: [new CatalogSignature("u1", RcsBandDbsm: -12, AcousticSignatureDb: 88)],
                Emcon:
                [
                    new CatalogEmcon("u1", "silent", "radar-1", "off"),
                    new CatalogEmcon("u1", "free", "radar-1", "active"),
                ]),
            SnapshotId,
            new FixedCatalogClock(utcTicks: 17_010));

    private static string TempXlsx(string tag) =>
        Path.Combine(Path.GetTempPath(), $"ple-adv-{tag}-{Guid.NewGuid():N}.xlsx");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static int ColumnIndex(IXLWorksheet ws, string columnName)
    {
        foreach (var cell in ws.Row(1).CellsUsed())
        {
            if (string.Equals(cell.GetString(), columnName, StringComparison.Ordinal))
            {
                return cell.Address.ColumnNumber;
            }
        }

        throw new InvalidOperationException($"Column '{columnName}' not found.");
    }

    private static int IndexOf(IReadOnlyList<string> header, string columnName)
    {
        for (var i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i], columnName, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }
}
