using ProjectAegis.Data.Scenario.Authoring;
using NUnit.Framework;

namespace ProjectAegis.Data.Tests.Scenario;

/// <summary>
/// Headless smoke: open the Baltic UI-dev authoring scenario via
/// <see cref="ScenarioAuthoringSession"/> and exercise place/rebuild paths so the
/// asset works for the Scenario Map Authoring editor. Does not mutate the asset file.
/// </summary>
public sealed class ScenarioUiDevBalticOpenSmokeTests
{
    private const string RelativeScenarioPath =
        "assets/data/scenarios/authoring/russia-vs-nato-baltic-ui-dev.json";

    [Test]
    public void Open_ui_dev_baltic_scenario_has_orbat_and_sides()
    {
        var path = ResolveUiDevBalticScenarioPath();
        Assert.That(            File.Exists(path), Is.True,             $"UI-dev Baltic scenario missing at '{path}'. Expected under repo root next to ProjectAegis.sln: {RelativeScenarioPath}");

        using var session = ScenarioAuthoringSession.Open(path);
        var dto = session.Editor.ToDto();

        Assert.That(session.EditVersion >= 1, Is.True, $"EditVersion should be >= 1, was {session.EditVersion}");

        Assert.That(dto.Orbat, Is.Not.Null);
        var units = dto.Orbat!.Units ?? Array.Empty<ScenarioOrbatUnitDto>();
        Assert.That(units.Count >= 10, Is.True, $"Expected >= 10 ORBAT units, got {units.Count}");

        Assert.That(units.Any(u => string.Equals(u.SideId, "blue", StringComparison.OrdinalIgnoreCase)), Is.True);
        Assert.That(units.Any(u => string.Equals(u.SideId, "red", StringComparison.OrdinalIgnoreCase)), Is.True);

        Assert.That(            units.Any(            u =>
            {
                var platform = u.PlatformId ?? string.Empty;
                var id = u.Id ?? string.Empty;
                return platform.Contains("m901", StringComparison.OrdinalIgnoreCase)
                       || platform.Contains("patriot", StringComparison.OrdinalIgnoreCase)
                       || id.Contains("swe-m901", StringComparison.OrdinalIgnoreCase);
            }), Is.True);

        Assert.That(            dto.ReferencePoints.Count >= 1, Is.True,             $"Expected >= 1 reference points, got {dto.ReferencePoints.Count}");

        if (dto.Sides is { Count: > 0 })
        {
            Assert.That(                dto.Sides.Any(                s => string.Equals(s.Id, "blue", StringComparison.OrdinalIgnoreCase)), Is.True);
            Assert.That(                dto.Sides.Any(                s => string.Equals(s.Id, "red", StringComparison.OrdinalIgnoreCase)), Is.True);
        }
    }

    [Test]
    public void Place_unit_on_ui_dev_scenario_commits()
    {
        var sourcePath = ResolveUiDevBalticScenarioPath();
        Assert.That(            File.Exists(sourcePath), Is.True,             $"UI-dev Baltic scenario missing at '{sourcePath}'. Expected under repo root next to ProjectAegis.sln: {RelativeScenarioPath}");

        var tempPath = Path.Combine(Path.GetTempPath(), $"aegis-ui-dev-baltic-{Guid.NewGuid():N}.json");
        try
        {
            File.Copy(sourcePath, tempPath, overwrite: true);

            using (var session = ScenarioAuthoringSession.Open(tempPath))
            {
                var place = session.Bus.PlaceUnit(
                    session.EditVersion,
                    new ScenarioOrbatUnitDto
                    {
                        Id = "ui-test-u1",
                        SideId = "blue",
                        PlatformId = "ffg",
                        Lat = 57,
                        Lon = 20,
                    },
                    save: true);
                Assert.That(place.Ok, Is.True, place.ErrorMessage ?? place.ErrorCode ?? "PlaceUnit failed");

                // In-session surface rebuild equivalent: re-read document after commit.
                Assert.That(
                    (session.Editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>())
                        .Any(u => string.Equals(u.Id, "ui-test-u1", StringComparison.OrdinalIgnoreCase)),
                    Is.True);
            }

            // Re-open proves file persistence (editor reload path).
            using var reopened = ScenarioAuthoringSession.Open(tempPath);
            var units = reopened.Editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>();
            Assert.That(                units.Any(                u => string.Equals(u.Id, "ui-test-u1", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(u.SideId, "blue", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(u.PlatformId, "ffg", StringComparison.OrdinalIgnoreCase)), Is.True);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Walks up from <see cref="AppContext.BaseDirectory"/> for <c>ProjectAegis.sln</c>,
    /// then resolves <see cref="RelativeScenarioPath"/> under that repo root.
    /// </summary>
    private static string ResolveUiDevBalticScenarioPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 16 && dir != null; i++)
        {
            var sln = Path.Combine(dir.FullName, "ProjectAegis.sln");
            if (File.Exists(sln))
            {
                return Path.GetFullPath(Path.Combine(dir.FullName, RelativeScenarioPath));
            }

            dir = dir.Parent;
        }

        return Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, RelativeScenarioPath));
    }
}
