namespace ProjectAegis.Data.Platform;

/// <summary>One documented workbook sheet: ordered columns and the subset the importer can stage.</summary>
public sealed record PlatformWorkbookSheetContract(
    string Name,
    IReadOnlyList<string> Columns,
    IReadOnlyList<string> StageableColumns)
{
    public bool AllColumnsStageable => StageableColumns.Count == Columns.Count;

    public bool IsColumnStageable(string column) => StageableColumns.Contains(column, StringComparer.Ordinal);
}

public enum PlatformWorkbookContractDriftKind
{
    MissingSheet,
    UnknownSheet,
    MissingColumn,
    UnknownColumn,
    ColumnOrder,
}

/// <summary>A single difference between a workbook and <see cref="PlatformWorkbookContract"/>.</summary>
public sealed record PlatformWorkbookContractDrift(
    PlatformWorkbookContractDriftKind Kind,
    string Sheet,
    string Column,
    string Detail);

/// <summary>
/// S125-06 / AUTH-08: the single source of truth for platform workbook sheets/columns (exporter schema
/// <see cref="SchemaVersion"/>), mirrored in <c>docs/engineering/platform-workbook-contract.md</c>.
/// The importer classifies stageable changes from this contract; CI fails when exporter, importer or doc drift.
/// </summary>
public static class PlatformWorkbookContract
{
    public const string SchemaVersion = "010";

    private static readonly string[] PlatformDamageColumns = ["MaxHp", "WithdrawThresholdPct", "CriticalFlags"];

    public static IReadOnlyList<PlatformWorkbookSheetContract> Sheets { get; } =
    [
        Partial("Platforms", ["PlatformId", "LatDeg", "LonDeg", "CombatRadiusNm", .. PlatformDamageColumns], PlatformDamageColumns),
        Full("Sensors", ["PlatformId", "SensorId", "BasePd", "ReviewState", "TrlLevel", "ValueTier", "CitationRef"]),
        Full("Mounts", ["PlatformId", "MountId", "MountType", "ArcDeg", "Capacity", "ReviewState"]),
        Full("Loadouts", ["PlatformId", "LoadoutId", "LoadoutName", "Role", "IsDefault"]),
        Full("Magazines", ["PlatformId", "LoadoutId", "MountId", "WeaponId", "Quantity", "ReloadTimeSec", "Depth"]),
        Full("Comms", ["PlatformId", "LinkId", "Role", "SatcomCapable", "ReviewState", "TrlLevel", "ValueTier", "CitationRef"]),
        Full("LinkCatalog", ["LinkId", "DisplayName", "LinkType", "LatencyMsNominal"]),
        Full("Mobility", ["PlatformId", "MaxSpeedKnots", "CruiseSpeedKnots", "MaxAltitudeFt", "MaxDepthM", "FuelCapacity", "RangeNm", "EnduranceHr"]),
        Full("Signatures", ["PlatformId", "RcsBandDbsm", "IrSignature", "AcousticSignatureDb", "MagneticSignature"]),
        Full("Emcon", ["PlatformId", "Condition", "EmitterId", "Posture"]),
        Full("Swarms",
        [
            "PlatformId", "IsSwarm", "MaxDrones", "ArmorClass", "DefaultSensorId", "DefaultWeaponId",
            "DefaultMode", "RequiresHost", "AllowedHostClasses", "CecCapable",
            "ReviewState", "TrlLevel", "ValueTier", "CitationRef",
        ]),
    ];

    public static PlatformWorkbookSheetContract? Find(string sheetName) =>
        Sheets.FirstOrDefault(s => string.Equals(s.Name, sheetName, StringComparison.Ordinal));

    /// <summary>Lists every sheet/column deviation from the contract; the <c>_Meta</c> sheet is exempt.</summary>
    public static IReadOnlyList<PlatformWorkbookContractDrift> CheckDrift(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var drift = new List<PlatformWorkbookContractDrift>();
        foreach (var contract in Sheets)
        {
            var sheet = workbook.FindSheet(contract.Name);
            if (sheet is null)
            {
                drift.Add(new(PlatformWorkbookContractDriftKind.MissingSheet, contract.Name, string.Empty, "documented sheet missing from workbook"));
                continue;
            }

            foreach (var column in contract.Columns.Where(c => !sheet.Header.Contains(c, StringComparer.Ordinal)))
            {
                drift.Add(new(PlatformWorkbookContractDriftKind.MissingColumn, contract.Name, column, "documented column missing"));
            }

            foreach (var column in sheet.Header.Where(c => !contract.Columns.Contains(c, StringComparer.Ordinal)))
            {
                drift.Add(new(PlatformWorkbookContractDriftKind.UnknownColumn, contract.Name, column, "column not in contract; values are not imported"));
            }

            var known = sheet.Header.Where(c => contract.Columns.Contains(c, StringComparer.Ordinal)).ToArray();
            var expected = contract.Columns.Where(c => known.Contains(c, StringComparer.Ordinal)).ToArray();
            if (!known.SequenceEqual(expected, StringComparer.Ordinal))
            {
                drift.Add(new(
                    PlatformWorkbookContractDriftKind.ColumnOrder,
                    contract.Name,
                    string.Empty,
                    $"expected [{string.Join(",", expected)}] got [{string.Join(",", known)}]"));
            }
        }

        foreach (var sheet in workbook.Sheets)
        {
            if (Find(sheet.Name) is null && !string.Equals(sheet.Name, PlatformWorkbookHash.MetaSheetName, StringComparison.Ordinal))
            {
                drift.Add(new(PlatformWorkbookContractDriftKind.UnknownSheet, sheet.Name, string.Empty, "sheet not in contract; rows are not imported"));
            }
        }

        return drift;
    }

    private static PlatformWorkbookSheetContract Full(string name, string[] columns) => new(name, columns, columns);

    private static PlatformWorkbookSheetContract Partial(string name, string[] columns, string[] stageable) => new(name, columns, stageable);
}
