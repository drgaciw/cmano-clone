using System.Text.Json;
using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Data.Validation;
using Xunit;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Phase 2.1 Task 8 — map authoring integration: place + polygon + findings parity
/// (design AC §15.1–15.3). Headless only; no CLI process shell-out.
/// </summary>
public sealed class ScenarioMapAuthoringIntegrationTests
{
    private static readonly ScenarioValidationEngine Engine = new();
    private static readonly ValidationConfig Config = new();

    [Fact]
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
            Assert.True(place.Ok, place.ErrorMessage);

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
            Assert.True(rp.Ok, rp.ErrorMessage);

            var patrol = session.Bus.AttachPatrolFromSelection(
                session.EditVersion,
                missionId: "patrol-1",
                unitIds: new[] { "u1" },
                zone: zoneVerts,
                save: true);
            Assert.True(patrol.Ok, patrol.ErrorMessage);

            var reloaded = ScenarioDocumentJsonLoader.LoadFromFile(path);
            Assert.NotNull(reloaded.Orbat);
            Assert.Contains(reloaded.Orbat!.Units, u => u.Id == "u1");
            Assert.Contains(reloaded.ReferencePoints, p => p.Id == "zone-patrol" && p.Type == "polygon");
            Assert.Contains(
                reloaded.Missions,
                m => m.Id == "patrol-1"
                     && string.Equals(m.Type, "Patrol", StringComparison.OrdinalIgnoreCase)
                     && m.AssignedUnitIds.Contains("u1"));

            var catalog = InMemoryCatalogReader.BalticPatrolFixture();
            var engineReport = Engine.Validate(reloaded, catalog, Config);
            var liveReport = session.Editor.LiveValidate();

            Assert.Equal(
                engineReport.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray(),
                liveReport.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray());
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
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
            Assert.True(place.Ok, place.ErrorMessage);

            var strike = session.Bus.AttachStrikeFromSelection(
                session.EditVersion,
                missionId: "strike-1",
                unitIds: new[] { "u1" },
                targetIds: new[] { "hostile-far" },
                save: true);
            Assert.True(strike.Ok, strike.ErrorMessage);
            Assert.NotNull(strike.Report);

            var live = session.Editor.LiveValidate();
            Assert.Contains(
                live.Findings,
                f => f.Code.StartsWith("STRIKE_UNREACHABLE", StringComparison.Ordinal));
            Assert.Contains(
                strike.Report!.Findings,
                f => f.Code.StartsWith("STRIKE_UNREACHABLE", StringComparison.Ordinal));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
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

        Assert.Equal(
            without.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray(),
            with.Findings.Select(f => f.Code).OrderBy(c => c, StringComparer.Ordinal).ToArray());
        Assert.Equal(without.ReportHash, with.ReportHash);
    }
}
