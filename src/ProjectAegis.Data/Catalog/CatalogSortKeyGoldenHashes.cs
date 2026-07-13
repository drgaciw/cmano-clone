namespace ProjectAegis.Data.Catalog;

/// <summary>Pinned golden hashes for Catalog* sort-key determinism tests (S23-06 / DBI-7.3).</summary>
public static class CatalogSortKeyGoldenHashes
{
    /// <summary>Shuffled platform-editor sample across all 7 Catalog* domains.</summary>
    public const string PlatformEditorSample =
        "b6ccbfd8171fabec51f79a04fcc5b7733ef3d9d6d35235819d885ba05fa9eeda";

    /// <summary><see cref="PlatformWorkbookExporter"/> hash for the platform-editor sample (excludes _Meta).</summary>
    public const string PlatformEditorWorkbook =
        "bfc0d10eefab70240929213466fbb0ab88582097d8e867fcb9f4d65458f88324";

    /// <summary>Baltic CMO markdown fixture (platform + weapon + mount keys only).</summary>
    public const string BalticCmoImport =
        "6253cd54d85092c964fe4bbd5eb114565b76b8bffa06c164613a9fbe4491e43b";

    /// <summary>Baltic CMO fixture including default loadouts + resolved magazine rows (S27-04).</summary>
    public const string BalticCmoImportWithFittings =
        "85e574cff8868a462aef5bdbe7e222c36d9154683c61d791560b4af4fd3461a6";

    /// <summary>Curated ship-slice-100 platform v2 nightly fixture (S28-03).</summary>
    public const string ShipSlice100PlatformV2 =
        "f0712b4225b14186d080636afdbcb0cdacdba895bb3247ae1b274f6c4421db90";

    /// <summary>Curated aircraft-slice-100 platform nightly fixture (S30-11).</summary>
    public const string AircraftSlice100PlatformV2 =
        "5d82b0aaa6f0be92f69a4062fe8e398061e38f5075dcfe222b6f934949a91d81";

    /// <summary>Curated submarine-slice-100 platform nightly fixture (S30-11).</summary>
    public const string SubmarineSlice100PlatformV2 =
        "dc0b03fa4cc890e40f5dcfdfb9317fb6f8071d2a0aebc79c249ae532f9446c69";

    /// <summary>Curated facility-slice-100 platform nightly fixture (S30-11).</summary>
    public const string FacilitySlice100PlatformV2 =
        "ba5d2b5f9af537ceb7799965f450f34bb16e13a106b851f3951b6d5db2699cf9";
}