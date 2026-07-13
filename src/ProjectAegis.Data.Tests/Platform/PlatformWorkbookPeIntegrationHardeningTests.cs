using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Excel;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// PE adversarial TDD — Track CROSS: integration / product-invariant pins for PE-W1 (ClosedXML
/// protection + enum validation metadata) and PE-W2 (quarantine / empty-diff / Stage gates).
/// Tests-only hardening; no CatalogWriteGate write-path or DelegationBridge changes.
/// </summary>
public sealed class PlatformWorkbookPeIntegrationHardeningTests
{
    private const string SnapshotId = "baltic_patrol";

    private static PlatformCatalogExportData BalticLikeData(
        int sensorBasePdMilli = 850,
        int magazineQty = 16) => new(
        Platforms: new[]
        {
            new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
            new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
        },
        Sensors: new[]
        {
            new CatalogSensorBinding("u1", "cmo-sensor-1", sensorBasePdMilli / 1000.0, CitationRef: "/sensor/1/"),
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
            new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", magazineQty, 0, 32),
        },
        Comms: new[]
        {
            new CatalogCommsBinding("u1", "NATO_TADIL_J", "txrx", SatcomCapable: false),
        },
        Links: new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
        });

    private static PlatformWorkbook Export(PlatformCatalogExportData data, long clockTicks = 0) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(clockTicks));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source, long clockTicks = 0) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(clockTicks));

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

    private static PlatformCatalogExportData ManySensors(int count, int basePdMilli) =>
        PlatformCatalogExportData.Empty with
        {
            Platforms = new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
            Sensors = Enumerable.Range(0, count)
                .Select(i => new CatalogSensorBinding("u1", $"sensor-{i:00}", basePdMilli / 1000.0))
                .ToArray(),
            Mounts = new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
            Loadouts = new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
            Magazines = new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
            Comms = new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
            Links = new[]
            {
                new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
            },
        };

    // -------------------------------------------------------------------------
    // 1. Empty-diff golden still holds after ClosedXML Write→Read
    // -------------------------------------------------------------------------

    /// <summary>
    /// PLE-2.1 empty-diff golden: unedited Baltic-like export survives ClosedXML binary Write→Read
    /// and Plan() reports zero changes (PE-W1 protection must not invent diffs).
    /// </summary>
    [Fact]
    public void Empty_diff_golden_holds_after_ClosedXml_write_read_unedited_baltic_export()
    {
        var source = BalticLikeData();
        var original = Export(source, clockTicks: 11_001);
        var path = Path.Combine(Path.GetTempPath(), $"pe-cross-empty-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);
            Assert.True(IsZipArchive(path), "export must emit a binary .xlsx (ZIP) workbook");

            var roundTripped = io.Read(path);
            Assert.True(PlatformWorkbookDiff.IsEmpty(original, roundTripped));

            var plan = ImporterFor(source, clockTicks: 11_001).Plan(roundTripped);
            Assert.True(plan.SnapshotResolved);
            Assert.False(plan.HasChanges);
            Assert.Empty(plan.Changes);
            Assert.False(plan.Blocked);
            Assert.False(plan.RequiresHumanApproval);
            Assert.Empty(plan.QuarantineEntries);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 2. Export→Write→Read→Plan never blocked solely by PE-W1 metadata
    // -------------------------------------------------------------------------

    /// <summary>
    /// PE-W1 protection / list-validation metadata on the binary workbook must not surface as
    /// validation errors that block Plan after ClosedXML Read.
    /// </summary>
    [Fact]
    public void Export_Write_Read_Plan_never_blocked_solely_by_protection_or_validation_metadata()
    {
        var source = BalticLikeData();
        var original = Export(source, clockTicks: 11_002);
        var path = Path.Combine(Path.GetTempPath(), $"pe-cross-meta-block-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);

            // Prove PE-W1 metadata is actually present on the binary artifact (protection applied).
            using (var xl = new ClosedXML.Excel.XLWorkbook(path))
            {
                var meta = xl.Worksheet(PlatformWorkbookHash.MetaSheetName);
                Assert.True(meta.IsProtected, "precondition: PE-W1 protects _Meta");
            }

            var roundTripped = io.Read(path);
            var plan = ImporterFor(source, clockTicks: 11_002).Plan(roundTripped);

            Assert.True(plan.SnapshotResolved);
            Assert.False(plan.Blocked);
            Assert.DoesNotContain(plan.Findings, f => f.Severity == ValidationSeverity.Error);
            Assert.False(plan.HasChanges);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 3. Hash / _Meta stability through ClosedXML with PE-W1 protection
    // -------------------------------------------------------------------------

    /// <summary>
    /// WorkbookHash + SourceSnapshotId survive ClosedXML round-trip with PE-W1 sheet protection.
    /// </summary>
    [Fact]
    public void WorkbookHash_and_SourceSnapshotId_survive_ClosedXml_round_trip_with_protection()
    {
        var original = Export(BalticLikeData(), clockTicks: 11_003);
        var expectedHash = PlatformWorkbookHash.Compute(original);
        var path = Path.Combine(Path.GetTempPath(), $"pe-cross-hash-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path);

            using (var xl = new ClosedXML.Excel.XLWorkbook(path))
            {
                Assert.True(xl.Worksheet(PlatformWorkbookHash.MetaSheetName).IsProtected);
            }

            var roundTripped = io.Read(path);
            var meta = roundTripped.FindSheet(PlatformWorkbookHash.MetaSheetName);
            Assert.NotNull(meta);

            Assert.Equal(SnapshotId, MetaValue(meta!, "SourceSnapshotId"));
            Assert.Equal(PlatformWorkbookExporter.SchemaVersion, MetaValue(meta!, "SchemaVersion"));
            Assert.Equal(expectedHash, MetaValue(meta!, "WorkbookHash"));
            Assert.Equal(expectedHash, PlatformWorkbookHash.Compute(roundTripped));
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

    // -------------------------------------------------------------------------
    // 4. PE-W2 quarantine + PE empty-diff orthogonality
    // -------------------------------------------------------------------------

    /// <summary>
    /// Unedited workbook: empty-diff AND empty QuarantineEntries (quarantine orthogonal to clean plan).
    /// </summary>
    [Fact]
    public void Unedited_workbook_quarantine_empty_and_empty_diff_orthogonal()
    {
        var source = BalticLikeData();
        var edited = Export(source, clockTicks: 11_004);

        var plan = ImporterFor(source, clockTicks: 11_004).Plan(edited);

        Assert.True(plan.SnapshotResolved);
        Assert.False(plan.HasChanges);
        Assert.Empty(plan.Changes);
        Assert.Empty(plan.QuarantineEntries);
        Assert.False(plan.Blocked);

        var gate = new FakeWriteGate();
        var result = ImporterFor(source, clockTicks: 11_004).Stage(edited, gate, "human", "drgamtd", "empty");
        Assert.False(result.Staged); // no changes → nothing staged
        Assert.Empty(result.QuarantineEntries);
        Assert.Equal(0, gate.TotalProposalCount);
    }

    // -------------------------------------------------------------------------
    // 5. Large human-approval flag independent of quarantine on orphan edit
    // -------------------------------------------------------------------------

    /// <summary>
    /// RequiresHumanApproval remains true when change count exceeds threshold even if a separate
    /// orphan magazine FK edit produces quarantine entries (independence pin).
    /// </summary>
    [Fact]
    public void RequiresHumanApproval_independent_of_quarantine_on_separate_orphan_edit()
    {
        var source = ManySensors(11, basePdMilli: 100);
        var editedData = ManySensors(11, basePdMilli: 101) with
        {
            // Separate orphan FK edit → quarantine finding, not a human-approval input.
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "does-not-exist-mount", "mvp-weapon", 8, 0, 8),
            },
        };
        var edited = Export(editedData, clockTicks: 11_005);

        var plan = ImporterFor(source, clockTicks: 11_005).Plan(edited);

        Assert.True(plan.Changes.Count > PlatformWorkbookImporter.HumanApprovalRecordThreshold);
        Assert.True(plan.RequiresHumanApproval);
        Assert.True(plan.Blocked);
        Assert.NotEmpty(plan.QuarantineEntries);
        Assert.Contains(plan.QuarantineEntries, q =>
            q.Reason == PlatformWorkbookValidator.MagazineUnknownMount
            && q.EntityId == "does-not-exist-mount"
            && q.SourceSheet == "Magazines");
    }

    // -------------------------------------------------------------------------
    // 6. Sim-visible filter + Excel export path does not reintroduce provisional
    // -------------------------------------------------------------------------

    /// <summary>
    /// Export data containing a provisional sensor; CatalogTlExportFilter removes it; Excel
    /// ClosedXML re-export of the filtered slice must not reintroduce provisional into the
    /// sim-visible workbook.
    /// </summary>
    [Fact]
    public void Sim_visible_filter_excel_export_path_does_not_reintroduce_provisional()
    {
        var approved = new CatalogSensorBinding(
            "u1",
            "sensor-approved",
            0.85,
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.SourceFact,
            ImportBatchId: "nightly-cmo-fixture");
        var provisional = new CatalogSensorBinding(
            "u1",
            "sensor-provisional",
            0.90,
            ReviewState: CatalogReviewStates.Provisional,
            TrlLevel: 9,
            ValueTier: CatalogProvenanceTier.InterpretedValue,
            ImportBatchId: "excel-edit");

        var data = BalticLikeData() with
        {
            Sensors = new[] { approved, provisional },
        };

        Assert.Contains(data.Sensors, s =>
            string.Equals(s.ReviewState, CatalogReviewStates.Provisional, StringComparison.OrdinalIgnoreCase));

        var filtered = CatalogTlExportFilter.Apply(data, CatalogTlTier.Tl5);
        Assert.Contains(filtered.Sensors, s => s.SensorId == "sensor-approved");
        Assert.DoesNotContain(filtered.Sensors, s => s.SensorId == "sensor-provisional");
        Assert.DoesNotContain(filtered.Sensors, s =>
            string.Equals(s.ReviewState, CatalogReviewStates.Provisional, StringComparison.OrdinalIgnoreCase));

        var filteredWorkbook = Export(filtered, clockTicks: 11_006);
        var path = Path.Combine(Path.GetTempPath(), $"pe-cross-tl-filter-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(filteredWorkbook, path);
            var roundTripped = io.Read(path);

            var sensors = roundTripped.FindSheet("Sensors");
            Assert.NotNull(sensors);
            var reviewCol = Array.IndexOf(sensors!.Header.ToArray(), "ReviewState");
            var sensorIdCol = Array.IndexOf(sensors.Header.ToArray(), "SensorId");
            Assert.True(reviewCol >= 0 && sensorIdCol >= 0);

            Assert.DoesNotContain(sensors.Rows, r =>
                r.Count > sensorIdCol
                && string.Equals(r[sensorIdCol], "sensor-provisional", StringComparison.Ordinal));
            if (reviewCol >= 0)
            {
                Assert.DoesNotContain(sensors.Rows, r =>
                    r.Count > reviewCol
                    && string.Equals(r[reviewCol], CatalogReviewStates.Provisional, StringComparison.OrdinalIgnoreCase));
            }

            // ClosedXML data-sheet round-trip of the filtered export stays empty-diff (no provisional resurrected).
            Assert.True(PlatformWorkbookDiff.IsEmpty(filteredWorkbook, roundTripped));

            // Hard pin: re-applying TL filter to the filtered slice stays provisional-free and stable.
            var reFiltered = CatalogTlExportFilter.Apply(filtered, CatalogTlTier.Tl5);
            Assert.DoesNotContain(reFiltered.Sensors, s =>
                string.Equals(s.ReviewState, CatalogReviewStates.Provisional, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(filtered.Sensors.Count, reFiltered.Sensors.Count);
            Assert.Equal(
                filtered.Sensors.Select(s => s.SensorId).OrderBy(x => x, StringComparer.Ordinal).ToArray(),
                reFiltered.Sensors.Select(s => s.SensorId).OrderBy(x => x, StringComparer.Ordinal).ToArray());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    // -------------------------------------------------------------------------
    // 7. Determinism — two Plan() calls yield equal counts (no wall clock)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Two Plan() calls on the same workbook yield equal change and quarantine counts (determinism).
    /// </summary>
    [Fact]
    public void Plan_twice_on_same_workbook_yields_equal_change_and_quarantine_counts()
    {
        var source = BalticLikeData(sensorBasePdMilli: 850, magazineQty: 16);
        var editedData = BalticLikeData(sensorBasePdMilli: 500, magazineQty: 16) with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "orphan-mount", "mvp-weapon", 8, 0, 8),
            },
        };
        var edited = Export(editedData, clockTicks: 11_008);
        var importer = ImporterFor(source, clockTicks: 11_008);

        var planA = importer.Plan(edited);
        var planB = importer.Plan(edited);

        Assert.Equal(planA.Changes.Count, planB.Changes.Count);
        Assert.Equal(planA.QuarantineEntries.Count, planB.QuarantineEntries.Count);
        Assert.Equal(planA.Findings.Count, planB.Findings.Count);
        Assert.Equal(planA.RequiresHumanApproval, planB.RequiresHumanApproval);
        Assert.Equal(planA.Blocked, planB.Blocked);
        Assert.Equal(
            planA.Changes.Select(c => (c.Sheet, c.Kind, c.RowIndex, c.Detail)).ToArray(),
            planB.Changes.Select(c => (c.Sheet, c.Kind, c.RowIndex, c.Detail)).ToArray());
        Assert.Equal(
            planA.QuarantineEntries.Select(q => (q.Reason, q.PlatformId, q.EntityId, q.EntityKind)).ToArray(),
            planB.QuarantineEntries.Select(q => (q.Reason, q.PlatformId, q.EntityId, q.EntityKind)).ToArray());
    }

    // -------------------------------------------------------------------------
    // 8. FakeWriteGate never proposed on blocked plan
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validation error blocks Stage: zero proposals on FakeWriteGate; quarantine/findings consistent.
    /// </summary>
    [Fact]
    public void FakeWriteGate_never_proposed_on_blocked_plan_findings_quarantine_consistent()
    {
        var source = BalticLikeData(magazineQty: 16);
        // Over-capacity magazine → Error finding → Blocked; no Stage proposals.
        var edited = Export(BalticLikeData(magazineQty: 99), clockTicks: 11_009);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source, clockTicks: 11_009)
            .Stage(edited, gate, "human", "drgamtd", "blocked over-capacity");

        Assert.False(result.Staged);
        Assert.True(result.Plan.Blocked);
        Assert.Equal(0, gate.TotalProposalCount);
        Assert.Empty(gate.SensorProposals);
        Assert.Empty(gate.MountProposals);
        Assert.Empty(gate.LoadoutProposals);
        Assert.Empty(gate.MagazineProposals);
        Assert.Empty(gate.CommsProposals);
        Assert.Empty(gate.LinkProposals);
        Assert.Empty(gate.MobilityProposals);
        Assert.Empty(gate.SignatureProposals);
        Assert.Empty(gate.EmconProposals);
        Assert.Empty(gate.DamageProposals);
        Assert.Empty(gate.PlatformProposals);
        Assert.Empty(gate.WeaponProposals);
        Assert.Null(result.SensorBatchId);
        Assert.Null(result.MountBatchId);
        Assert.Null(result.LoadoutBatchId);
        Assert.Null(result.MagazineBatchId);
        Assert.Null(result.CommsBatchId);
        Assert.Null(result.LinkBatchId);
        Assert.Null(result.MobilityBatchId);
        Assert.Null(result.SignatureBatchId);
        Assert.Null(result.EmconBatchId);
        Assert.Null(result.DamageBatchId);

        Assert.Contains(result.Plan.Findings, f =>
            f.Code == PlatformWorkbookValidator.MagazineOverCapacity
            && f.Severity == ValidationSeverity.Error);

        // Over-capacity is an error finding but not an FK quarantine code — quarantine empty
        // while findings remain; plan/result quarantine counts stay consistent.
        Assert.Empty(result.Plan.QuarantineEntries);
        Assert.Empty(result.QuarantineEntries);
        Assert.Equal(result.Plan.QuarantineEntries.Count, result.QuarantineEntries.Count);
        Assert.Contains(result.Notes, n => n.Contains("validation finding", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Strengthening pin for vector 8: FK quarantine blocked plan also never proposes.
    /// </summary>
    [Fact]
    public void FakeWriteGate_never_proposed_when_fk_quarantine_blocks_plan()
    {
        var source = BalticLikeData();
        var editedData = BalticLikeData() with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "missing-mount", "mvp-weapon", 8, 0, 8),
            },
        };
        var edited = Export(editedData, clockTicks: 11_010);
        var gate = new FakeWriteGate();

        var result = ImporterFor(source, clockTicks: 11_010)
            .Stage(edited, gate, "human", "drgamtd", "fk quarantine block");

        Assert.False(result.Staged);
        Assert.True(result.Plan.Blocked);
        Assert.Equal(0, gate.TotalProposalCount);
        Assert.NotEmpty(result.QuarantineEntries);
        Assert.Contains(result.QuarantineEntries, q => q.Reason == PlatformWorkbookValidator.MagazineUnknownMount);
        Assert.Equal(result.Plan.QuarantineEntries.Count, result.QuarantineEntries.Count);
    }

    // -------------------------------------------------------------------------
    // Bonus cross pin: ClosedXML empty plan still empty after cell-level re-export path
    // -------------------------------------------------------------------------

    /// <summary>
    /// Export→ClosedXML Write→Read→Plan empty path is stable across two binary round-trips
    /// (determinism of PE-W1 binary path + PE empty-diff).
    /// </summary>
    [Fact]
    public void ClosedXml_double_round_trip_remains_empty_diff_and_empty_plan()
    {
        var source = BalticLikeData();
        var original = Export(source, clockTicks: 11_011);
        var path1 = Path.Combine(Path.GetTempPath(), $"pe-cross-d1-{Guid.NewGuid():N}.xlsx");
        var path2 = Path.Combine(Path.GetTempPath(), $"pe-cross-d2-{Guid.NewGuid():N}.xlsx");
        try
        {
            var io = new ClosedXmlPlatformWorkbookIo();
            io.Write(original, path1);
            var once = io.Read(path1);
            io.Write(once, path2);
            var twice = io.Read(path2);

            Assert.True(PlatformWorkbookDiff.IsEmpty(original, once));
            Assert.True(PlatformWorkbookDiff.IsEmpty(once, twice));
            Assert.True(PlatformWorkbookDiff.IsEmpty(original, twice));

            var plan = ImporterFor(source, clockTicks: 11_011).Plan(twice);
            Assert.False(plan.HasChanges);
            Assert.Empty(plan.QuarantineEntries);
            Assert.False(plan.Blocked);
        }
        finally
        {
            if (File.Exists(path1))
            {
                File.Delete(path1);
            }

            if (File.Exists(path2))
            {
                File.Delete(path2);
            }
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

        public int TotalProposalCount =>
            SensorProposals.Count
            + MountProposals.Count
            + LoadoutProposals.Count
            + MagazineProposals.Count
            + CommsProposals.Count
            + LinkProposals.Count
            + MobilityProposals.Count
            + SignatureProposals.Count
            + EmconProposals.Count
            + DamageProposals.Count
            + PlatformProposals.Count
            + WeaponProposals.Count;

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
