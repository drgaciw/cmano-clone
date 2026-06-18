namespace ProjectAegis.Data.Catalog;

/// <summary>Pinned golden hashes for Catalog* sort-key determinism tests (S23-06 / DBI-7.3).</summary>
public static class CatalogSortKeyGoldenHashes
{
    /// <summary>Shuffled platform-editor sample across all 7 Catalog* domains.</summary>
    public const string PlatformEditorSample =
        "b6ccbfd8171fabec51f79a04fcc5b7733ef3d9d6d35235819d885ba05fa9eeda";

    /// <summary><see cref="PlatformWorkbookExporter"/> hash for the platform-editor sample (excludes _Meta).</summary>
    public const string PlatformEditorWorkbook =
        "3c5712b3e52cfd4b9638af996f7317fab31d4603df77d4e692fabb258b17ea1a";

    /// <summary>Baltic CMO markdown fixture (platform + weapon + mount keys only).</summary>
    public const string BalticCmoImport =
        "6253cd54d85092c964fe4bbd5eb114565b76b8bffa06c164613a9fbe4491e43b";

    /// <summary>Baltic CMO fixture including default loadouts + resolved magazine rows (S27-04).</summary>
    public const string BalticCmoImportWithFittings =
        "85e574cff8868a462aef5bdbe7e222c36d9154683c61d791560b4af4fd3461a6";
}