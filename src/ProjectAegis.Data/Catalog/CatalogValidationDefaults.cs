namespace ProjectAegis.Data.Catalog;

/// <summary>Baltic harness platform rows for validation when SQLite platform table is empty.</summary>
public static class CatalogValidationDefaults
{
    public const string BalticSnapshotId = "baltic_patrol";

    public static IReadOnlyList<CatalogPlatformEntry> BalticPlatforms() =>
    [
        new CatalogPlatformEntry("u1", 57.0, 20.0, 400.0),
        new CatalogPlatformEntry("hostile-1", 58.5, 21.0, 200.0),
        new CatalogPlatformEntry("hostile-far", 65.0, 35.0, 200.0),
    ];

    public static bool TryResolveBalticDbRef(string dbRef, out string snapshotId)
    {
        if (string.Equals(dbRef, BalticSnapshotId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(dbRef, "baltic-patrol", StringComparison.OrdinalIgnoreCase) ||
            dbRef.Contains("baltic", StringComparison.OrdinalIgnoreCase))
        {
            snapshotId = BalticSnapshotId;
            return true;
        }

        snapshotId = "";
        return false;
    }
}