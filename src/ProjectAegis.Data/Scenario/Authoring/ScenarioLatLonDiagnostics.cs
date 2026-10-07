namespace ProjectAegis.Data.Scenario.Authoring;

using System.Globalization;

/// <summary>One actionable lat/lon problem: stable code, offending field, raw value, and a fix-it message.</summary>
public sealed record ScenarioLatLonDiagnostic(string Code, string Field, string RawValue, string Message);

/// <summary>
/// S125-08 / AUTH-09: shared lat/lon messaging for Mission Editor mutations and Platform Editor
/// workbook cells. Numeric range checks over decimal degrees only — no scenario-format parser
/// (ADR-013) and no degree/minute/second text parsing.
/// </summary>
public static class ScenarioLatLonDiagnostics
{
    public const string NotFinite = "LATLON_NOT_FINITE";
    public const string NotNumeric = "LATLON_NOT_NUMERIC";
    public const string LatOutOfRange = "LAT_OUT_OF_RANGE";
    public const string LonOutOfRange = "LON_OUT_OF_RANGE";

    /// <summary>Returns <c>null</c> when both values are finite and in range; otherwise the first problem.</summary>
    public static ScenarioLatLonDiagnostic? Check(double lat, double lon)
    {
        if (!double.IsFinite(lat) || !double.IsFinite(lon))
        {
            return new ScenarioLatLonDiagnostic(
                NotFinite,
                double.IsFinite(lat) ? "lon" : "lat",
                $"{Format(lat)},{Format(lon)}",
                $"Latitude/longitude must be finite decimal degrees (got lat={Format(lat)}, lon={Format(lon)}).");
        }

        if (lat < -90.0 || lat > 90.0)
        {
            return new ScenarioLatLonDiagnostic(
                LatOutOfRange,
                "lat",
                Format(lat),
                $"Latitude {Format(lat)} is out of range: enter decimal degrees between -90 and 90 " +
                "(north positive, south negative), e.g. 57.25.");
        }

        if (lon < -180.0 || lon > 180.0)
        {
            return new ScenarioLatLonDiagnostic(
                LonOutOfRange,
                "lon",
                Format(lon),
                $"Longitude {Format(lon)} is out of range: enter decimal degrees between -180 and 180 " +
                "(east positive, west negative), e.g. 20.5.");
        }

        return null;
    }

    /// <summary>
    /// Validates invariant-culture decimal text (workbook cells, CLI flags). Returns <c>null</c> and the
    /// parsed values when valid.
    /// </summary>
    public static ScenarioLatLonDiagnostic? CheckText(string? latText, string? lonText, out double lat, out double lon)
    {
        lon = 0;
        if (!TryParse(latText, out lat))
        {
            return NotNumericDiagnostic("lat", "Latitude", latText, "57.25");
        }

        if (!TryParse(lonText, out lon))
        {
            return NotNumericDiagnostic("lon", "Longitude", lonText, "20.5");
        }

        return Check(lat, lon);
    }

    private static ScenarioLatLonDiagnostic NotNumericDiagnostic(string field, string label, string? raw, string example)
    {
        var shown = string.IsNullOrWhiteSpace(raw) ? "(empty)" : $"'{raw}'";
        return new ScenarioLatLonDiagnostic(
            NotNumeric,
            field,
            raw ?? string.Empty,
            $"{label} {shown} is not a decimal-degree number: use a plain number with '.' as the decimal " +
            $"separator, e.g. {example} (degree/minute/second text is not parsed).");
    }

    private static bool TryParse(string? text, out double value) =>
        double.TryParse(
            text,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite |
            NumberStyles.AllowTrailingWhite | NumberStyles.AllowExponent,
            CultureInfo.InvariantCulture,
            out value);

    private static string Format(double value) => value.ToString("G", CultureInfo.InvariantCulture);
}

/// <summary>Raised by <see cref="ScenarioDocumentEditor"/> position mutations when lat/lon is invalid.</summary>
public sealed class ScenarioLatLonException : InvalidOperationException
{
    public ScenarioLatLonException(ScenarioLatLonDiagnostic diagnostic)
        : base(diagnostic.Message)
    {
        Diagnostic = diagnostic;
    }

    public ScenarioLatLonDiagnostic Diagnostic { get; }

    /// <summary>Stable error code (e.g. <see cref="ScenarioLatLonDiagnostics.LatOutOfRange"/>).</summary>
    public string Code => Diagnostic.Code;
}
