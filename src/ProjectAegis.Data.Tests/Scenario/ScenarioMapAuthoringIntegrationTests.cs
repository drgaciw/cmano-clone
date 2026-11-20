using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Phase 2.1 Task 8 — map authoring integration: place + polygon + findings parity
/// (design AC §15.1–15.3). Headless only; no CLI process shell-out.
/// </summary>
public sealed class ScenarioMapAuthoringIntegrationTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Test]
    public void Place_unit_and_patrol_polygon_persist_and_validate_codes_match()
    {
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-auth-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var place = session.Bus.PlaceUnit(
                session.EditVersion,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);
            Assert.That(place.Ok, Is.True, place.ErrorMessage);

            var zoneVerts = new[]
            {
                new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
            };

            var rp = session.Bus.UpsertReferencePoint(
                session.EditVersion,
                new ScenarioReferencePointDto
                {
                    Id = "zone-patrol",
                    Type = "polygon",
                    Geometry = zoneVerts,
                },
                save: true);
            Assert.That(rp.Ok, Is.True, rp.ErrorMessage);

            var patrol = session.Bus.AttachPatrolFromSelection(
                session.EditVersion,
                missionId: "patrol-1",
                unitIds: new[] { "u1" },
                zone: zoneVerts,
                save: true);
            Assert.That(patrol.Ok, Is.True, patrol.ErrorMessage);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.That(reloaded.Orbat, Is.Not.Null);
            Assert.That(reloaded.Orbat!.Units.Any(u => u.Id == "u1"), Is.True);
            Assert.That(reloaded.ReferencePoints.Any(p => p.Id == "zone-patrol" && p.Type == "polygon"), Is.True);
            Assert.That(                reloaded.Missions.Any(                m => m.Id == "patrol-1"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase)
                     && m.AssignedUnitIds.Contains("u1")), Is.True);

            var catalog = InMemoryCatalogReader.BalticPatrolFixture();
            var engineReport = Engine.Validate(reloaded, catalog, Config);
            var liveReport = session.Editor.LiveValidate();

            Assert.That(                liveReport.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray(), Is.EqualTo(                engineReport.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray()));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void Unreachable_strike_findings_include_STRIKE_UNREACHABLE_family()
    {
        // Catalog fixture: u1 @ (57,20) combat radius 400 nm; hostile-far @ (65,35) is beyond radius
        // (same geometry as assets/data/scenarios/validation/golden_strike_unreachable.json).
        var path = Path.Combine(Path.GetTempPath(), $"aegis-map-strike-{Guid.NewGuid():N}.json");
        try
        {
            ScenarioDocumentEditor.CreateNew().Save(path);
            using var session = ScenarioAuthoringSession.Open(path);

            var place = session.Bus.PlaceUnit(
                session.EditVersion,
                new ScenarioOrbatUnitDto
                {
                    Id = "u1",
                    SideId = "blue",
                    PlatformId = "ffg",
                    Lat = 57,
                    Lon = 20,
                },
                save: true);
            Assert.That(place.Ok, Is.True, place.ErrorMessage);

            var strike = session.Bus.AttachStrikeFromSelection(
                session.EditVersion,
                missionId: "strike-1",
                unitIds: new[] { "u1" },
                targetIds: new[] { "hostile-far" },
                save: true);
            Assert.That(strike.Ok, Is.True, strike.ErrorMessage);
            Assert.That(strike.Report, Is.Not.Null);

            var live = session.Editor.LiveValidate();
            Assert.That(                live.Findings.Any(                f => f.Code.StartsWith("STRIKE_UNREACHABLE", StringComparison.Ordinal)), Is.True);
            Assert.That(                strike.Report!.Findings.Any(                f => f.Code.StartsWith("STRIKE_UNREACHABLE", StringComparison.Ordinal)), Is.True);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Test]
    public void EditorState_does_not_change_validation_codes()
    {
        var baseline = new ScenarioDocumentDto
        {
            Metadata = new ScenarioMetadataDto
            {
                DbRef = "baltic_patrol",
                TlBranch = CatalogTlTier.Default,
            },
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
                        new ScenarioWaypointDto { Lat = 57.2, Lon = 20.0 },
                    ],
                },
            ],
            Orbat = new ScenarioOrbatDto
            {
                Units =
                [
                    new ScenarioOrbatUnitDto
                    {
                        Id = "u1",
                        SideId = "blue",
                        PlatformId = "ffg",
                        Lat = 57,
                        Lon = 20,
                    },
                ],
            },
        };

        var withEditorState = new ScenarioDocumentDto
        {
            Metadata = baseline.Metadata,
            Missions = baseline.Missions,
            Orbat = baseline.Orbat,
            EditorState = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["camera"] = JsonSerializer.SerializeToElement(new
                {
                    lat = 58.5,
                    lon = 21.0,
                    zoom = 3.5,
                }),
                ["layersVisible"] = JsonSerializer.SerializeToElement(true),
            },
        };

        var catalog = InMemoryCatalogReader.BalticPatrolFixture();
        var without = Engine.Validate(baseline, catalog, Config);
        var with = Engine.Validate(withEditorState, catalog, Config);

        Assert.That(            with.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray(), Is.EqualTo(            without.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray()));
        Assert.That(with.ReportHash, Is.EqualTo(without.ReportHash));
    }
}
