namespace ProjectAegis.Delegation.Projection;

/// <summary>
/// Repo-relative style paths for Specced C2 panels (ASSET-007…013).
/// Path parity helpers only — does <b>not</b> invent Approved status.
/// </summary>
public static class SpeccedC2PanelStylePaths
{
    public const string Asset007LeftDrawerUss = "production/assets/c2/C2LeftDrawerPanel.uss";
    public const string Asset008RightUnitDetailUss = "production/assets/c2/RightUnitDetailPanel.uss";
    public const string Asset009MapPlaceholderUss = "production/assets/c2/MapPlaceholderPanel.uss";
    public const string Asset010MapCommsDegradeUss = "production/assets/c2/MapCommsDegradeModifiers.uss";
    public const string Asset011DelegationBadgeUss = "production/assets/c2/DelegationBadgeOverlay.uss";
    public const string Asset012PolicyEmconHudUss = "production/assets/c2/PolicyEmconHud.uss";
    public const string Asset013ReplayScrubberUss = "production/assets/c2/ReplayScrubberOverlay.uss";

    public const string UnityLeftDrawerUss =
        "unity/ProjectAegis/Assets/UI/C2LeftDrawer/C2LeftDrawerPanel.uss";

    public const string UnityRightUnitDetailUss =
        "unity/ProjectAegis/Assets/UI/UnitDetail/UnitDetailPanel.uss";

    public const string UnityMapPlaceholderUss =
        "unity/ProjectAegis/Assets/UI/MapPlaceholder/MapPlaceholderPanel.uss";

    public static bool TryResolveUnderRepoRoot(string repoRoot, string relativePath, out string fullPath)
        => ApprovedC2AssetPaths.TryResolveUnderRepoRoot(repoRoot, relativePath, out fullPath);

    public static bool ProductionLeftDrawerUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset007LeftDrawerUss, "c2-drawer", "drawer");

    public static bool ProductionRightUnitUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset008RightUnitDetailUss, "unit-detail", "unit");

    public static bool ProductionMapPlaceholderUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset009MapPlaceholderUss, "map-placeholder", "map");

    public static bool ProductionMapCommsDegradeUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset010MapCommsDegradeUss, "map-symbol", "comms");

    public static bool ProductionDelegationBadgeUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset011DelegationBadgeUss, "delegation-badge", "delegation");

    public static bool ProductionPolicyEmconHudUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset012PolicyEmconHudUss, "policy-emcon", "emcon");

    public static bool ProductionReplayScrubberUssHasStyleParity(string repoRoot)
        => ProductionUssHasMarkers(repoRoot, Asset013ReplayScrubberUss, "replay-scrubber", "replay");

    /// <summary>ASSET-007 and ASSET-008 both present with style parity (S106).</summary>
    public static bool BothProductionPanelsHaveStyleParity(string repoRoot)
        => ProductionLeftDrawerUssHasStyleParity(repoRoot)
           && ProductionRightUnitUssHasStyleParity(repoRoot);

    /// <summary>ASSET-009…013 production USS all resolve with style markers (S107).</summary>
    public static bool Wave2ProductionPanelsHaveStyleParity(string repoRoot)
        => ProductionMapPlaceholderUssHasStyleParity(repoRoot)
           && ProductionMapCommsDegradeUssHasStyleParity(repoRoot)
           && ProductionDelegationBadgeUssHasStyleParity(repoRoot)
           && ProductionPolicyEmconHudUssHasStyleParity(repoRoot)
           && ProductionReplayScrubberUssHasStyleParity(repoRoot);

    private static bool ProductionUssHasMarkers(string repoRoot, string relativePath, params string[] markers)
    {
        if (!TryResolveUnderRepoRoot(repoRoot, relativePath, out var path))
        {
            return false;
        }

        var text = File.ReadAllText(path);
        if (text.Contains("AegisTokens", StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var marker in markers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
