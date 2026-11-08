namespace ProjectAegis.Data.Catalog;

/// <summary>Pinned link catalog report hashes for CI golden catalogs (DBI-4.5).</summary>
public static class LinkCatalogGoldenHashes
{
    /// <summary>Clean Baltic patrol fixture — default link_catalog rows.</summary>
    public const string BalticPatrol = "e9531408e91c8219a5475bd7539f0e365e582699bd1a90b7dca9cf7ed599dad0";

    /// <summary>Clean Baltic patrol fixture — no link validation findings (S34-05).</summary>
    public const string BalticPatrolClean = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";
}