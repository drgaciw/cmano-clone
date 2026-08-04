namespace ProjectAegis.Data.Scenario;

/// <summary>Resolves repo-relative scenario, campaign, and catalog paths (DATA-3 path seam; profiles remain in Sim until full move).</summary>
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
