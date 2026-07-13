namespace ProjectAegis.MissionEditor.Cli.Tests;

using Xunit;

/// <summary>Resolves validation fixtures under <c>assets/data/scenarios/validation</c>; fails tests when missing.</summary>
internal static class ScenarioValidationFixturePaths
{
    internal static string Require(string fileName)
    {
        var path = TryResolve(fileName);
        Assert.True(
            path != null,
            $"Required fixture '{fileName}' not found. Walked up from '{AppContext.BaseDirectory}' " +
            $"looking for assets/data/scenarios/validation/{fileName}. " +
            "Ensure the test project copies validation assets to output or run from repo root.");
        return path!;
    }

    internal static string? TryResolve(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 12; i++)
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