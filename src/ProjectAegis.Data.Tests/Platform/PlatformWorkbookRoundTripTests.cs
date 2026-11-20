using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 acceptance PLE-1.* / PLE-2.1: the platform-editor export is deterministic and an
/// unedited round-trip produces an empty diff (so the importer stages no spurious changes).
/// Pure / in-memory — no SQLite, no spreadsheet library.
/// </summary>
public sealed class PlatformWorkbookRoundTripTests
{
    private const string SnapshotId = "baltic_patrol";

    private static PlatformCatalogExportData SampleData() => new(
        Platforms: new[]
        {
            new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
            new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
        },
        Sensors: new[]
        {
            new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85, CitationRef: "/sensor/1/"),
            new CatalogSensorBinding("u1", "cmo-sensor-2", 0.40),
        },
        Mounts: new[]
        {
            new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32),
            new CatalogMount("u1", "gun-1", "gun", 270.0, 1),
        },
        Loadouts: new[]
        {
            new CatalogLoadout("u1", "asuw-default", "ASuW Strike", "asuw", IsDefault: true),
        },
        Magazines: new[]
        {
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32),
        },
        Comms: new[]
        {
            new CatalogCommsBinding("u1", "NATO_TADIL_J", "txrx", SatcomCapable: false),
        },
        Links: new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
            new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, LatencyMsNominal: 250),
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(utcTicks: 0));

    [Test]
    public void Export_is_deterministic()
    {
        var a = CanonicalTextWorkbookIo.Serialize(Export(SampleData()));
        var b = CanonicalTextWorkbookIo.Serialize(Export(SampleData()));
        Assert.That(b, Is.EqualTo(a));
    }

    [Test]
    public void CatalogSortKey_export_sheet_row_order_matches_comparer()
    {
        var data = SampleData();
        var workbook = Export(data);

        Assert.That(            MountKeys(workbook), Is.EqualTo(            CatalogSortKeyComparer.SortMounts(data.Mounts).Select(CatalogSortKeyComparer.FormatMountKey).ToArray()));

        Assert.That(            LoadoutKeys(workbook), Is.EqualTo(            CatalogSortKeyComparer.SortLoadouts(data.Loadouts).Select(CatalogSortKeyComparer.FormatLoadoutKey).ToArray()));

        Assert.That(            MagazineKeys(workbook), Is.EqualTo(            CatalogSortKeyComparer.SortMagazines(data.Magazines).Select(CatalogSortKeyComparer.FormatMagazineKey).ToArray()));

        Assert.That(            CommsKeys(workbook), Is.EqualTo(            CatalogSortKeyComparer.SortComms(data.Comms).Select(CatalogSortKeyComparer.FormatCommsKey).ToArray()));

        Assert.That(            LinkKeys(workbook), Is.EqualTo(            CatalogSortKeyComparer.SortLinks(data.Links ?? Array.Empty<CatalogLinkEntry>())
                .Select(CatalogSortKeyComparer.FormatLinkKey)
                .ToArray()));
    }

    [Test]
    public void Meta_sheet_binds_snapshot_and_content_hash()
    {
        var workbook = Export(SampleData());
        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.That(meta, Is.Not.Null);

        Assert.That(MetaValue(meta!, "SourceSnapshotId"), Is.EqualTo(SnapshotId));
        Assert.That(MetaValue(meta!, "SchemaVersion"), Is.EqualTo(PlatformWorkbookExporter.SchemaVersion));
        Assert.That(MetaValue(meta!, "WorkbookHash"), Is.EqualTo(PlatformWorkbookHash.Compute(workbook)));
    }

    [Test]
    public void Unedited_round_trip_yields_empty_diff()
    {
        var original = Export(SampleData());
        var roundTripped = CanonicalTextWorkbookIo.Deserialize(CanonicalTextWorkbookIo.Serialize(original));

        Assert.That(PlatformWorkbookDiff.IsEmpty(original, roundTripped), Is.True);
    }

    [Test]
    public void ClosedXml_unedited_round_trip_matches_canonical_empty_diff_contract()
    {
        var original = Export(SampleData());
        var path = Path.Combine(Path.GetTempPath(), $"platform-rt-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            var roundTripped = io.Read(path);
            Assert.That(PlatformWorkbookDiff.IsEmpty(original, roundTripped), Is.True);
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
    public void Edited_cell_is_detected_as_a_single_change()
    {
        var original = Export(SampleData());

        // Edit one magazine quantity (16 -> 24) as an author would in Excel.
        var edited = SampleData() with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 24, 0, 32),
            },
        };

        var changes = PlatformWorkbookDiff.Compare(original, Export(edited));
        var change = AssertEx.Single(changes);
        Assert.That(change.Sheet, Is.EqualTo("Magazines"));
        Assert.That(change.Kind, Is.EqualTo(PlatformWorkbookChangeKind.CellChanged));
        Assert.That(change.Detail, Does.Contain("Quantity"));
    }

    private static string MetaValue(PlatformWorkbookSheet meta, string key)
    {
        foreach (var row in meta.Rows)
        {
            if (row.Count >= 2 && string.Equals(row[0], key, StringComparison.Ordinal))
            {
                return row[1];
            }
        }

        return string.Empty;
    }

    private static string[] MountKeys(PlatformWorkbook workbook) =>
        SheetCompositeKeys(workbook, "Mounts", 0, 1);

    private static string[] LoadoutKeys(PlatformWorkbook workbook) =>
        SheetCompositeKeys(workbook, "Loadouts", 0, 1);

    private static string[] MagazineKeys(PlatformWorkbook workbook) =>
        SheetCompositeKeys(workbook, "Magazines", 0, 1, 2, 3);

    private static string[] CommsKeys(PlatformWorkbook workbook) =>
        SheetCompositeKeys(workbook, "Comms", 0, 1);

    private static string[] LinkKeys(PlatformWorkbook workbook) =>
        SheetCompositeKeys(workbook, "LinkCatalog", 0);

    private static string[] SheetCompositeKeys(PlatformWorkbook workbook, string sheetName, params int[] columns)
    {
        var sheet = workbook.FindSheet(sheetName);
        Assert.That(sheet, Is.Not.Null);
        return sheet!.Rows
            .Select(row => string.Join('\t', columns.Select(i => row[i])))
            .ToArray();
    }
}
