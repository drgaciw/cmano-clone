namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

public sealed class ScenarioValidationEngineTests
{
    private readonly ScenarioValidationEngine _engine = new();
    private readonly ValidationConfig _config = new();

    [Fact]
    public void Validate_same_scenario_produces_identical_report_hash()
    {
        var scenario = StrikeScenario();
        var catalog = ValidationCatalogFixture.Default();

        var a = _engine.Validate(scenario, catalog, _config);
        var b = _engine.Validate(scenario, catalog, _config);

        Assert.Equal(a.ReportHash, b.ReportHash);
        Assert.True(a.Passed);
    }

    [Fact]
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
        Assert.Contains(report.Findings, f => f.Code == "STRIKE_NO_TARGETS");
        Assert.False(report.CanExport(_config));
    }

    [Fact]
    public void Strike_unreachable_emits_STRIKE_UNREACHABLE_with_excess_nm()
    {
        var scenario = StrikeScenario();
        var report = _engine.Validate(scenario, ValidationCatalogFixture.Unreachable(), _config);
        var finding = Assert.Single(report.Findings, f => f.Code == "STRIKE_UNREACHABLE");
        Assert.NotNull(finding.Data);
        Assert.False(string.IsNullOrEmpty(finding.Data!["excess_nm"]));
    }

    [Fact]
    public void Strike_within_combat_radius_but_over_fuel_budget_emits_STRIKE_UNREACHABLE_FUEL()
    {
        var scenario = StrikeScenario();
        var report = _engine.Validate(scenario, ValidationCatalogFixture.FuelDominated(), _config);
        var finding = Assert.Single(report.Findings, f => f.Code == "STRIKE_UNREACHABLE_FUEL");
        Assert.NotNull(finding.Data);
        Assert.False(string.IsNullOrEmpty(finding.Data!["excess_nm"]));
    }

    [Fact]
    public void Mission_no_units_emits_MISSION_NO_UNITS()
    {
        var scenario = new ScenarioDocumentDto
        {
            Missions = [new ScenarioMissionDto { Id = "patrol-1", Type = "Patrol" }],
        };

        var report = _engine.Validate(scenario, ValidationCatalogFixture.Default(), _config);
        Assert.Contains(report.Findings, f => f.Code == "MISSION_NO_UNITS");
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

    [Fact]
    public void Editor_migration_preview_from_clean_editor_plus_legacy_unit_produces_nonzero_ObsoleteCount_BrokenMounts_and_real_delta()
    {
        // clean start state, real shipped editor + pure preview Compute, no mocks of preview
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("__verif-legacy", new[] { "legacy-patrol-ship" }, new string[0]);
        var current = InMemoryCatalogReader.BalticPatrolFixture();
        var target = InMemoryCatalogReader.BalticV3Fixture();
        var res = ScenarioDbMigrationPreview.Compute(editor.ToDto(), current, target);
        // numeric from real preview + legacy in patrol platforms (position) + mount vs v3
        Assert.True(res.ObsoleteCount >= 1, "ObsoleteCount");
        Assert.True(res.BrokenMounts >= 1, "BrokenMounts");
        var report = res.Report;
        Assert.Contains("obsolete=1", report);
        Assert.Contains("broken_mounts=1", report);
    }

    [Fact]
    public void Editor_live_validation_and_migration_from_clean_state_emits_findings_and_preview_counts()
    {
        var editor = ScenarioDocumentEditor.CreateNew(dbRef: "baltic_patrol");
        editor.AddStrikeMission("s-no-tgt", new[] { "u1" }, new string[0]);
        var engine = new ScenarioValidationEngine();
        var config = new ValidationConfig();
        var catalog = ValidationCatalogFixture.Default();
        var report = engine.Validate(editor.ToDto(), catalog, config);
        Assert.Contains(report.Findings, f => f.Code == "STRIKE_NO_TARGETS");
        // also drive preview numeric
        editor.AddStrikeMission("__mig-legacy", new[] { "legacy-patrol-ship" }, new string[0]);
        var mig = ScenarioDbMigrationPreview.Compute(editor.ToDto(), InMemoryCatalogReader.BalticPatrolFixture(), InMemoryCatalogReader.BalticV3Fixture());
        Assert.True(mig.ObsoleteCount >= 1);
        Assert.True(mig.BrokenMounts >= 1);
    }
}