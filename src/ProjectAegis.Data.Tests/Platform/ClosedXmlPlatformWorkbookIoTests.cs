using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S23-01: integration tests for binary <c>.xlsx</c> round-trip via
/// <see cref="ClosedXmlPlatformWorkbookIo"/>. Proves PLE-2.1 empty-diff golden and parity with
/// <see cref="CanonicalTextWorkbookIo"/>.
/// </summary>
public sealed class ClosedXmlPlatformWorkbookIoTests
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
    public void ClosedXml_unedited_round_trip_yields_empty_diff()
    {
        var original = Export(SampleData());
        var path = Path.Combine(Path.GetTempPath(), $"ple-21-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            Assert.True(IsZipArchive(path), "export must emit a binary .xlsx (ZIP) workbook");

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
    public void ClosedXml_meta_snapshot_and_workbook_hash_survive_binary_round_trip()
    {
        var original = Export(SampleData());
        var path = Path.Combine(Path.GetTempPath(), $"ple-meta-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            var roundTripped = io.Read(path);
            var meta = roundTripped.FindSheet(PlatformWorkbookHash.MetaSheetName);
            Assert.NotNull(meta);

            Assert.Equal(SnapshotId, MetaValue(meta!, "SourceSnapshotId"));
            Assert.Equal(PlatformWorkbookExporter.SchemaVersion, MetaValue(meta!, "SchemaVersion"));
            Assert.Equal(PlatformWorkbookHash.Compute(original), MetaValue(meta!, "WorkbookHash"));
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
    public void ClosedXml_text_format_prevents_numeric_coercion()
    {
        var workbook = Export(SampleData());
        var path = Path.Combine(Path.GetTempPath(), $"ple-text-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(workbook, path);
            var roundTripped = io.Read(path);
            var platforms = roundTripped.FindSheet("Platforms");
            Assert.NotNull(platforms);
            var row = Assert.Single(platforms!.Rows.Where(r => r.Count > 0 && r[0] == "u1"));
            Assert.Equal("57", row[1]);
            Assert.Equal("20", row[2]);
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
    public void ClosedXml_and_canonical_produce_same_logical_diff_for_edited_workbook()
    {
        var original = Export(SampleData());
        var edited = Export(SampleData() with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 24, 0, 32),
            },
        });

        var canonicalPath = Path.Combine(Path.GetTempPath(), $"ple-canonical-{Guid.NewGuid():N}.platform.txt");
        var xlsxPath = Path.Combine(Path.GetTempPath(), $"ple-xlsx-{Guid.NewGuid():N}.xlsx");
        try
        {
            var canonicalIo = new CanonicalTextWorkbookIo();
            var closedXmlIo = new ClosedXmlPlatformWorkbookIo();
            canonicalIo.Write(edited, canonicalPath);
            closedXmlIo.Write(edited, xlsxPath);

            var canonicalRoundTrip = canonicalIo.Read(canonicalPath);
            var closedXmlRoundTrip = closedXmlIo.Read(xlsxPath);

            var canonicalChanges = PlatformWorkbookDiff.Compare(original, canonicalRoundTrip);
            var closedXmlChanges = PlatformWorkbookDiff.Compare(original, closedXmlRoundTrip);

            Assert.Equal(canonicalChanges.Count, closedXmlChanges.Count);
            Assert.Equal(canonicalChanges, closedXmlChanges);
        }
        finally
        {
            if (File.Exists(canonicalPath))
            {
                File.Delete(canonicalPath);
            }

            if (File.Exists(xlsxPath))
            {
                File.Delete(xlsxPath);
            }
        }
    }

    [Fact]
    public void IoSelection_defaults_to_closedxml_for_xlsx_paths()
    {
        var io = PlatformWorkbookIoSelection.Resolve("export.xlsx", ioFlag: null, PlatformWorkbookIoFactories.ClosedXml);
        Assert.IsType<ClosedXmlPlatformWorkbookIo>(io);
    }

    [Fact]
    public void IoSelection_canonical_flag_uses_text_adapter()
    {
        var io = PlatformWorkbookIoSelection.Resolve("export.xlsx", PlatformWorkbookIoSelection.CanonicalFlag, PlatformWorkbookIoFactories.ClosedXml);
        Assert.IsType<CanonicalTextWorkbookIo>(io);
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

    private static bool IsZipArchive(string path)
    {
        using var stream = File.OpenRead(path);
        return stream.Length >= 2
            && stream.ReadByte() == 0x50
            && stream.ReadByte() == 0x4B;
    }
}