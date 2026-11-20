using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

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
        "LinkCatalog",
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

    private static readonly string[] ExpectedSwarmsHeader =
    [
        "PlatformId", "IsSwarm", "MaxDrones", "ArmorClass", "DefaultSensorId", "DefaultWeaponId",
        "DefaultMode", "RequiresHost", "AllowedHostClasses", "CecCapable",
        "ReviewState", "TrlLevel", "ValueTier", "CitationRef",
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

    [Test]
    public void Export_includes_Phase_B_stub_sheets_with_Req21_headers()
    {
        var workbook = Export(PlatformCatalogExportData.Empty);

        var mobility = workbook.FindSheet("Mobility");
        var signatures = workbook.FindSheet("Signatures");
        var emcon = workbook.FindSheet("Emcon");

        var swarms = workbook.FindSheet("Swarms");
        Assert.That(mobility, Is.Not.Null);
        Assert.That(signatures, Is.Not.Null);
        Assert.That(emcon, Is.Not.Null);
        Assert.That(swarms, Is.Not.Null);

        Assert.That(mobility!.Header, Is.EqualTo(ExpectedMobilityHeader));
        Assert.That(signatures!.Header, Is.EqualTo(ExpectedSignaturesHeader));
        Assert.That(emcon!.Header, Is.EqualTo(ExpectedEmconHeader));
        Assert.That(swarms!.Header, Is.EqualTo(ExpectedSwarmsHeader));

        Assert.That(mobility.Rows, Is.Empty);
        Assert.That(signatures.Rows, Is.Empty);
        Assert.That(emcon.Rows, Is.Empty);
        Assert.That(swarms.Rows, Is.Empty);
    }

    [Test]
    public void Phase_A_sheet_order_is_unchanged_before_Phase_B_stubs()
    {
        var dataSheetNames = Export(PlatformCatalogExportData.Empty).Sheets
            .Where(s => !string.Equals(s.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            .Select(s => s.Name)
            .ToArray();

        var phaseAEnd = Array.IndexOf(dataSheetNames, "LinkCatalog");
        Assert.That(dataSheetNames.Take(phaseAEnd + 1).ToArray(), Is.EqualTo(PhaseADataSheetOrder));
        Assert.That(dataSheetNames.Skip(phaseAEnd + 1).ToArray(), Is.EqualTo(["Mobility", "Signatures", "Emcon", "Swarms"]));
    }

    [Test]
    public void Phase_B_rows_export_with_stable_OrderBy_keys()
    {
        var workbook = Export(SampleData());

        var mobility = workbook.FindSheet("Mobility")!;
        Assert.That(mobility.Rows, Has.Exactly(1).Items);
        Assert.That(mobility.Rows[0][0], Is.EqualTo("u1"));
        Assert.That(mobility.Rows[0][1], Is.EqualTo("30"));

        var signatures = workbook.FindSheet("Signatures")!;
        Assert.That(signatures.Rows, Has.Exactly(1).Items);
        Assert.That(signatures.Rows[0][1], Is.EqualTo("-10"));

        var emcon = workbook.FindSheet("Emcon")!;
        Assert.That(emcon.Rows.Count, Is.EqualTo(2));
        Assert.That(emcon.Rows[0][1], Is.EqualTo("free"));
        Assert.That(emcon.Rows[1][1], Is.EqualTo("silent"));
    }

    [Test]
    public void Unedited_Phase_B_stub_sheets_do_not_produce_spurious_diff_entries()
    {
        var original = Export(PlatformCatalogExportData.Empty);
        var roundTripped = CanonicalTextWorkbookIo.Deserialize(CanonicalTextWorkbookIo.Serialize(original));

        Assert.That(PlatformWorkbookDiff.IsEmpty(original, roundTripped), Is.True);
        Assert.That(PlatformWorkbookDiff.Compare(original, roundTripped), Is.Empty);
    }

    [Test]
    public void Meta_sheet_reports_schema_version_010()
    {
        var meta = Export(PlatformCatalogExportData.Empty).FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.That(meta, Is.Not.Null);
        Assert.That(meta!.Rows.Any(row => row.Count >= 2
            && string.Equals(row[0], "SchemaVersion", StringComparison.Ordinal)
            && string.Equals(row[1], "010", StringComparison.Ordinal)), Is.True);
    }

    [Test]
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
        Assert.That(Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo(1));
    }
}