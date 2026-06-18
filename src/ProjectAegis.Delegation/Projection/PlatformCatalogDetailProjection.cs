namespace ProjectAegis.Delegation.Projection;

using System.Globalization;

/// <summary>ADR-011 Phase C: read-only detail labels for selected platform browse row.</summary>
public sealed record PlatformCatalogDetailEntry(
    string LatLabel,
    string LonLabel,
    string CombatRadiusLabel,
    string MaxHpLabel,
    string MaxSpeedLabel);

public static class PlatformCatalogDetailProjection
{
    private const string Missing = "—";

    public static PlatformCatalogDetailEntry Format(CatalogPlatformBrowseRow? row)
    {
        if (row == null)
        {
            return Empty();
        }

        return new PlatformCatalogDetailEntry(
            FormatField("LAT", row.LatDeg, suffix: "°"),
            FormatField("LON", row.LonDeg, suffix: "°"),
            FormatField("RADIUS", row.CombatRadiusNm, suffix: " nm"),
            FormatField("HP", row.MaxHp),
            FormatField("SPEED", row.MaxSpeedKnots, suffix: " kt"));
    }

    private static PlatformCatalogDetailEntry Empty() =>
        new(
            "LAT: —",
            "LON: —",
            "RADIUS: —",
            "HP: —",
            "SPEED: —");

    private static string FormatField(string name, double? value, string suffix = "")
    {
        if (!value.HasValue)
        {
            return $"{name}: {Missing}";
        }

        var formatted = value.Value.ToString("G", CultureInfo.InvariantCulture);
        return $"{name}: {formatted}{suffix}";
    }
}