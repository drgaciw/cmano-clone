namespace ProjectAegis.Data.Catalog;

/// <summary>Pinned golden hashes for Catalog* sort-key determinism tests (S23-06 / DBI-7.3).</summary>
public static class CatalogSortKeyGoldenHashes
{
    /// <summary>Shuffled platform-editor sample across all 7 Catalog* domains.</summary>
    public const string PlatformEditorSample =
        "b6ccbfd8171fabec51f79a04fcc5b7733ef3d9d6d35235819d885ba05fa9eeda";

    /// <summary><see cref="PlatformWorkbookExporter"/> hash for the platform-editor sample (excludes _Meta).</summary>
    public const string PlatformEditorWorkbook =
        "6817cb1562d07eb8ffd16419a1d4255d3711e7c79b6301cf1be8e1ac2236bf45";

    /// <summary>Baltic CMO markdown fixture (platform + weapon + mount keys only).</summary>
    public const string BalticCmoImport =
        "6253cd54d85092c964fe4bbd5eb114565b76b8bffa06c164613a9fbe4491e43b";
}