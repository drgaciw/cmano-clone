namespace ProjectAegis.Data.Scenario;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Validation;

/// <summary>
/// Enumerates scenario documents under a directory for the scenario library browse list (CMD-27).
/// Deterministic sort by <see cref="ScenarioLibraryEntry.ScenarioId"/>; never throws on one bad file.
/// </summary>
public static class ScenarioLibraryLister
{
    private static readonly string[] ScenarioDocumentPatterns =
    {
        "*.scenario.json",
        "*.aegis-scenario",
    };

    /// <summary>
    /// List scenario library entries from <paramref name="scenariosDir"/>.
    /// Enumerates known scenario document extensions (recursive). Policy JSON files are sim policies,
    /// not authoring documents, and are intentionally excluded.
    /// </summary>
    public static IReadOnlyList<ScenarioLibraryEntry> ListFromDirectory(
        string scenariosDir,
        ICatalogReader? catalog = null,
        IScenarioValidationEngine? validation = null,
        ValidationConfig? validationConfig = null)
    {
        if (string.IsNullOrWhiteSpace(scenariosDir) || !Directory.Exists(scenariosDir))
        {
            return Array.Empty<ScenarioLibraryEntry>();
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in ScenarioDocumentPatterns)
        {
            try
            {
                foreach (var path in Directory.EnumerateFiles(scenariosDir, pattern, SearchOption.AllDirectories))
                {
                    paths.Add(path);
                }
            }
            catch (IOException)
            {
                // Skip unreadable subtrees; continue with what we have.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        // Also accept plain *.json under examples/ and validation/ that are not policy/schema.
        try
        {
            foreach (var path in Directory.EnumerateFiles(scenariosDir, "*.json", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(path);
                if (name.EndsWith(".policy.json", StringComparison.OrdinalIgnoreCase) ||
                    name.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("scenario-policy-ids.md", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Prefer already-matched *.scenario.json; also pick validation fixtures (golden_*.json).
                var dirName = Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty);
                var isScenarioExt = name.EndsWith(".scenario.json", StringComparison.OrdinalIgnoreCase);
                var isValidationFixture =
                    string.Equals(dirName, "examples", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(dirName, "validation", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("golden_", StringComparison.OrdinalIgnoreCase);

                if (isScenarioExt || isValidationFixture)
                {
                    paths.Add(path);
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        var entries = new List<ScenarioLibraryEntry>(paths.Count);
        foreach (var path in paths)
        {
            entries.Add(ScenarioLibraryProjection.ProjectFromPath(path, catalog, validation, validationConfig));
        }

        return entries
            .OrderBy(e => e.ScenarioId, StringComparer.Ordinal)
            .ThenBy(e => e.SourcePath, StringComparer.Ordinal)
            .ToArray();
    }
}
