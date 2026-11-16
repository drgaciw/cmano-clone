using System.Globalization;
using System.Text.RegularExpressions;

namespace ProjectAegis.Ai.Host.Aar;

/// <summary>Rejects model prose that invents numbers not present in the host report.</summary>
public static partial class AarNumberValidator
{
    /// <summary>
    /// Returns null when every numeric token in <paramref name="proseParts"/> is allowed;
    /// otherwise the first offending token.
    /// </summary>
    public static string? FindContradictoryNumber(
        IEnumerable<string> proseParts,
        IReadOnlySet<string> allowedNumericTokens)
    {
        ArgumentNullException.ThrowIfNull(proseParts);
        ArgumentNullException.ThrowIfNull(allowedNumericTokens);

        foreach (var part in proseParts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            foreach (var token in ExtractNumericTokens(part))
            {
                if (!allowedNumericTokens.Contains(token)
                    && !IsAllowedByNormalization(token, allowedNumericTokens))
                {
                    return token;
                }
            }
        }

        return null;
    }

    /// <summary>Extracts invariant-culture numeric tokens (integers and decimals).</summary>
    public static IEnumerable<string> ExtractNumericTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        foreach (Match match in NumberRegex().Matches(text))
        {
            yield return match.Value;
        }
    }

    private static bool IsAllowedByNormalization(string token, IReadOnlySet<string> allowed)
    {
        if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        foreach (var candidate in allowed)
        {
            if (double.TryParse(candidate, NumberStyles.Float, CultureInfo.InvariantCulture, out var allowedValue)
                && Math.Abs(value - allowedValue) < 1e-9)
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"-?\d+(?:\.\d+)?", RegexOptions.CultureInvariant)]
    private static partial Regex NumberRegex();
}
