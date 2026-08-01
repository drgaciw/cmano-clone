namespace ProjectAegis.Data.Scenario.Campaign;

/// <summary>
/// Enumerates campaign documents under a directory for the campaign library browse list (CMD-27.12).
/// Deterministic sort by <see cref="CampaignLibraryEntry.CampaignId"/>; never throws on one bad file.
/// Campaigns are an artifact class under <c>data/campaigns/</c> — not a folder of scenarios.
/// </summary>
public static class CampaignLibraryLister
{
    private const string CampaignDocumentPattern = "*.campaign.json";

    /// <summary>
    /// List campaign library entries from <paramref name="campaignsDir"/>.
    /// When <paramref name="scenariosDir"/> is provided (or resolvable as a sibling / via
    /// <see cref="ScenarioDataPaths"/>), membership feasibility marks missing members as
    /// <see cref="CampaignLibraryReasons.MemberMissing"/>.
    /// </summary>
    public static IReadOnlyList<CampaignLibraryEntry> ListFromDirectory(
        string campaignsDir,
        string? scenariosDir = null)
    {
        if (string.IsNullOrWhiteSpace(campaignsDir) || !Directory.Exists(campaignsDir))
        {
            return Array.Empty<CampaignLibraryEntry>();
        }

        scenariosDir ??= TryResolveSiblingScenariosDirectory(campaignsDir)
                         ?? ScenarioDataPaths.TryResolveScenariosDirectory();

        IReadOnlySet<string>? scenarioIds = scenariosDir is not null && Directory.Exists(scenariosDir)
            ? IndexScenarioIds(scenariosDir)
            : null;

        var paths = new List<string>();
        try
        {
            foreach (var path in Directory.EnumerateFiles(
                         campaignsDir,
                         CampaignDocumentPattern,
                         SearchOption.AllDirectories))
            {
                paths.Add(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        var entries = new List<CampaignLibraryEntry>(paths.Count);
        foreach (var path in paths)
        {
            entries.Add(CampaignLibraryProjection.ProjectFromPath(path, scenarioIds));
        }

        return entries
            .OrderBy(e => e.CampaignId, StringComparer.Ordinal)
            .ThenBy(e => e.SourcePath, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Index scenario ids present under <paramref name="scenariosDir"/> using the same path → id
    /// rules as <see cref="ScenarioLibraryProjection.ScenarioIdFromPath"/>, plus bare stems.
    /// </summary>
    public static HashSet<string> IndexScenarioIds(string scenariosDir)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(scenariosDir) || !Directory.Exists(scenariosDir))
        {
            return set;
        }

        foreach (var path in EnumerateScenarioDocumentPaths(scenariosDir))
        {
            var id = ScenarioLibraryProjection.ScenarioIdFromPath(path);
            if (!string.IsNullOrWhiteSpace(id))
            {
                set.Add(id);
                if (id.EndsWith(".scenario", StringComparison.OrdinalIgnoreCase))
                {
                    set.Add(id[..^".scenario".Length]);
                }
            }

            var fileName = Path.GetFileName(path);
            if (fileName.EndsWith(".scenario.json", StringComparison.OrdinalIgnoreCase))
            {
                set.Add(fileName[..^".scenario.json".Length]);
            }
            else if (fileName.EndsWith(".aegis-scenario", StringComparison.OrdinalIgnoreCase))
            {
                set.Add(fileName[..^".aegis-scenario".Length]);
            }
        }

        return set;
    }

    private static IEnumerable<string> EnumerateScenarioDocumentPaths(string scenariosDir)
    {
        var patterns = new[] { "*.scenario.json", "*.aegis-scenario" };
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in patterns)
        {
            IEnumerable<string> batch;
            try
            {
                batch = Directory.EnumerateFiles(scenariosDir, pattern, SearchOption.AllDirectories);
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var path in batch)
            {
                if (seen.Add(path))
                {
                    yield return path;
                }
            }
        }

        // Validation / examples fixtures (same allowance as ScenarioLibraryLister).
        IEnumerable<string> jsonBatch;
        try
        {
            jsonBatch = Directory.EnumerateFiles(scenariosDir, "*.json", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            yield break;
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var path in jsonBatch)
        {
            var name = Path.GetFileName(path);
            if (name.EndsWith(".policy.json", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase) ||
                name.EndsWith(".campaign.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dirName = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
            var isScenarioExt = name.EndsWith(".scenario.json", StringComparison.OrdinalIgnoreCase);
            var isValidationFixture =
                string.Equals(dirName, "examples", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dirName, "validation", StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("golden_", StringComparison.OrdinalIgnoreCase);

            if ((isScenarioExt || isValidationFixture) && seen.Add(path))
            {
                yield return path;
            }
        }
    }

    private static string? TryResolveSiblingScenariosDirectory(string campaignsDir)
    {
        try
        {
            var parent = Directory.GetParent(campaignsDir);
            if (parent is null)
            {
                return null;
            }

            var candidate = Path.Combine(parent.FullName, "scenarios");
            return Directory.Exists(candidate) ? candidate : null;
        }
        catch
        {
            return null;
        }
    }
}
