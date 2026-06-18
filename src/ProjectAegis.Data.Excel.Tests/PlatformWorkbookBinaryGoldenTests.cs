using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Excel.Tests;

/// <summary>
/// Req-21 / S24-11: binary <c>.xlsx</c> golden harness for Phase B sheets (Mobility/Signatures/Emcon).
/// Pins export hash stability and proves edited Emcon cells round-trip through ClosedXML with a
/// deterministic importer diff.
/// </summary>
public sealed class PlatformWorkbookBinaryGoldenTests
{
    private const string SnapshotId = "phase-b-binary-golden";

    // Pinned logical workbook hash for ExportPhaseB() @ FixedCatalogClock(17_001).
    private const string PhaseBWorkbookHashGolden =
        "6bb2776e12fd90541c097f593c4bab41a348d2d168cbc6f51df49bb4f89275cb";

    [Fact]
    public void Phase_B_binary_round_trip_preserves_workbook_hash_golden()
    {
        var workbook = ExportPhaseB();
        Assert.Equal(PhaseBWorkbookHashGolden, PlatformWorkbookHash.Compute(workbook));

        var path = Path.Combine(Path.GetTempPath(), $"ple-phase-b-golden-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(workbook, path);
            Assert.True(IsZipArchive(path), "export must emit a binary .xlsx (ZIP) workbook");

            var roundTripped = io.Read(path);
            Assert.Equal(PhaseBWorkbookHashGolden, PlatformWorkbookHash.Compute(roundTripped));
            Assert.True(PlatformWorkbookDiff.IsEmpty(workbook, roundTripped));
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
    public void Phase_B_edited_emcon_posture_round_trips_with_deterministic_diff()
    {
        var source = ExportPhaseBData();
        var original = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(17_002));
        var edited = WithEmconPosture(original, rowIndex: 0, posture: "standby");

        var path = Path.Combine(Path.GetTempPath(), $"ple-phase-b-edit-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(edited, path);
            var roundTripped = io.Read(path);

            Assert.False(PlatformWorkbookDiff.IsEmpty(original, roundTripped));

            var plan = new PlatformWorkbookImporter(
                id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null,
                new FixedCatalogClock(17_003)).Plan(roundTripped);

            Assert.True(plan.SnapshotResolved);
            Assert.True(plan.HasChanges);
            var change = Assert.Single(plan.SupportedChanges);
            Assert.Equal("Emcon", change.Sheet);
            Assert.Equal(PlatformWorkbookChangeKind.CellChanged, change.Kind);
            Assert.Contains("standby", change.Detail, StringComparison.OrdinalIgnoreCase);
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
    public void Phase_B_unedited_binary_round_trip_yields_empty_diff()
    {
        var original = ExportPhaseB();
        var path = Path.Combine(Path.GetTempPath(), $"ple-phase-b-rt-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            var roundTripped = io.Read(path);
            Assert.True(PlatformWorkbookDiff.IsEmpty(original, roundTripped));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private static PlatformWorkbook ExportPhaseB() =>
        new PlatformWorkbookExporter().Export(ExportPhaseBData(), SnapshotId, new FixedCatalogClock(17_001));

    private static PlatformCatalogExportData ExportPhaseBData() => new(
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
        ],
        Damage: [new CatalogPlatformDamage("u1")]);

    private static PlatformWorkbook WithEmconPosture(PlatformWorkbook workbook, int rowIndex, string posture)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, PlatformEmconEnums.EmconSheetName, StringComparison.Ordinal))
            {
                return sheet;
            }

            var postureCol = Array.IndexOf(sheet.Header.ToArray(), PlatformEmconEnums.PostureColumn);
            Assert.True(postureCol >= 0);

            var rows = sheet.Rows.Select((row, i) =>
            {
                if (i != rowIndex)
                {
                    return row;
                }

                var cells = row.ToList();
                while (cells.Count <= postureCol)
                {
                    cells.Add(string.Empty);
                }

                cells[postureCol] = posture;
                return (IReadOnlyList<string>)cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return workbook with { Sheets = sheets };
    }

    private static bool IsZipArchive(string path)
    {
        using var stream = File.OpenRead(path);
        return stream.Length >= 2
            && stream.ReadByte() == 0x50
            && stream.ReadByte() == 0x4B;
    }
}