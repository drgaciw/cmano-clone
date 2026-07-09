using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Osint;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Catalog;

[Collection("CatalogSqlite")]
public sealed class CatalogTlExportFilterTests
{
    private static readonly CatalogSensorBinding Tl0Sensor = new(
        "u-fielded",
        "sensor-tl0",
        0.75,
        ImportBatchId: "nightly-cmo-20261001",
        TrlLevel: 9,
        ValueTier: CatalogProvenanceTier.SourceFact);

    private static readonly CatalogSensorBinding Tl2Sensor = new(
        "u-nearfuture",
        "sensor-tl2",
        0.55,
        ImportBatchId: OsintCatalogMapper.BranchTagPrefix + "09",
        TrlLevel: 6,
        ValueTier: CatalogProvenanceTier.InterpretedValue);

    private static readonly CatalogSensorBinding Tl5Sensor = new(
        "u-speculative",
        "sensor-tl5",
        0.35,
        ImportBatchId: OsintCatalogMapper.BranchTagPrefix + "10",
        TrlLevel: 8,
        ValueTier: CatalogProvenanceTier.InterpretedValue);

    [Fact]
    public void CatalogTlTierResolver_maps_provenance_bands_to_tl_labels()
    {
        Assert.Equal(CatalogTlTier.Tl0, CatalogTlTierResolver.ResolveFromSensor(Tl0Sensor));
        Assert.Equal(CatalogTlTier.Tl2, CatalogTlTierResolver.ResolveFromSensor(Tl2Sensor));
        Assert.Equal(CatalogTlTier.Tl5, CatalogTlTierResolver.ResolveFromSensor(Tl5Sensor));
        Assert.Equal(CatalogTlTier.Tl3, CatalogTlTierResolver.ResolveFromPlatform(3));
    }

    [Fact]
    public void Filtered_export_keeps_records_at_or_below_requested_tier()
    {
        var data = SampleExportData();
        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl2);

        Assert.Equal(
            ["u-fielded", "u-nearfuture"],
            filtered.Sensors.Select(s => s.PlatformId).ToArray());
        Assert.Equal(
            ["sensor-tl0", "sensor-tl2"],
            filtered.Sensors.Select(s => s.SensorId).ToArray());
        Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "sensor-tl5");
    }

    /// <summary>
    /// PLE-5.3 / DBI-6.3: provisional (non-approved) sensors edited into export data are excluded from
    /// sim-visible <see cref="CatalogTlExportFilter.Apply"/> output until promoted to approved.
    /// </summary>
    [Fact]
    public void Filtered_export_excludes_provisional_non_approved_sensors_PLE_5_3()
    {
        var provisional = new CatalogSensorBinding(
            "u-provisional",
            "sensor-provisional",
            0.90,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.InterpretedValue,
            ImportBatchId: "excel-edit");
        var approved = Tl0Sensor;
        var data = new PlatformCatalogExportData(
            Platforms:
            [
                new CatalogPlatformEntry("u-fielded", 57.0, 20.0, 400),
                new CatalogPlatformEntry("u-provisional", 58.0, 21.0, 350),
            ],
            Sensors: [approved, provisional],
            Mounts: [],
            Loadouts: [],
            Magazines: [],
            Comms: []);

        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl5);

        Assert.Contains(filtered.Sensors, s => s.SensorId == "sensor-tl0");
        Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "sensor-provisional");
        Assert.DoesNotContain(filtered.Sensors, s =>
            string.Equals(s.ReviewState, CatalogReviewStates.Provisional, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// PLE-5.3 adversarial: provisional sensor stays excluded across multiple max TL ceilings
    /// (not only the permissive TL-5 slice).
    /// </summary>
    [Fact]
    public void Adversarial_provisional_sensor_excluded_at_multiple_max_tl_tiers()
    {
        var provisional = new CatalogSensorBinding(
            "u-fielded",
            "sensor-provisional-multi",
            0.95,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.SourceFact,
            ImportBatchId: "nightly-cmo-20261001");
        var data = SampleExportData() with
        {
            Sensors = [Tl0Sensor, provisional],
        };

        foreach (var tier in new[] { CatalogTlTier.Tl0, CatalogTlTier.Tl2, CatalogTlTier.Tl5 })
        {
            var filtered = CatalogTlExportFilter.Apply(data, tier);
            Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "sensor-provisional-multi");
            Assert.Contains(filtered.Sensors, s => s.SensorId == "sensor-tl0");
        }
    }

    /// <summary>PLE-5.3 adversarial: rejected review state is never sim-visible.</summary>
    [Fact]
    public void Adversarial_rejected_review_state_excluded_from_tl_export()
    {
        var rejected = new CatalogSensorBinding(
            "u-fielded",
            "sensor-rejected",
            0.99,
            ReviewState: CatalogReviewStates.Rejected,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.SourceFact,
            ImportBatchId: "nightly-cmo-20261001");
        var data = SampleExportData() with
        {
            Sensors = [Tl0Sensor, rejected],
        };

        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl5);
        Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "sensor-rejected");
        Assert.DoesNotContain(filtered.Sensors, s =>
            string.Equals(s.ReviewState, CatalogReviewStates.Rejected, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(filtered.Sensors, s => s.SensorId == "sensor-tl0");
    }

    /// <summary>PLE-5.3 adversarial control: approved sensor is retained when under the TL ceiling.</summary>
    [Fact]
    public void Adversarial_approved_sensor_retained_control()
    {
        var approved = new CatalogSensorBinding(
            "u-fielded",
            "sensor-approved-control",
            0.88,
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.SourceFact,
            ImportBatchId: "nightly-cmo-20261001");
        var data = SampleExportData() with
        {
            Sensors = [approved],
        };

        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl0);
        Assert.Contains(filtered.Sensors, s =>
            s.SensorId == "sensor-approved-control"
            && string.Equals(s.ReviewState, CatalogReviewStates.Approved, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Filtered_export_tl0_default_excludes_near_future_rows()
    {
        var data = SampleExportData();
        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl0);

        Assert.Single(filtered.Sensors);
        Assert.Equal("sensor-tl0", filtered.Sensors[0].SensorId);
    }

    [Fact]
    public void Filtered_export_empty_slice_when_ceiling_below_all_rows()
    {
        var data = SampleExportData() with
        {
            Sensors = [Tl5Sensor],
            Platforms =
            [
                new CatalogPlatformEntry("u-speculative", 0, 0, 100),
            ],
        };

        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl2);
        Assert.Empty(filtered.Sensors);
        Assert.Empty(filtered.Platforms);
    }

    [Fact]
    public void Tl_export_sort_keys_are_deterministic_across_repeated_runs()
    {
        var shuffled = new[]
        {
            Tl5Sensor,
            Tl0Sensor,
            Tl2Sensor,
        };

        var first = CatalogTlExportSortKey.SortSensors(shuffled).Select(CatalogSortKeyComparer.FormatSensorKey).ToArray();
        var second = CatalogTlExportSortKey.SortSensors(shuffled.Reverse()).Select(CatalogSortKeyComparer.FormatSensorKey).ToArray();

        Assert.Equal(first, second);
        Assert.Equal(
            [
                "u-fielded\tsensor-tl0",
                "u-nearfuture\tsensor-tl2",
                "u-speculative\tsensor-tl5",
            ],
            first);
    }

    [Fact]
    public void Tl_export_sort_key_orders_by_canonical_id_then_tl_tier_then_value_tier()
    {
        var rows = new[]
        {
            new CatalogSensorBinding("alpha", "s1", 0.5, ValueTier: CatalogProvenanceTier.InterpretedValue, ImportBatchId: "nightly"),
            new CatalogSensorBinding("alpha", "s1", 0.5, ValueTier: CatalogProvenanceTier.SourceFact, ImportBatchId: OsintCatalogMapper.BranchTagPrefix + "09", TrlLevel: 6),
        };

        var keys = CatalogTlExportSortKey.SortSensors(rows)
            .Select(row => CatalogTlExportSortKey.Format(
                CatalogSortKeyComparer.FormatSensorKey(row),
                CatalogTlTierResolver.ResolveFromSensor(row),
                row.ValueTier))
            .ToArray();

        Assert.Equal(
            [
                "alpha\ts1\t" + CatalogTlTier.Tl0 + "\t" + CatalogProvenanceTier.InterpretedValue,
                "alpha\ts1\t" + CatalogTlTier.Tl2 + "\t" + CatalogProvenanceTier.SourceFact,
            ],
            keys);
    }

    [Fact]
    public void SqliteCatalogReader_LoadExportData_honors_tl_tier_filter()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-tl-export-{Guid.NewGuid():N}.db");
        try
        {
            SeedTieredSensors(dbPath);

            using var reader = new SqliteCatalogReader(dbPath, "tl-export-test");
            var unfiltered = reader.LoadExportData();
            var tl2 = reader.LoadExportData(CatalogTlTier.Tl2);

            Assert.Equal(3, unfiltered.Sensors.Count);
            Assert.Equal(2, tl2.Sensors.Count);
            Assert.DoesNotContain(tl2.Sensors, s => s.SensorId == "sensor-tl5");
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void PlatformCatalogExportResolver_passes_tl_tier_filter_to_reader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-tl-resolver-{Guid.NewGuid():N}.db");
        try
        {
            var snapshotId = SeedTieredSensors(dbPath);

            Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, snapshotId, out var tl2, CatalogTlTier.Tl2));
            Assert.Equal(2, tl2.Sensors.Count);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void Platform_export_manifest_honors_requested_tl_tier_filter()
    {
        var exporter = new PlatformWorkbookExporter();
        var manifest = new CatalogExportManifest(
            DbVersion: "db-test",
            TlTier: CatalogTlTier.Tl2,
            SchemaVersion: CatalogTlTier.CatalogSchemaVersion,
            ContentHash: "hash",
            ExportSchemaVersion: CatalogTlTier.ExportManifestSchemaVersion);

        var filtered = CatalogTlExportFilter.Apply(SampleExportData(), CatalogTlTier.Tl2);
        var workbook = exporter.Export(filtered, "snap-tl2", new FixedCatalogClock(0), manifest);

        var meta = workbook.FindSheet(PlatformWorkbookHash.MetaSheetName);
        Assert.NotNull(meta);
        Assert.Equal(CatalogTlTier.Tl2, MetaValue(meta!, "TlTier"));
        Assert.Equal(2, workbook.FindSheet("Sensors")!.Rows.Count);
    }

    private static PlatformCatalogExportData SampleExportData() => new(
        Platforms:
        [
            new CatalogPlatformEntry("u-fielded", 57.0, 20.0, 400),
            new CatalogPlatformEntry("u-nearfuture", 58.0, 21.0, 350),
            new CatalogPlatformEntry("u-speculative", 59.0, 22.0, 300),
        ],
        Sensors: [Tl0Sensor, Tl2Sensor, Tl5Sensor],
        Mounts: [],
        Loadouts: [],
        Magazines: [],
        Comms: []);

    private static string SeedTieredSensors(string dbPath)
    {
        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9400));
        var batchId = gate.ProposeSensorBatch(
            [Tl0Sensor, Tl2Sensor, Tl5Sensor],
            "agent",
            "tl-export-filter-test");
        Assert.True(gate.ApproveBatch(batchId, "human", "qa").Committed);

        var bind = CatalogSnapshotBinder.BindAfterApprove(
            dbPath,
            batchId,
            new FixedCatalogClock(9401),
            tlTier: CatalogTlTier.Tl5);

        return bind.SnapshotId;
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

    private static void Cleanup(string dbPath)
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}