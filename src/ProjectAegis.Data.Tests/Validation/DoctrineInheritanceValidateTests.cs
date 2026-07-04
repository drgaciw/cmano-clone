namespace ProjectAegis.Data.Tests.Validation;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

using CatalogTlTier = ProjectAegis.Data.Catalog.CatalogTlTier;

// S82-01 Doctrine inheritance validation (AC-4) hardening.
// Extends src/ProjectAegis.Data.Tests/Validation/DoctrineInheritanceValidateTests.cs + data fixture.
// Covers parent/child doctrine resolution (side=parent -> mission=child), ROE overrides, defaults, serialization.
// Per: qa-plan-scenario-editor-2026-07-01.md (unit #4), scenario-editor-scope-boundary-2026-07-04.md,
// sprint-82-validation-tracks-ac.md, roadmap-execute-plan-07042026.md .
// Only touches doctrine AC-4 files; no DelegationBridge; no other validation breakage.
// Uses data/scenarios/validation/doctrine-inheritance.json as complete canonical fixture (also in assets for packaging).

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
        Assert.Contains("WeaponsHold", json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.True(root.TryGetProperty("doctrineResolution", out var doctrineResolution));
        // Fixture complete (S82-01): 3 missions exercising parent side -> child mission inheritance + overrides.
        Assert.Equal(3, doctrineResolution.GetArrayLength());

        var strikeResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "strike-1");
        Assert.Equal("WeaponsTight", strikeResolution.GetProperty("resolvedRoe").GetString());

        var patrolResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "patrol-1");
        Assert.Equal("WeaponsFree", patrolResolution.GetProperty("resolvedRoe").GetString());

        var supportResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "support-1");
        Assert.Equal("WeaponsHold", supportResolution.GetProperty("resolvedRoe").GetString());
        // Note: DoctrineResolutionJsonDto (in ValidationReportJson.cs) only exposes missionId + resolvedRoe;
        // inheritanceSource lives in the full finding.Data (parent/child source asserted on findings elsewhere).
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

    [Fact]
    public void scenario_validate_resolves_explicit_side_roe_and_various_overrides()
    {
        // Given a scenario with explicit sideRoe (parent) and mixed mission roeOverrides (child)
        var scenario = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                TlBranch = CatalogTlTier.Default,
                SideRoe = "WeaponsTight"
            },
            Missions =
            [
                new ScenarioMissionDto
                {
                    Id = "strike-roe",
                    Type = "Strike",
                    AssignedUnitIds = ["u1"],
                    TargetIds = ["tgt-1"],
                    RoeOverride = "WeaponsFree"  // child override wins
                },
                new ScenarioMissionDto
                {
                    Id = "patrol-inherit",
                    Type = "Patrol",
                    AssignedUnitIds = ["u2"],
                    PatrolZone =
                    [
                        new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                        new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.2 },
                    ],
                    // no roeOverride -> inherits parent side
                },
            ],
        };

        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);

        // When/Then: override source for strike, side (parent) for patrol
        var strike = Assert.Single(report.Findings, f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "strike-roe");
        Assert.Equal("WeaponsFree", strike.Data!["resolvedRoe"]);
        Assert.Equal("override", strike.Data!["inheritanceSource"]);

        var patrol = Assert.Single(report.Findings, f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "patrol-inherit");
        Assert.Equal("WeaponsTight", patrol.Data!["resolvedRoe"]);
        Assert.Equal("side", patrol.Data!["inheritanceSource"]);
    }

    [Fact]
    public void doctrine_inheritance_reports_parent_child_resolution_and_info_severity()
    {
        // Given/When: use fixture (complete data/ version) which exercises side(parent)->mission(child)
        var path = ResolveFixture("doctrine-inheritance.json");
        Assert.NotNull(path);
        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path!);
        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);

        // Then: all DOCTRINE_RESOLVED are Info (not errors); data carries parent/child provenance
        var doctrineFindings = report.Findings.Where(f => f.Code == "DOCTRINE_RESOLVED").ToList();
        Assert.Equal(3, doctrineFindings.Count);
        Assert.All(doctrineFindings, f => Assert.Equal(ValidationSeverity.Info, f.Severity));

        foreach (var f in doctrineFindings)
        {
            Assert.NotNull(f.Data);
            Assert.True(f.Data!.ContainsKey("missionId"));
            Assert.True(f.Data.ContainsKey("resolvedRoe"));
            Assert.True(f.Data.ContainsKey("inheritanceSource"));
            // inheritanceSource is either "side" (parent default) or "override" (child)
            Assert.Contains(f.Data["inheritanceSource"], new[] { "side", "override" });
        }
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

            // Prefer data/ (canonical per S82 docs + tracker) so data/scenarios/validation/doctrine-inheritance.json is used.
            // Fallback to assets/ for test runtime packaging (see ProjectAegis.Data.Tests.csproj + Cli.Tests).
            var dataCandidate = Path.Combine(dir.FullName, "data", "scenarios", "validation", fileName);
            if (File.Exists(dataCandidate))
            {
                return dataCandidate;
            }

            var assetsCandidate = Path.Combine(dir.FullName, "assets", "data", "scenarios", "validation", fileName);
            if (File.Exists(assetsCandidate))
            {
                return assetsCandidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}