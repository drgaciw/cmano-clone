namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Resolves retrieval fixture roots copied beside the LiveEval test assembly.
/// </summary>
public static class RetrievalFixturePaths
{
    /// <summary>
    /// Finds <c>ai-fixtures/retrieval</c> under the output directory or walking up from the test base.
    /// </summary>
    public static string ResolveRoot()
    {
        var baseDir = AppContext.BaseDirectory;
        var direct = Path.Combine(baseDir, "ai-fixtures", "retrieval");
        if (Directory.Exists(direct))
        {
            return direct;
        }

        var dir = new DirectoryInfo(baseDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "tests", "ai-fixtures", "retrieval");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate tests/ai-fixtures/retrieval relative to " + baseDir);
    }
}