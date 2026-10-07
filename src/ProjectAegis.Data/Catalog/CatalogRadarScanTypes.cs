namespace ProjectAegis.Data.Catalog;

/// <summary>EW-01 / DRG-386 radar scan architectures accepted in catalog rows (case-insensitive).</summary>
public static class CatalogRadarScanTypes
{
    public const string Unspecified = "";

    public const string Mechanical = "Mechanical";

    public const string Pesa = "Pesa";

    public const string Aesa = "Aesa";

    /// <summary>Highest radar technology generation accepted by validation.</summary>
    public const int MaxTechGeneration = 6;

    public static bool IsKnown(string? value) =>
        string.IsNullOrEmpty(value)
        || string.Equals(value, Mechanical, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Pesa, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, Aesa, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns validation errors for one binding's ECCM attributes (empty when valid).</summary>
    public static IReadOnlyList<string> Validate(CatalogSensorBinding binding)
    {
        var errors = new List<string>();
        if (!IsKnown(binding.RadarScanType))
        {
            errors.Add($"{binding.PlatformId}/{binding.SensorId}: unknown radarScanType '{binding.RadarScanType}'.");
        }

        if (binding.RadarTechGeneration < 0 || binding.RadarTechGeneration > MaxTechGeneration)
        {
            errors.Add($"{binding.PlatformId}/{binding.SensorId}: radarTechGeneration must be 0..{MaxTechGeneration}.");
        }

        if (binding.EccmFactor is <= 0 or > 1 || double.IsNaN(binding.EccmFactor))
        {
            errors.Add($"{binding.PlatformId}/{binding.SensorId}: eccmFactor must be in (0, 1].");
        }

        var hasEccm = !string.IsNullOrEmpty(binding.RadarScanType) || binding.FrequencyAgile || binding.RadarTechGeneration > 0;
        if (hasEccm && !string.Equals(binding.Modality, CatalogSensorModalities.Radar, StringComparison.Ordinal))
        {
            errors.Add($"{binding.PlatformId}/{binding.SensorId}: ECCM attributes only apply to Radar sensors.");
        }

        return errors;
    }
}
