namespace ProjectAegis.MissionEditor.Cli;

/// <summary>
/// Shared guard for read-only catalog verbs: an explicitly passed <c>--db</c> that does not exist
/// yields the <c>NOT_FOUND</c> envelope (exit 1) instead of being silently created or ignored (UAT CLI-12).
/// Default resolution (no <c>--db</c>) is unchanged.
/// </summary>
internal static class CatalogDbGuard
{
    /// <summary>Returns true and writes the NOT_FOUND envelope when <paramref name="databasePath"/> was supplied but does not exist.</summary>
    public static bool IsMissing(string? databasePath, TextWriter output, out int exitCode)
    {
        if (!string.IsNullOrWhiteSpace(databasePath) && !File.Exists(databasePath))
        {
            exitCode = McpToolResult.WriteError(output, "NOT_FOUND", $"Catalog database not found: {databasePath}");
            return true;
        }

        exitCode = 0;
        return false;
    }
}
