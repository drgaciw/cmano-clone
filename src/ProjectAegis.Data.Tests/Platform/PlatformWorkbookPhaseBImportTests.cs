using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S24-04 / S25-05: Phase B importer wiring — Mobility/Signatures/Emcon/Damage stage via write gate,
/// empty-diff golden on unedited round-trip, FK guard on PlatformId, E2E read-back via SqliteCatalogReader.
/// </summary>
[ProjectAegis.Data.Tests.Runner.CatalogSqliteCollection]
public sealed class PlatformWorkbookPhaseBImportTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static PlatformCatalogExportData PhaseBData(
        double maxSpeedKnots = 30,
        double rcsBandDbsm = -10,
        string emconPosture = "off",
        double maxHp = 100,
        double withdrawThresholdPct = 25,
        int criticalFlags = 0) => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85) },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: maxSpeedKnots, CruiseSpeedKnots: 18, RangeNm: 4000) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: rcsBandDbsm, AcousticSignatureDb: 95) },
        Emcon: new[] { new CatalogEmcon("u1", "silent", "cmo-sensor-1", emconPosture) },
        Damage: new[] { new CatalogPlatformDamage("u1", MaxHp: maxHp, WithdrawThresholdPct: withdrawThresholdPct, CriticalFlags: criticalFlags) });

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

    [Test]
    public void Plan_unedited_Phase_B_round_trip_has_no_supported_changes()
    {
        var source = PhaseBData();
        var edited = Export(source);

        var plan = ImporterFor(source).Plan(edited);

        Assert.That(plan.SnapshotResolved, Is.True);
        Assert.That(plan.HasChanges, Is.False);
        Assert.That(plan.SupportedChanges, Is.Empty);
        Assert.That(plan.UnsupportedChanges, Is.Empty);
    }

    [Test]
    public void Stage_unedited_Phase_B_round_trip_stages_nothing()
    {
        var source = PhaseBData();
        var edited = Export(source);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.That(result.Staged, Is.False);
        Assert.That(result.MobilityBatchId, Is.Null);
        Assert.That(result.SignatureBatchId, Is.Null);
        Assert.That(result.EmconBatchId, Is.Null);
        Assert.That(result.DamageBatchId, Is.Null);
        Assert.That(gate.MobilityProposals, Is.Empty);
        Assert.That(gate.SignatureProposals, Is.Empty);
        Assert.That(gate.EmconProposals, Is.Empty);
        Assert.That(gate.DamageProposals, Is.Empty);
    }

    [Test]
    public void Stage_edited_mobility_proposes_mobility_batch()
    {
        var source = PhaseBData(maxSpeedKnots: 30);
        var edited = Export(PhaseBData(maxSpeedKnots: 35));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit mobility");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.MobilityBatchId, Is.Not.Null);
        var proposed = ProjectAegis.Data.Tests.AssertEx.Single(gate.MobilityProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].MaxSpeedKnots, Is.EqualTo(35).Within(0.000005));
    }

    [Test]
    public void Stage_edited_signature_proposes_signature_batch()
    {
        var source = PhaseBData(rcsBandDbsm: -10);
        var edited = Export(PhaseBData(rcsBandDbsm: -15));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit signature");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.SignatureBatchId, Is.Not.Null);
        var proposed = ProjectAegis.Data.Tests.AssertEx.Single(gate.SignatureProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].RcsBandDbsm, Is.EqualTo(-15).Within(0.000005));
    }

    [Test]
    public void Stage_edited_MaxHp_proposes_damage_batch()
    {
        var source = PhaseBData(maxHp: 100);
        var edited = Export(PhaseBData(maxHp: 120));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit damage");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.DamageBatchId, Is.Not.Null);
        var proposed = ProjectAegis.Data.Tests.AssertEx.Single(gate.DamageProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].MaxHp, Is.EqualTo(120).Within(0.000005));
    }

    [Test]
    public void Stage_edited_emcon_proposes_emcon_batch()
    {
        var source = PhaseBData(emconPosture: "off");
        var edited = Export(PhaseBData(emconPosture: "active"));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit emcon");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.EmconBatchId, Is.Not.Null);
        var proposed = ProjectAegis.Data.Tests.AssertEx.Single(gate.EmconProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].Posture, Is.EqualTo("active"));
    }

    [Test]
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

        Assert.That(result.Staged, Is.False);
        Assert.That(result.Plan.Blocked, Is.True);
        Assert.That(gate.MobilityProposals, Is.Empty);
        Assert.That(result.Plan.Findings.Any(f =>
            f.Code == PlatformWorkbookValidator.PhaseBOrphanPlatform
            && f.UnitId == "unknown-platform"
            && f.Severity == ValidationSeverity.Error), Is.True);
    }

    /// <summary>PLE-2.3: orphan PlatformId FK emits quarantine-style report entry; nothing staged/committed.</summary>
    [Test]
    public void Stage_orphan_platform_mobility_emits_quarantine_report_entry_PLE_2_3()
    {
        var source = PhaseBData();
        var editedData = PhaseBData(maxSpeedKnots: 30) with
        {
            Mobility = new[] { new CatalogMobility("unknown-platform", MaxSpeedKnots: 99) },
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.That(result.Staged, Is.False);
        Assert.That(gate.MobilityProposals, Is.Empty);
        Assert.That(result.Plan.QuarantineEntries, Is.Not.Empty);
        Assert.That(result.QuarantineEntries, Is.Not.Empty);
        Assert.That(result.QuarantineEntries.Any(q =>
            q.Reason == PlatformWorkbookValidator.PhaseBOrphanPlatform
            && q.PlatformId == "unknown-platform"
            && q.EntityKind == "platform_fk"), Is.True);
    }

    [Test]
    public void Stage_multiple_Phase_B_entity_types_proposes_all_batches()
    {
        var source = PhaseBData(maxSpeedKnots: 30, rcsBandDbsm: -10, emconPosture: "off");
        var edited = Export(PhaseBData(maxSpeedKnots: 35, rcsBandDbsm: -15, emconPosture: "active"));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit phase b");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.MobilityBatchId, Is.Not.Null);
        Assert.That(result.SignatureBatchId, Is.Not.Null);
        Assert.That(result.EmconBatchId, Is.Not.Null);
        Assert.That(gate.MobilityProposals, Is.Not.Empty);
        Assert.That(gate.SignatureProposals, Is.Not.Empty);
        Assert.That(gate.EmconProposals, Is.Not.Empty);
    }

    [Test]
    public void E2E_damage_export_edit_stage_approve_readback_via_SqliteCatalogReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-e2e-damage-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var source), Is.True);

            var exported = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(9650));
            var edited = WithPlatformSheetCell(exported, "u1", "MaxHp", "120");

            var importer = ImporterForDb(dbPath);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9651)))
            {
                var result = importer.Stage(edited, gate, "human", "drgamtd", "e2e damage");
                Assert.That(result.Staged, Is.True);
                Assert.That(result.DamageBatchId, Is.Not.Null);
                Assert.That(gate.ApproveBatch(result.DamageBatchId!, "human", "qa-reviewer").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-e2e-damage");
            Assert.That(reader.TryGetPlatformDamage("u1", out var damage), Is.True);
            Assert.That(damage.MaxHp, Is.EqualTo(120).Within(0.005));
            Assert.That(damage.WithdrawThresholdPct, Is.EqualTo(25).Within(0.005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void E2E_mobility_export_edit_stage_approve_readback_via_SqliteCatalogReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-e2e-mobility-{Guid.NewGuid():N}.db");
        try
        {
            SeedPhaseBDatabase(dbPath);
            Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var source), Is.True);

            var exported = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(9600));
            var edited = WithSheetCell(exported, "Mobility", 0, "MaxSpeedKnots", "36");

            var importer = ImporterForDb(dbPath);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9601)))
            {
                var result = importer.Stage(edited, gate, "human", "drgamtd", "e2e mobility");
                Assert.That(result.Staged, Is.True);
                Assert.That(result.MobilityBatchId, Is.Not.Null);
                Assert.That(gate.ApproveBatch(result.MobilityBatchId!, "human", "qa-reviewer").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-e2e-mobility");
            Assert.That(reader.TryGetMobility("u1", out var mobility), Is.True);
            Assert.That(mobility.MaxSpeedKnots, Is.EqualTo(36).Within(0.005));
            Assert.That(mobility.RangeNm, Is.EqualTo(4200).Within(0.005));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
    public void E2E_all_Phase_B_sheets_export_edit_stage_approve_readback_via_SqliteCatalogReader()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-phase-b-e2e-all-{Guid.NewGuid():N}.db");
        try
        {
            SeedPhaseBDatabase(dbPath);
            Assert.That(PlatformCatalogExportResolver.TryResolve(dbPath, SnapshotId, out var source), Is.True);

            var exported = new PlatformWorkbookExporter().Export(source, SnapshotId, new FixedCatalogClock(9610));
            var edited = WithSheetCell(exported, "Mobility", 0, "MaxSpeedKnots", "36");
            edited = WithSheetCell(edited, "Signatures", 0, "RcsBandDbsm", "-18");
            edited = WithSheetCell(edited, "Emcon", 1, "Posture", "standby");

            var importer = ImporterForDb(dbPath);
            using (var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9611)))
            {
                var result = importer.Stage(edited, gate, "human", "drgamtd", "e2e phase b all");
                Assert.That(result.Staged, Is.True);
                Assert.That(result.MobilityBatchId, Is.Not.Null);
                Assert.That(result.SignatureBatchId, Is.Not.Null);
                Assert.That(result.EmconBatchId, Is.Not.Null);
                Assert.That(gate.ApproveBatch(result.MobilityBatchId!, "human", "qa-reviewer").Committed, Is.True);
                Assert.That(gate.ApproveBatch(result.SignatureBatchId!, "human", "qa-reviewer").Committed, Is.True);
                Assert.That(gate.ApproveBatch(result.EmconBatchId!, "human", "qa-reviewer").Committed, Is.True);
            }

            using var reader = new SqliteCatalogReader(dbPath, "phase-b-e2e-all");
            Assert.That(reader.TryGetMobility("u1", out var mobility), Is.True);
            Assert.That(mobility.MaxSpeedKnots, Is.EqualTo(36).Within(0.005));

            Assert.That(reader.TryGetSignature("u1", out var signature), Is.True);
            Assert.That(signature.RcsBandDbsm, Is.EqualTo(-18).Within(0.005));

            Assert.That(reader.TryGetEmcon("u1", "silent", "radar-1", out var emcon), Is.True);
            Assert.That(emcon.Posture, Is.EqualTo("standby"));
            Assert.That(reader.TryGetEmcon("u1", "free", "radar-1", out var unchanged), Is.True);
            Assert.That(unchanged.Posture, Is.EqualTo("active"));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    [Test]
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
                Assert.That(decision.Committed, Is.True);
            }

            using var connection = new SqliteConnection($"Data Source={dbPath};Pooling=false");
            connection.Open();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT max_speed_knots, range_nm FROM platform_mobility WHERE platform_id = 'u1'";
            using var reader = cmd.ExecuteReader();
            Assert.That(reader.Read(), Is.True);
            Assert.That(reader.GetDouble(0), Is.EqualTo(35).Within(0.000005));
            Assert.That(reader.GetDouble(1), Is.EqualTo(5000).Within(0.000005));
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

    private static PlatformWorkbook WithPlatformSheetCell(
        PlatformWorkbook workbook,
        string platformId,
        string columnName,
        string value)
    {
        var sheet = workbook.FindSheet("Platforms");
        Assert.That(sheet, Is.Not.Null);
        var platformCol = Array.IndexOf(sheet!.Header.ToArray(), "PlatformId");
        Assert.That(platformCol >= 0, Is.True);

        var rowIndex = -1;
        for (var i = 0; i < sheet.Rows.Count; i++)
        {
            if (i < sheet.Rows[i].Count
                && string.Equals(sheet.Rows[i][platformCol], platformId, StringComparison.Ordinal))
            {
                rowIndex = i;
                break;
            }
        }

        Assert.That(rowIndex >= 0, Is.True, $"Platform '{platformId}' not found on Platforms sheet.");
        return WithSheetCell(workbook, "Platforms", rowIndex, columnName, value);
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
            Assert.That(colIndex >= 0, Is.True, $"Column '{columnName}' not found on sheet '{sheetName}'.");

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
                return cells;
            }).ToArray();

            return sheet with { Rows = rows };
        }).ToArray();

        return new PlatformWorkbook(sheets);
    }

    private static void SeedPlatform(string dbPath, string platformId)
    {
        using var gate = new CatalogWriteGate(dbPath, new FixedCatalogClock(9300));
        var batchId = gate.ProposePlatformBatch(
            [new CatalogPlatformBinding(platformId, "Test Platform", Domain: "surface")],
            "agent",
            "seed");
        Assert.That(gate.ApproveBatch(batchId, "human", "seed").Committed, Is.True);
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
        public List<IReadOnlyList<CatalogLinkEntry>> LinkProposals { get; } = new();
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

        public string ProposeLinkCatalogBatch(IReadOnlyList<CatalogLinkEntry> proposed, string actorType, string actorId, string rationale = "")
        {
            LinkProposals.Add(proposed);
            return $"fake-batch-link-{LinkProposals.Count}";
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

        public string ProposeSwarmBatch(IReadOnlyList<CatalogSwarmPlatform> proposed, string actorType, string actorId, string rationale = "")
            => "fake-swarm";

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