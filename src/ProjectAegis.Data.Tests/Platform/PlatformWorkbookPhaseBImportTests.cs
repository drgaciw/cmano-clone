using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S24-04: Phase B importer wiring — Mobility/Signatures/Emcon stage via write gate,
/// empty-diff golden on unedited round-trip, FK guard on PlatformId, E2E read-back via SqliteCatalogReader.
/// </summary>
[Collection("CatalogSqlite")]
public sealed class PlatformWorkbookPhaseBImportTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static PlatformCatalogExportData PhaseBData(
        double maxSpeedKnots = 30,
        double rcsBandDbsm = -10,
        string emconPosture = "off") => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85) },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: maxSpeedKnots, CruiseSpeedKnots: 18, RangeNm: 4000) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: rcsBandDbsm, AcousticSignatureDb: 95) },
        Emcon: new[] { new CatalogEmcon("u1", "silent", "cmo-sensor-1", emconPosture) });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterForDb(string dbPath) =>
        new(id =>
        {
            if (!string.Equals(id, SnapshotId, StringComparison.Ordinal))
            {
                return null;
            }

            return PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var data) ? data : null;
        }, new FixedCatalogClock(0));

    [Fact]
    public void Plan_unedited_Phase_B_round_trip_has_no_supported_changes()
    {
        var source = PhaseBData();
        var edited = Export(source);

        var plan = ImporterFor(source).Plan(edited);

        Assert.True(plan.SnapshotResolved);
        Assert.False(plan.HasChanges);
        Assert.Empty(plan.SupportedChanges);
        Assert.Empty(plan.UnsupportedChanges);
    }

    [Fact]
    public void Stage_unedited_Phase_B_round_trip_stages_nothing()
    {
        var source = PhaseBData();
        var edited = Export(source);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.False(result.Staged);
        Assert.Null(result.MobilityBatchId);
        Assert.Null(result.SignatureBatchId);
        Assert.Null(result.EmconBatchId);
        Assert.Empty(gate.MobilityProposals);
        Assert.Empty(gate.SignatureProposals);
        Assert.Empty(gate.EmconProposals);
    }

    [Fact]
    public void Stage_edited_mobility_proposes_mobility_batch()
    {
        var source = PhaseBData(maxSpeedKnots: 30);
        var edited = Export(PhaseBData(maxSpeedKnots: 35));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit mobility");

        Assert.True(result.Staged);
        Assert.NotNull(result.MobilityBatchId);
        var proposed = Assert.Single(gate.MobilityProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal(35, proposed[0].MaxSpeedKnots, precision: 6);
    }

    [Fact]
    public void Stage_edited_signature_proposes_signature_batch()
    {
        var source = PhaseBData(rcsBandDbsm: -10);
        var edited = Export(PhaseBData(rcsBandDbsm: -15));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit signature");

        Assert.True(result.Staged);
        Assert.NotNull(result.SignatureBatchId);
        var proposed = Assert.Single(gate.SignatureProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal(-15, proposed[0].RcsBandDbsm, precision: 6);
    }

    [Fact]
    public void Stage_edited_emcon_proposes_emcon_batch()
    {
        var source = PhaseBData(emconPosture: "off");
        var edited = Export(PhaseBData(emconPosture: "active"));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit emcon");

        Assert.True(result.Staged);
        Assert.NotNull(result.EmconBatchId);
        var proposed = Assert.Single(gate.EmconProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("active", proposed[0].Posture);
    }

    [Fact]
    public void Stage_orphan_platform_mobility_is_rejected_not_proposed()
    {
        var source = PhaseBData();
        var editedData = PhaseBData(maxSpeedKnots: 30) with
        {
            Mobility = new[] { new CatalogMobility("unknown-platform", MaxSpeedKnots: 99) },
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.False(result.Staged);
        Assert.True(result.Plan.Blocked);
        Assert.Empty(gate.MobilityProposals);
        Assert.Contains(result.Plan.Findings, f =>
            f.Code == PlatformWorkbookValidator.PhaseBOrphanPlatform
            && f.UnitId == "unknown-platform"
            && f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Stage_multiple_Phase_B_entity_types_proposes_all_batches()
    {
        var source = PhaseBData(maxSpeedKnots: 30, rcsBandDbsm: -10, emconPosture: "off");
        var edited = Export(PhaseBData(maxSpeedKnots: 35, rcsBandDbsm: -15, emconPosture: "active"));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit phase b");

        Assert.True(result.Staged);
        Assert.NotNull(result.MobilityBatchId);
        Assert.NotNull(result.SignatureBatchId);
        Assert.NotNull(result.EmconBatchId);
        Assert.NotEmpty(gate.MobilityProposals);
        Assert.NotEmpty(gate.SignatureProposals);
        Assert.NotEmpty(gate.EmconProposals);
    }

    [Fact]
    public void E2E_mobility_export_edit_stage_approve_readback_via_SqliteCatalogReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-e2e-mobility-{Guid.NewGuid():N}.db");
        try
        {
            SeedPhaseBDatabase(dbPath);
            Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var source));

            var exported = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(9600));
            var edited = WithSheetCell(exported, "Mobility", 0, "MaxSpeedKnots", "36");

            var importer = ImporterForDb(dbPath);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9601)))
            {
                var result = importer.Stage(edited, gate, "human", "drgamtd", "e2e mobility");
                Assert.True(result.Staged);
                Assert.NotNull(result.MobilityBatchId);
                Assert.True(gate.ApproveBatch(result.MobilityBatchId!, "human", "qa-reviewer").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-e2e-mobility");
            Assert.True(reader.TryGetMobility("u1", out var mobility));
            Assert.Equal(36, mobility.MaxSpeedKnots, precision: 3);
            Assert.Equal(4200, mobility.RangeNm, precision: 3);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void E2E_all_Phase_B_sheets_export_edit_stage_approve_readback_via_SqliteCatalogReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-e2e-all-{Guid.NewGuid():N}.db");
        try
        {
            SeedPhaseBDatabase(dbPath);
            Assert.True(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var source));

            var exported = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(9610));
            var edited = WithSheetCell(exported, "Mobility", 0, "MaxSpeedKnots", "36");
            edited = WithSheetCell(edited, "Signatures", 0, "RcsBandDbsm", "-18");
            edited = WithSheetCell(edited, "Emcon", 1, "Posture", "standby");

            var importer = ImporterForDb(dbPath);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9611)))
            {
                var result = importer.Stage(edited, gate, "human", "drgamtd", "e2e phase b all");
                Assert.True(result.Staged);
                Assert.NotNull(result.MobilityBatchId);
                Assert.NotNull(result.SignatureBatchId);
                Assert.NotNull(result.EmconBatchId);
                Assert.True(gate.ApproveBatch(result.MobilityBatchId!, "human", "qa-reviewer").Committed);
                Assert.True(gate.ApproveBatch(result.SignatureBatchId!, "human", "qa-reviewer").Committed);
                Assert.True(gate.ApproveBatch(result.EmconBatchId!, "human", "qa-reviewer").Committed);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-e2e-all");
            Assert.True(reader.TryGetMobility("u1", out var mobility));
            Assert.Equal(36, mobility.MaxSpeedKnots, precision: 3);

            Assert.True(reader.TryGetSignature("u1", out var signature));
            Assert.Equal(-18, signature.RcsBandDbsm, precision: 3);

            Assert.True(reader.TryGetEmcon("u1", "silent", "radar-1", out var emcon));
            Assert.Equal("standby", emcon.Posture);
            Assert.True(reader.TryGetEmcon("u1", "free", "radar-1", out var unchanged));
            Assert.Equal("active", unchanged.Posture);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Fact]
    public void ApproveBatch_mobility_batch_commits_to_live_table()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-mobility-{Guid.NewGuid():N}.db");
        try
        {
            SeedPlatform(dbPath, "u1");
            var mobility = new CatalogMobility("u1", MaxSpeedKnots: 35, CruiseSpeedKnots: 20, RangeNm: 5000);

            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9301)))
            {
                var batchId = gate.ProposeMobilityBatch([mobility], "human", "phase-b-test");
                var decision = gate.ApproveBatch(batchId, "human", "qa-reviewer");
                Assert.True(decision.Committed);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT max_speed_knots, range_nm FROM platform_mobility WHERE platform_id = 'u1'";
            using var reader = cmd.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(35, reader.GetDouble(0), precision: 6);
            Assert.Equal(5000, reader.GetDouble(1), precision: 6);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    private static void SeedPhaseBDatabase(string dbPath)
    {
        CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
        using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
        connection.Open();
        using var cmd = connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT OR REPLACE INTO platform_mobility
                (platform_id, max_speed_knots, cruise_speed_knots, range_nm, review_state, trl_level, value_tier, citation_ref)
            VALUES ('u1', 32, 18, 4200, 'approved', 9, 'interpreted_value', 'unit-test');
            INSERT OR REPLACE INTO platform_signature
                (platform_id, rcs_band_dbsm, acoustic_signature_db, review_state, trl_level, value_tier, citation_ref)
            VALUES ('u1', -12, 88, 'approved', 9, 'interpreted_value', 'unit-test');
            INSERT OR REPLACE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
            VALUES ('u1', 'free', 'radar-1', 'active', 'approved');
            INSERT OR REPLACE INTO platform_emcon (platform_id, condition, emitter_id, posture, review_state)
            VALUES ('u1', 'silent', 'radar-1', 'off', 'approved');
            """;
        cmd.ExecuteNonQuery();
    }

    private static PlatformWorkbook WithSheetCell(
        PlatformWorkbook workbook,
        string sheetName,
        int rowIndex,
        string columnName,
        string value)
    {
        var sheets = workbook.Sheets.Select(sheet =>
        {
            if (!string.Equals(sheet.Name, sheetName, StringComparison.Ordinal))
            {
                return sheet;
            }

            var colIndex = Array.IndexOf(sheet.Header.ToArray(), columnName);
            Assert.True(colIndex >= 0, $"Column '{columnName}' not found on sheet '{sheetName}'.");

            var rows = sheet.Rows.Select((row, i) =>
            {
                if (i != rowIndex)
                {
                    return row;
                }

                var cells = row.ToList();
                while (cells.Count <= colIndex)
                {
                    cells.Add(string.Empty);
                }

                cells[colIndex] = value;
                return (IReadOnlyList<string>)cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return workbook with { Sheets = sheets };
    }

    private static void SeedPlatform(string dbPath, string platformId)
    {
        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9300));
        var batchId = gate.ProposePlatformBatch(
            [new CatalogPlatformBinding(platformId, "Test Platform", Domain: "surface")],
            "agent",
            "seed");
        Assert.True(gate.ApproveBatch(batchId, "human", "seed").Committed);
    }

    private static void Cleanup(string dbPath)
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }

    private sealed class FakeWriteGate : IWriteGate
    {
        public List<IReadOnlyList<CatalogSensorBinding>> SensorProposals { get; } = new();
        public List<IReadOnlyList<CatalogMount>> MountProposals { get; } = new();
        public List<IReadOnlyList<CatalogLoadout>> LoadoutProposals { get; } = new();
        public List<IReadOnlyList<CatalogMagazineEntry>> MagazineProposals { get; } = new();
        public List<IReadOnlyList<CatalogCommsBinding>> CommsProposals { get; } = new();
        public List<IReadOnlyList<CatalogMobility>> MobilityProposals { get; } = new();
        public List<IReadOnlyList<CatalogSignature>> SignatureProposals { get; } = new();
        public List<IReadOnlyList<CatalogEmcon>> EmconProposals { get; } = new();
        public List<IReadOnlyList<CatalogPlatformDamage>> DamageProposals { get; } = new();
        public List<IReadOnlyList<CatalogPlatformBinding>> PlatformProposals { get; } = new();
        public List<IReadOnlyList<CatalogWeaponRecord>> WeaponProposals { get; } = new();

        public string ProposeSensorBatch(IReadOnlyList<CatalogSensorBinding> proposed, string actorType, string actorId, string rationale = "")
        {
            SensorProposals.Add(proposed);
            return $"fake-batch-sensor-{SensorProposals.Count}";
        }

        public string ProposeMountBatch(IReadOnlyList<CatalogMount> proposed, string actorType, string actorId, string rationale = "")
        {
            MountProposals.Add(proposed);
            return $"fake-batch-mount-{MountProposals.Count}";
        }

        public string ProposeLoadoutBatch(IReadOnlyList<CatalogLoadout> proposed, string actorType, string actorId, string rationale = "")
        {
            LoadoutProposals.Add(proposed);
            return $"fake-batch-loadout-{LoadoutProposals.Count}";
        }

        public string ProposeMagazineBatch(IReadOnlyList<CatalogMagazineEntry> proposed, string actorType, string actorId, string rationale = "")
        {
            MagazineProposals.Add(proposed);
            return $"fake-batch-magazine-{MagazineProposals.Count}";
        }

        public string ProposeCommsBatch(IReadOnlyList<CatalogCommsBinding> proposed, string actorType, string actorId, string rationale = "")
        {
            CommsProposals.Add(proposed);
            return $"fake-batch-comms-{CommsProposals.Count}";
        }

        public string ProposeMobilityBatch(IReadOnlyList<CatalogMobility> proposed, string actorType, string actorId, string rationale = "")
        {
            MobilityProposals.Add(proposed);
            return $"fake-batch-mobility-{MobilityProposals.Count}";
        }

        public string ProposeSignatureBatch(IReadOnlyList<CatalogSignature> proposed, string actorType, string actorId, string rationale = "")
        {
            SignatureProposals.Add(proposed);
            return $"fake-batch-signature-{SignatureProposals.Count}";
        }

        public string ProposeEmconBatch(IReadOnlyList<CatalogEmcon> proposed, string actorType, string actorId, string rationale = "")
        {
            EmconProposals.Add(proposed);
            return $"fake-batch-emcon-{EmconProposals.Count}";
        }

        public string ProposePlatformDamageBatch(
            IReadOnlyList<CatalogPlatformDamage> proposed,
            string actorType,
            string actorId,
            string rationale = "")
        {
            DamageProposals.Add(proposed);
            return $"fake-batch-damage-{DamageProposals.Count}";
        }

        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformBinding> proposed, string actorType, string actorId, string rationale = "")
        {
            PlatformProposals.Add(proposed);
            return $"fake-batch-platform-{PlatformProposals.Count}";
        }

        public string ProposeWeaponBatch(IReadOnlyList<CatalogWeaponRecord> proposed, string actorType, string actorId, string rationale = "")
        {
            WeaponProposals.Add(proposed);
            return $"fake-batch-weapon-{WeaponProposals.Count}";
        }

        public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId) => new(true, batchId, []);

        public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "") => new(true, batchId, []);

        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() => [];
    }
}