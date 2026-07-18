namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Repo-relative style paths for Specced C2 panels (ASSET-007 Left Drawer, ASSET-008 Right Unit).
/// Path parity helpers only — does <b>not</b> invent Approved status.
/// </summary>
public static class SpeccedC2PanelStylePaths
{
    public const string Asset007LeftDrawerUss = "production/assets/c2/C2LeftDrawerPanel.uss";
    public const string Asset008RightUnitDetailUss = "production/assets/c2/RightUnitDetailPanel.uss";

    public const string UnityLeftDrawerUss =
        "unity/ProjectAegis/Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uss";

    public const string UnityRightUnitDetailUss =
        "unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uss";

    public static bool TryResolveUnderRepoRoot(string repoRoot, string relativePath, out string fullPath)
        => ApprovedC2AssetPaths.TryResolveUnderRepoRoot(repoRoot, relativePath, out fullPath);

    /// <summary>True when production ASSET-007 USS exists and references drawer / token markers.</summary>
    public static bool ProductionLeftDrawerUssHasStyleParity(string repoRoot)
    {
        if (!TryResolveUnderRepoRoot(repoRoot, Asset007LeftDrawerUss, out var path))
        {
            return false;
        }

        var text = File.ReadAllText(path);
        return text.Contains("c2-drawer", StringComparison.OrdinalIgnoreCase)
               || text.Contains("AegisTokens", StringComparison.Ordinal)
               || text.Contains("drawer", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when production ASSET-008 USS exists and references unit-detail markers.</summary>
    public static bool ProductionRightUnitUssHasStyleParity(string repoRoot)
    {
        if (!TryResolveUnderRepoRoot(repoRoot, Asset008RightUnitDetailUss, out var path))
        {
            return false;
        }

        var text = File.ReadAllText(path);
        return text.Contains("unit-detail", StringComparison.OrdinalIgnoreCase)
               || text.Contains("AegisTokens", StringComparison.Ordinal)
               || text.Contains("unit", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Both Specced production USS resolve and pass style-parity heuristics.</summary>
    public static bool BothProductionPanelsHaveStyleParity(string repoRoot)
        => ProductionLeftDrawerUssHasStyleParity(repoRoot)
           && ProductionRightUnitUssHasStyleParity(repoRoot);
}
