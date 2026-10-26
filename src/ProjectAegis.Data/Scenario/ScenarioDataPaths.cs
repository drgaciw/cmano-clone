namespace ProjectAegis.Data.Scenario;

/// <summary>Resolves repo-relative scenario, campaign, and catalog paths (DATA-3 path seam; profiles remain in Sim until full move).</summary>
public static class ScenarioDataPaths
{
    private const int MaxParentWalk = 16;

    /// <summary>
    /// Walk ancestors of <paramref name="startPath"/> looking for <c>data/scenarios</c>.
    /// Used by Unity Editor hosts where <see cref="AppContext.BaseDirectory"/> may be the
    /// Editor install or <c>Library/ScriptAssemblies</c> rather than the repo root.
    /// </summary>
    public static string? TryResolveFrom(string? startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        DirectoryInfo? dir;
        try
        {
            dir = new DirectoryInfo(Path.GetFullPath(startPath));
            if (dir.Exists == false && dir.Parent != null)
            {
                dir = dir.Parent;
            }
        }
        catch (Exception)
        {
            return null;
        }

        for (var i = 0; i < MaxParentWalk; i++)
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

    public static string? TryResolveScenariosDirectory()
    {
        return TryResolveFrom(AppContext.BaseDirectory)
            ?? TryResolveFrom(Directory.GetCurrentDirectory())
            ?? TryResolveFrom(Environment.GetEnvironmentVariable("AEGIS_REPO_ROOT"));
    }

    /// <summary>
    /// Resolve <c>data/campaigns</c> (CMD-27.12 campaign artifact class). Walks up from
    /// <see cref="AppContext.BaseDirectory"/> the same way as scenarios.
    /// </summary>
    public static string? TryResolveCampaignsDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10; i++)
        {
            if (dir == null)
            {
                break;
            }

            var candidate = Path.Combine(dir.FullName, "data", "campaigns");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        // Sibling of scenarios when only scenarios resolve.
        var scenarios = TryResolveScenariosDirectory();
        if (scenarios != null)
        {
            var parent = Directory.GetParent(scenarios);
            if (parent != null)
            {
                var sibling = Path.Combine(parent.FullName, "campaigns");
                if (Directory.Exists(sibling))
                {
                    return sibling;
                }
            }
        }

        return null;
    }

    public static string? TryResolveRepoRoot()
    {
        var scenarios = TryResolveScenariosDirectory();
        if (scenarios != null)
        {
            return Directory.GetParent(scenarios)?.FullName;
        }

        var campaigns = TryResolveCampaignsDirectory();
        if (campaigns != null)
        {
            return Directory.GetParent(campaigns)?.FullName;
        }

        return null;
    }
}
