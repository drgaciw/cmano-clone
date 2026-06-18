using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using Xunit;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S24-05: Phase B validator rule pack — header parity, FK/orphan guard,
/// Emcon enum sanity, mobility sanity bounds, deterministic validation hash (PLE-4.1–4.3).
/// </summary>
public sealed class CatalogPhaseBValidationTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static PlatformCatalogExportData PhaseBData() => new(
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
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    [Fact]
    public void CatalogPhaseB_clean_workbook_has_no_validation_findings()
    {
        var workbook = Export(PhaseBData());

        Assert.Empty(PlatformWorkbookValidator.Validate(workbook));
    }

    [Fact]
    public void CatalogPhaseB_mobility_header_mismatch_is_blocking()
    {
        var workbook = WithSheetHeader(Export(PhaseBData()), "Mobility", ["PlatformId", "BadColumn"]);

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.MobilityHeaderMismatch, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_signatures_header_mismatch_is_blocking()
    {
        var workbook = WithSheetHeader(Export(PhaseBData()), "Signatures", ["PlatformId", "BadColumn"]);

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.SignaturesHeaderMismatch, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_emcon_header_mismatch_is_blocking()
    {
        var workbook = WithSheetHeader(Export(PhaseBData()), "Emcon", ["PlatformId", "BadColumn"]);

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.EmconHeaderMismatch, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_orphan_mobility_platform_is_blocking()
    {
        var data = PhaseBData() with
        {
            Mobility = new[] { new CatalogMobility("ghost-platform", MaxSpeedKnots: 20) },
        };

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(Export(data)));
        Assert.Equal(PlatformWorkbookValidator.PhaseBOrphanPlatform, finding.Code);
        Assert.Equal("ghost-platform", finding.UnitId);
    }

    [Fact]
    public void CatalogPhaseB_orphan_signature_platform_is_blocking()
    {
        var data = PhaseBData() with
        {
            Signatures = new[] { new CatalogSignature("ghost-platform", RcsBandDbsm: -5) },
        };

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(Export(data)));
        Assert.Equal(PlatformWorkbookValidator.PhaseBOrphanPlatform, finding.Code);
        Assert.Equal("ghost-platform", finding.UnitId);
    }

    [Fact]
    public void CatalogPhaseB_orphan_emcon_platform_is_blocking()
    {
        var data = PhaseBData() with
        {
            Emcon = new[] { new CatalogEmcon("ghost-platform", "silent", "radar-1", "off") },
        };

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(Export(data)));
        Assert.Equal(PlatformWorkbookValidator.PhaseBOrphanPlatform, finding.Code);
        Assert.Equal("ghost-platform", finding.UnitId);
    }

    [Fact]
    public void CatalogPhaseB_invalid_emcon_condition_is_blocking()
    {
        var workbook = WithSheetCell(Export(PhaseBData()), "Emcon", 0, "Condition", "loud");

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.EmconInvalidCondition, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_invalid_emcon_posture_is_blocking()
    {
        var workbook = WithSheetCell(Export(PhaseBData()), "Emcon", 0, "Posture", "passive");

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.EmconInvalidPosture, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_emcon_enums_accept_case_insensitive_values()
    {
        var workbook = WithSheetCell(
            WithSheetCell(Export(PhaseBData()), "Emcon", 0, "Condition", "SILENT"),
            "Emcon",
            0,
            "Posture",
            "ACTIVE");

        Assert.Empty(PlatformWorkbookValidator.Validate(workbook));
    }

    [Fact]
    public void CatalogPhaseB_negative_max_speed_is_blocking()
    {
        var workbook = WithSheetCell(Export(PhaseBData()), "Mobility", 0, "MaxSpeedKnots", "-1");

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.MobilityNegativeSpeed, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_negative_range_is_blocking()
    {
        var workbook = WithSheetCell(Export(PhaseBData()), "Mobility", 0, "RangeNm", "-5");

        var finding = Assert.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.Equal(PlatformWorkbookValidator.MobilityNegativeRange, finding.Code);
        Assert.Equal(ValidationSeverity.Error, finding.Severity);
    }

    [Fact]
    public void CatalogPhaseB_validation_findings_are_sorted_deterministically()
    {
        var data = PhaseBData() with
        {
            Mobility = new[] { new CatalogMobility("ghost-platform", MaxSpeedKnots: -1, RangeNm: -2) },
            Emcon = new[] { new CatalogEmcon("u1", "invalid", "radar-1", "passive") },
        };
        var workbook = Export(data);

        var first = PlatformWorkbookValidator.Validate(workbook);
        var second = PlatformWorkbookValidator.Validate(workbook);

        Assert.Equal(first.Select(f => f.Code), second.Select(f => f.Code));
        Assert.Equal(first.Select(f => f.Message), second.Select(f => f.Message));
    }

    [Fact]
    public void CatalogPhaseB_clean_workbook_validation_hash_matches_golden()
    {
        var report = ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(Export(PhaseBData())));

        Assert.True(report.Passed);
        Assert.Equal(ValidationGoldenHashes.PhaseBCleanWorkbook, report.ReportHash);
        Assert.Equal(report.ReportHash, ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(Export(PhaseBData()))).ReportHash);
    }

    [Fact]
    public void CatalogPhaseB_fixture_workbook_validation_hash_matches_golden()
    {
        var workbook = BuildFixtureWorkbookWithErrors();
        var report = ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(workbook));

        Assert.False(report.Passed);
        Assert.Equal(ValidationGoldenHashes.PhaseBFixtureErrors, report.ReportHash);
        Assert.Equal(report.ReportHash, ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(workbook)).ReportHash);
    }

    [Fact]
    public void CatalogPhaseB_validation_error_blocks_importer_staging()
    {
        var source = PhaseBData();
        var edited = WithSheetCell(Export(source), "Emcon", 0, "Posture", "passive");
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.True(result.Plan.Blocked);
        Assert.False(result.Staged);
        Assert.Empty(gate.EmconProposals);
        Assert.Contains(result.Plan.Findings, f => f.Code == PlatformWorkbookValidator.EmconInvalidPosture);
    }

    private static PlatformWorkbook BuildFixtureWorkbookWithErrors()
    {
        var data = PhaseBData() with
        {
            Mobility = new[] { new CatalogMobility("ghost-platform", MaxSpeedKnots: -1, RangeNm: -2) },
            Emcon = new[] { new CatalogEmcon("u1", "invalid", "radar-1", "passive") },
        };

        return WithSheetHeader(Export(data), "Signatures", ["PlatformId", "BadColumn"]);
    }

    private static PlatformWorkbook WithSheetHeader(
        PlatformWorkbook workbook,
        string sheetName,
        IReadOnlyList<string> header)
    {
        var sheets = workbook.Sheets.Select(sheet =>
            string.Equals(sheet.Name, sheetName, StringComparison.Ordinal)
                ? sheet with { Header = header }
                : sheet).ToArray();

        return workbook with { Sheets = sheets };
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