namespace ProjectAegis.Data.Platform;

using System.Globalization;
using Scenario.Authoring;
using Validation;

/// <summary>
/// S125-08 / AUTH-09: actionable warnings for invalid <c>Platforms.LatDeg</c> / <c>LonDeg</c> cells.
/// Position is scenario placement (doc 11) and is not stageable from the Platform Editor, so these are
/// warnings that explain the fix rather than blocking staging of other edits.
/// </summary>
public static class PlatformWorkbookLatLonDiagnostics
{
    public const string FindingCode = "PLE-PLT-LATLON";

    private const string PlacementHint =
        " Platforms.LatDeg/LonDeg is scenario placement and is not staged from the Platform Editor; " +
        "correct the cell or set the unit position in the Mission Editor.";

    public static IReadOnlyList<ValidationFinding> Check(PlatformWorkbook workbook)
    {
        if (workbook is null) throw new ArgumentNullException(nameof(workbook));

        var sheet = workbook.FindSheet("Platforms");
        if (sheet is null)
        {
            return [];
        }

        var idCol = IndexOf(sheet, "PlatformId");
        var latCol = IndexOf(sheet, "LatDeg");
        var lonCol = IndexOf(sheet, "LonDeg");
        if (latCol < 0 || lonCol < 0)
        {
            return [];
        }

        var findings = new List<ValidationFinding>();
        for (var r = 0; r < sheet.Rows.Count; r++)
        {
            var row = sheet.Rows[r];
            var platformId = Cell(row, idCol);
            var latText = Cell(row, latCol);
            var lonText = Cell(row, lonCol);

            var latProblem = ScenarioLatLonDiagnostics.CheckText(latText, "0", out _, out _);
            var lonProblem = ScenarioLatLonDiagnostics.CheckText("0", lonText, out _, out _);
            foreach (var (problem, column) in new[] { (latProblem, "LatDeg"), (lonProblem, "LonDeg") })
            {
                if (problem is null)
                {
                    continue;
                }

                findings.Add(new ValidationFinding(
                    FindingCode,
                    ValidationSeverity.Warning,
                    $"Platform '{platformId}' row {(r + 1).ToString(CultureInfo.InvariantCulture)} {column}: {problem.Message}{PlacementHint}",
                    UnitId: platformId,
                    Data: new Dictionary<string, string>
                    {
                        ["sheet"] = sheet.Name,
                        ["column"] = column,
                        ["value"] = problem.RawValue,
                        ["diagnostic"] = problem.Code,
                    }));
            }
        }

        return findings;
    }

    private static int IndexOf(PlatformWorkbookSheet sheet, string column)
    {
        for (var i = 0; i < sheet.Header.Count; i++)
        {
            if (string.Equals(sheet.Header[i], column, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static string Cell(IReadOnlyList<string> row, int index) =>
        index >= 0 && index < row.Count ? row[index] : string.Empty;
}
