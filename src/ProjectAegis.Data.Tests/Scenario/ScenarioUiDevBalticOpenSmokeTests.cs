using ProjectAegis.Data.Scenario.Authoring;
using Xunit;

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

    [Fact]
    public void Open_ui_dev_baltic_scenario_has_orbat_and_sides()
    {
        var path = ResolveUiDevBalticScenarioPath();
        Assert.True(
            File.Exists(path),
            $"UI-dev Baltic scenario missing at '{path}'. Expected under repo root next to ProjectAegis.sln: {RelativeScenarioPath}");

        using var session = ScenarioAuthoringSession.Open(path);
        var dto = session.Editor.ToDto();

        Assert.True(session.EditVersion >= 1, $"EditVersion should be >= 1, was {session.EditVersion}");

        Assert.NotNull(dto.Orbat);
        var units = dto.Orbat!.Units ?? Array.Empty<ScenarioOrbatUnitDto>();
        Assert.True(units.Count >= 10, $"Expected >= 10 ORBAT units, got {units.Count}");

        Assert.Contains(units, u => string.Equals(u.SideId, "blue", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(units, u => string.Equals(u.SideId, "red", StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            units,
            u =>
            {
                var platform = u.PlatformId ?? string.Empty;
                var id = u.Id ?? string.Empty;
                return platform.Contains("m901", StringComparison.OrdinalIgnoreCase)
                       || platform.Contains("patriot", StringComparison.OrdinalIgnoreCase)
                       || id.Contains("swe-m901", StringComparison.OrdinalIgnoreCase);
            });

        Assert.True(
            dto.ReferencePoints.Count >= 1,
            $"Expected >= 1 reference points, got {dto.ReferencePoints.Count}");

        if (dto.Sides is { Count: > 0 })
        {
            Assert.Contains(
                dto.Sides,
                s => string.Equals(s.Id, "blue", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(
                dto.Sides,
                s => string.Equals(s.Id, "red", StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Place_unit_on_ui_dev_scenario_commits()
    {
        var sourcePath = ResolveUiDevBalticScenarioPath();
        Assert.True(
            File.Exists(sourcePath),
            $"UI-dev Baltic scenario missing at '{sourcePath}'. Expected under repo root next to ProjectAegis.sln: {RelativeScenarioPath}");

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
                Assert.True(place.Ok, place.ErrorMessage ?? place.ErrorCode ?? "PlaceUnit failed");

                // In-session surface rebuild equivalent: re-read document after commit.
                Assert.Contains(
                    session.Editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>(),
                    u => string.Equals(u.Id, "ui-test-u1", StringComparison.OrdinalIgnoreCase));
            }

            // Re-open proves file persistence (editor reload path).
            using var reopened = ScenarioAuthoringSession.Open(tempPath);
            var units = reopened.Editor.ToDto().Orbat?.Units ?? Array.Empty<ScenarioOrbatUnitDto>();
            Assert.Contains(
                units,
                u => string.Equals(u.Id, "ui-test-u1", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(u.SideId, "blue", StringComparison.OrdinalIgnoreCase)
                     && string.Equals(u.PlatformId, "ffg", StringComparison.OrdinalIgnoreCase));
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
