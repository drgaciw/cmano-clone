using ClosedXML.Excel;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// Req-21 PLE-1.2 / S24-11 / PE-W1: ClosedXML exports carry list validation on enum columns
/// (Emcon + full matrix) and OQ5 best-effort sheet/PK protection.
/// </summary>
public sealed class ClosedXmlValidationMetadataTests
{
    private const string SnapshotId = "phase-b-ux-fixture";

    /// <summary>Minimum known enum columns that must receive list validation after export.</summary>
    private const int MinimumEnumColumnsWithListValidation = 12;

    [Test]
    public void Emcon_sheet_has_Condition_and_Posture_list_validation()
    {
        var workbook = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-emcon-dv-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var ws = wb.Worksheet(PlatformEmconEnums.EmconSheetName);

            var conditionCol = ColumnIndex(ws, PlatformEmconEnums.ConditionColumn);
            var postureCol = ColumnIndex(ws, PlatformEmconEnums.PostureColumn);

            AssertListValidation(ws.Cell(2, conditionCol), PlatformEmconEnums.Conditions);
            AssertListValidation(ws.Cell(2, postureCol), PlatformEmconEnums.Postures);
            AssertListValidation(ws.Cell(EnumValidationProbeRow(ws), conditionCol), PlatformEmconEnums.Conditions);
            AssertListValidation(ws.Cell(EnumValidationProbeRow(ws), postureCol), PlatformEmconEnums.Postures);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Enum_catalog_columns_have_list_validation_after_export()
    {
        var workbook = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-enum-matrix-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var validated = new List<(string Sheet, string Column)>();

            foreach (var entry in PlatformWorkbookEnumCatalog.Columns)
            {
                if (!wb.Worksheets.TryGetWorksheet(entry.SheetName, out var ws))
                {
                    continue;
                }

                if (!TryColumnIndex(ws, entry.ColumnName, out var col))
                {
                    continue;
                }

                AssertListValidation(ws.Cell(2, col), entry.AllowedValues);
                validated.Add((entry.SheetName, entry.ColumnName));
            }

            Assert.That(
                validated.Count >= MinimumEnumColumnsWithListValidation,
                Is.True,
                $"Expected at least {MinimumEnumColumnsWithListValidation} enum columns with list validation, got {validated.Count}: "
                + string.Join(", ", validated.Select(v => $"{v.Sheet}.{v.Column}")));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Mobility_and_Signatures_sheets_do_not_add_Emcon_enum_validation()
    {
        var workbook = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-phase-b-dv-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var mobility = wb.Worksheet("Mobility");
            var signatures = wb.Worksheet("Signatures");

            Assert.That(mobility.Cell(2, 1).GetDataValidation()!.AllowedValues, Is.Not.EqualTo(XLAllowedValues.List));
            Assert.That(signatures.Cell(2, 1).GetDataValidation()!.AllowedValues, Is.Not.EqualTo(XLAllowedValues.List));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Meta_sheet_is_protected_after_export()
    {
        var workbook = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-meta-protect-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var meta = wb.Worksheet(PlatformWorkbookHash.MetaSheetName);
            Assert.That(meta.IsProtected, Is.True, "OQ5: _Meta worksheet should be protected after export");
            Assert.That(meta.Cell(1, 1).Style.Protection.Locked, Is.True, "OQ5: _Meta Key header cell remains locked");
            Assert.That(meta.Cell(2, 1).Style.Protection.Locked, Is.True, "OQ5: _Meta Key data cell remains locked");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Primary_key_columns_are_locked_on_protected_data_sheets()
    {
        var workbook = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-pk-lock-{Guid.NewGuid():N}.xlsx");
        try
        {
            new ClosedXmlPlatformWorkbookIo().Write(workbook, path);

            using var wb = new XLWorkbook(path);
            var mounts = wb.Worksheet("Mounts");
            Assert.That(mounts.IsProtected, Is.True, "data sheets with PK columns should be protected");

            var platformIdCol = ColumnIndex(mounts, "PlatformId");
            var mountIdCol = ColumnIndex(mounts, "MountId");
            var mountTypeCol = ColumnIndex(mounts, "MountType");

            Assert.That(mounts.Cell(2, platformIdCol).Style.Protection.Locked, Is.True);
            Assert.That(mounts.Cell(2, mountIdCol).Style.Protection.Locked, Is.True);
            Assert.That(
                mounts.Cell(2, mountTypeCol).Style.Protection.Locked,
                Is.False,
                "non-PK columns should remain unlocked so editors can change values under soft protect");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static void AssertListValidation(IXLCell cell, IReadOnlyList<string> expectedValues)
    {
        var validation = cell.GetDataValidation();
        Assert.That(validation, Is.Not.Null);
        Assert.That(validation.AllowedValues, Is.EqualTo(XLAllowedValues.List));
        Assert.That(validation.InCellDropdown, Is.True);

        var listFormula = validation.Value ?? string.Empty;
        foreach (var value in expectedValues)
        {
            Assert.That(listFormula.Contains(value, StringComparison.Ordinal), Is.True);
        }
    }

    private static int ColumnIndex(IXLWorksheet ws, string columnName)
    {
        if (!TryColumnIndex(ws, columnName, out var index))
        {
            throw new InvalidOperationException($"Column '{columnName}' not found.");
        }

        return index;
    }

    private static bool TryColumnIndex(IXLWorksheet ws, string columnName, out int columnIndex)
    {
        var header = ws.Row(1);
        foreach (var cell in header.CellsUsed())
        {
            if (string.Equals(cell.GetString(), columnName, StringComparison.Ordinal))
            {
                columnIndex = cell.Address.ColumnNumber;
                return true;
            }
        }

        columnIndex = -1;
        return false;
    }

    private static int EnumValidationProbeRow(IXLWorksheet ws) => Math.Min(500, ws.LastRowUsed()?.RowNumber() ?? 2);

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
            new FixedCatalogClock(utcTicks: 17_000));
}
