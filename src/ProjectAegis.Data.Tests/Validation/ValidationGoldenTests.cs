namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

public sealed class ValidationGoldenTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Fact]
    public void Golden_clean_scenario_passes_export()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path);
        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        Assert.True(report.Passed);
        Assert.True(report.CanExport(Config));
        Assert.Equal(ValidationGoldenHashes.CleanPatrol, report.ReportHash);
        Assert.Equal(report.ReportHash, Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config).ReportHash);
    }

    [Fact]
    public void Golden_strike_unreachable_blocks_export_with_stable_hash()
    {
        var path = ResolveFixture("golden_strike_unreachable.json");
        if (path == null)
        {
            return;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = Engine.Validate(scenario, catalog, Config);
        Assert.Contains(
            report.Findings,
            f => f.Code is "STRIKE_UNREACHABLE" or "STRIKE_UNREACHABLE_FUEL");
        Assert.False(report.CanExport(Config));
        Assert.Equal(ValidationGoldenHashes.StrikeUnreachable, report.ReportHash);
        Assert.Equal(report.ReportHash, Engine.Validate(scenario, catalog, Config).ReportHash);
    }

    [Fact]
    public void Sqlite_catalog_supports_validation_db_ref_and_platform_hooks()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-val-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "test-sqlite-val");
            Assert.True(reader.TryResolveDbRef("baltic_patrol", out var snap));
            Assert.Equal("baltic_patrol", snap);
            Assert.True(reader.TryGetCombatRadiusNm("u1", out var radius));
            Assert.Equal(400.0, radius);
            Assert.True(reader.TryGetPlatformPosition("hostile-far", out var lat, out var lon));
            Assert.Equal(65.0, lat);
            Assert.Equal(35.0, lon);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public void All_six_v1_rules_emit_expected_codes()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var badDb = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { DbRef = "missing-db-xyz" } };
        Assert.Contains(Engine.Validate(badDb, catalog, Config).Findings, f => f.Code == "DB_MISMATCH");

        var noUnits = new ScenarioDocumentDto
        {
            Missions = [new ScenarioMissionDto { Id = "m1", Type = "Patrol" }],
        };
        Assert.Contains(Engine.Validate(noUnits, catalog, Config).Findings, f => f.Code == "MISSION_NO_UNITS");

        var patrol = new ScenarioDocumentDto
        {
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "p1",
                    Type = "Patrol",
                    AssignedUnitIds = ["u1"],
                    PatrolZone = [new ScenarioWaypointDto { Lat = 1, Lon = 1 }],
                },
            ],
        };
        Assert.Contains(Engine.Validate(patrol, catalog, Config).Findings, f => f.Code == "PATROL_ZONE_DEGENERATE");

        var ferry = new ScenarioDocumentDto
        {
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "f1",
                    Type = "Ferry",
                    AssignedUnitIds = ["u1"],
                },
            ],
        };
        Assert.Contains(Engine.Validate(ferry, catalog, Config).Findings, f => f.Code == "FERRY_NO_DESTINATION");
    }

    private static string? ResolveFixture(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}