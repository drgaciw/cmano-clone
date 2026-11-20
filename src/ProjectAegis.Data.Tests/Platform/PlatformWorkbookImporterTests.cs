using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 PLE-2.* / PLE-3.* / PLE-4.*: importer stages supported entities through the write
/// gate, refuses on unresolved snapshot or validation errors, and flags large changesets for human
/// approval. Pure / in-memory — a fake gate stands in for the SQLite write gate.
/// </summary>
public sealed class PlatformWorkbookImporterTests
{
    private const string SnapshotId = "baltic_patrol";

    private static PlatformCatalogExportData BaseData(int sensorBasePdMilli = 850, int magazineQty = 16) => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[]
        {
            new CatalogSensorBinding("u1", "cmo-sensor-1", sensorBasePdMilli / 1000.0, CitationRef: "/sensor/1/"),
            new CatalogSensorBinding("u1", "cmo-sensor-2", 0.40),
        },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", magazineQty, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Links: new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
            new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, LatencyMsNominal: 250),
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data, string snapshotId = SnapshotId) =>
        new PlatformWorkbookExporter().Export(data, snapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    [Test]
    public void Plan_unedited_round_trip_has_no_changes()
    {
        var source = BaseData();
        var edited = Export(source); // identical to what the importer re-exports from the source

        var plan = ImporterFor(source).Plan(edited);

        Assert.That(plan.SnapshotResolved, Is.True);
        Assert.That(plan.HasChanges, Is.False);
        Assert.That(plan.Blocked, Is.False);
    }

    [Test]
    public void Plan_unknown_snapshot_is_unresolved()
    {
        var edited = Export(BaseData(), snapshotId: "does-not-exist");

        var plan = ImporterFor(BaseData()).Plan(edited);

        Assert.That(plan.SnapshotResolved, Is.False);
        Assert.That(plan.Changes, Is.Empty);
    }

    [Test]
    public void Stage_edited_sensor_proposes_one_batch()
    {
        var source = BaseData(sensorBasePdMilli: 850);
        var edited = Export(BaseData(sensorBasePdMilli: 500)); // cmo-sensor-1 BasePd 0.85 -> 0.50
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit pd");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.SensorBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.SensorProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].SensorId, Is.EqualTo("cmo-sensor-1"));
        Assert.That(proposed[0].BasePd, Is.EqualTo(0.50).Within(0.000005));
    }

    [Test]
    public void Stage_blocked_by_validation_error_stages_nothing()
    {
        var source = BaseData();
        var edited = Export(BaseData(magazineQty: 99)); // 99 > mount capacity 32
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.That(result.Staged, Is.False);
        Assert.That(gate.SensorProposals, Is.Empty);
        Assert.That(gate.MountProposals, Is.Empty);
        Assert.That(gate.LoadoutProposals, Is.Empty);
        Assert.That(gate.MagazineProposals, Is.Empty);
        Assert.That(gate.CommsProposals, Is.Empty);
        Assert.That(gate.MobilityProposals, Is.Empty);
        Assert.That(gate.SignatureProposals, Is.Empty);
        Assert.That(gate.EmconProposals, Is.Empty);
        Assert.That(result.Plan.Findings.Any(f =>
            f.Code == PlatformWorkbookValidator.MagazineOverCapacity && f.Severity == ValidationSeverity.Error), Is.True);
    }

    [Test]
    public void Stage_reports_unsupported_entity_changes()
    {
        var source = BaseData();
        // Change a mount capacity (still valid, 40 >= magazine 16). Platforms are not write-gate-stageable.
        var editedData = BaseData() with { Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 40) } };
        var gate = new FakeWriteGate();

        _ = ImporterFor(source).Stage(Export(editedData), gate, "human", "drgamtd");

        Assert.That(gate.SensorProposals, Is.Empty);                       // no sensor changes
        Assert.That(gate.MountProposals, Is.Not.Empty);                     // mount changes are now supported
        Assert.That(gate.MountProposals[0].Any(m => m.Capacity == 40), Is.True);
    }

    [Test]
    public void Stage_edited_mount_proposes_mount_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 40) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit mount");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.MountBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.MountProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].MountId, Is.EqualTo("vls-fwd"));
        Assert.That(proposed[0].Capacity, Is.EqualTo(40));
    }

    [Test]
    public void Stage_edited_loadout_proposes_loadout_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Loadouts = new[] { new CatalogLoadout("u1", "asuw-default", "ASuW Heavy", "asuw", IsDefault: true) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit loadout");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.LoadoutBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.LoadoutProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].LoadoutId, Is.EqualTo("asuw-default"));
        Assert.That(proposed[0].LoadoutName, Is.EqualTo("ASuW Heavy"));
    }

    [Test]
    public void Stage_edited_magazine_proposes_magazine_batch()
    {
        var source = BaseData(magazineQty: 16);
        var edited = Export(BaseData(magazineQty: 24)); // 16 -> 24
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit magazine");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.MagazineBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.MagazineProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].WeaponId, Is.EqualTo("mvp-weapon"));
        Assert.That(proposed[0].Quantity, Is.EqualTo(24));
    }

    [Test]
    public void Stage_edited_link_proposes_link_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with
        {
            Links = new[]
            {
                new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 75),
                new CatalogLinkEntry("SATCOM_B", "SATCOM Wideband", CatalogLinkTypes.Satcom, LatencyMsNominal: 250),
            },
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit link");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.LinkBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.LinkProposals);
        Assert.That(proposed[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(proposed[0].LatencyMsNominal, Is.EqualTo(75));
    }

    [Test]
    public void Stage_edited_comms_proposes_comms_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Comms = new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J", Role: "tx", SatcomCapable: true) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit comms");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.CommsBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.CommsProposals);
        Assert.That(proposed[0].PlatformId, Is.EqualTo("u1"));
        Assert.That(proposed[0].LinkId, Is.EqualTo("NATO_TADIL_J"));
        Assert.That(proposed[0].Role, Is.EqualTo("tx"));
        Assert.That(proposed[0].SatcomCapable, Is.True);
    }

    [Test]
    public void Stage_multiple_entity_types_proposes_all_batches()
    {
        var source = BaseData(sensorBasePdMilli: 850, magazineQty: 16);
        var editedData = BaseData(sensorBasePdMilli: 500, magazineQty: 24) with
        {
            Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 40) }
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit multiple");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.SensorBatchId, Is.Not.Null);
        Assert.That(result.MountBatchId, Is.Not.Null);
        Assert.That(result.MagazineBatchId, Is.Not.Null);
        Assert.That(gate.SensorProposals, Is.Not.Empty);
        Assert.That(gate.MountProposals, Is.Not.Empty);
        Assert.That(gate.MagazineProposals, Is.Not.Empty);
    }

    [Test]
    public void Plan_large_changeset_requires_human_approval()
    {
        var source = ManySensors(11, basePdMilli: 100);
        var edited = Export(ManySensors(11, basePdMilli: 101)); // every BasePd nudged -> 11 changes > threshold 10

        var plan = ImporterFor(source).Plan(edited);

        Assert.That(plan.Changes.Count > PlatformWorkbookImporter.HumanApprovalRecordThreshold, Is.True);
        Assert.That(plan.RequiresHumanApproval, Is.True);
    }

    /// <summary>PLE-2.3: dangling magazine FK produces quarantine report entry and never stages.</summary>
    [Test]
    public void Stage_dangling_magazine_mount_fk_emits_quarantine_entry_and_stages_nothing()
    {
        var source = BaseData(magazineQty: 16);
        var editedData = BaseData(magazineQty: 16) with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "does-not-exist-mount", "mvp-weapon", 8, 0, 8),
            },
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "dangling mount fk");

        Assert.That(result.Staged, Is.False);
        Assert.That(gate.MagazineProposals, Is.Empty);
        Assert.That(gate.SensorProposals, Is.Empty);
        Assert.That(result.Plan.Blocked, Is.True);
        Assert.That(result.Plan.Findings.Any(f => f.Code == PlatformWorkbookValidator.MagazineUnknownMount), Is.True);
        Assert.That(result.QuarantineEntries, Is.Not.Empty);
        Assert.That(result.QuarantineEntries.Any(q =>
            q.Reason == PlatformWorkbookValidator.MagazineUnknownMount
            && q.PlatformId == "u1"
            && q.EntityId == "does-not-exist-mount"
            && q.SourceSheet == "Magazines"), Is.True);
        Assert.That(result.QuarantineEntries.Count, Is.EqualTo(result.Plan.QuarantineEntries.Count));
    }

    /// <summary>PLE-4.4: low-TRL / provisional sensors are quarantined and not proposed; high-TRL approved still stage.</summary>
    [Test]
    public void Stage_trl_gate_quarantines_low_trl_provisional_sensor_and_stages_approved_high_trl()
    {
        var source = new PlatformCatalogExportData(
            Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Sensors: new[]
            {
                new CatalogSensorBinding(
                    "u1",
                    "sensor-approved-high",
                    0.80,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9),
                new CatalogSensorBinding(
                    "u1",
                    "sensor-provisional-low",
                    0.80,
                    ReviewState: CatalogReviewStates.Provisional,
                    TrlLevel: 1),
            },
            Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
            Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
            Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
            Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
            Links: new[]
            {
                new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
            });

        var editedData = source with
        {
            Sensors = new[]
            {
                new CatalogSensorBinding(
                    "u1",
                    "sensor-approved-high",
                    0.50,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9),
                new CatalogSensorBinding(
                    "u1",
                    "sensor-provisional-low",
                    0.50,
                    ReviewState: CatalogReviewStates.Provisional,
                    TrlLevel: 1),
            },
        };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "trl gate");

        Assert.That(result.Staged, Is.True);
        Assert.That(result.SensorBatchId, Is.Not.Null);
        var proposed = AssertEx.Single(gate.SensorProposals);
        var only = AssertEx.Single(proposed);
        Assert.That(only.SensorId, Is.EqualTo("sensor-approved-high"));
        Assert.That(only.BasePd, Is.EqualTo(0.50).Within(0.000005));
        Assert.That(only.ReviewState, Is.EqualTo(CatalogReviewStates.Approved));
        Assert.That(only.TrlLevel, Is.EqualTo(9));

        Assert.That(result.QuarantineEntries.Any(q =>
            q.EntityKind == "sensor"
            && q.EntityId == "sensor-provisional-low"
            && q.PlatformId == "u1"
            && (q.Reason is "trl_below_minimum" or "review_state_provisional")
            && q.SourceSheet == "Sensors"), Is.True);
        Assert.That(result.QuarantineEntries.Any(q => q.EntityId == "sensor-approved-high"), Is.False);
    }

    private static PlatformCatalogExportData ManySensors(int count, int basePdMilli)
    {
        var sensors = new List<CatalogSensorBinding>();
        for (var i = 0; i < count; i++)
        {
            sensors.Add(new CatalogSensorBinding("u1", $"sensor-{i:00}", basePdMilli / 1000.0));
        }

        return PlatformCatalogExportData.Empty with
        {
            Platforms = new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Sensors = sensors,
        };
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
