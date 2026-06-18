using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

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
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(utcTicks: 0));

    [Fact]
    public void Export_is_deterministic()
    {
        var a = CanonicalTextWorkbookIo.Serialize(Export(SampleData()));
        var b = CanonicalTextWorkbookIo.Serialize(Export(SampleData()));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Meta_sheet_binds_snapshot_and_content_hash()
    {
        var workbook = Export(SampleData());
        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.NotNull(meta);

        Assert.Equal(SnapshotId, MetaValue(meta!, "SourceSnapshotId"));
        Assert.Equal(PlatformWorkbookExporter.SchemaVersion, MetaValue(meta!, "SchemaVersion"));
        Assert.Equal(PlatformWorkbookHash.Compute(workbook), MetaValue(meta!, "WorkbookHash"));
    }

    [Fact]
    public void Unedited_round_trip_yields_empty_diff()
    {
        var original = Export(SampleData());
        var roundTripped = CanonicalTextWorkbookIo.Deserialize(CanonicalTextWorkbookIo.Serialize(original));

        Assert.True(PlatformWorkbookDiff.IsEmpty(original, roundTripped));
    }

    [Fact]
    public void ClosedXml_unedited_round_trip_matches_canonical_empty_diff_contract()
    {
        var original = Export(SampleData());
        var path = Path.Combine(Path.GetTempPath(), $"platform-rt-{Guid.NewGuid():N}.xlsx");
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

    [Fact]
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
        var change = Assert.Single(changes);
        Assert.Equal("Magazines", change.Sheet);
        Assert.Equal(PlatformWorkbookChangeKind.CellChanged, change.Kind);
        Assert.Contains("Quantity", change.Detail);
    }

    private static string MetaValue(PlatformWorkbookSheet meta, string key)
    {
        foreach (var row in meta.Rows)
        {
            if (row.Count >= 2 && string.Equals(row[0], key, System.StringComparison.Ordinal))
            {
                return row[1];
            }
        }

        return string.Empty;
    }
}
