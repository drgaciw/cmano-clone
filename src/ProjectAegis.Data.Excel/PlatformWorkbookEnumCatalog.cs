namespace ProjectAegis.Data.Excel;

using ProjectAegis.Data.Catalog;
using ProjectAegis.Data.Platform;

/// <summary>
/// Req-21 PLE-1.2: canonical allowed string lists for known platform-workbook enum columns.
/// Applied as Excel list data-validation on ClosedXML export only — the importer does not reject
/// rows solely because these lists are incomplete (export-time UX; not an import gate).
/// Emcon values remain sourced from <see cref="PlatformEmconEnums"/>.
/// </summary>
public static class PlatformWorkbookEnumCatalog
{
    /// <summary>One enum-validated column on a named sheet.</summary>
    public sealed record EnumColumn(
        string SheetName,
        string ColumnName,
        IReadOnlyList<string> AllowedValues);

    /// <summary>Catalog review states (req-06 / write-gate).</summary>
    public static readonly IReadOnlyList<string> ReviewStates =
    [
        CatalogReviewStates.Approved,
        CatalogReviewStates.Provisional,
        CatalogReviewStates.Rejected,
    ];

    /// <summary>Provenance value tiers (req-06 §6).</summary>
    public static readonly IReadOnlyList<string> ValueTiers =
    [
        CatalogProvenanceTier.SourceFact,
        CatalogProvenanceTier.InterpretedValue,
        CatalogProvenanceTier.GameplayAbstraction,
    ];

    /// <summary>Link catalog link types (req-21 / doc 19).</summary>
    public static readonly IReadOnlyList<string> LinkTypes =
    [
        CatalogLinkTypes.Strategic,
        CatalogLinkTypes.Tactical,
        CatalogLinkTypes.Voice,
        CatalogLinkTypes.Satcom,
    ];

    /// <summary>
    /// Curated mount types seen in fixtures/importers (vls, rail, gun, …).
    /// Not a full taxonomy — list incompleteness does not block import.
    /// </summary>
    public static readonly IReadOnlyList<string> MountTypes =
    [
        "vls",
        "rail",
        "gun",
        "tube",
        "canister",
        "launcher",
        "torpedo",
        "hangar",
    ];

    /// <summary>Common loadout roles from Baltic/catalog fixtures.</summary>
    public static readonly IReadOnlyList<string> LoadoutRoles =
    [
        "asuw",
        "aaa",
        "asw",
        "aaw",
        "aa",
        "general",
        "patrol",
        "strike",
    ];

    /// <summary>Comms binding roles (tx/rx/relay).</summary>
    public static readonly IReadOnlyList<string> CommsRoles =
    [
        "txrx",
        "tx",
        "rx",
        "relay",
    ];

    /// <summary>TRL 1–9 as text cells (export uses invariant int formatting).</summary>
    public static readonly IReadOnlyList<string> TrlLevels =
    [
        "1", "2", "3", "4", "5", "6", "7", "8", "9",
    ];

    /// <summary>Boolean columns serialized as lowercase true/false by the exporter.</summary>
    public static readonly IReadOnlyList<string> BooleanFlags =
    [
        "true",
        "false",
    ];

    /// <summary>
    /// Full enum-column matrix applied when the named header exists on the sheet.
    /// </summary>
    public static readonly IReadOnlyList<EnumColumn> Columns =
    [
        new("Sensors", "ReviewState", ReviewStates),
        new("Sensors", "ValueTier", ValueTiers),
        new("Sensors", "TrlLevel", TrlLevels),
        new("Mounts", "MountType", MountTypes),
        new("Mounts", "ReviewState", ReviewStates),
        new("Loadouts", "Role", LoadoutRoles),
        new("Loadouts", "IsDefault", BooleanFlags),
        new("Comms", "Role", CommsRoles),
        new("Comms", "SatcomCapable", BooleanFlags),
        new("Comms", "ReviewState", ReviewStates),
        new("Comms", "ValueTier", ValueTiers),
        new("Comms", "TrlLevel", TrlLevels),
        new("LinkCatalog", "LinkType", LinkTypes),
        new(PlatformEmconEnums.EmconSheetName, PlatformEmconEnums.ConditionColumn, PlatformEmconEnums.Conditions),
        new(PlatformEmconEnums.EmconSheetName, PlatformEmconEnums.PostureColumn, PlatformEmconEnums.Postures),
    ];

    /// <summary>
    /// Primary-key / identity columns locked under sheet protection (OQ5 best-effort).
    /// </summary>
    public static readonly IReadOnlyList<string> ProtectedPrimaryKeyColumns =
    [
        "PlatformId",
        "SensorId",
        "MountId",
        "LoadoutId",
        "WeaponId",
        "LinkId",
        "EmitterId",
        "Key",
    ];

    /// <summary>Enum columns registered for a given sheet name (ordinal match).</summary>
    public static IEnumerable<EnumColumn> ForSheet(string sheetName) =>
        Columns.Where(c => string.Equals(c.SheetName, sheetName, StringComparison.Ordinal));

    /// <summary>Formats allowed values as an Excel list-validation formula fragment.</summary>
    internal static string ToExcelList(IReadOnlyList<string> values) =>
        $"\"{string.Join(",", values)}\"";

    /// <summary>Whether <paramref name="sheetName"/> is the export <c>_Meta</c> sheet.</summary>
    public static bool IsMetaSheet(string sheetName) =>
        string.Equals(sheetName, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal);
}
