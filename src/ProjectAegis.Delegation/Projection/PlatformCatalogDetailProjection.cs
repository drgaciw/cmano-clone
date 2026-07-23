namespace ProjectAegis.Delegation.Projection;

using System.Globalization;

/// <summary>ADR-011 Phase C + PE-UX-W2: read-only detail labels for selected platform browse row.</summary>
public sealed record PlatformCatalogDetailEntry(
    string LatLabel,
    string LonLabel,
    string CombatRadiusLabel,
    string MaxHpLabel,
    string ResilienceLabel,
    string WithdrawThresholdLabel,
    string CriticalFlagsLabel,
    string MaxSpeedLabel,
    string MountsLabel = "MOUNTS: —",
    string SensorsLabel = "SENSORS: —",
    string PlatformIdLabel = "ID: —");

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
            FormatScenarioCoord("SCENARIO LAT", row.LatDeg),
            FormatScenarioCoord("SCENARIO LON", row.LonDeg),
            FormatField("RADIUS", row.CombatRadiusNm, suffix: " nm"),
            FormatField("HP", row.MaxHp),
            FormatField("RESILIENCE", row.Resilience),
            FormatField("WITHDRAW", row.WithdrawThresholdPct, suffix: "%"),
            FormatIntField("FLAGS", row.CriticalFlags),
            FormatField("SPEED", row.MaxSpeedKnots, suffix: " kt"),
            FormatIntField("MOUNTS", row.MountCount),
            FormatIntField("SENSORS", row.SensorCount),
            $"ID: {row.PlatformId}");
    }

    private static PlatformCatalogDetailEntry Empty() =>
        new(
            "SCENARIO LAT: — (doc 11)",
            "SCENARIO LON: — (doc 11)",
            "RADIUS: —",
            "HP: —",
            "RESILIENCE: —",
            "WITHDRAW: —",
            "FLAGS: —",
            "SPEED: —",
            "MOUNTS: —",
            "SENSORS: —",
            "ID: —");

    /// <summary>Scenario placement coords — demoted per Req 21 (class editor ≠ doc 11 placement).</summary>
    private static string FormatScenarioCoord(string name, double? value)
    {
        if (!value.HasValue)
        {
            return $"{name}: {Missing} (doc 11)";
        }

        var formatted = value.Value.ToString("G", CultureInfo.InvariantCulture);
        return $"{name}: {formatted}° (doc 11)";
    }

    private static string FormatField(string name, double? value, string suffix = "")
    {
        if (!value.HasValue)
        {
            return $"{name}: {Missing}";
        }

        var formatted = value.Value.ToString("G", CultureInfo.InvariantCulture);
        return $"{name}: {formatted}{suffix}";
    }

    private static string FormatIntField(string name, int? value)
    {
        if (!value.HasValue)
        {
            return $"{name}: {Missing}";
        }

        return $"{name}: {value.Value.ToString(CultureInfo.InvariantCulture)}";
    }
}
