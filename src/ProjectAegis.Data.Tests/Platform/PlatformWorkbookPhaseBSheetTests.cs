using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S23-05: Phase B export-only sheet stubs (Mobility, Signatures, Emcon).
/// Import wiring deferred to Sprint 24.
/// </summary>
public sealed class PlatformWorkbookPhaseBSheetTests
{
    private const string SnapshotId = "baltic_patrol";

    private static readonly string[] PhaseADataSheetOrder =
    [
        "Platforms",
        "Sensors",
        "Mounts",
        "Loadouts",
        "Magazines",
        "Comms",
    ];

    private static readonly string[] ExpectedMobilityHeader =
    [
        "PlatformId", "MaxSpeedKnots", "CruiseSpeedKnots", "MaxAltitudeFt", "MaxDepthM",
        "FuelCapacity", "RangeNm", "EnduranceHr",
    ];

    private static readonly string[] ExpectedSignaturesHeader =
    [
        "PlatformId", "RcsBandDbsm", "IrSignature", "AcousticSignatureDb", "MagneticSignature",
    ];

    private static readonly string[] ExpectedEmconHeader =
    [
        "PlatformId", "Condition", "EmitterId", "Posture",
    ];

    private static PlatformCatalogExportData SampleData() => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85) },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: 30, CruiseSpeedKnots: 18, RangeNm: 4000) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: -10, AcousticSignatureDb: 95) },
        Emcon: new[]
        {
            new CatalogEmcon("u1", "silent", "cmo-sensor-1", "off"),
            new CatalogEmcon("u1", "free", "cmo-sensor-1", "active"),
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(utcTicks: 0));

    [Fact]
    public void Export_includes_Phase_B_stub_sheets_with_Req21_headers()
    {
        var workbook = Export(PlatformCatalogExportData.Empty);

        var mobility = workbook.FindSheet("Mobility");
        var signatures = workbook.FindSheet("Signatures");
        var emcon = workbook.FindSheet("Emcon");

        Assert.NotNull(mobility);
        Assert.NotNull(signatures);
        Assert.NotNull(emcon);

        Assert.Equal(ExpectedMobilityHeader, mobility!.Header);
        Assert.Equal(ExpectedSignaturesHeader, signatures!.Header);
        Assert.Equal(ExpectedEmconHeader, emcon!.Header);

        Assert.Empty(mobility.Rows);
        Assert.Empty(signatures.Rows);
        Assert.Empty(emcon.Rows);
    }

    [Fact]
    public void Phase_A_sheet_order_is_unchanged_before_Phase_B_stubs()
    {
        var dataSheetNames = Export(PlatformCatalogExportData.Empty).Sheets
            .Where(s => !string.Equals(s.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            .Select(s => s.Name)
            .ToArray();

        var phaseAEnd = Array.IndexOf(dataSheetNames, "Comms");
        Assert.Equal(PhaseADataSheetOrder, dataSheetNames.Take(phaseAEnd + 1).ToArray());
        Assert.Equal(["Mobility", "Signatures", "Emcon"], dataSheetNames.Skip(phaseAEnd + 1).ToArray());
    }

    [Fact]
    public void Phase_B_rows_export_with_stable_OrderBy_keys()
    {
        var workbook = Export(SampleData());

        var mobility = workbook.FindSheet("Mobility")!;
        Assert.Single(mobility.Rows);
        Assert.Equal("u1", mobility.Rows[0][0]);
        Assert.Equal("30", mobility.Rows[0][1]);

        var signatures = workbook.FindSheet("Signatures")!;
        Assert.Single(signatures.Rows);
        Assert.Equal("-10", signatures.Rows[0][1]);

        var emcon = workbook.FindSheet("Emcon")!;
        Assert.Equal(2, emcon.Rows.Count);
        Assert.Equal("free", emcon.Rows[0][1]);
        Assert.Equal("silent", emcon.Rows[1][1]);
    }

    [Fact]
    public void Unedited_Phase_B_stub_sheets_do_not_produce_spurious_diff_entries()
    {
        var original = Export(PlatformCatalogExportData.Empty);
        var roundTripped = CanonicalTextWorkbookIo.Deserialize(CanonicalTextWorkbookIo.Serialize(original));

        Assert.True(PlatformWorkbookDiff.IsEmpty(original, roundTripped));
        Assert.Empty(PlatformWorkbookDiff.Compare(original, roundTripped));
    }

    [Fact]
    public void Meta_sheet_reports_schema_version_008()
    {
        var meta = Export(PlatformCatalogExportData.Empty).FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.NotNull(meta);
        Assert.Contains(meta!.Rows, row => row.Count >= 2
            && string.Equals(row[0], "SchemaVersion", StringComparison.Ordinal)
            && string.Equals(row[1], "008", StringComparison.Ordinal));
    }

    [Fact]
    public void Migration_008_applies_idempotently()
    {
        var jsonPath = CatalogJsonImporter.ResolveBalticSensorsJsonPath();
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-{Guid.NewGuid():N}.db");
        try
        {
            CatalogJsonImporter.ImportToSqlite(jsonPath, dbPath);
            AssertTableExists(dbPath, "platform_mobility");
            AssertTableExists(dbPath, "platform_signature");
            AssertTableExists(dbPath, "platform_emcon");
            AssertTableExists(dbPath, "catalog_staging_mobility");

            using (var reader = new SqliteCatalogReader(dbPath, "phase-b-idempotent"))
            {
                _ = reader.GetSortedSensorBindings();
            }

            AssertTableExists(dbPath, "platform_mobility");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    private static void AssertTableExists(string dbPath, string table)
    {
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$name";
        cmd.Parameters.AddWithValue("$name", table);
        Assert.Equal(1, Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture));
    }
}