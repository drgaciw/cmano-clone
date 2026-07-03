namespace ProjectAegis.Data.Tests.Validation;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

public sealed class DoctrineInheritanceValidateTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Fact]
    public void scenario_validate_resolves_strike_tight_patrol_inherited()
    {
        var path = ResolveFixture("doctrine-inheritance.json");
        Assert.NotNull(path);

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path!);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = Engine.Validate(scenario, catalog, Config);

        var strike = Assert.Single(
            report.Findings,
            f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "strike-1");
        Assert.NotNull(strike.Data);
        Assert.Equal("WeaponsTight", strike.Data!["resolvedRoe"]);
        Assert.Equal("override", strike.Data!["inheritanceSource"]);

        var patrol = Assert.Single(
            report.Findings,
            f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "patrol-1");
        Assert.NotNull(patrol.Data);
        Assert.Equal("WeaponsFree", patrol.Data!["resolvedRoe"]);
        Assert.Equal("side", patrol.Data!["inheritanceSource"]);

        var json = ValidationReportJsonDto.Serialize(report, Config);
        Assert.Contains("DOCTRINE_RESOLVED", json);
        Assert.Contains("WeaponsTight", json);
        Assert.Contains("WeaponsFree", json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("doctrineResolution", out var doctrineResolution));
        Assert.Equal(2, doctrineResolution.GetArrayLength());

        var strikeResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "strike-1");
        Assert.Equal("WeaponsTight", strikeResolution.GetProperty("resolvedRoe").GetString());

        var patrolResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "patrol-1");
        Assert.Equal("WeaponsFree", patrolResolution.GetProperty("resolvedRoe").GetString());
    }

    [Fact]
    public void Doctrine_inheritance_defaults_side_roe_to_weapons_free_when_unset()
    {
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto { TlBranch = CatalogTlTier.Default },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "patrol-1",
                    Type = "Patrol",
                    AssignedUnitIds = ["u1"],
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                },
            ],
        };

        var finding = Assert.Single(
            Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config).Findings,
            f => f.Code == "DOCTRINE_RESOLVED");
        Assert.Equal("WeaponsFree", finding.Data!["resolvedRoe"]);
        Assert.Equal("side", finding.Data!["inheritanceSource"]);
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