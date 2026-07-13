using Microsoft.Data.Sqlite;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Snapshots;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// PE-W2 adversarial TDD: quarantine (PLE-2.3), post-approve release (PLE-3.5),
/// TRL gate (PLE-4.4). Strengthens existing governance against silent gate leakage,
/// sort drift, and release bookkeeping corruption.
/// </summary>
[Collection("CatalogSqlite")]
public sealed class PlatformWorkbookGovernanceAdversarialTests
{
    private const string SnapshotId = "baltic_patrol";

    // ── PLE-2.3 quarantine ──────────────────────────────────────────────────

    /// <summary>
    /// Attack: orphan FK must never touch any FakeWriteGate proposal collection
    /// (not only MobilityProposals).
    /// </summary>
    [Fact]
    public void Adversarial_orphan_fk_never_reaches_any_FakeWriteGate_collection()
    {
        var source = BaseDataWithPhaseB();
        var editedData = BaseDataWithPhaseB(maxSpeedKnots: 30) with
        {
            Mobility = new[] { new CatalogMobility("unknown-platform", MaxSpeedKnots: 99) },
        };
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(Export(editedData), gate, "human", "adv-w2", "orphan");

        Assert.False(result.Staged);
        AssertGateUntouched(gate);
        Assert.NotEmpty(result.QuarantineEntries);
        Assert.Contains(result.QuarantineEntries, q =>
            q.Reason == PlatformWorkbookValidator.PhaseBOrphanPlatform
            && q.PlatformId == "unknown-platform");
    }

    /// <summary>
    /// Attack: two dangling magazine FKs → ≥2 quarantine entries, sorted by
    /// (EntityKind, PlatformId, EntityId).
    /// </summary>
    [Fact]
    public void Adversarial_multiple_fk_findings_quarantine_sorted_deterministically()
    {
        var source = BaseData();
        // One magazine row: unknown loadout + unknown mount → two FK findings.
        var editedData = BaseData() with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "ghost-loadout", "ghost-mount", "mvp-weapon", 4, 0, 4),
            },
        };
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(Export(editedData), gate, "human", "adv-w2", "multi-fk");

        Assert.False(result.Staged);
        AssertGateUntouched(gate);
        Assert.True(result.QuarantineEntries.Count >= 2, $"expected ≥2 quarantine, got {result.QuarantineEntries.Count}");
        Assert.Contains(result.QuarantineEntries, q => q.Reason == PlatformWorkbookValidator.MagazineUnknownMount);
        Assert.Contains(result.QuarantineEntries, q => q.Reason == PlatformWorkbookValidator.MagazineUnknownLoadout);

        AssertQuarantineSortedByEntityKindPlatformIdEntityId(result.QuarantineEntries);
        AssertQuarantineSortedByEntityKindPlatformIdEntityId(result.Plan.QuarantineEntries);
    }

    /// <summary>
    /// Attack: plan-time quarantine for pure FK must be a subset of stage-time when Stage
    /// adds no TRL rows (blocked early on validation errors).
    /// </summary>
    [Fact]
    public void Adversarial_plan_time_quarantine_subset_of_stage_for_pure_fk()
    {
        var source = BaseData();
        var editedData = BaseData() with
        {
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "asuw-default", "does-not-exist-mount", "mvp-weapon", 8, 0, 8),
            },
        };
        var edited = Export(editedData);
        var importer = ImporterFor(source);

        var plan = importer.Plan(edited);
        var stage = importer.Stage(edited, new FakeWriteGate(), "human", "adv-w2", "plan-stage-parity");

        Assert.NotEmpty(plan.QuarantineEntries);
        Assert.True(plan.Blocked);
        // Pure FK: Stage returns EmptyImportResult with plan quarantine only (no TRL append).
        Assert.Equal(plan.QuarantineEntries.Count, stage.QuarantineEntries.Count);
        foreach (var planEntry in plan.QuarantineEntries)
        {
            Assert.Contains(stage.QuarantineEntries, s =>
                s.EntityKind == planEntry.EntityKind
                && s.PlatformId == planEntry.PlatformId
                && s.EntityId == planEntry.EntityId
                && s.Reason == planEntry.Reason
                && s.SourceSheet == planEntry.SourceSheet);
        }
    }

    /// <summary>Attack: every quarantine entry must carry non-empty Reason and SourceSheet.</summary>
    [Fact]
    public void Adversarial_quarantine_detail_reason_and_source_sheet_always_set()
    {
        var source = BaseDataWithPhaseB();
        var editedData = BaseDataWithPhaseB() with
        {
            Mobility = new[] { new CatalogMobility("orphan-a", MaxSpeedKnots: 10) },
            Magazines = new[]
            {
                new CatalogMagazineEntry("u1", "missing-lo", "missing-mt", "w1", 1, 0, 1),
            },
        };
        var result = ImporterFor(source).Stage(Export(editedData), new FakeWriteGate(), "human", "adv-w2");

        Assert.NotEmpty(result.QuarantineEntries);
        foreach (var q in result.QuarantineEntries)
        {
            Assert.False(string.IsNullOrWhiteSpace(q.Reason), $"Reason empty for {q.EntityKind}/{q.PlatformId}/{q.EntityId}");
            Assert.False(string.IsNullOrWhiteSpace(q.SourceSheet), $"SourceSheet empty for {q.EntityKind}/{q.PlatformId}/{q.EntityId}");
        }
    }

    /// <summary>Attack: clean / unedited paths must leave quarantine empty.</summary>
    [Fact]
    public void Adversarial_approved_clean_paths_have_zero_quarantine()
    {
        var source = BaseData(
            sensors: new[]
            {
                new CatalogSensorBinding(
                    "u1",
                    "sensor-clean",
                    0.80,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9),
            });
        var editedClean = source with
        {
            Sensors = new[]
            {
                new CatalogSensorBinding(
                    "u1",
                    "sensor-clean",
                    0.55,
                    ReviewState: CatalogReviewStates.Approved,
                    TrlLevel: 9),
            },
        };

        var unedited = ImporterFor(source).Stage(Export(source), new FakeWriteGate(), "human", "adv-w2");
        Assert.Empty(unedited.QuarantineEntries);
        Assert.Empty(unedited.Plan.QuarantineEntries);

        var cleanEdit = ImporterFor(source).Stage(Export(editedClean), new FakeWriteGate(), "human", "adv-w2");
        Assert.True(cleanEdit.Staged);
        Assert.Empty(cleanEdit.QuarantineEntries);
    }

    // ── PLE-4.4 TRL gate ────────────────────────────────────────────────────

    /// <summary>
    /// Attack: mixed batch — only high-TRL approved stages; low-TRL provisional quarantined
    /// with CatalogImportGate rejection reason string present.
    /// </summary>
    [Fact]
    public void Adversarial_mixed_batch_only_high_trl_approved_stages()
    {
        var source = SensorPairSource(
            high: ("sensor-hi", CatalogReviewStates.Approved, 9, 0.80),
            low: ("sensor-lo", CatalogReviewStates.Provisional, 1, 0.80));
        var edited = SensorPairSource(
            high: ("sensor-hi", CatalogReviewStates.Approved, 9, 0.50),
            low: ("sensor-lo", CatalogReviewStates.Provisional, 1, 0.50));
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(Export(edited), gate, "human", "adv-w2", "mixed");

        Assert.True(result.Staged);
        var proposed = Assert.Single(Assert.Single(gate.SensorProposals));
        Assert.Equal("sensor-hi", proposed.SensorId);

        var q = Assert.Single(result.QuarantineEntries, e => e.EntityId == "sensor-lo");
        Assert.Equal("sensor", q.EntityKind);
        Assert.Contains(q.Reason, new[] { "trl_below_minimum", "review_state_provisional" });
        // CatalogImportGate reason must match PartitionForImport output for same binding.
        var lowBinding = edited.Sensors.Single(s => s.SensorId == "sensor-lo");
        var (_, gatedOut) = CatalogImportGate.PartitionForImport([lowBinding]);
        Assert.Equal(gatedOut[0].RejectionReason, q.Reason);
    }

    /// <summary>Attack: TrlLevel == DefaultMinimumTrl (4) + approved stages; 3 + approved quarantines.</summary>
    [Fact]
    public void Adversarial_boundary_trl_default_minimum_inclusive()
    {
        Assert.Equal(4, CatalogImportGate.DefaultMinimumTrl);

        var atFloor = StageSingleSensor(
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: CatalogImportGate.DefaultMinimumTrl,
            basePdBefore: 0.80,
            basePdAfter: 0.60);
        Assert.True(atFloor.Result.Staged);
        Assert.Empty(atFloor.Result.QuarantineEntries);
        Assert.Equal("sensor-boundary", Assert.Single(Assert.Single(atFloor.Gate.SensorProposals)).SensorId);

        var below = StageSingleSensor(
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: CatalogImportGate.DefaultMinimumTrl - 1,
            basePdBefore: 0.80,
            basePdAfter: 0.60);
        Assert.False(below.Result.Staged);
        Assert.Empty(below.Gate.SensorProposals);
        var q = Assert.Single(below.Result.QuarantineEntries);
        Assert.Equal("trl_below_minimum", q.Reason);
        Assert.Equal("sensor-boundary", q.EntityId);
    }

    /// <summary>Attack: Approved review state with TRL below floor must still quarantine (both gates).</summary>
    [Fact]
    public void Adversarial_approved_but_below_trl_quarantines()
    {
        var staged = StageSingleSensor(
            ReviewState: CatalogReviewStates.Approved,
            TrlLevel: 2,
            basePdBefore: 0.70,
            basePdAfter: 0.40);

        Assert.False(staged.Result.Staged);
        AssertGateUntouched(staged.Gate);
        var q = Assert.Single(staged.Result.QuarantineEntries);
        Assert.Equal("trl_below_minimum", q.Reason);
        Assert.Contains("TrlLevel=2", q.Detail, StringComparison.Ordinal);
        Assert.Contains("ReviewState=approved", q.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Attack: ReviewState casing must match CatalogImportGate OrdinalIgnoreCase —
    /// "Approved" and "approved" both pass when TRL is high.
    /// </summary>
    [Fact]
    public void Adversarial_review_state_casing_matches_CatalogImportGate()
    {
        // Direct gate: mixed case approved is accepted.
        var titleCase = new CatalogSensorBinding(
            "u1", "s1", 0.9, ReviewState: "Approved", TrlLevel: 9);
        var lowerCase = new CatalogSensorBinding(
            "u1", "s2", 0.9, ReviewState: "approved", TrlLevel: 9);
        var (gateApproved, gateQuarantined) = CatalogImportGate.PartitionForImport([titleCase, lowerCase]);
        Assert.Equal(2, gateApproved.Length);
        Assert.Empty(gateQuarantined);

        // Importer path: Title Case "Approved" written into workbook must stage like lowercase.
        var source = BaseData(sensors: new[]
        {
            new CatalogSensorBinding("u1", "sensor-case", 0.80, ReviewState: CatalogReviewStates.Approved, TrlLevel: 9),
        });
        var editedWb = Export(source with
        {
            Sensors = new[]
            {
                new CatalogSensorBinding("u1", "sensor-case", 0.50, ReviewState: "Approved", TrlLevel: 9),
            },
        });
        // Force cell casing in case exporter normalizes.
        editedWb = WithSheetCell(editedWb, "Sensors", 0, "ReviewState", "Approved");
        editedWb = WithSheetCell(editedWb, "Sensors", 0, "BasePd", "0.50");
        editedWb = WithSheetCell(editedWb, "Sensors", 0, "TrlLevel", "9");

        var gate = new FakeWriteGate();
        var result = ImporterFor(source).Stage(editedWb, gate, "human", "adv-w2", "case");

        Assert.True(result.Staged);
        Assert.Empty(result.QuarantineEntries);
        var proposed = Assert.Single(Assert.Single(gate.SensorProposals));
        Assert.Equal("sensor-case", proposed.SensorId);
        // Gate accepts Title Case the same as lowercase approved.
        Assert.Equal(
            CatalogReviewStates.Approved,
            proposed.ReviewState,
            ignoreCase: true,
            ignoreLineEndingDifferences: false,
            ignoreWhiteSpaceDifferences: false);
    }

    // ── PLE-3.5 release ─────────────────────────────────────────────────────

    /// <summary>
    /// Attack: RejectBatches / empty Approve must not invent a db_release row or ReleaseVersion.
    /// </summary>
    [Fact]
    public void Adversarial_no_commit_no_new_release()
    {
        var writeService = new PlatformWorkbookWriteService();
        var dbPath = CreateTempDbPath("adv-no-release");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            int releaseCountBefore;
            using (var storeBefore = new DbSnapshotStore(dbPath))
            {
                releaseCountBefore = storeBefore.GetSortedReleases().Count;
            }

            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9800));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.41");

            var propose = writeService.Propose(
                dbPath, edited, new FixedCatalogClock(9801), "human", "adv-w2", "no-release");
            var batchId = Assert.Single(propose.BatchIds);

            // Empty approve list: no commit → no release metadata.
            var emptyApprove = writeService.ApproveBatches(
                dbPath, Array.Empty<string>(), new FixedCatalogClock(9802), "human", "qa");
            Assert.Empty(emptyApprove.CommittedBatchIds);
            Assert.Null(emptyApprove.ReleaseVersion);
            Assert.Null(emptyApprove.SnapshotId);
            Assert.Null(emptyApprove.ContentHashSha256);

            var reject = writeService.RejectBatches(
                dbPath, [batchId], new FixedCatalogClock(9803), "human", "qa", "reject");
            Assert.Empty(reject.CommittedBatchIds);
            Assert.Null(reject.ReleaseVersion);
            Assert.Null(reject.SnapshotId);
            Assert.Null(reject.ContentHashSha256);

            using var storeAfter = new DbSnapshotStore(dbPath);
            Assert.Equal(releaseCountBefore, storeAfter.GetSortedReleases().Count);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    /// <summary>
    /// Attack: second ApproveBatches of already-committed ids must not crash and must not
    /// invent a second release version (staging gone → no commit → no BindAfterApprove).
    /// </summary>
    [Fact]
    public void Adversarial_double_ApproveBatches_idempotent_release_bookkeeping()
    {
        var writeService = new PlatformWorkbookWriteService();
        var dbPath = CreateTempDbPath("adv-double-approve");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9810));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.52");

            var propose = writeService.Propose(
                dbPath, edited, new FixedCatalogClock(9811), "human", "adv-w2", "double");
            var batchId = Assert.Single(propose.BatchIds);

            var first = writeService.ApproveBatches(
                dbPath, [batchId], new FixedCatalogClock(9812), "human", "qa");
            Assert.True(first.AllCommitted);
            Assert.False(string.IsNullOrWhiteSpace(first.ReleaseVersion));

            int releaseCountAfterFirst;
            using (var store = new DbSnapshotStore(dbPath))
            {
                releaseCountAfterFirst = store.GetSortedReleases().Count;
                Assert.Contains(store.GetSortedReleases(), r =>
                    string.Equals(r.ReleaseVersion, first.ReleaseVersion, StringComparison.Ordinal));
            }

            // Second approve of the same batch: staging cleared → explicit error, no new commit/release.
            var second = writeService.ApproveBatches(
                dbPath, [batchId], new FixedCatalogClock(9813), "human", "qa");
            Assert.Empty(second.CommittedBatchIds);
            Assert.Contains(batchId, second.ProcessedBatchIds);
            Assert.True(second.Errors.ContainsKey(batchId));
            Assert.Null(second.ReleaseVersion);
            Assert.Null(second.SnapshotId);

            using var storeAfter = new DbSnapshotStore(dbPath);
            Assert.Equal(releaseCountAfterFirst, storeAfter.GetSortedReleases().Count);
            Assert.Equal(1, storeAfter.GetSortedReleases()
                .Count(r => string.Equals(r.ReleaseVersion, first.ReleaseVersion, StringComparison.Ordinal)));
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    /// <summary>
    /// Attack: propose-only path never creates a release (negative of BindAfterApprove on commit).
    /// </summary>
    [Fact]
    public void Adversarial_propose_only_never_creates_release()
    {
        var writeService = new PlatformWorkbookWriteService();
        var dbPath = CreateTempDbPath("adv-propose-only");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            int releaseCountBefore;
            using (var storeBefore = new DbSnapshotStore(dbPath))
            {
                releaseCountBefore = storeBefore.GetSortedReleases().Count;
            }

            var exported = writeService.ExportFromDatabase(dbPath, SnapshotId, new FixedCatalogClock(9820));
            var edited = WithSheetCell(exported, "Sensors", 0, "BasePd", "0.44");

            var propose = writeService.Propose(
                dbPath, edited, new FixedCatalogClock(9821), "human", "adv-w2", "propose-only");
            Assert.True(propose.Proposed);
            Assert.NotEmpty(propose.BatchIds);

            using var storeAfter = new DbSnapshotStore(dbPath);
            Assert.Equal(releaseCountBefore, storeAfter.GetSortedReleases().Count);
        }
        finally
        {
            Cleanup(dbPath);
        }
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    private static PlatformCatalogExportData BaseData(
        int magazineQty = 16,
        IReadOnlyList<CatalogSensorBinding>? sensors = null) => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: sensors ?? new[]
        {
            new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85, CitationRef: "/sensor/1/"),
            new CatalogSensorBinding("u1", "cmo-sensor-2", 0.40),
        },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", magazineQty, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Links: new[]
        {
            new CatalogLinkEntry("NATO_TADIL_J", "NATO Link 16", CatalogLinkTypes.Tactical, LatencyMsNominal: 50),
        });

    private static PlatformCatalogExportData BaseDataWithPhaseB(double maxSpeedKnots = 30) =>
        BaseData() with
        {
            Mobility = new[] { new CatalogMobility("u1", MaxSpeedKnots: maxSpeedKnots, CruiseSpeedKnots: 18, RangeNm: 4000) },
            Signatures = new[] { new CatalogSignature("u1", RcsBandDbsm: -10, AcousticSignatureDb: 95) },
            Emcon = new[] { new CatalogEmcon("u1", "silent", "cmo-sensor-1", "off") },
        };

    private static PlatformCatalogExportData SensorPairSource(
        (string Id, string Review, int Trl, double Pd) high,
        (string Id, string Review, int Trl, double Pd) low) =>
        BaseData(sensors: new[]
        {
            new CatalogSensorBinding("u1", high.Id, high.Pd, ReviewState: high.Review, TrlLevel: high.Trl),
            new CatalogSensorBinding("u1", low.Id, low.Pd, ReviewState: low.Review, TrlLevel: low.Trl),
        });

    private static (PlatformImportResult Result, FakeWriteGate Gate) StageSingleSensor(
        string ReviewState,
        int TrlLevel,
        double basePdBefore,
        double basePdAfter)
    {
        var source = BaseData(sensors: new[]
        {
            new CatalogSensorBinding(
                "u1",
                "sensor-boundary",
                basePdBefore,
                ReviewState: ReviewState,
                TrlLevel: TrlLevel),
        });
        var edited = BaseData(sensors: new[]
        {
            new CatalogSensorBinding(
                "u1",
                "sensor-boundary",
                basePdAfter,
                ReviewState: ReviewState,
                TrlLevel: TrlLevel),
        });
        var gate = new FakeWriteGate();
        var result = ImporterFor(source).Stage(Export(edited), gate, "human", "adv-w2", "boundary");
        return (result, gate);
    }

    private static PlatformWorkbook Export(PlatformCatalogExportData data, string snapshotId = SnapshotId) =>
        new PlatformWorkbookExporter().Export(data, snapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    private static void AssertGateUntouched(FakeWriteGate gate)
    {
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
    }

    private static void AssertQuarantineSortedByEntityKindPlatformIdEntityId(
        IReadOnlyList<PlatformImportQuarantineEntry> entries)
    {
        var expected = entries
            .OrderBy(e => e.EntityKind, StringComparer.Ordinal)
            .ThenBy(e => e.PlatformId, StringComparer.Ordinal)
            .ThenBy(e => e.EntityId, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expected, entries.ToArray());
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

    private static string CreateTempDbPath(string label) =>
        Path.Combine(Path.GetTempPath(), $"aegis-{label}-{Guid.NewGuid():N}.db");

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
