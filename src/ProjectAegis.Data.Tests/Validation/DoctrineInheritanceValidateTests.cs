namespace ProjectAegis.Data.Tests.Validation;

using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

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

    [Test]
    public void scenario_validate_resolves_strike_tight_patrol_inherited()
    {
        var path = ResolveFixture("doctrine-inheritance.json");
        Assert.That(path, Is.Not.Null);

        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path!);
        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var report = Engine.Validate(scenario, catalog, Config);

        var strike = ProjectAegis.Data.Tests.AssertEx.Single(            report.Findings,             f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "strike-1");
        Assert.That(strike.Data, Is.Not.Null);
        Assert.That(strike.Data!["resolvedRoe"], Is.EqualTo("WeaponsTight"));
        Assert.That(strike.Data!["inheritanceSource"], Is.EqualTo("override"));

        var patrol = ProjectAegis.Data.Tests.AssertEx.Single(            report.Findings,             f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "patrol-1");
        Assert.That(patrol.Data, Is.Not.Null);
        Assert.That(patrol.Data!["resolvedRoe"], Is.EqualTo("WeaponsFree"));
        Assert.That(patrol.Data!["inheritanceSource"], Is.EqualTo("side"));

        var json = ValidationReportJsonDto.Serialize(report, Config);
        Assert.That(json, Does.Contain("DOCTRINE_RESOLVED"));
        Assert.That(json, Does.Contain("WeaponsTight"));
        Assert.That(json, Does.Contain("WeaponsFree"));
        Assert.That(json, Does.Contain("WeaponsHold"));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.That(root.TryGetProperty("doctrineResolution", out var doctrineResolution), Is.True);
        // Fixture complete (S82-01): 3 missions exercising parent side -> child mission inheritance + overrides.
        Assert.That(doctrineResolution.GetArrayLength(), Is.EqualTo(3));

        var strikeResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "strike-1");
        Assert.That(strikeResolution.GetProperty("resolvedRoe").GetString(), Is.EqualTo("WeaponsTight"));

        var patrolResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "patrol-1");
        Assert.That(patrolResolution.GetProperty("resolvedRoe").GetString(), Is.EqualTo("WeaponsFree"));

        var supportResolution = doctrineResolution.EnumerateArray()
            .Single(e => e.GetProperty("missionId").GetString() == "support-1");
        Assert.That(supportResolution.GetProperty("resolvedRoe").GetString(), Is.EqualTo("WeaponsHold"));
        // Note: DoctrineResolutionJsonDto (in ValidationReportJson.cs) only exposes missionId + resolvedRoe;
        // inheritanceSource lives in the full finding.Data (parent/child source asserted on findings elsewhere).
    }

    [Test]
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

        var finding = ProjectAegis.Data.Tests.AssertEx.Single(            Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config).Findings,             f => f.Code == "DOCTRINE_RESOLVED");
        Assert.That(finding.Data!["resolvedRoe"], Is.EqualTo("WeaponsFree"));
        Assert.That(finding.Data!["inheritanceSource"], Is.EqualTo("side"));
    }

    [Test]
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
        var strike = ProjectAegis.Data.Tests.AssertEx.Single(report.Findings, f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "strike-roe");
        Assert.That(strike.Data!["resolvedRoe"], Is.EqualTo("WeaponsFree"));
        Assert.That(strike.Data!["inheritanceSource"], Is.EqualTo("override"));

        var patrol = ProjectAegis.Data.Tests.AssertEx.Single(report.Findings, f => f.Code == "DOCTRINE_RESOLVED" && f.MissionId == "patrol-inherit");
        Assert.That(patrol.Data!["resolvedRoe"], Is.EqualTo("WeaponsTight"));
        Assert.That(patrol.Data!["inheritanceSource"], Is.EqualTo("side"));
    }

    [Test]
    public void doctrine_inheritance_reports_parent_child_resolution_and_info_severity()
    {
        // Given/When: use fixture (complete data/ version) which exercises side(parent)->mission(child)
        var path = ResolveFixture("doctrine-inheritance.json");
        Assert.That(path, Is.Not.Null);
        var scenario = ScenarioDocumentJsonLoader.LoadFromFile(path!);
        var report = Engine.Validate(scenario, InMemoryCatalogReader.BalticPatrolFixture(), Config);

        // Then: all DOCTRINE_RESOLVED are Info (not errors); data carries parent/child provenance
        var doctrineFindings = report.Findings.Where(f => f.Code == "DOCTRINE_RESOLVED").ToList();
        Assert.That(doctrineFindings.Count, Is.EqualTo(3));
        foreach (var f in doctrineFindings)
{
Assert.That(f.Severity, Is.EqualTo(ValidationSeverity.Info));
}

        foreach (var f in doctrineFindings)
        {
            Assert.That(f.Data, Is.Not.Null);
            Assert.That(f.Data!.ContainsKey("missionId"), Is.True);
            Assert.That(f.Data.ContainsKey("resolvedRoe"), Is.True);
            Assert.That(f.Data.ContainsKey("inheritanceSource"), Is.True);
            // inheritanceSource is either "side" (parent default) or "override" (child)
            Assert.That(new[] { "side", "override" }, Does.Contain(f.Data["inheritanceSource"]));
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