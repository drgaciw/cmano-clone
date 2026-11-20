using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;
using ProjectAegis.Data.Validation;
using ProjectAegis.Data.WriteGate;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Platform;

/// <summary>
/// Req-21 / ADR-011 S25-06: Phase B damage validator rule pack — HP bounds, withdraw threshold,
/// flag sanity, deterministic validation hash (PLE-4.1–4.3, DBI-2.2).
/// </summary>
public sealed class CatalogPhaseBDamageValidationTests
{
    private const string SnapshotId = CatalogValidationDefaults.BalticSnapshotId;

    private static PlatformCatalogExportData PhaseBData(
        double maxHp = 100,
        double withdrawThresholdPct = 25,
        int criticalFlags = 0) => new(
        Platforms: new[] { new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0) },
        Sensors: new[] { new CatalogSensorBinding("u1", "cmo-sensor-1", 0.85) },
        Mounts: new[] { new CatalogMount("u1", "vls-fwd", "vls", 360.0, 32) },
        Loadouts: new[] { new CatalogLoadout("u1", "asuw-default", "ASuW", "asuw", IsDefault: true) },
        Magazines: new[] { new CatalogMagazineEntry("u1", "asuw-default", "vls-fwd", "mvp-weapon", 16, 0, 32) },
        Comms: new[] { new CatalogCommsBinding("u1", "NATO_TADIL_J") },
        Mobility: new[] { new CatalogMobility("u1", MaxSpeedKnots: 30, CruiseSpeedKnots: 18, RangeNm: 4000) },
        Signatures: new[] { new CatalogSignature("u1", RcsBandDbsm: -10, AcousticSignatureDb: 95) },
        Emcon: new[] { new CatalogEmcon("u1", "silent", "cmo-sensor-1", "off") },
        Damage: new[] { new CatalogPlatformDamage("u1", MaxHp: maxHp, WithdrawThresholdPct: withdrawThresholdPct, CriticalFlags: criticalFlags) });

    private static PlatformWorkbook Export(PlatformCatalogExportData data) =>
        new PlatformWorkbookExporter().Export(data, SnapshotId, new FixedCatalogClock(0));

    private static PlatformWorkbookImporter ImporterFor(PlatformCatalogExportData source) =>
        new(id => string.Equals(id, SnapshotId, StringComparison.Ordinal) ? source : null, new FixedCatalogClock(0));

    [Test]
    public void CatalogPhaseB_damage_clean_workbook_has_no_validation_findings()
    {
        Assert.That(PlatformWorkbookValidator.Validate(Export(PhaseBData())), Is.Empty);
    }

    [Test]
    public void CatalogPhaseB_platforms_header_mismatch_is_blocking()
    {
        var workbook = WithPlatformsHeader(Export(PhaseBData()), ["PlatformId", "BadColumn"]);

        var findings = PlatformWorkbookValidator.Validate(workbook);
        var finding = AssertEx.Single(findings, f => f.Code == PlatformWorkbookValidator.PlatformsHeaderMismatch);
        Assert.That(finding.Severity, Is.EqualTo(ValidationSeverity.Error));
    }

    [Test]
    public void CatalogPhaseB_non_positive_MaxHp_is_blocking()
    {
        var workbook = WithPlatformCell(
            WithPlatformCell(Export(PhaseBData()), "u1", "WithdrawThresholdPct", "0"),
            "u1",
            "MaxHp",
            "0");

        var findings = PlatformWorkbookValidator.Validate(workbook);
        var finding = AssertEx.Single(findings, f => f.Code == PlatformWorkbookValidator.DamageNonPositiveMaxHp);
        Assert.That(finding.UnitId, Is.EqualTo("u1"));
    }

    [Test]
    public void CatalogPhaseB_negative_MaxHp_is_blocking()
    {
        var workbook = WithPlatformCell(
            WithPlatformCell(Export(PhaseBData()), "u1", "WithdrawThresholdPct", "0"),
            "u1",
            "MaxHp",
            "-5");

        var findings = PlatformWorkbookValidator.Validate(workbook);
        Assert.That(findings.Any(f => f.Code == PlatformWorkbookValidator.DamageNonPositiveMaxHp), Is.True);
    }

    [Test]
    public void CatalogPhaseB_MaxHp_above_ceiling_is_blocking()
    {
        var over = (PlatformWorkbookValidator.MaxHpCeiling + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var workbook = WithPlatformCell(Export(PhaseBData()), "u1", "MaxHp", over);

        var finding = AssertEx.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.That(finding.Code, Is.EqualTo(PlatformWorkbookValidator.DamageMaxHpExceedsCeiling));
    }

    [Test]
    public void CatalogPhaseB_negative_withdraw_threshold_is_blocking()
    {
        var workbook = WithPlatformCell(Export(PhaseBData()), "u1", "WithdrawThresholdPct", "-1");

        var finding = AssertEx.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.That(finding.Code, Is.EqualTo(PlatformWorkbookValidator.DamageWithdrawThresholdInvalid));
    }

    [Test]
    public void CatalogPhaseB_withdraw_threshold_above_MaxHp_is_blocking()
    {
        var workbook = WithPlatformCell(
            WithPlatformCell(Export(PhaseBData()), "u1", "MaxHp", "50"),
            "u1",
            "WithdrawThresholdPct",
            "75");

        var finding = AssertEx.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.That(finding.Code, Is.EqualTo(PlatformWorkbookValidator.DamageWithdrawThresholdInvalid));
    }

    [Test]
    public void CatalogPhaseB_withdraw_threshold_at_zero_and_MaxHp_boundary_passes()
    {
        var workbook = WithPlatformCell(
            WithPlatformCell(Export(PhaseBData()), "u1", "MaxHp", "100"),
            "u1",
            "WithdrawThresholdPct",
            "0");

        Assert.That(PlatformWorkbookValidator.Validate(workbook), Is.Empty);
    }

    [Test]
    public void CatalogPhaseB_withdraw_threshold_equal_to_MaxHp_passes()
    {
        var workbook = WithPlatformCell(
            WithPlatformCell(Export(PhaseBData()), "u1", "MaxHp", "100"),
            "u1",
            "WithdrawThresholdPct",
            "100");

        Assert.That(PlatformWorkbookValidator.Validate(workbook), Is.Empty);
    }

    [Test]
    public void CatalogPhaseB_negative_critical_flags_is_blocking()
    {
        var workbook = WithPlatformCell(Export(PhaseBData()), "u1", "CriticalFlags", "-1");

        var finding = AssertEx.Single(PlatformWorkbookValidator.Validate(workbook));
        Assert.That(finding.Code, Is.EqualTo(PlatformWorkbookValidator.DamageCriticalFlagsInvalid));
    }

    [Test]
    public void CatalogPhaseB_damage_fixture_validation_hash_matches_golden()
    {
        var workbook = BuildDamageFixtureWorkbookWithErrors();
        var report = ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(workbook));

        Assert.That(report.Passed, Is.False);
        Assert.That(report.ReportHash, Is.EqualTo(ValidationGoldenHashes.PhaseBDamageFixtureErrors));
        Assert.That(ValidationReport.FromFindings(PlatformWorkbookValidator.Validate(workbook)).ReportHash, Is.EqualTo(report.ReportHash));
    }

    [Test]
    public void CatalogPhaseB_damage_validation_error_blocks_importer_staging()
    {
        var source = PhaseBData();
        var edited = WithPlatformCell(Export(source), "u1", "MaxHp", "-1");
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.That(result.Plan.Blocked, Is.True);
        Assert.That(result.Staged, Is.False);
        Assert.That(gate.DamageProposals, Is.Empty);
        Assert.That(result.Plan.Findings.Any(f => f.Code == PlatformWorkbookValidator.DamageNonPositiveMaxHp), Is.True);
    }

    [Test]
    public void CatalogPhaseB_valid_damage_edit_still_stages_when_other_fields_valid()
    {
        var source = PhaseBData(maxHp: 100);
        var edited = WithPlatformCell(Export(PhaseBData(maxHp: 120)), "u1", "MaxHp", "120");
        var gate = new FakeWriteGate();

        var result = ImporterFor(source).Stage(edited, gate, "human", "drgamtd");

        Assert.That(result.Plan.Blocked, Is.False);
        Assert.That(result.Staged, Is.True);
        Assert.That(result.DamageBatchId, Is.Not.Null);
    }

    private static PlatformWorkbook BuildDamageFixtureWorkbookWithErrors()
    {
        var data = PhaseBData(maxHp: 100, withdrawThresholdPct: 25) with
        {
            Platforms =
            [
                new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
                new CatalogPlatformEntry("u2", 58.0, 21.0, 300.0),
            ],
            Damage =
            [
                new CatalogPlatformDamage("u1", MaxHp: -1, WithdrawThresholdPct: -5, CriticalFlags: -2),
                new CatalogPlatformDamage("u2", MaxHp: 50, WithdrawThresholdPct: 75),
            ],
        };

        return Export(data);
    }

    private static PlatformWorkbook WithPlatformsHeader(PlatformWorkbook workbook, IReadOnlyList<string> header)
    {
        var sheets = workbook.Sheets.Select(sheet =>
            string.Equals(sheet.Name, "Platforms", StringComparison.Ordinal)
                ? sheet with { Header = header }
                : sheet).ToArray();

        return new PlatformWorkbook(sheets);
    }

    private static PlatformWorkbook WithPlatformCell(
        PlatformWorkbook workbook,
        string platformId,
        string columnName,
        string value)
    {
        var sheet = workbook.FindSheet("Platforms");
        Assert.That(sheet, Is.Not.Null);
        var platformCol = Array.IndexOf(sheet!.Header.ToArray(), "PlatformId");
        var targetCol = Array.IndexOf(sheet.Header.ToArray(), columnName);
        Assert.That(platformCol >= 0, Is.True);
        Assert.That(targetCol >= 0, Is.True);

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

        var sheets = workbook.Sheets.Select(s =>
        {
            if (!string.Equals(s.Name, "Platforms", StringComparison.Ordinal))
            {
                return s;
            }

            var rows = s.Rows.Select((row, i) =>
            {
                if (i != rowIndex)
                {
                    return row;
                }

                var cells = row.ToList();
                while (cells.Count <= targetCol)
                {
                    cells.Add(string.Empty);
                }

                cells[targetCol] = value;
                return cells;
            }).ToArray();

            return s with { Rows = rows };
        }).ToArray();

        return new PlatformWorkbook(sheets);
    }

    private sealed class FakeWriteGate : IWriteGate
    {
        public List<IReadOnlyList<CatalogPlatformDamage>> DamageProposals { get; } = new();

        public string ProposeSensorBatch(IReadOnlyList<CatalogSensorBinding> proposed, string actorType, string actorId, string rationale = "") => "fake-sensor";
        public string ProposeMountBatch(IReadOnlyList<CatalogMount> proposed, string actorType, string actorId, string rationale = "") => "fake-mount";
        public string ProposeLoadoutBatch(IReadOnlyList<CatalogLoadout> proposed, string actorType, string actorId, string rationale = "") => "fake-loadout";
        public string ProposeMagazineBatch(IReadOnlyList<CatalogMagazineEntry> proposed, string actorType, string actorId, string rationale = "") => "fake-magazine";
        public string ProposeCommsBatch(IReadOnlyList<CatalogCommsBinding> proposed, string actorType, string actorId, string rationale = "") => "fake-comms";
        public string ProposeLinkCatalogBatch(IReadOnlyList<CatalogLinkEntry> proposed, string actorType, string actorId, string rationale = "") => "fake-link";
        public string ProposeMobilityBatch(IReadOnlyList<CatalogMobility> proposed, string actorType, string actorId, string rationale = "") => "fake-mobility";
        public string ProposeSignatureBatch(IReadOnlyList<CatalogSignature> proposed, string actorType, string actorId, string rationale = "") => "fake-signature";
        public string ProposeEmconBatch(IReadOnlyList<CatalogEmcon> proposed, string actorType, string actorId, string rationale = "") => "fake-emcon";
        public string ProposePlatformBatch(IReadOnlyList<CatalogPlatformBinding> proposed, string actorType, string actorId, string rationale = "") => "fake-platform";
        public string ProposeWeaponBatch(IReadOnlyList<CatalogWeaponRecord> proposed, string actorType, string actorId, string rationale = "") => "fake-weapon";

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

        public WriteGateDecision ApproveBatch(string batchId, string actorType, string actorId) => new(true, batchId, []);
        public WriteGateDecision RejectBatch(string batchId, string actorType, string actorId, string rationale = "") => new(true, batchId, []);
        public IReadOnlyList<CatalogStagingBatchSummary> ListPendingBatches() => [];
    }
}