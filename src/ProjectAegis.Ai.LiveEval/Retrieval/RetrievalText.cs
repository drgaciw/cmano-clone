using System.Text.RegularExpressions;

namespace ProjectAegis.Ai.LiveEval.Retrieval;

/// <summary>
/// Shared tokenization helpers for keyword and offline embedding pilots.
/// </summary>
public static partial class RetrievalText
{
    /// <summary>
    /// Lowercases and splits on non-alphanumeric boundaries; keeps tokens length ≥ 2.
    /// </summary>
    public static IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return TokenRegex()
            .Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(t => t.Length >= 2)
            .ToArray();
    }

    [GeneratedRegex("[a-z0-9]+(?:-[a-z0-9]+)*", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRegex();
}