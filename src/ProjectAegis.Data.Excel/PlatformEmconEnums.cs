namespace ProjectAegis.Data.Excel;

/// <summary>
/// Req-21 / migration 008: allowed EMCON enum values surfaced as Excel list validation on the
/// <c>Emcon</c> sheet (<c>Condition</c>, <c>Posture</c> columns). Included in the broader
/// <see cref="PlatformWorkbookEnumCatalog"/> matrix (PLE-1.2).
/// </summary>
public static class PlatformEmconEnums
{
    /// <summary>platform_emcon.condition — silent | restricted | free.</summary>
    public static readonly IReadOnlyList<string> Conditions = ["silent", "restricted", "free"];

    /// <summary>platform_emcon.posture — off | standby | active.</summary>
    public static readonly IReadOnlyList<string> Postures = ["off", "standby", "active"];

    public const string EmconSheetName = "Emcon";
    public const string ConditionColumn = "Condition";
    public const string PostureColumn = "Posture";

    /// <summary>Formats allowed values as an Excel list-validation formula fragment.</summary>
    internal static string ToExcelList(IReadOnlyList<string> values) =>
        PlatformWorkbookEnumCatalog.ToExcelList(values);
}