namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

public sealed class ScenarioValidationEngineTests
{
    private readonly ScenarioValidationEngine _engine = new();
    private readonly ValidationConfig _config = new();

    [Test]
    public void Validate_same_scenario_produces_identical_report_hash()
    {
        var scenario = StrikeScenario();
        var catalog = ValidationCatalogFixture.Default();

        var a = _engine.Validate(scenario, catalog, _config);
        var b = _engine.Validate(scenario, catalog, _config);

        Assert.That(b.ReportHash, Is.EqualTo(a.ReportHash));
        Assert.That(a.Passed, Is.True);
    }

    [Test]
    public void Strike_no_targets_emits_STRIKE_NO_TARGETS()
    {
        var scenario = new ScenarioDocumentDto
        {
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = [],
                },
            ],
        };

        var report = _engine.Validate(scenario, ValidationCatalogFixture.Default(), _config);
        Assert.That(report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
        Assert.That(report.CanExport(_config), Is.False);
    }

    [Test]
    public void Strike_unreachable_emits_STRIKE_UNREACHABLE_with_excess_nm()
    {
        var scenario = StrikeScenario();
        var report = _engine.Validate(scenario, ValidationCatalogFixture.Unreachable(), _config);
        var finding = AssertEx.Single(report.Findings, f => f.Code == "STRIKE_UNREACHABLE");
        Assert.That(finding.Data, Is.Not.Null);
        Assert.That(string.IsNullOrEmpty(finding.Data!["excess_nm"]), Is.False);
    }

    /// <summary>
    /// Doc 11 AC-1: the STRIKE_UNREACHABLE finding message must take the exact form
    /// "…out of combat radius by N nm…" (see StrikeReachabilityRule in ValidationRules.cs).
    /// ValidationCatalogFixture.Unreachable() positions 'u1' and 'tgt-1' 800 nm apart (along a
    /// meridian) against a 400 nm combat radius, which Haversine resolves to ~800.539 nm —
    /// excess = distance - combatRadiusNm = ~400.539 nm, rounded to 400.5.
    /// </summary>
    [Test]
    public void Strike_unreachable_message_matches_AC1_format_with_computed_excess_nm()
    {
        var scenario = StrikeScenario();
        var report = _engine.Validate(scenario, ValidationCatalogFixture.Unreachable(), _config);
        var finding = AssertEx.Single(report.Findings, f => f.Code == "STRIKE_UNREACHABLE");

        Assert.That(            finding.Message, Is.EqualTo(            "Strike mission 'strike-1' target 'tgt-1' is out of combat radius by 400.5 nm."));
    }

    [Test]
    public void Strike_within_combat_radius_but_over_fuel_budget_emits_STRIKE_UNREACHABLE_FUEL()
    {
        var scenario = StrikeScenario();
        var report = _engine.Validate(scenario, ValidationCatalogFixture.FuelDominated(), _config);
        var finding = AssertEx.Single(report.Findings, f => f.Code == "STRIKE_UNREACHABLE_FUEL");
        Assert.That(finding.Data, Is.Not.Null);
        Assert.That(string.IsNullOrEmpty(finding.Data!["excess_nm"]), Is.False);
    }

    [Test]
    public void Mission_no_units_emits_MISSION_NO_UNITS()
    {
        var scenario = new ScenarioDocumentDto
        {
            Missions = [new ScenarioMissionDto { Id = "patrol-1", Type = "Patrol" }],
        };

        var report = _engine.Validate(scenario, ValidationCatalogFixture.Default(), _config);
        Assert.That(report.Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);
    }

    /// <summary>
    /// One of the six doc-11 AME-6.2 v1 rule codes (AC-3a-f). Prior coverage only exercised
    /// DB_MISMATCH inside the combined ValidationGoldenTests.All_six_v1_rules_emit_expected_codes
    /// test — this pins it down as its own focused unit test, mirroring
    /// Mission_no_units_emits_MISSION_NO_UNITS above. Uses InMemoryCatalogReader (not the local
    /// ValidationCatalogFixture, whose TryResolveDbRef always returns true and would never fire
    /// DB_MISMATCH).
    /// </summary>
    [Test]
    public void Db_ref_mismatch_emits_DB_MISMATCH()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { DbRef = "missing-db-xyz" },
        };

        var report = _engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), _config);
        Assert.That(report.Findings.Any(f => f.Code == "DB_MISMATCH"), Is.True);
    }

    private static ScenarioDocumentDto StrikeScenario() =>
        new()
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-1",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["tgt-1"],
                },
            ],
        };

    private sealed class ValidationCatalogFixture : ICatalogReader
    {
        private readonly double _combatRadiusNm;
        private readonly double _separationNm;

        private ValidationCatalogFixture(double combatRadiusNm, double separationNm)
        {
            _combatRadiusNm = combatRadiusNm;
            _separationNm = separationNm;
        }

        public static ICatalogReader Default() => new ValidationCatalogFixture(500, 200);

        public static ICatalogReader Unreachable() => new ValidationCatalogFixture(400, 800);

        /// <summary>~350 nm separation: inside 400 nm combat radius but over 290 nm fuel budget.</summary>
        public static ICatalogReader FuelDominated() => new ValidationCatalogFixture(400, 350);

        public string LayerVersion => "validation-test";

        public IReadOnlyList<CatalogSensorBinding> GetSortedSensorBindings() => Array.Empty<CatalogSensorBinding>();

        public bool TryGetBasePd(string platformId, string sensorId, out double basePd)
        {
            basePd = 0;
            return false;
        }

        public bool TryResolveDbRef(string dbRef, out string resolvedSnapshotId)
        {
            resolvedSnapshotId = dbRef;
            return true;
        }

        public bool TryGetSnapshotBranch(string snapshotId, out string branch)
        {
            branch = CatalogTlTier.Tl0;
            return true;
        }

        public bool TryResolveSnapshotForTlBranch(string tlBranch, out string snapshotId, out string dbRef)
        {
            snapshotId = "";
            dbRef = "";

            if (!string.Equals(CatalogTlTier.Normalize(tlBranch), CatalogTlTier.Tl0, StringComparison.Ordinal))
            {
                return false;
            }

            snapshotId = "validation-snapshot";
            dbRef = "validation-snapshot";
            return true;
        }

        public bool TryGetCombatRadiusNm(string platformId, out double combatRadiusNm)
        {
            combatRadiusNm = _combatRadiusNm;
            return platformId == "u1";
        }

        public bool TryGetPlatformPosition(string platformId, out double latDeg, out double lonDeg)
        {
            if (platformId == "u1")
            {
                latDeg = 57.0;
                lonDeg = 20.0;
                return true;
            }

            if (platformId == "tgt-1")
            {
                latDeg = 57.0 + (_separationNm / 60.0);
                lonDeg = 20.0;
                return true;
            }

            latDeg = 0;
            lonDeg = 0;
            return false;
        }
    }

    [Test]
    public void Editor_migration_preview_from_clean_editor_plus_legacy_unit_produces_nonzero_ObsoleteCount_BrokenMounts_and_real_delta() // proof update
    {
        // clean start state, real shipped editor + pure preview Compute, no mocks of preview
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("__verif-legacy", new[] { "legacy-patrol-ship" }, new string[0]);
        var current = InMemoryCatalogReader.BalticPatrolFixture();
        var target = InMemoryCatalogReader.BalticV3Fixture();
        var res = ScenarioDbMigrationPreview.Compute(editor.ToDto(), current, target);
        // numeric from real preview + legacy in patrol platforms (position) + mount vs v3
        Assert.That(res.ObsoleteCount >= 1, Is.True, "ObsoleteCount");
        Assert.That(res.BrokenMounts >= 1, Is.True, "BrokenMounts");
        var report = res.Report;
        Assert.That(report, Does.Contain("obsolete=1"));
        Assert.That(report, Does.Contain("broken_mounts=1"));
    }

    [Test]
    public void Editor_live_validation_and_migration_from_clean_state_emits_findings_and_preview_counts()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("s-no-tgt", new[] { "u1" }, new string[0]);
        var engine = new ScenarioValidationEngine();
        var config = new ValidationConfig();
        var catalog = ValidationCatalogFixture.Default();
        var report = engine.Validate(editor.ToDto(), catalog, config);
        Assert.That(report.Findings.Any(f => f.Code == "STRIKE_NO_TARGETS"), Is.True);
        // also drive preview numeric
        editor.AddStrikeMission("__mig-legacy", new[] { "legacy-patrol-ship" }, new string[0]);
        var mig = ScenarioDbMigrationPreview.Compute(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), InMemoryCatalogReader.BalticV3Fixture());
        Assert.That(mig.ObsoleteCount >= 1, Is.True);
        Assert.That(mig.BrokenMounts >= 1, Is.True);
    }
}