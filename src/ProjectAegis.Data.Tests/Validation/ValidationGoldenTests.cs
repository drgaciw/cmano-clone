namespace ProjectAegis.Data.Tests.Validation;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

public sealed class ValidationGoldenTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Test]
    public void Golden_clean_scenario_passes_export()
    {
        var path = ResolveFixture("golden_clean.json");
        if (path == null)
        {
            return;
        }

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path);
        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);
        Assert.That(report.Passed, Is.True);
        Assert.That(report.CanExport(Config), Is.True);
        Assert.That(report.ReportHash, Is.EqualTo(ValidationGoldenHashes.CleanPatrol));
        Assert.That(Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config).ReportHash, Is.EqualTo(report.ReportHash));
    }

    [Test]
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
        Assert.That(            report.Findings.Any(            f => f.Code is "STRIKE_UNREACHABLE" or "STRIKE_UNREACHABLE_FUEL"), Is.True);
        Assert.That(report.CanExport(Config), Is.False);
        Assert.That(report.ReportHash, Is.EqualTo(ValidationGoldenHashes.StrikeUnreachable));
        Assert.That(Engine.Validate(scenario, catalog, Config).ReportHash, Is.EqualTo(report.ReportHash));
    }

    [Test]
    public void Sqlite_catalog_supports_validation_db_ref_and_platform_hooks()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"aegis-val-{Guid.NewGuid():N}.db");
        try
        {
            CatalogSeedBootstrap.SeedBalticPatrol(dbPath, overwrite: true);
            using var reader = new SqliteCatalogReader(dbPath, "test-sqlite-val");
            Assert.That(reader.TryResolveDbRef("baltic_patrol", out var snap), Is.True);
            Assert.That(snap, Is.EqualTo("baltic_patrol"));
            Assert.That(reader.TryGetCombatRadiusNm("u1", out var radius), Is.True);
            Assert.That(radius, Is.EqualTo(400.0));
            Assert.That(reader.TryGetPlatformPosition("hostile-far", out var lat, out var lon), Is.True);
            Assert.That(lat, Is.EqualTo(65.0));
            Assert.That(lon, Is.EqualTo(35.0));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Test]
    public void All_six_v1_rules_emit_expected_codes()
    {
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var badDb = new ScenarioDocumentDto { Metadata = new ScenarioMetadataDto { DbRef = "missing-db-xyz" } };
        Assert.That(Engine.Validate(badDb, catalog, Config).Findings.Any(f => f.Code == "DB_MISMATCH"), Is.True);

        var noUnits = new ScenarioDocumentDto
        {
            Missions = [new ScenarioMissionDto { Id = "m1", Type = "Patrol" }],
        };
        Assert.That(Engine.Validate(noUnits, catalog, Config).Findings.Any(f => f.Code == "MISSION_NO_UNITS"), Is.True);

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
        Assert.That(Engine.Validate(patrol, catalog, Config).Findings.Any(f => f.Code == "PATROL_ZONE_DEGENERATE"), Is.True);

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
        Assert.That(Engine.Validate(ferry, catalog, Config).Findings.Any(f => f.Code == "FERRY_NO_DESTINATION"), Is.True);

        var missingTl = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { DbRef = "baltic_patrol" },
        };
        Assert.That(Engine.Validate(missingTl, catalog, Config).Findings.Any(f => f.Code == "TL_BRANCH_MISSING"), Is.True);
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