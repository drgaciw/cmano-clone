using ClosedXML.Excel;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// Req-21 PLE-1.2 / S24-11: ClosedXML exports carry list validation on Emcon enum columns.
/// </summary>
public sealed class ClosedXmlValidationMetadataTests
{
    private const string SnapshotId = "phase-b-ux-fixture";

    [Fact]
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

    [Fact]
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

            Assert.NotEqual(XLAllowedValues.List, mobility.Cell(2, 1).GetDataValidation()!.AllowedValues);
            Assert.NotEqual(XLAllowedValues.List, signatures.Cell(2, 1).GetDataValidation()!.AllowedValues);
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
        Assert.NotNull(validation);
        Assert.Equal(XLAllowedValues.List, validation!.AllowedValues);
        Assert.True(validation.InCellDropdown);

        var listFormula = validation.Value ?? string.Empty;
        foreach (var value in expectedValues)
        {
            Assert.Contains(value, listFormula, StringComparison.Ordinal);
        }
    }

    private static int ColumnIndex(IXLWorksheet ws, string columnName)
    {
        var header = ws.Row(1);
        foreach (var cell in header.CellsUsed())
        {
            if (string.Equals(cell.GetString(), columnName, StringComparison.Ordinal))
            {
                return cell.Address.ColumnNumber;
            }
        }

        throw new InvalidOperationException($"Column '{columnName}' not found.");
    }

    private static int EnumValidationProbeRow(IXLWorksheet ws) => Math.Min(500, ws.LastRowUsed()?.RowNumber() ?? 2);

    private static PlatformWorkbook ExportPhaseB() =>
        new PlatformWorkbookExporter().Export(
            new PlatformCatalogExportData(
                Platforms: [new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0)],
                Sensors: [new CatalogSensorBinding("u1", "radar-1", 0.85)],
                Mounts: [new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32)],
                Loadouts: [new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true)],
                Magazines: [new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32)],
                Comms: [new CatalogCommsBinding("u1", "NATO_TADIL_J")],
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