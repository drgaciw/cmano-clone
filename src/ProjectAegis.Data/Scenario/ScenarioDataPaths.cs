namespace ProjectAegis.Data.Scenario;

/// <summary>Resolves repo-relative scenario and catalog paths (DATA-3 path seam; profiles remain in Sim until full move).</summary>
public static class ScenarioDataPaths
{
    public static string? TryResolveScenariosDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "data", "scenarios");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }

    public static string? TryResolveRepoRoot()
    {
        var scenarios = TryResolveScenariosDirectory();
        if (scenarios == null)
        {
            return null;
        }

        return Directory.GetParent(scenarios)?.FullName;
    }
}