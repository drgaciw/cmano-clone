using ProjectAegis.Data.Scenario.Authoring;
using ProjectAegis.Delegation.UnityAdapter.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Delegation.UnityAdapter.Tests.Authoring;

/// <summary>
/// Headless stand-in for Scenario Map Authoring button flows on the Baltic UI-dev scenario:
/// Open → Rebuild → domain/catalog policy → Begin/Commit place → RP polygon → Save → findings.
/// Mutates only a temp copy of the asset.
/// </summary>
[TestFixture]
public sealed class ScenarioMapAuthoringHostFlowSmokeTests
{
    private const string RelativeUiDev =
        "assets/data/scenarios/authoring/russia-vs-nato-baltic-ui-dev.json";

    [Test]
    public void Host_button_flow_open_place_rp_save_rebuild_findings()
    {
        var source = ResolveUiDevPath();
        Assert.That(File.Exists(source), Is.True, $"Missing UI-dev scenario: {source}");

        // Policy: default seed prefers this file.
        var repoRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(source)!))!;
        // source = <root>/assets/data/scenarios/authoring/file.json → walk up to root
        repoRoot = FindRepoRoot() ?? repoRoot;
        var seeded = ScenarioMapAuthoringHostPolicy.ResolveDefaultScenarioPath(repoRoot);
        Assert.That(seeded, Is.Not.Null.And.Not.Empty);
        Assert.That(seeded!, Does.Contain("russia-vs-nato-baltic-ui-dev"));

        var tempPath = Path.Combine(Path.GetTempPath(), $"aegis-host-flow-{Guid.NewGuid():N}.json");
        try
        {
            File.Copy(source, tempPath, overwrite: true);

            // Open (scenario-open-button / Load UI Dev Baltic)
            using var session = ScenarioAuthoringSession.Open(tempPath);
            var surface = new MapAuthoringSurface(session);
            var findings = new LiveFindingsPresenter(session, debounceMs: 0);

            surface.RebuildFromDocument();
            findings.RefreshImmediate();

            Assert.That(session.EditVersion, Is.GreaterThanOrEqualTo(1));
            Assert.That(surface.Units.Count, Is.GreaterThanOrEqualTo(10), "Open should surface ORBAT units");
            Assert.That(surface.ReferencePoints.Count, Is.GreaterThanOrEqualTo(1));

            // Domain/catalog chrome policy: form stays enabled without session; write requires session
            Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableCatalogAndFormChrome(false), Is.True);
            Assert.That(ScenarioMapAuthoringHostPolicy.ShouldEnableSessionWriteActions(true), Is.True);

            // Catalog has ships + Swedish M901 ground preset
            var ships = ScenarioPlatformDomainCatalog.GetPresets(ScenarioPlatformDomainCatalog.DomainShips);
            Assert.That(ships, Is.Not.Empty);
            var ground = ScenarioPlatformDomainCatalog.GetPresets(ScenarioPlatformDomainCatalog.DomainGround);
            Assert.That(
                ground.Any(p => p.PlatformId == "m901-patriot-pac3-erint-mse"),
                Is.True,
                "Ground catalog must include M901 Patriot for UI place form");

            // Domain/catalog change invalidates staged gesture (host policy)
            surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
            {
                Id = "ui-host-flow-staging",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.0,
                Lon = 20.0,
            });
            Assert.That(surface.TentativeUnit, Is.Not.Null);
            ScenarioMapAuthoringHostPolicy.InvalidateStagedGesturesForFormOrDomainChange(surface);
            Assert.That(surface.TentativeUnit, Is.Null, "Domain/catalog change must cancel staged place");

            // Begin Place Unit + Commit Place Unit (Place + Commit path)
            surface.BeginPlaceUnit(new ScenarioOrbatUnitDto
            {
                Id = "ui-host-flow-u1",
                SideId = "blue",
                PlatformId = "ffg",
                Lat = 57.11,
                Lon = 20.22,
            });
            var place = surface.CommitPlaceUnit(save: true);
            Assert.That(place, Is.Not.Null);
            Assert.That(place!.Ok, Is.True, place.ErrorMessage ?? place.ErrorCode);
            Assert.That(surface.Units.Any(u => u.UnitId == "ui-host-flow-u1"), Is.True);

            // Upsert RP polygon (Begin + Commit RP Polygon)
            surface.BeginDrawReferencePoint(new ScenarioReferencePointDto
            {
                Id = "ui-host-flow-rp",
                Type = "polygon",
                Geometry =
                [
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.0 },
                    new ScenarioWaypointDto { Lat = 57.1, Lon = 20.1 },
                    new ScenarioWaypointDto { Lat = 57.0, Lon = 20.2 },
                ],
            });
            var rp = surface.CommitReferencePoint(save: true);
            Assert.That(rp, Is.Not.Null);
            Assert.That(rp!.Ok, Is.True, rp.ErrorMessage ?? rp.ErrorCode);

            // Rebuild
            surface.RebuildFromDocument();
            Assert.That(surface.Units.Any(u => u.UnitId == "ui-host-flow-u1"), Is.True);
            Assert.That(surface.ReferencePoints.Any(r => r.Id == "ui-host-flow-rp"), Is.True);

            // Save (session already saved on commits; explicit save mirrors Save button)
            session.Save();
            findings.RefreshImmediate();

            // Cancel gesture no-op when idle
            surface.CancelGesture();
            Assert.That(surface.TentativeUnit, Is.Null);

            // Re-open proves persistence (editor reload)
            using var reopened = ScenarioAuthoringSession.Open(tempPath);
            var dto = reopened.Editor.ToDto();
            Assert.That(dto.Orbat?.Units.Any(u => u.Id == "ui-host-flow-u1") == true, Is.True);
            Assert.That(dto.ReferencePoints.Any(r => r.Id == "ui-host-flow-rp"), Is.True);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void NoSessionMessage_is_actionable_for_dead_button_guidance()
    {
        Assert.That(ScenarioMapAuthoringHostPolicy.NoSessionMessage, Does.Contain("scenario.json").IgnoreCase);
        Assert.That(
            ScenarioMapAuthoringHostPolicy.NoSessionMessage.ToLowerInvariant(),
            Does.Contain("open").Or.Contain("browse"));
    }

    private static string ResolveUiDevPath()
    {
        var root = FindRepoRoot();
        Assert.That(root, Is.Not.Null, "Could not find ProjectAegis.sln repo root");
        return Path.GetFullPath(Path.Combine(root!, RelativeUiDev));
    }

    private static string? FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 16 && dir != null; i++)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ProjectAegis.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
