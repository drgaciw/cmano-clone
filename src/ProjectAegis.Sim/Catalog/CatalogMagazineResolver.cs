namespace ProjectAegis.Sim.Catalog;

using ProjectAegis.Data.Catalog;

/// <summary>
/// Req-16: resolves initial magazine counts from catalog loadout/magazine rows for engage readiness.
/// Additive-only — when catalog rows are absent, callers fall back to scenario defaults.
/// </summary>
public static class CatalogMagazineResolver
{
    public sealed record MagazineReadiness(
        int TotalRounds,
        string? LoadoutId,
        bool CatalogResolved);

    public static MagazineReadiness EvaluateInitialMagazine(
        string platformId,
        ICatalogReader catalog,
        string? loadoutId = null)
    {
        var loadouts = catalog.GetSortedLoadouts()
            .Where(l => string.Equals(l.PlatformId, platformId, StringComparison.Ordinal))
            .ToArray();

        if (loadouts.Length == 0)
        {
            return new MagazineReadiness(0, null, CatalogResolved: false);
        }

        var resolvedLoadoutId = loadoutId ?? ResolveDefaultLoadoutId(loadouts);
        if (resolvedLoadoutId == null)
        {
            return new MagazineReadiness(0, null, CatalogResolved: false);
        }

        var total = 0;
        var found = false;
        foreach (var row in catalog.GetSortedMagazines())
        {
            if (!string.Equals(row.PlatformId, platformId, StringComparison.Ordinal) ||
                !string.Equals(row.LoadoutId, resolvedLoadoutId, StringComparison.Ordinal))
            {
                continue;
            }

            total += Math.Max(0, row.Quantity);
            found = true;
        }

        return new MagazineReadiness(
            found ? total : 0,
            resolvedLoadoutId,
            CatalogResolved: found);
    }

    private static string? ResolveDefaultLoadoutId(IReadOnlyList<CatalogLoadout> loadouts)
    {
        foreach (var loadout in loadouts)
        {
            if (loadout.IsDefault)
            {
                return loadout.LoadoutId;
            }
        }

        return loadouts[0].LoadoutId;
    }
}