using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

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
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") });

    private static PlatformWorkbook Export(PlatformCatalogExportData data, string snapshotId = SnapshotId) =>
        new PlatformWorkbookExporter().Export(data, snapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, System.StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    [Fact]
    public void Plan_unedited_round_trip_has_no_changes()
    {
        var source = BaseData();
        var edited = Export(source); // identical to what the importer re-exports from the source

        var plan = ImporterFor(source).Plan(edited);

        Assert.True(plan.SnapshotResolved);
        Assert.False(plan.HasChanges);
        Assert.False(plan.Blocked);
    }

    [Fact]
    public void Plan_unknown_snapshot_is_unresolved()
    {
        var edited = Export(BaseData(), snapshotId: "does-not-exist");

        var plan = ImporterFor(BaseData()).Plan(edited);

        Assert.False(plan.SnapshotResolved);
        Assert.Empty(plan.Changes);
    }

    [Fact]
    public void Stage_edited_sensor_proposes_one_batch()
    {
        var source = BaseData(sensorBasePdMilli: 850);
        var edited = Export(BaseData(sensorBasePdMilli: 500)); // cmo-sensor-1 BasePd 0.85 -> 0.50
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit pd");

        Assert.True(result.Staged);
        Assert.NotNull(result.SensorBatchId);
        var proposed = Assert.Single(gate.SensorProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("cmo-sensor-1", proposed[0].SensorId);
        Assert.Equal(0.50, proposed[0].BasePd, precision: 6);
    }

    [Fact]
    public void Stage_blocked_by_validation_error_stages_nothing()
    {
        var source = BaseData();
        var edited = Export(BaseData(magazineQty: 99)); // 99 > mount capacity 32
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.False(result.Staged);
        Assert.Empty(gate.SensorProposals);
        Assert.Empty(gate.MountProposals);
        Assert.Empty(gate.LoadoutProposals);
        Assert.Empty(gate.MagazineProposals);
        Assert.Empty(gate.CommsProposals);
        Assert.Empty(gate.PlatformProposals); // S22-04: new ProposePlatformBatch path covered by fake (empty on block)
        Assert.Contains(result.Plan.Findings, f =>
            f.Code == PlatformWorkbookValidator.MagazineOverCapacity && f.Severity == ValidationSeverity.Error);
    }

    [Fact]
    public void Stage_reports_unsupported_entity_changes()
    {
        var source = BaseData();
        // Change a mount capacity (still valid, 40 >= magazine 16). Platforms are not write-gate-stageable.
        var editedData = BaseData() with { Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 40) } };
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(Export(editedData), gate, "human", "drgamtd");

        Assert.Empty(gate.SensorProposals);                       // no sensor changes
        Assert.NotEmpty(gate.MountProposals);                     // mount changes are now supported
        Assert.Contains(gate.MountProposals[0], m => m.Capacity == 40);
    }

    [Fact]
    public void Stage_edited_mount_proposes_mount_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 40) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit mount");

        Assert.True(result.Staged);
        Assert.NotNull(result.MountBatchId);
        var proposed = Assert.Single(gate.MountProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("vls-fwd", proposed[0].MountId);
        Assert.Equal(40, proposed[0].Capacity);
    }

    [Fact]
    public void Stage_edited_loadout_proposes_loadout_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Loadouts = new[] { new CatalogLoadout("u1", "asuw-default", "ASuW Heavy", "asuw", IsDefault: true) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit loadout");

        Assert.True(result.Staged);
        Assert.NotNull(result.LoadoutBatchId);
        var proposed = Assert.Single(gate.LoadoutProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("asuw-default", proposed[0].LoadoutId);
        Assert.Equal("ASuW Heavy", proposed[0].LoadoutName);
    }

    [Fact]
    public void Stage_edited_magazine_proposes_magazine_batch()
    {
        var source = BaseData(magazineQty: 16);
        var edited = Export(BaseData(magazineQty: 24)); // 16 -> 24
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit magazine");

        Assert.True(result.Staged);
        Assert.NotNull(result.MagazineBatchId);
        var proposed = Assert.Single(gate.MagazineProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("mvp-weapon", proposed[0].WeaponId);
        Assert.Equal(24, proposed[0].Quantity);
    }

    [Fact]
    public void Stage_edited_comms_proposes_comms_batch()
    {
        var source = BaseData();
        var editedData = BaseData() with { Comms = new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J", Role: "tx", SatcomCapable: true) } };
        var edited = Export(editedData);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd", "edit comms");

        Assert.True(result.Staged);
        Assert.NotNull(result.CommsBatchId);
        var proposed = Assert.Single(gate.CommsProposals);
        Assert.Equal("u1", proposed[0].PlatformId);
        Assert.Equal("NATO_TADIL_J", proposed[0].LinkId);
        Assert.Equal("tx", proposed[0].Role);
        Assert.True(proposed[0].SatcomCapable);
    }

    [Fact]
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

        Assert.True(result.Staged);
        Assert.NotNull(result.SensorBatchId);
        Assert.NotNull(result.MountBatchId);
        Assert.NotNull(result.MagazineBatchId);
        Assert.NotEmpty(gate.SensorProposals);
        Assert.NotEmpty(gate.MountProposals);
        Assert.NotEmpty(gate.MagazineProposals);
    }

    [Fact]
    public void Plan_large_changeset_requires_human_approval()
    {
        var source = ManySensors(11, basePdMilli: 100);
        var edited = Export(ManySensors(11, basePdMilli: 101)); // every BasePd nudged -> 11 changes > threshold 10

        var plan = ImporterFor(source).Plan(edited);

        Assert.True(plan.Changes.Count > PlatformWorkbookImporter.HumanApprovalRecordThreshold);
        Assert.True(plan.RequiresHumanApproval);
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
        public List<IReadOnlyList<CatalogPlatformEntry>> PlatformProposals { get; } = new();

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

        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformEntry> proposed, string actorType, string actorId, string rationale = "")
        {
            PlatformProposals.Add(proposed);
            return $"fake-batch-platform-{PlatformProposals.Count}";
        }

        public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId) => new(true, batchId, []);

        public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "") => new(true, batchId, []);

        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() => [];
    }
}
