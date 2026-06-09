namespace ProjectAegis.Data.Platform;

using System.Globalization;
using ProjectAegis.Data.Validation;

/// <summary>
/// Req-21 / ADR-011 PLE-4.*: deterministic fitting validation over an edited workbook. Cross-sheet
/// referential and capacity rules that the SQLite CHECK constraints cannot express at edit time.
/// Pure — operates on the in-memory <see cref="PlatformWorkbook"/>; findings sorted by (Code, Message)
/// for golden-hash stability.
/// </summary>
public static class PlatformWorkbookValidator
{
    public const string MagazineUnknownMount = "PLE-MAG-MOUNT";
    public const string MagazineUnknownLoadout = "PLE-MAG-LOADOUT";
    public const string MagazineOverCapacity = "PLE-MAG-CAPACITY";
    public const string MountRangeInvalid = "PLE-MOUNT-RANGE";

    private static readonly char KeySeparator = (char)31; // US — unit separator, absent from catalog IDs

    public static IReadOnlyList<ValidationFinding> Validate(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var findings = new List<ValidationFinding>();
        var mounts = SheetView.For(workbook, "Mounts");
        var loadouts = SheetView.For(workbook, "Loadouts");
        var magazines = SheetView.For(workbook, "Magazines");

        // Index mount capacity by (platformId, mountId).
        var mountCapacity = new Dictionary<string, int>(StringComparer.Ordinal);
        if (mounts is not null)
        {
            foreach (var row in mounts.Rows)
            {
                var platformId = mounts.Cell(row, "PlatformId");
                var mountId = mounts.Cell(row, "MountId");
                mountCapacity[Key(platformId, mountId)] = ParseInt(mounts.Cell(row, "Capacity"));

                var arc = ParseDouble(mounts.Cell(row, "ArcDeg"));
                var cap = ParseInt(mounts.Cell(row, "Capacity"));
                if (arc < 0 || arc > 360 || cap < 0)
                {
                    findings.Add(new ValidationFinding(
                        MountRangeInvalid,
                        ValidationSeverity.Error,
                        $"Mount '{mountId}' on '{platformId}' has invalid arc/capacity (arc={arc}, capacity={cap}).",
                        UnitId: platformId,
                        TargetId: mountId));
                }
            }
        }

        var loadoutKeys = new HashSet<string>(StringComparer.Ordinal);
        if (loadouts is not null)
        {
            foreach (var row in loadouts.Rows)
            {
                loadoutKeys.Add(Key(loadouts.Cell(row, "PlatformId"), loadouts.Cell(row, "LoadoutId")));
            }
        }

        if (magazines is not null)
        {
            foreach (var row in magazines.Rows)
            {
                var platformId = magazines.Cell(row, "PlatformId");
                var loadoutId = magazines.Cell(row, "LoadoutId");
                var mountId = magazines.Cell(row, "MountId");
                var weaponId = magazines.Cell(row, "WeaponId");
                var quantity = ParseInt(magazines.Cell(row, "Quantity"));

                if (!loadoutKeys.Contains(Key(platformId, loadoutId)))
                {
                    findings.Add(new ValidationFinding(
                        MagazineUnknownLoadout,
                        ValidationSeverity.Error,
                        $"Magazine on '{platformId}' references unknown loadout '{loadoutId}'.",
                        UnitId: platformId,
                        TargetId: loadoutId));
                }

                if (!mountCapacity.TryGetValue(Key(platformId, mountId), out var capacity))
                {
                    findings.Add(new ValidationFinding(
                        MagazineUnknownMount,
                        ValidationSeverity.Error,
                        $"Magazine on '{platformId}' references unknown mount '{mountId}'.",
                        UnitId: platformId,
                        TargetId: mountId));
                }
                else if (quantity > capacity)
                {
                    findings.Add(new ValidationFinding(
                        MagazineOverCapacity,
                        ValidationSeverity.Error,
                        $"Magazine '{weaponId}' loads {quantity} into mount '{mountId}' (capacity {capacity}) on '{platformId}'.",
                        UnitId: platformId,
                        TargetId: mountId));
                }
            }
        }

        return findings
            .OrderBy(f => f.Code, StringComparer.Ordinal)
            .ThenBy(f => f.Message, StringComparer.Ordinal)
            .ToArray();
    }

    private static string Key(string a, string b) => a + KeySeparator + b;

    private static int ParseInt(string value) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static double ParseDouble(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    /// <summary>Header-indexed read access over a single sheet.</summary>
    private sealed class SheetView
    {
        private readonly Dictionary<string, int> _columns;

        private SheetView(PlatformWorkbookSheet sheet)
        {
            Rows = sheet.Rows;
            _columns = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var i = 0; i < sheet.Header.Count; i++)
            {
                _columns[sheet.Header[i]] = i;
            }
        }

        public IReadOnlyList<IReadOnlyList<string>> Rows { get; }

        public static SheetView? For(PlatformWorkbook workbook, string name)
        {
            var sheet = workbook.FindSheet(name);
            return sheet is null ? null : new SheetView(sheet);
        }

        public string Cell(IReadOnlyList<string> row, string column) =>
            _columns.TryGetValue(column, out var i) && i < row.Count ? row[i] : string.Empty;
    }
}
