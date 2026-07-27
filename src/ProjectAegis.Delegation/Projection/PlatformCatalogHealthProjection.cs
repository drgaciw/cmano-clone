namespace ProjectAegis.Delegation.Projection;

/// <summary>PE-UX-W5: read-only catalog health strip for Platform Editor shell (no write path).</summary>
public static class PlatformCatalogHealthProjection
{
    public static string Format(
        int blockedFindingCount,
        int pendingDiffCount,
        int dependencyEdgeCount)
    {
        var attention = blockedFindingCount > 0 || pendingDiffCount > 0;
        var level = attention ? "ATTENTION" : "OK";
        return
            $"Health: {level} · edges {dependencyEdgeCount} · pending {pendingDiffCount} · blocked {blockedFindingCount}";
    }
}
