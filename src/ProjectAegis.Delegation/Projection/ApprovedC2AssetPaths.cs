namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Repo-relative paths for Approved C2 production assets (Path A graduates).
/// Headless resolvers for style parity tests and host documentation.
/// </summary>
public static class ApprovedC2AssetPaths
{
    public const string Asset004App6Atlas = "production/assets/c2/App6FrameAtlas.png";
    public const string Asset005TopBarUss = "production/assets/c2/C2TopBarPanel.uss";
    public const string Asset006MessageLogUss = "production/assets/c2/MessageLogPanel.uss";
    public const string Asset014AegisTokensUss = "production/assets/c2/AegisTokens.uss";
    public const string Asset021CombatDomainsUss = "production/assets/baltic/CombatDomainsHotTick.uss";

    public const string UnityTopBarUss = "unity/ProjectAegis/Assets/UI/TopBar/C2TopBarPanel.uss";
    public const string UnityMessageLogUss = "unity/ProjectAegis/Assets/UI/MessageLog/MessageLogPanel.uss";
    public const string UnityCombatDomainsUss = "unity/ProjectAegis/Assets/UI/CombatDomains/CombatDomainsHotTick.uss";
    public const string UnityAegisTokensUss = "unity/ProjectAegis/Assets/UI/AegisTokens.uss";

    public static bool TryResolveUnderRepoRoot(string repoRoot, string relativePath, out string fullPath)
    {
        fullPath = string.Empty;
        if (string.IsNullOrWhiteSpace(repoRoot) || string.IsNullOrWhiteSpace(relativePath))
        {
            return false;
        }

        var combined = Path.GetFullPath(Path.Combine(repoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!File.Exists(combined))
        {
            return false;
        }

        fullPath = combined;
        return true;
    }

    /// <summary>True when production ASSET-005 USS exists and references token import / top-bar classes.</summary>
    public static bool ProductionTopBarUssHasTokenParity(string repoRoot)
    {
        if (!TryResolveUnderRepoRoot(repoRoot, Asset005TopBarUss, out var path))
        {
            return false;
        }

        var text = File.ReadAllText(path);
        return text.Contains("AegisTokens", StringComparison.Ordinal)
               || text.Contains("c2-topbar", StringComparison.OrdinalIgnoreCase)
               || text.Contains("--", StringComparison.Ordinal);
    }
}
