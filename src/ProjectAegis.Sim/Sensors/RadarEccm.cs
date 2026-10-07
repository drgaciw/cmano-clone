namespace ProjectAegis.Sim.Sensors;

using ProjectAegis.Sim;

/// <summary>Radar antenna / scan architecture (EW-01 / DRG-386).</summary>
public enum RadarScanType
{
    /// <summary>Not authored — no ECCM adjustment (legacy behaviour).</summary>
    Unspecified = 0,
    Mechanical = 1,
    Pesa = 2,
    Aesa = 3,
}

/// <summary>
/// ECCM attributes of a radar sensor. <see cref="TechGeneration"/> 0 means unspecified.
/// PESA/AESA arrays are always treated as frequency-agile.
/// </summary>
public readonly record struct RadarEccmProfile(
    RadarScanType ScanType = RadarScanType.Unspecified,
    bool FrequencyAgile = false,
    int TechGeneration = 0)
{
    /// <summary>Highest authored radar technology generation. 0 means unspecified. Matches catalog validation.</summary>
    public const int MaxTechGeneration = 6;

    public static RadarEccmProfile Unspecified => default;

    public bool IsEffectivelyFrequencyAgile =>
        FrequencyAgile || ScanType is RadarScanType.Pesa or RadarScanType.Aesa;

    /// <summary>
    /// Parses a scan-type name. Null or whitespace is <see cref="RadarScanType.Unspecified"/>.
    /// Unknown names, numeric tokens, and comma-separated combinations throw.
    /// </summary>
    public static RadarScanType ParseScanType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return RadarScanType.Unspecified;
        }

        if (NamedEnumParser.TryParse<RadarScanType>(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidDataException($"Unknown radar scan type '{value}' (expected Mechanical|Pesa|Aesa).");
    }
}

/// <summary>
/// Table-driven noise-jam effectiveness vs radar ECCM (EW-01 / DRG-386).
/// Returns a multiplier in [<see cref="MinEffectiveness"/>, 1] applied to raw jam strength.
/// An unspecified profile and unspecified jammer generation return exactly 1.0 so existing
/// scenarios and replay goldens are unchanged.
/// </summary>
public static class EccmJamMatrix
{
    public const double MinEffectiveness = 0.1;

    /// <summary>Scan-architecture factor (lower = more jam-resistant).</summary>
    public static double ScanFactor(RadarScanType scan) => scan switch
    {
        RadarScanType.Mechanical => 1.0,
        RadarScanType.Pesa => 0.75,
        RadarScanType.Aesa => 0.5,
        _ => 1.0,
    };

    /// <summary>Applied once when the radar is frequency-agile (explicitly or via PESA/AESA).</summary>
    public const double FrequencyAgileFactor = 0.8;

    /// <summary>Effect per generation the radar is ahead of (or behind) the jammer.</summary>
    public const double PerGenerationStep = 0.15;

    public static double GenerationFactor(int radarGeneration, int jammerGeneration)
    {
        if (radarGeneration <= 0 || jammerGeneration <= 0)
        {
            return 1.0;
        }

        var delta = radarGeneration - jammerGeneration;
        // Older radar vs newer jammer cannot exceed full jam effect (cap 1.0).
        return Math.Clamp(1.0 - (PerGenerationStep * delta), MinEffectiveness, 1.0);
    }

    public static double Effectiveness(in RadarEccmProfile radar, int jammerGeneration)
    {
        var f = ScanFactor(radar.ScanType);
        if (radar.IsEffectivelyFrequencyAgile)
        {
            f *= FrequencyAgileFactor;
        }

        f *= GenerationFactor(radar.TechGeneration, jammerGeneration);
        return Math.Clamp(f, MinEffectiveness, 1.0);
    }
}
