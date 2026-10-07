namespace ProjectAegis.Sim.Core;

using System.Globalization;

/// <summary>
/// Parses one defined enum name. Numeric tokens (<c>"2"</c>) and comma-separated combinations
/// (<c>"OnGrid,OffGrid"</c>) are rejected even when <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/>
/// accepts them.
/// </summary>
internal static class NamedEnumParser
{
    public static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value) || value.Contains(',', StringComparison.Ordinal))
        {
            return false;
        }

        if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        return Enum.TryParse(value, ignoreCase: true, out parsed)
            && Enum.IsDefined(typeof(TEnum), parsed)
            && value.Equals(parsed.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
